using System.Collections;
using TMPro;
using UnityEngine;

// Code the Park - written by us.
// "Codey", the robot teacher (the visible face of the AI).
// Model: "BOT NEIL ANIMATION" by OSCAR CREATIVO (CC BY 4.0, see CREDITS.md).
// The model's own idle animation plays all the time; on top of it this script
// adds: floating, always facing the player, flying to a station, talking
// (wings + glowing eyes + speech bubble), happy jump, sad tilt and a final dance.
public class RobotTeacher : MonoBehaviour
{
    [Header("Parts (found automatically by name)")]
    public Transform model;            // the imported robot
    public Transform wingLeft, wingRight, eyes;
    public TextMeshPro bubbleText;
    public Transform bubble;

    [Header("Behaviour")]
    public float hoverHeight = 1.25f;  // height of the robot above the ground
    [Range(0.3f, 1.5f)] public float size = 0.7f;
    [Range(0.4f, 1.2f)] public float bubbleSize = 0.75f;   // smaller Codey (1 = the size made by RobotSetup)
    public float flySpeed = 2.5f;      // metres per second when moving to a station
    [Header("Voice (cute robot beeps, made by us)")]
    public AudioClip[] talkBeeps;      // short "syllables" played while the text appears
    public AudioClip happyClip, sadClip, hoverClip;
    [Range(0f, 1f)] public float voiceVolume = 0.6f;
    public float lettersPerSecond = 28f;
    public float beepTime = 0.9f;      // Codey beeps only during the first part of each sentence
    public float wordsPerSecond = 2f;  // how fast the player can read (slower = more time for each sentence)

    [Tooltip("Tick to see all the moves in Play mode (for testing).")]
    public bool demoMode = false;

    Transform head;                    // the player's camera
    Vector3 target;                    // where the robot wants to be
    float wingFlap, jump, tilt, spin, waveAmount;  // extra motion added on top of the idle animation
    float yawOffset;                   // turns the model so its eyes look at the player
    Quaternion wingLRest, wingRRest;
    Coroutine talking, queue;
    AudioSource voice, hover;

    // Awake runs before any Start, so a station can make Codey talk on the very first frame
    void Awake()
    {
        if (model == null && transform.childCount > 0) model = transform.GetChild(0);
        if (model != null && Mathf.Abs(size - 1f) > 0.01f)   // only the robot gets smaller, not his speech bubble
        {
            Vector3 before = BodyCentre();
            model.localScale *= size;
            model.position += before - BodyCentre();          // keep him in the same place
        }
        target = transform.position;     // (a station may change it with FlyTo before Start)
        target.y = 0; targetHeight = curHeight = hoverHeight;
        lettersPerSecond = Mathf.Min(lettersPerSecond, 16f);   // letters appear slowly enough to follow
        if (bubble != null) { bubble.gameObject.SetActive(false); bubble.localScale *= bubbleSize; }   // a smaller speech box
        if (bubbleText != null) { bubbleText.richText = true; bubbleText.enableAutoSizing = true; bubbleText.fontSizeMin = 0.3f; bubbleText.fontSizeMax = 1.1f; }   // long lessons still fit

        // two 3D sound sources on the robot: its voice, and a soft hover hum
        voice = gameObject.AddComponent<AudioSource>();
        voice.spatialBlend = 1f; voice.minDistance = 1.5f; voice.maxDistance = 20f; voice.playOnAwake = false;
        if (hoverClip != null)
        {
            hover = gameObject.AddComponent<AudioSource>();
            hover.clip = hoverClip; hover.loop = true; hover.volume = 0.05f;
            hover.spatialBlend = 1f; hover.minDistance = 1f; hover.maxDistance = 8f;
            hover.Play();
        }
    }

