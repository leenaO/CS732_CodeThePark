using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Code the Park - written by us.
// Runs the whole game: 4 levels x 5 challenges.
//  - Codey introduces himself, then guides the player (he flies AHEAD, towards the next station).
//  - Before each challenge Codey flies to the broken thing (sign, doors, ride) so the player looks at it,
//    then comes back to the screen and teaches: idea -> example -> task.
//  - After a correct RUN Codey flies to the thing again and shows the change happening.
//  - A level opens when the previous one is done AND its 3 coins are found (the screen shows a checklist).
//  - After the carousel works, its screen becomes a playground: the player sets the speed (0 / 3 / 6).
//  - Keeps the numbers for the results screen (time, first tries, mistakes, hints).
public class LevelManager : MonoBehaviour
{
    public static LevelManager I { get; private set; }

    [Header("Stations (level 1..4)")]
    public CodeStation[] stations = new CodeStation[4];

    [Header("Park parts")]
    public GateEffects gate;
    public RideController carousel, wheel, coaster;
    public Material bulbBlue, bulbGreen, bulbPink, bulbYellow, bulbWhite;

    [Header("End of the game")]
    public Celebration celebration;
    public float TimePlayed => (celebration != null && celebration.Finished && endTime > 0f ? endTime : Time.time) - startTime;
    [HideInInspector] public float endTime;     // set when the park is fixed (the clock stops)
    public int Score() => solved * 100 + firstTries * 50 - wrongRuns * 10 - hintsUsed * 5
                          + (Inventory.Instance != null ? Inventory.Instance.coins * 10 + Inventory.Instance.gems * 50 + Inventory.Instance.tools * 20 : 0);

    [Header("Results")]
    public int firstTries, wrongRuns, hintsUsed, solved;
    public float startTime;

    public int Level { get; private set; } = 1;
    RobotTeacher codey;
    Transform head;
    int waitingForCoins;      // >0: this level's station waits for the 3 coins of the level before
    bool introDone, showing;  // showing: Codey is busy showing something (do not follow the player)

    void Awake()
    {
        I = this;
        foreach (var s in stations) if (s != null) { s.testChallenge = 0; s.managed = true; }
    }

    void Start()
    {
        codey = FindAnyObjectByType<RobotTeacher>();
        for (int i = 0; i < stations.Length; i++)
        {
            var s = stations[i];
            if (s == null) continue;
            s.ReadyPressed += st => st.StartTry();
            s.RunFinished += OnRun;
            if (i == 0) s.SetOff("Codey is coming...");
            else s.SetOff($"Level {i + 1}: {ChallengeData.LevelNames[i + 1]}\n\nTo open this ride:\n1. fix {ChallengeData.LevelNames[i]}\n2. have {3 * i} coins, {i} {(i > 1 ? "gems" : "gem")} and {i} {(i > 1 ? "Fix Tools" : "Fix Tool")} in total");
        }
        startTime = Time.time;
        StartCoroutine(Begin());
    }

    IEnumerator Begin()
    {
        var startPanel = FindAnyObjectByType<StartPanel>();        // the START screen with the controls
        if (startPanel != null) yield return startPanel.WaitForStart();
        startTime = Time.time;                                       // the clock starts with START
        yield return new WaitForSeconds(1f);
        showing = true;
        if (codey != null) yield return codey.Introduce(stations[0].CodeyPoint());
        introDone = true;
        yield return ShowChallenge(stations[0], ChallengeData.Get(1));
    }

    // ---------------- one challenge: look at the broken thing, then learn ----------------
    IEnumerator ShowChallenge(CodeStation st, Challenge c)
    {
        showing = true;
        st.Show(c);
        if (codey != null)
        {
            yield return LeadTo(st);                                   // the player follows Codey to the screen
            codey.FlyTo(st.CodeyPoint());
            if (Focus(c.effect, out Vector3 p, out float h, out Vector3 look))
            {
                codey.PointAt(look, 5f);                              // he points from beside the screen, he does not fly there
                codey.SayLines(new[] { Before(c.effect) });
                yield return new WaitForSeconds(5f);
            }
            codey.SayLines(ChallengeData.Lesson(c));                  // idea -> example -> your task
        }
        showing = false;
    }

