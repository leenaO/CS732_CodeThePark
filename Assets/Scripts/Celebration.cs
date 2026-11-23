using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Code the Park - written by us.
// The end of the game.
//  WIN  (celebrate(); runs): fireworks in the sky, confetti over the player, fireworks sound + fanfare,
//       a big glowing message, Codey dances, then the results screen.
//  (Lose() is not used any more: there is no time limit. The time played is shown in the results.)
// The results screen shows time, first tries, mistakes, items and the score, and a RESTART button.
// Fireworks and confetti are Unity Particle Systems made here in code (our own effect).
public class Celebration : MonoBehaviour
{
    public Material sparkMat, confettiMat, panelMat, buttonMat;
    public bool Finished { get; private set; }
    Transform head;

    [ContextMenu("Test: Win")]
    public void Win()  { if (!Finished) { Finished = true; StartCoroutine(WinRoutine()); } }
    [ContextMenu("Test: Lose")]
    public void Lose() { if (!Finished) { Finished = true; StartCoroutine(LoseRoutine()); } }

    IEnumerator WinRoutine()
    {
        head = Camera.main != null ? Camera.main.transform : transform;
        if (SoundFX.I != null)
        {
            SoundFX.UI(SoundFX.I.celebrate, 0.2f);          // the fanfare, very soft (it was too loud)
            if (SoundFX.I.fireworksShow != null)
            {
                var loop = gameObject.AddComponent<AudioSource>();
                loop.clip = SoundFX.I.fireworksShow; loop.loop = true; loop.spatialBlend = 0f; loop.volume = 0.8f; loop.Play();
            }
        }
        Confetti();
        Message("Congratulations!\n<size=60%>You fixed the park.\nYou will be a great programmer!</size>", new Color(1f, 0.85f, 0.3f));
        var codey = FindAnyObjectByType<RobotTeacher>();
        if (codey != null) codey.SayLines(new[] { "Congratulations! You fixed the park. You will be a great programmer!",
                                                   "You learned output, variables, decisions, loops and functions. Real C++!" });
        float t = 0;
        while (t < 14f)                                         // a firework every half second
        {
            StartCoroutine(Firework());
            if (codey != null && Random.value < 0.15f) codey.Dance();
            float w = Random.Range(0.3f, 0.7f); t += w;
            yield return new WaitForSeconds(w);
        }
        Results(true);
        while (true) { StartCoroutine(Firework()); yield return new WaitForSeconds(Random.Range(1f, 2.5f)); }   // a few more
    }

    IEnumerator LoseRoutine()
    {
        head = Camera.main != null ? Camera.main.transform : transform;
        Message("Time is up!\n<size=60%>The park stays dark... but you can try again!</size>", new Color(0.7f, 0.8f, 1f));
        var codey = FindAnyObjectByType<RobotTeacher>();
        if (codey != null) { codey.Sad(); codey.Say("Oh no, time is up! Don't worry - every programmer tries again. Press RESTART!", 6f); }
        yield return new WaitForSeconds(4f);
        Results(false);
    }