    void Start()
    {
        if (model == null && transform.childCount > 0) model = transform.GetChild(0);
        if (wingLeft == null)  wingLeft  = Find("ALA_IZQ");
        if (wingRight == null) wingRight = Find("ALA_DERE");
        if (eyes == null)      eyes      = Find("OJO_1");

        // play the model's own looping animation as the idle pose (if it has one)
        var anim = GetComponentInChildren<Animation>();
        if (anim != null && anim.clip != null) { anim.clip.wrapMode = WrapMode.Loop; anim.wrapMode = WrapMode.Loop; anim.Play(); }

        // find which way the model's face points, so we can turn it to the player
        Transform eye2 = Find("OJO");
        if (eyes != null && eye2 != null && model != null)
        {
            Vector3 face = ((eyes.position + eye2.position) * 0.5f) - BodyCentre();
            face.y = 0;
            if (face.sqrMagnitude > 0.0001f)
                yawOffset = Vector3.SignedAngle(face.normalized, transform.forward, Vector3.up);
        }
        if (demoMode) StartCoroutine(Demo());
    }

    // ---------- public API used by the game (LevelManager / AI) ----------
    float targetHeight = -1f, curHeight = -1f;

    // fly to a thing the player must look at (a sign up high, a door ...), at that height
    public void FlyToLook(Vector3 point, float height)
    {
        target = new Vector3(point.x, 0, point.z);
        targetHeight = height;
    }

    public bool Arrived => (new Vector3(transform.position.x, 0, transform.position.z) - target).sqrMagnitude < 0.09f && Mathf.Abs(curHeight - targetHeight) < 0.2f;

    public void FlyTo(Vector3 groundPoint)
    {
        targetHeight = hoverHeight;
        var p = new Vector3(groundPoint.x, 0, groundPoint.z);
        if ((p - target).sqrMagnitude > 0.25f) target = p;     // small changes are ignored, so Codey does not shake
    }

    public void Say(string text, float seconds = 6f)
    {
        if (talking != null) StopCoroutine(talking);
        if (queue != null) StopCoroutine(queue);
        talking = StartCoroutine(Talk(text, seconds));
    }

    // say several sentences one after another; returns how long it takes
    public float SayLines(string[] lines)
    {
        if (talking != null) StopCoroutine(talking);
        if (queue != null) StopCoroutine(queue);
        float total = 0f;
        foreach (var l in lines) total += LineTime(l);
        queue = StartCoroutine(Queue(lines));
        return total;
    }

    // time to READ a sentence (beginners, maybe reading English as a second language): ~2 words a second + a pause
    float LineTime(string l) => Mathf.Max(4f, System.Text.RegularExpressions.Regex.Replace(l, "<[^>]*>", "").Split(' ').Length / wordsPerSecond + 2.5f);

    IEnumerator Queue(string[] lines)
    {
        foreach (var l in lines)
        {
            float t = LineTime(l);
            talking = StartCoroutine(Talk(l, t));
            yield return new WaitForSeconds(t + 0.3f);
        }
    }