    // Codey flies ahead to a station; if the player stays far behind he waits and calls them
    IEnumerator LeadTo(CodeStation st)
    {
        float lastCall = -100f;
        while (codey != null && head != null)
        {
            Vector3 me = Flat0(head.position), cp = Flat0(codey.transform.position), goal = Flat0(st.CodeyPoint());
            if (Vector3.Distance(me, Flat0(st.transform.position)) < 4f) break;           // the player is at the screen
            if (Vector3.Distance(me, cp) > 7f)                                              // too far behind: wait and call
            {
                codey.FlyTo(codey.transform.position);
                if (Time.time - lastCall > 9f) { codey.Wave(); codey.Say("Come on, follow me! This way!", 3f); lastCall = Time.time; }
            }
            else
            {
                Vector3 step = Via(cp, goal);                                              // through the gate, not through the fence
                codey.FlyTo(Vector3.Distance(cp, step) < 0.5f ? step : cp + (step - cp).normalized * Mathf.Min(3f, Vector3.Distance(cp, step)));
            }
            yield return null;
        }
        codey.FlyTo(st.CodeyPoint());
    }

    static Vector3 Flat0(Vector3 v) => new Vector3(v.x, 0, v.z);

    // the park fence runs along the gate line: to go in or out, Codey uses the gate opening
    Vector3 Via(Vector3 from, Vector3 goal)
    {
        float line = gate != null ? gate.transform.position.z + 0.6f : -7.4f;
        bool fromOut = from.z < line, goalOut = goal.z < line;
        if (fromOut == goalOut) return goal;
        Vector3 outside = new Vector3(0, 0, line - 2.5f), inside = new Vector3(0, 0, line + 2.5f);
        Vector3 near = fromOut ? outside : inside, far = fromOut ? inside : outside;
        return (Mathf.Abs(from.x) > 1.2f && Vector3.Distance(from, near) > 0.6f) ? near : far;
    }

    IEnumerator WaitArrive(float max)
    {
        float t = 0;
        while (t < max && codey != null && !codey.Arrived) { t += Time.deltaTime; yield return null; }
    }

    // ---------------- a RUN finished ----------------
    void OnRun(CodeStation st, bool ok, string block)
    {
        if (!ok) { wrongRuns++; return; }
        solved++;
        if (st.wrongTries == 0) firstTries++;
        StartCoroutine(AfterCorrect(st, st.current));
    }

    IEnumerator AfterCorrect(CodeStation st, Challenge c)
    {
        showing = true;
        yield return new WaitForSeconds(1.5f);
        // Codey flies to the thing and the player sees it change
        if (codey != null && Focus(c.effect, out Vector3 p, out float h, out Vector3 look))
        {
            codey.PointAt(look, 6f);                                   // from beside the screen
            codey.SayLines(new[] { "Look over there!" });
            yield return new WaitForSeconds(1.2f);
        }
        PlayEffect(c.effect);
        var learnInv = Inventory.Instance;                 // the wrist counter shows code only after the idea was learned
        if (learnInv != null) learnInv.SetLearned(c.id >= 10, c.id >= 13, c.id >= 16);
        if (codey != null) { codey.Happy(); codey.SayLines(new[] { After(c.effect) }); }
        yield return new WaitForSeconds(c.effect == "gate_open" ? 6f : 4.5f);

        var next = ChallengeData.Get(c.id + 1);
        if (next != null && next.level == c.level) { yield return ShowChallenge(st, next); yield break; }

        // ----- the level is finished -----
        if (c.level == 2) StartSpeedPlayground(st);
        else st.SetOff("Level " + c.level + " fixed!\n" + ChallengeData.LevelNames[c.level] + " works again.");
        if (c.level == 4) { showing = false; yield break; }   // celebrate() is the end (step 5)
        Level = c.level + 1;
        var inv = Inventory.Instance;
        if (inv != null) inv.StartLevel(Level);
        Needs(Level, out int hc, out int hg, out int ht, out int nc, out int ng, out int nt);
        if (hc < nc || hg < ng || ht < nt)
        {
            waitingForCoins = Level;
            if (codey != null) codey.SayLines(new[] {
                "Great job! " + ChallengeData.LevelNames[c.level] + " is fixed!",
                "To fix " + ChallengeData.LevelNames[Level] + " you need " + nc + " coins, " + ng + (ng > 1 ? " gems" : " gem") + " and " + nt + (nt > 1 ? " Fix Tools" : " Fix Tool") + " in total. Coins are the ticket, gems give power, Fix Tools open the machine.",
                "You have " + hc + " coins, " + hg + (hg == 1 ? " gem" : " gems") + " and " + ht + (ht == 1 ? " Fix Tool" : " Fix Tools") + ". Coins twinkle, gems hide behind things. Look at your wrist!" });
        }
        else OpenLevel(Level);
        if (waitingForCoins > 0)
        {
            yield return new WaitForSeconds(9f);
            if (codey != null) codey.Say("Let's go to " + ChallengeData.LevelNames[Level] + ". Follow me!", 4f);
            yield return LeadTo(stations[Level - 1]);                  // he waits there with the checklist
        }
        showing = false;
    }

