using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Code the Park - written by us.
// Makes a ride "come alive" when the player fixes its code:
//   lights flicker on, the ride glows (emission + Bloom), its bulbs run in a chase pattern,
//   a warm light shines on the ride and the ground, it starts moving slowly then at full speed,
//   and its music fades in (3D sound, so it is louder near the ride).
// Before that the ride is dark, still and silent.
public class RideController : MonoBehaviour
{
    [Header("Look")]
    public string[] glowMaterials;          // names of the ride's materials that glow when ON
    public Color glowColor = new Color(1f, 0.82f, 0.5f);
    public float glowStrength = 3f;
    public Transform[] bulbHolders;         // our bulbs (spheres) under these objects
    public Material bulbOff, bulbOnA, bulbOnB;   // chase pattern uses two colours
    public Light rideLight;                  // one warm light (no shadows, cheap)
    public float lightIntensity = 3f;

    [Header("Our bulbs that turn with the ride")]
    public string attachToBone;             // e.g. "DEF.WHEEL" (Ferris wheel) or the carousel's turning part
    public Transform[] attach;              // they are put on that part when the game starts

    [Header("Motion")]
    public Animator animator;               // the model's own animation (carousel, Ferris wheel)
    public RollerCoasterRide coaster;       // the coaster cart
    public float speedUpTime = 4f;

    [Header("Sound")]
    public AudioSource music;
    public float musicVolume = 0.7f;

    [Header("Test")]
    public bool powerOnAtStart = false;

    public bool IsOn { get; private set; }

    readonly List<Material> glowInstances = new List<Material>();
    readonly List<Renderer> bulbs = new List<Renderer>();
    float glow;            // 0..1
    float chaseDelay = 0.18f;
    bool chase;

    void Start()
    {
        // put our bulbs on the turning part of the model (done in Play, not saved in the scene,
        // so re-importing the model can never lose them)
        if (!string.IsNullOrEmpty(attachToBone) && attach != null)
        {
            Transform bone = null;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == attachToBone) { bone = t; break; }
            if (bone != null) foreach (var a in attach) if (a != null) a.SetParent(bone, true);
        }