    // point at something (a sign up high, the doors, a ride): a golden beam from Codey to it, wing up
    LineRenderer beam; Vector3 pointTarget; float pointUntil;
    public void PointAt(Vector3 worldTarget, float seconds = 4f)
    {
        if (beam == null)
        {
            var g = new GameObject("PointBeam"); g.transform.SetParent(transform, false);
            beam = g.AddComponent<LineRenderer>();
            beam.positionCount = 2; beam.widthMultiplier = 0.03f; beam.numCapVertices = 4;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));    // Lit is always in the build
            m.SetColor("_BaseColor", new Color(1f, 0.8f, 0.25f));
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.2f) * 2f);
            beam.material = m;
        }
        pointTarget = worldTarget; pointUntil = Time.time + seconds;
        StartCoroutine(Pulse(v => waveAmount = v, 0.6f, seconds));
    }

    // "thinking": while the AI prepares an answer, the bubble shows moving dots and the eyes pulse
    Coroutine thinkingRoutine;
    public void Thinking(bool on)
    {
        if (thinkingRoutine != null) { StopCoroutine(thinkingRoutine); thinkingRoutine = null; }
        if (on)
        {
            if (talking != null) StopCoroutine(talking);
            if (queue != null) StopCoroutine(queue);
            thinkingRoutine = StartCoroutine(ThinkingDots());
        }
        else { tilt = 0f; if (bubble != null) bubble.gameObject.SetActive(false); }
    }

    IEnumerator ThinkingDots()
    {
        if (bubble != null) bubble.gameObject.SetActive(true);
        int n = 0;
        while (true)
        {
            if (bubbleText != null)
            {
                bubbleText.maxVisibleCharacters = 99999;
                bubbleText.text = "<color=#7FE8FF>Codey is thinking</color>  " + new string('.', n % 4) + new string(' ', 3 - n % 4);
            }
            tilt = Mathf.Sin(n * 0.8f) * 6f;           // the head tilts a little, like thinking
            n++;
            yield return new WaitForSeconds(0.4f);
        }
    }

    // wave hello with the left wing
    public void Wave() { Play(happyClip); StartCoroutine(Pulse(v => waveAmount = v, 1f, 2.2f)); }

    // the first meeting: fly in front of the player, wave, a happy spin, and tell the story
    public IEnumerator Introduce(Vector3 thenGoTo)
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (head != null)
        {
            Vector3 f = head.forward; f.y = 0; f = f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
            FlyTo(head.position + f * 2.6f);      // in front of the player, not too close (easier to read)
        }
        yield return new WaitForSeconds(1.5f);
        Wave();
        float t = SayLines(new[] {
            "Hi! I'm Codey, the robot of this park!",
            "Oh no... the park's code is broken. All the rides are dark and the gate is locked.",
            "But YOU can fix it! I will teach you C++, step by step. You don't need to know anything yet.",
            "On the way, collect coins, gems and Fix Tools. Each ride needs some of them - its screen will tell you how many. Now follow me to the first screen!" });
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(SpinOnce());
        yield return new WaitForSeconds(t - 2.5f);
        FlyTo(thenGoTo);
        yield return new WaitForSeconds(2f);
    }

    IEnumerator SpinOnce()
    {
        float t = 0;
        while (t < 1.2f) { spin = Mathf.SmoothStep(0, 360, t / 1.2f); jump = Mathf.Sin(t / 1.2f * Mathf.PI) * 0.25f; t += Time.deltaTime; yield return null; }
        spin = 0; jump = 0;
    }

    public void Happy()  { Play(happyClip); StartCoroutine(Pulse(v => jump = v, 0.35f, 1.0f)); StartCoroutine(Pulse(v => wingFlap = v, 35f, 1.2f)); }
    public void Sad()    { Play(sadClip); StartCoroutine(Pulse(v => tilt = v, 18f, 1.6f)); }

    void Play(AudioClip c) { if (voice != null && c != null) voice.PlayOneShot(c, voiceVolume); }
    public void Dance()  { StartCoroutine(DanceRoutine()); }

    // ---------- every frame ----------
    void LateUpdate()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;

        // fly smoothly towards the target and float up and down
        Vector3 pos = transform.position;
        // do not fly through walls: if a fence, railing or wall is in the way, fly OVER it first
        Vector3 to = target - new Vector3(pos.x, 0, pos.z);
        float climb = 0f;
        if (to.sqrMagnitude > 0.01f &&
            Physics.SphereCast(pos, 0.35f, to.normalized, out RaycastHit hit, Mathf.Min(1.2f, to.magnitude), ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<RobotTeacher>() == null && hit.collider.attachedRigidbody == null && !(hit.collider is CharacterController))
            climb = Mathf.Min(hit.collider.bounds.max.y + 0.7f, 4.5f);
        if (climb > curHeight + 0.1f)
        {
            curHeight = Mathf.MoveTowards(curHeight, climb, flySpeed * Time.deltaTime);   // go up, do not move forward yet
            transform.position = new Vector3(pos.x, curHeight + Mathf.Sin(Time.time * 2f) * 0.06f + jump, pos.z);
            goto Turn;
        }
        Vector3 flat = Vector3.MoveTowards(new Vector3(pos.x, 0, pos.z), target, flySpeed * Time.deltaTime);
        float bob = Mathf.Sin(Time.time * 2f) * 0.06f;
        curHeight = Mathf.MoveTowards(curHeight, targetHeight, flySpeed * Time.deltaTime);
        transform.position = new Vector3(flat.x, curHeight + bob + jump, flat.z);

        Turn:
        // always turn towards the player (only around the vertical axis)
        if (head != null)
        {
            Vector3 d = head.position - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(d) * Quaternion.Euler(0, -yawOffset + spin, tilt);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 5f * Time.deltaTime);
            }
        }

        // wings: flap on top of the idle animation
        if (wingFlap != 0f)
        {
            float a = Mathf.Sin(Time.time * 14f) * wingFlap;
            if (wingLeft != null)  wingLeft.localRotation  *= Quaternion.Euler(0, 0,  a);
            if (wingRight != null) wingRight.localRotation *= Quaternion.Euler(0, 0, -a);
        }

        // waving: only the left wing goes up and down, big and slow
        if (waveAmount > 0.01f && wingLeft != null)
            wingLeft.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 9f) * 40f * waveAmount + 25f * waveAmount);

        // the pointing beam: from Codey's body to the target, gently pulsing
        if (beam != null)
        {
            bool on = Time.time < pointUntil;
            beam.enabled = on;
            if (on)
            {
                beam.SetPosition(0, transform.position + Vector3.up * 0.1f);
                beam.SetPosition(1, pointTarget);
                beam.widthMultiplier = 0.025f + 0.015f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            }
        }

        // the speech bubble always faces the player
        if (bubble != null && head != null && bubble.gameObject.activeSelf)
        {
            Vector3 d = bubble.position - head.position; d.y = 0;
            if (d.sqrMagnitude > 0.001f) bubble.rotation = Quaternion.LookRotation(d);
        }
    }

    // ---------- small routines ----------
    IEnumerator Talk(string text, float seconds)
    {
        int letters = text.Length;
        if (bubble != null)
        {
            bubble.gameObject.SetActive(true); bubbleText.text = text; bubbleText.maxVisibleCharacters = 0;
            bubbleText.ForceMeshUpdate(); letters = bubbleText.textInfo.characterCount;   // colours are not letters
        }
        float t = 0, nextBeep = 0;
        int shown = 0;
        while (t < seconds || (bubbleText != null && shown < letters))
        {
            wingFlap = 8f;                                   // small "talking" wing movement
            // typewriter: the letters appear one by one, with a robot beep every few letters
            if (bubbleText != null && shown < letters)
            {
                shown = Mathf.Min(letters, Mathf.FloorToInt(t * lettersPerSecond));
                bubbleText.maxVisibleCharacters = shown;
                // only a few beeps at the start of each sentence (a full sentence of beeps was annoying)
                if (t < beepTime && t >= nextBeep && talkBeeps != null && talkBeeps.Length > 0)
                {
                    if (voice != null && talkBeeps.Length > 0) voice.PlayOneShot(talkBeeps[Random.Range(0, talkBeeps.Length)], voiceVolume * 0.6f);
                    nextBeep = t + Random.Range(0.14f, 0.22f);
                }
            }
            t += Time.deltaTime;
            yield return null;
        }
        wingFlap = 0f;
        if (bubble != null) bubble.gameObject.SetActive(false);
    }

    IEnumerator Pulse(System.Action<float> set, float amount, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            set(Mathf.Sin(t / duration * Mathf.PI) * amount);
            t += Time.deltaTime;
            yield return null;
        }
        set(0f);
    }

    IEnumerator DanceRoutine()
    {
        float t = 0;
        Play(happyClip);
        while (t < 6f)
        {
            spin = t * 240f;
            jump = Mathf.Abs(Mathf.Sin(t * 6f)) * 0.3f;
            wingFlap = 40f;
            t += Time.deltaTime;
            yield return null;
        }
        spin = 0; jump = 0; wingFlap = 0;
    }

    IEnumerator Demo()
    {
        yield return new WaitForSeconds(1f);
        Say("Hi! I'm Codey, the park robot. Let's fix the park together!", 5f);
        yield return new WaitForSeconds(6f); Happy();
        yield return new WaitForSeconds(2f); Sad();
        yield return new WaitForSeconds(2.5f);
        Say("You will be a great programmer!", 4f); Dance();
    }

    Vector3 BodyCentre()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return transform.position;
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    Transform Find(string exact)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == exact) return t;
        return null;
    }
}