    // what a ride needs: 3 coins (the ticket), 1 gem (the power crystal) and 1 Fix Tool (to open the broken machine).
    // Counted from everything found so far: level 2 needs 3/1/1, level 3 needs 6/2/2, level 4 needs 9/3/3.
    void Needs(int level, out int coins, out int gems, out int tools) { Needs(level, out coins, out gems, out tools, out _, out _, out _); }
    // totals for a ride: the carousel 3 coins + 1 gem + 1 tool, the wheel 6 + 2 + 2, the coaster 9 + 3 + 3
    void Needs(int level, out int coins, out int gems, out int tools, out int needC, out int needG, out int needT)
    {
        var inv = Inventory.Instance;
        needC = 3 * (level - 1); needG = level - 1; needT = level - 1;
        coins = inv == null ? needC : Mathf.Min(inv.coins, needC);
        gems  = inv == null ? needG : Mathf.Min(inv.gems, needG);
        tools = inv == null ? needT : Mathf.Min(inv.tools, needT);
    }

    static string Tick(bool ok) => ok ? "<color=#7CFF8A>[done]</color>" : "[    ]";

        void OpenLevel(int level)
    {
        waitingForCoins = 0;
        if (codey != null) codey.SayLines(new[] { "You have everything! Let's fix " + ChallengeData.LevelNames[level] + ". Follow me!",
                                                    "Here we learn " + ChallengeData.LevelIdeas[level].ToLower() + "." });
        StartCoroutine(ShowChallenge(stations[level - 1], ChallengeData.Get((level - 1) * 5 + 1)));
    }