        // runtime copies of the glowing materials, so the material files are never changed
        var copies = new Dictionary<Material, Material>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || System.Array.IndexOf(glowMaterials, mats[i].name) < 0) continue;
                if (!copies.TryGetValue(mats[i], out var copy))
                {
                    copy = new Material(mats[i]);
                    copy.EnableKeyword("_EMISSION");
                    copies[mats[i]] = copy;
                    glowInstances.Add(copy);
                }
                mats[i] = copy; changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
        foreach (var h in bulbHolders)
            if (h != null)
                foreach (var r in h.GetComponentsInChildren<Renderer>(true))
                    if (r.name.Contains("Bulb")) bulbs.Add(r);

        bulbs.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
        SetGlow(0f);
        SetBulbs(false, 0);
        if (rideLight != null) rideLight.intensity = 0f;
        if (animator != null) animator.speed = 0f;
        if (music != null) { music.loop = true; music.volume = 0f; }
        if (powerOnAtStart) PowerOn();
    }

    // ---------- small steps before the full power-on (one per solved challenge) ----------
    float fraction = 1f;          // part of the bulbs that are on (coaster: 5 of 10 lights)

    public void LightsOnly()      // the ride glows and its bulbs are on, but it does not move yet
    {
        if (IsOn) return;
        StopAllCoroutines();
        StartCoroutine(FlickerTo(1f));
    }

    public void BulbFraction(float f) { fraction = Mathf.Clamp01(f); if (!IsOn) StartCoroutine(FlickerTo(Mathf.Max(glow, 0.6f))); }

    public void Slow(float speed = 0.3f)   // starts turning slowly (carousel / wheel)
    {
        if (animator != null) animator.speed = speed;
        if (music != null && music.clip != null) { if (!music.isPlaying) music.Play(); music.volume = musicVolume * 0.4f; }
    }

    public void SetBulbColors(Material a, Material b, float chaseDelay = 0.18f)
    {
        if (a != null) bulbOnA = a;
        if (b != null) bulbOnB = b;
        this.chaseDelay = chaseDelay;
        if (!chase) SetBulbs(true, 0);
    }

    // make one more of the model's materials glow (e.g. the Ferris wheel cabins, lit from inside)
    readonly Dictionary<string, Material> extraGlow = new Dictionary<string, Material>();
    public void GlowMaterial(string matName, Color color, float strength)
    {
        if (!extraGlow.ContainsKey(matName))
        {
            Material copy = null;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < ms.Length; i++)
                    if (ms[i] != null && (ms[i].name == matName || ms[i] == copy))
                    {
                        if (copy == null)
                        {
                            copy = new Material(ms[i]); copy.EnableKeyword("_EMISSION");
                            // no glow picture? use the colour picture: the cabins look lit from inside and keep their details
                            if (copy.GetTexture("_EmissionMap") == null) copy.SetTexture("_EmissionMap", copy.GetTexture("_BaseMap"));
                        }
                        ms[i] = copy; changed = true;
                    }
                if (changed) r.sharedMaterials = ms;
            }
            extraGlow[matName] = copy;
        }
        var m = extraGlow[matName];
        if (m != null) StartCoroutine(FadeGlow(m, color * strength));
        if (rideLight != null) rideLight.intensity = Mathf.Max(rideLight.intensity, lightIntensity * 0.5f);
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.powerOn, transform.position + Vector3.up * 3f);
    }

    IEnumerator FadeGlow(Material m, Color to)
    {
        foreach (float f in new[] { 0.6f, 0f, 0.9f, 0.2f, 1f }) { m.SetColor("_EmissionColor", to * f); yield return new WaitForSeconds(0.12f); }
    }

    public void SetSpeed(float s) { if (animator != null) animator.speed = s; }   // the playground: the player picks the speed

    IEnumerator FlickerTo(float level)
    {
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.powerOn, transform.position + Vector3.up * 2f);
        foreach (float f in new[] { 0.5f, 0f, 0.8f, 0.1f, 1f })
        {
            SetGlow(f * level); SetBulbs(f > 0.5f, 0);
            if (rideLight != null) rideLight.intensity = f * level * lightIntensity * 0.7f;
            yield return new WaitForSeconds(0.12f);
        }
    }

    [ContextMenu("Power On")]
    public void PowerOn()
    {
        if (IsOn) return;
        IsOn = true;
        StartCoroutine(PowerOnRoutine());
    }

    IEnumerator PowerOnRoutine()
    {
        fraction = 1f;
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.powerOn, transform.position + Vector3.up * 2f);   // electric hum + clunk
        // 1) flicker, like old lights warming up
        foreach (float f in new[] { 0.6f, 0f, 0.9f, 0.1f, 1f })
        {
            SetGlow(f); SetBulbs(f > 0.5f, 0);
            if (rideLight != null) rideLight.intensity = f * lightIntensity;
            yield return new WaitForSeconds(0.12f);
        }
        chase = true;
        StartCoroutine(Chase());

        // 2) music fades in and the ride speeds up
        if (music != null && music.clip != null) music.Play();
        if (coaster != null) coaster.Play();
        float t = 0;
        while (t < speedUpTime)
        {
            float k = t / speedUpTime;
            if (animator != null) animator.speed = Mathf.Max(animator.speed, Mathf.SmoothStep(0f, 1f, k));
            if (music != null) music.volume = Mathf.Max(music.volume, k * musicVolume);
            t += Time.deltaTime;
            yield return null;
        }
        if (animator != null) animator.speed = 1f;
        if (music != null) music.volume = musicVolume;
    }

    // running lights: every third bulb is dark and the pattern moves
    IEnumerator Chase()
    {
        int step = 0;
        while (chase)
        {
            SetBulbs(true, step++);
            yield return new WaitForSeconds(chaseDelay);
        }
    }

    void SetGlow(float k)
    {
        glow = k;
        foreach (var m in glowInstances) m.SetColor("_EmissionColor", glowColor * glowStrength * k);
    }

    void SetBulbs(bool on, int step)
    {
        for (int i = 0; i < bulbs.Count; i++)
        {
            Material m = bulbOff;
            if (on && i < Mathf.CeilToInt(bulbs.Count * fraction))
            {
                int phase = (i + step) % 3;
                m = phase == 0 ? bulbOff : (i % 2 == 0 ? bulbOnA : bulbOnB);
                if (step == 0) m = i % 2 == 0 ? bulbOnA : bulbOnB;      // all on during the flicker
            }
            if (m != null) bulbs[i].sharedMaterial = m;
        }
    }
}