    // ---------------- fireworks: a rocket goes up, then bursts into coloured sparks ----------------
    IEnumerator Firework()
    {
        Vector3 f = Flat(head.forward);
        Vector3 side = Vector3.Cross(Vector3.up, f);
        Vector3 ground = head.position + f * Random.Range(18f, 30f) + side * Random.Range(-15f, 15f);
        ground.y = 0;
        Vector3 top = ground + Vector3.up * Random.Range(14f, 24f);
        Color c = Color.HSVToRGB(Random.value, 0.75f, 1f);

        // rocket: a small bright dot flying up
        var rocket = new GameObject("Rocket");
        var rp = Particles(rocket, sparkMat, 60, 0.6f, 0.12f, c, 0f, 0.3f);
        var em = rp.emission; em.rateOverTime = 60;
        var sh = rp.shape; sh.enabled = false;
        rp.Play();
        if (SoundFX.I != null && Random.value < 0.4f) SoundFX.At(SoundFX.I.fireworkLaunch, ground, 0.5f);
        float t = 0, up = 1.1f;
        while (t < up) { rocket.transform.position = Vector3.Lerp(ground, top, Mathf.Sqrt(t / up)); t += Time.deltaTime; yield return null; }
        em.rateOverTime = 0;
        Destroy(rocket, 1f);

        // burst: up to 180 sparks in a ball, falling slowly (gravity), fading out
        var burst = new GameObject("Burst");
        burst.transform.position = top;
        var bp = Particles(burst, sparkMat, 180, Random.Range(1.4f, 2.2f), 0.35f, c, 0.35f, Random.Range(7f, 11f));
        var bs = bp.shape; bs.shapeType = ParticleSystemShapeType.Sphere; bs.radius = 0.2f;
        var col = bp.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(c, 0.25f), new GradientColorKey(c, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        bp.Emit(Random.Range(120, 180));
        if (SoundFX.I != null && Random.value < 0.3f) SoundFX.At(SoundFX.I.fireworkBoom, top, 1f);
        Destroy(burst, 3f);
    }

    void Confetti()
    {
        var go = new GameObject("Confetti");
        go.transform.position = head.position + Vector3.up * 3.5f;
        var p = Particles(go, confettiMat, 400, 5f, 0.06f, Color.white, 0.12f, 0.6f);
        var main = p.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.3f), new Color(0.3f, 0.6f, 1f));
        main.startColor = new ParticleSystem.MinMaxGradient(RainbowGradient()) { mode = ParticleSystemGradientMode.RandomColor };
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0, 6.28f); main.startRotationY = new ParticleSystem.MinMaxCurve(0, 6.28f);
        var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 3f; sh.rotation = new Vector3(90, 0, 0);
        var rot = p.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(2f, 6f); rot.y = new ParticleSystem.MinMaxCurve(2f, 6f);
        var em = p.emission; em.rateOverTime = 80;
        p.Play();
        Destroy(go, 12f);
    }

    static Gradient RainbowGradient()
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.25f, 0.25f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.33f),
                          new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 0.66f), new GradientColorKey(new Color(0.35f, 0.55f, 1f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    static ParticleSystem Particles(GameObject go, Material m, int max, float life, float size, Color c, float gravity, float speed)
    {
        var p = go.AddComponent<ParticleSystem>();
        p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = p.main;
        main.maxParticles = max; main.startLifetime = life; main.startSize = size; main.startColor = c;
        main.gravityModifier = gravity; main.startSpeed = speed; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false; main.loop = true;
        var em = p.emission; em.rateOverTime = 0;
        var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m;
        return p;
    }

    // ---------------- the big message in the sky ----------------
    void Message(string text, Color c)
    {
        var go = new GameObject("BigMessage");
        Vector3 f = Flat(head.forward);
        go.transform.position = head.position + f * 16f + Vector3.up * 4.5f;    // far away and up in the sky, like a real sign
        go.transform.rotation = Quaternion.LookRotation(f);
        var t = go.AddComponent<TextMeshPro>();
        t.text = text; t.color = c; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 9f; t.rectTransform.sizeDelta = new Vector2(14f, 6f);      // smaller text
        StartCoroutine(Pop(go.transform));
    }

    IEnumerator Pop(Transform t)
    {
        float k = 0;
        while (k < 1f) { t.localScale = Vector3.one * Mathf.SmoothStep(0.2f, 1f, k); k += Time.deltaTime * 1.5f; yield return null; }
        t.localScale = Vector3.one;
    }

    // ---------------- results screen with RESTART ----------------
    void Results(bool won)
    {
        var lm = LevelManager.I; var inv = Inventory.Instance;
        float secs = lm != null ? lm.TimePlayed : 0;
        int gems = inv != null ? inv.gems : 0;
        int score = lm != null ? lm.Score() : 0;

        Vector3 f = Flat(head.forward);
        var root = new GameObject("ResultsScreen");
        root.transform.position = head.position + f * 2.2f - Vector3.up * 0.2f;
        root.transform.rotation = Quaternion.LookRotation(f);

        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(panel.GetComponent<Collider>());
        panel.transform.SetParent(root.transform, false);
        panel.transform.localPosition = new Vector3(0, 0, 0.03f); panel.transform.localScale = new Vector3(1.6f, 1.1f, 0.02f);
        panel.GetComponent<Renderer>().sharedMaterial = panelMat;

        string code = (inv != null && inv.learnedFor)
            ? "\n<size=75%><mspace=0.55em>" + CodeColors.Color("for (int i = 0; i < gems; i++) score += 50;") + "</mspace>   <color=#AAAAAA>// your gems, counted with a loop!</color></size>"
            : "";
        var t = new GameObject("Text").AddComponent<TextMeshPro>();
        t.transform.SetParent(root.transform, false); t.transform.localPosition = new Vector3(0, 0.08f, 0);
        t.rectTransform.sizeDelta = new Vector2(1.45f, 0.85f);
        t.enableAutoSizing = true; t.fontSizeMin = 0.2f; t.fontSizeMax = 0.9f; t.alignment = TextAlignmentOptions.Center;
        t.text = (won ? "<color=#FFD54A><b>YOU FIXED THE PARK!</b></color>" : "<color=#9EC9FF><b>TIME IS UP</b></color>") + "\n" +
                 $"Time:  {(int)secs / 60:00}:{(int)secs % 60:00}\n" +
                 $"Challenges fixed:  {(lm != null ? lm.solved : 0)} / 20\n" +
                 $"Right on the first try:  {(lm != null ? lm.firstTries : 0)}\n" +
                 $"Wrong runs:  {(lm != null ? lm.wrongRuns : 0)}\n" +
                 $"Coins {(inv != null ? inv.coins : 0)}   Gems {gems}   Fix Tools {(inv != null ? inv.tools : 0)}\n" +
                 $"<color=#FFD54A><b>Score:  {score}</b></color>" + code;

        // RESTART: press with the finger or the ray + Trigger
        var btn = new GameObject("RestartButton");
        btn.transform.SetParent(root.transform, false); btn.transform.localPosition = new Vector3(0, -0.45f, -0.05f);
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cap.GetComponent<Collider>());
        cap.transform.SetParent(btn.transform, false); cap.transform.localScale = new Vector3(0.42f, 0.13f, 0.05f);
        cap.GetComponent<Renderer>().sharedMaterial = buttonMat;
        var lbl = new GameObject("Label").AddComponent<TextMeshPro>();
        lbl.transform.SetParent(btn.transform, false); lbl.transform.localPosition = new Vector3(0, 0, -0.03f);
        lbl.text = "<b>RESTART</b>"; lbl.fontSize = 0.6f; lbl.alignment = TextAlignmentOptions.Center; lbl.rectTransform.sizeDelta = new Vector2(0.4f, 0.12f);
        var box = btn.AddComponent<BoxCollider>(); box.size = new Vector3(0.42f, 0.13f, 0.08f);
        btn.AddComponent<XRSimpleInteractable>();
        var pb = btn.AddComponent<PressButton>(); pb.cap = cap.transform;
        pb.onPress.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 0.01f ? v.normalized : Vector3.forward; }
}