    void Update()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        // no time limit: the game never ends by itself, the time is only counted for the results
        if (waitingForCoins > 0 && Inventory.Instance != null)
        {
            Needs(waitingForCoins, out int hc, out int hg, out int ht, out int nc, out int ng, out int nt);
            stations[waitingForCoins - 1].SetOff($"<b>To fix {ChallengeData.LevelNames[waitingForCoins]} you need (in total):</b>\n\n" +
                $"{Tick(hc >= nc)}  {nc} coins  (the ticket)  -  <color=#FFD54A>{hc} / {nc}</color>\n" +
                $"{Tick(hg >= ng)}  {ng} {(ng > 1 ? "gems" : "gem")}  (power crystals)  -  <color=#7FE8FF>{hg} / {ng}</color>\n" +
                $"{Tick(ht >= nt)}  {nt} {(nt > 1 ? "Fix Tools" : "Fix Tool")}  (to open the machine)  -  <color=#FFA45C>{ht} / {nt}</color>\n" +
                $"<size=70%><color=#AAAAAA>Coins twinkle. Gems hide behind things. Look at your wrist.</color></size>");
            if (hc >= nc && hg >= ng && ht >= nt) { if (codey != null) codey.Happy(); OpenLevel(waitingForCoins); }
        }
        FollowPlayer();
#if UNITY_EDITOR
        // testing in the editor: F5 = get one coin of the level you need (instead of walking to it)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        // F5 / F6 / F7 = collect the NEAREST coin / gem / Fix Tool in the park (it really disappears, like by hand)
        if (kb != null && kb.f5Key.wasPressedThisFrame) CollectNearest(Inventory.Kind.Coin);
        if (kb != null && kb.f6Key.wasPressedThisFrame) CollectNearest(Inventory.Kind.Gem);
        if (kb != null && kb.f7Key.wasPressedThisFrame) CollectNearest(Inventory.Kind.Tool);
        // F8 = test the final celebration now (all rides on + fireworks + results),  F9 = test "time is up"
        if (kb != null && kb.f8Key.wasPressedThisFrame && celebration != null)
        {
            foreach (var r in new[] { carousel, wheel, coaster }) if (r != null) r.PowerOn();
            if (gate != null) gate.Open();
            celebration.Win();
        }
        if (kb != null && kb.f9Key.wasPressedThisFrame && celebration != null) celebration.Lose();
#endif
    }

    // Codey does NOT chase the player: during a lesson he stays beside the screen (calm for the eyes).
    // He only moves to LEAD the player to the next station (LeadTo).
    void FollowPlayer()
    {
        if (!introDone || showing || codey == null) return;
        codey.FlyTo(stations[Level - 1].CodeyPoint());
    }

    void CollectNearest(Inventory.Kind kind)
    {
        Collectible best = null; float bd = float.MaxValue;
        foreach (var root in gameObject.scene.GetRootGameObjects())
        foreach (var c in root.GetComponentsInChildren<Collectible>())
        {
            if (c.taken || c.kind != kind || !c.gameObject.activeInHierarchy) continue;
            float d = head != null ? Vector3.Distance(head.position, c.transform.position) : 0f;
            if (d < bd) { bd = d; best = c; }
        }
        if (best != null) best.CollectNow();
        else Debug.Log("CS732: no " + kind + " left to collect (or it belongs to a later level).");
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 0.01f ? v.normalized : Vector3.forward; }

    // ---------------- the carousel playground: the player is the boss of the speed ----------------
    void StartSpeedPlayground(CodeStation st)
    {
        st.StartPlayground("Carousel speed",
            "The carousel works! Now YOU choose its speed.\nTry <color=#B5CEA8>3</color>, then <color=#B5CEA8>6</color>, then <color=#B5CEA8>0</color> - and watch the horses.",
            "speed = ___;", new[] { "0", "3", "6" },
            v => {
                float n = float.Parse(v);
                carousel.SetSpeed(n / 4f);                  // 3 -> 0.75 (slow), 6 -> 1.5 (fast), 0 -> stop
                if (codey != null) codey.Say(n == 0 ? "speed = 0: the carousel stops!" : n < 5 ? "speed = 3: nice and slow." : "speed = 6: twice as fast! Bigger number, faster ride.", 4f);
            });
    }

    // ---------------- where Codey shows things, and what he says ----------------
    // where Codey goes to show something: he stays LOW (easy to read his words) and points at the thing
    bool Focus(string e, out Vector3 p, out float h) { return Focus(e, out p, out h, out _); }
    bool Focus(string e, out Vector3 p, out float h, out Vector3 look)
    {
        p = Vector3.zero; h = 1.5f; look = Vector3.zero;
        Vector3 me = head != null ? head.position : Vector3.zero;
        if (e.StartsWith("gate"))
        {
            Vector3 g = gate.transform.position;
            switch (e)
            {
                case "gate_sign":     look = g + new Vector3(0, 6.3f, -0.4f); break;      // the name sign, up high
                case "gate_year":     look = g + new Vector3(0, 3.2f, -0.35f); break;
                case "gate_bulbs":    look = g + new Vector3(0, 2.92f, -0.35f); break;
                case "gate_sunwheel": look = g + new Vector3(0, 7.5f, 0.5f); break;
                default:              look = g + new Vector3(0, 1.5f, 0f); break;        // the doors
            }
            p = g + new Vector3(2.6f, 0, -2.2f);          // on the ground in front of the gate, to the side
            return true;
        }
        RideController r = e.StartsWith("carousel") ? carousel : e.StartsWith("wheel") ? wheel : e.StartsWith("coaster") ? coaster : null;
        if (r == null) return false;
        Vector3 c = r.transform.position;
        Vector3 toMe = Flat(me - c);
        float dist = r == carousel ? 8.5f : r == wheel ? 7f : 10f;
        p = c + toMe * dist + Vector3.Cross(Vector3.up, toMe) * 2f;
        look = c + Vector3.up * (e == "carousel_name" ? 6.2f : e == "carousel_price" ? 5.4f : r == wheel ? 6f : 3f);
        return true;
    }

    static readonly Dictionary<string, string> BeforeLines = new Dictionary<string, string> {
        { "gate_sign", "Look at the big sign up here. It is dark - nobody can read the park's name!" },
        { "gate_year", "Look under the name. This line is empty. Let's show the year the park opened." },
        { "gate_bulbs", "See the bulbs under the sign? They are all off." },
        { "gate_sunwheel", "And this golden wheel is dark too." },
        { "gate_open", "The gate is still locked. Look - the doors are closed." },
        { "carousel_lights", "This is the carousel. Its lights are off and the horses are asleep." },
        { "carousel_name", "The carousel has no name. Up here we will show it." },
        { "carousel_price", "Visitors want to know the ticket price. It goes here." },
        { "carousel_slow", "The carousel is not moving. Let's make it turn." },
        { "carousel_on", "One more step and the carousel comes alive!" },
        { "wheel_cabins", "Look at the Ferris wheel cabins. They are dark inside." },
        { "wheel_gate", "The wheel only works for visitors with enough coins." },
        { "wheel_turn", "The wheel is standing still." },
        { "wheel_color", "The wheel's own lights are still off." },
        { "wheel_on", "Last step: everything must be ready together." },
        { "coaster_five", "Look at the roller coaster. All its lights are off." },
        { "coaster_all", "Only half of the lights work." },
        { "coaster_run", "The train is waiting on the track." },
        { "coaster_show", "Let's make a light show!" },
        { "celebrate", "Everything is ready for a party!" },
    };
    static readonly Dictionary<string, string> AfterLines = new Dictionary<string, string> {
        { "gate_sign", "Look! Our sign says CODE THE PARK!" },
        { "gate_year", "Now it shows the year: 2026!" },
        { "gate_bulbs", "The bulbs are on!" },
        { "gate_sunwheel", "The golden wheel shines!" },
        { "gate_open", "The gate is open! Welcome to Code the Park!" },
        { "carousel_lights", "The lights are on!" },
        { "carousel_name", "Now everyone can read its name: Star Carousel!" },
        { "carousel_price", "Now visitors know the price: 2.5" },
        { "carousel_slow", "It turns! A bigger speed number makes it faster." },
        { "carousel_on", "The carousel is alive! Listen to the music!" },
        { "wheel_cabins", "The cabins are lit!" },
        { "wheel_gate", "Green means: you have enough coins!" },
        { "wheel_turn", "The wheel is turning!" },
        { "wheel_color", "Look at the wheel lights - blue and gold!" },
        { "wheel_on", "The Ferris wheel is fully working!" },
        { "coaster_five", "Five lights - just like your loop said!" },
        { "coaster_all", "All ten lights!" },
        { "coaster_run", "Here comes the train! Three laps!" },
        { "coaster_show", "What a light show!" },
        { "celebrate", "Congratulations! You fixed the park. You will be a great programmer!" },
    };
    static string Before(string e) => BeforeLines.TryGetValue(e, out var s) ? s : "Look here!";
    static string After(string e) => AfterLines.TryGetValue(e, out var s) ? s : "It works!";

    // ---------------- what happens in the park ----------------
    void PlayEffect(string e)
    {
        switch (e)
        {
            case "gate_sign":     gate.NeonOn(); break;
            case "gate_year":     gate.Year(); break;
            case "gate_bulbs":    gate.BulbsOn(); break;
            case "gate_sunwheel": gate.SunWheelOn(); break;
            case "gate_open":     gate.Open(); if (codey != null) codey.Dance(); break;

            case "carousel_lights": carousel.LightsOnly(); break;
            case "carousel_name":   Sign(carousel, "STAR  CAROUSEL", 6.2f); break;
            case "carousel_price":  Sign(carousel, "Ticket: 2.5", 5.4f, 0.55f); break;
            case "carousel_slow":   carousel.Slow(0.35f); break;
            case "carousel_on":     carousel.PowerOn(); break;

            // Ferris wheel: cabins light up -> the wheel turns -> its own lights -> everything
            case "wheel_cabins": wheel.GlowMaterial("FW_Cabin", new Color(1f, 0.85f, 0.6f), 1.4f); break;   // warm light inside the cabins
            case "wheel_gate":   wheel.LightsOnly(); wheel.SetBulbColors(bulbGreen, bulbGreen); break;
            case "wheel_turn":   wheel.Slow(0.3f); break;
            case "wheel_color":  wheel.SetBulbColors(bulbBlue, bulbYellow); break;
            case "wheel_on":     wheel.PowerOn(); break;

            case "coaster_five": coaster.BulbFraction(0.5f); break;
            case "coaster_all":  coaster.BulbFraction(1f); break;
            case "coaster_run":  coaster.PowerOn(); break;
            case "coaster_show": coaster.SetBulbColors(bulbPink, bulbBlue, 0.08f); break;
            case "celebrate":
                foreach (var r in new[] { carousel, wheel, coaster }) r.PowerOn();
                if (codey != null) codey.Dance();
                endTime = Time.time;                                   // stop the clock
                if (celebration != null) celebration.Win();          // fireworks, confetti, message, results
                break;
        }
    }

    // a floating glowing sign above a ride (its name, its price)
    void Sign(RideController ride, string text, float height, float size = 0.8f)
    {
        var g = new GameObject("Sign_" + text);
        g.transform.position = ride.transform.position + Vector3.up * height;
        var t = g.AddComponent<TextMeshPro>();
        t.text = text; t.fontSize = size * 10f; t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.color = new Color(1f, 0.85f, 0.35f);
        t.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        g.AddComponent<FaceCameraMarker>();
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.correct, g.transform.position);
    }
}
