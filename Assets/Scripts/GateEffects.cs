using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Code the Park - written by us.
// The park gate (built by GateBuilder) is dark and locked at the start.
// Each of the 5 level-1 challenges fixes one part, and the last one opens the doors:
//   1 the neon name lights up, 2 the year appears, 3 the bulbs light up,
//   4 the golden sun wheel lights up, 5 the doors swing open.
// Everything is changed on runtime copies, so the material files never change.
public class GateEffects : MonoBehaviour
{
    public Material bulbOff, bulbOn;
    public float openAngle = 95f;
    public float openTime = 3f;

    readonly List<TextMeshPro> neon = new List<TextMeshPro>();
    readonly List<Color> neonColors = new List<Color>();
    readonly List<Renderer> bulbs = new List<Renderer>(), sunBulbs = new List<Renderer>(), towerBulbs = new List<Renderer>();
    readonly List<Material> sunMats = new List<Material>(), lanternMats = new List<Material>();
    readonly List<Color> sunGlow = new List<Color>(), lanternGlow = new List<Color>();
    TextMeshPro subtitle;
    Transform doorL, doorR;
    public bool IsOpen { get; private set; }

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("Neon_")) { var tmp = t.GetComponent<TextMeshPro>(); neon.Add(tmp); neonColors.Add(tmp.color); }
            if (t.name == "GateSignText") subtitle = t.GetComponent<TextMeshPro>();
            if (t.name == "GateDoor_L") doorL = t;
            if (t.name == "GateDoor_R") doorR = t;
        }
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.name != "GateBulb") continue;
            if (r.transform.parent.name == "GateBulbs_Tower") towerBulbs.Add(r);
            else if (r.transform.localPosition.y > 3.6f && Mathf.Abs(r.transform.localPosition.y - 2.92f) > 0.1f && IsOnSunRim(r.transform)) sunBulbs.Add(r);
            else bulbs.Add(r);
        }
        // runtime copies of the glowing materials (sun wheel + tower lanterns)
        Copy("Gate_Sun", sunMats, sunGlow);
        Copy("Gate_Lantern", lanternMats, lanternGlow);

        // the dark, broken gate
        for (int i = 0; i < neon.Count; i++) { neon[i].color = new Color(0.18f, 0.18f, 0.2f); neon[i].fontMaterial.SetFloat("_GlowPower", 0f); }
        if (subtitle != null) subtitle.text = "";
        SetBulbs(bulbs, false); SetBulbs(sunBulbs, false); SetBulbs(towerBulbs, false);
        Glow(sunMats, sunGlow, 0f); Glow(lanternMats, lanternGlow, 0f);
    }

    bool IsOnSunRim(Transform t)
    {
        var p = t.localPosition;   // the sun wheel's bulbs sit on a circle of radius 4.4 around (0, 4.4)
        return Mathf.Abs(new Vector2(p.x, p.y - 4.4f).magnitude - 4.4f) < 0.2f;
    }

    // ---------------- the five steps ----------------
    public void NeonOn() => StartCoroutine(NeonRoutine());
    public void Year() { if (subtitle != null) StartCoroutine(TypeText(subtitle, "LEARN  C++  IN  VR   ·   SINCE  2026")); }
    public void BulbsOn() => StartCoroutine(Flicker(bulbs, null, null));
    public void SunWheelOn() => StartCoroutine(Flicker(sunBulbs, sunMats, sunGlow));
    public void Open() { if (!IsOpen) StartCoroutine(OpenRoutine()); }

    IEnumerator NeonRoutine()
    {
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.powerOn, transform.position + Vector3.up * 6f);
        for (int i = 0; i < neon.Count; i++)          // the letters light up one after another
        {
            neon[i].color = neonColors[i];
            neon[i].fontMaterial.SetFloat("_GlowPower", 1f);
            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator TypeText(TextMeshPro t, string s)
    {
        for (int i = 0; i <= s.Length; i++) { t.text = s.Substring(0, i); yield return new WaitForSeconds(0.04f); }
    }

    IEnumerator Flicker(List<Renderer> rs, List<Material> mats, List<Color> glow)
    {
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.powerOn, transform.position + Vector3.up * 4f);
        foreach (float f in new[] { 1f, 0f, 1f, 0.2f, 1f })
        {
            SetBulbs(rs, f > 0.5f);
            if (mats != null) Glow(mats, glow, f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator OpenRoutine()
    {
        IsOpen = true;
        SetBulbs(towerBulbs, true); Glow(lanternMats, lanternGlow, 1f);
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.gateOpen, transform.position + Vector3.up * 1.5f);
        Quaternion l0 = doorL.localRotation, r0 = doorR.localRotation;
        Quaternion l1 = l0 * Quaternion.Euler(0, -openAngle, 0), r1 = r0 * Quaternion.Euler(0, openAngle, 0);   // swing into the park
        float t = 0;
        while (t < openTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / openTime);
            doorL.localRotation = Quaternion.Slerp(l0, l1, k);
            doorR.localRotation = Quaternion.Slerp(r0, r1, k);
            t += Time.deltaTime;
            yield return null;
        }
        doorL.localRotation = l1; doorR.localRotation = r1;
        // running lights on the whole gate
        var all = new List<Renderer>(bulbs); all.AddRange(sunBulbs); all.AddRange(towerBulbs);
        int step = 0;
        while (true)
        {
            for (int i = 0; i < all.Count; i++) all[i].sharedMaterial = (i + step) % 3 == 0 ? bulbOff : bulbOn;
            step++;
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ---------------- helpers ----------------
    void SetBulbs(List<Renderer> rs, bool on) { foreach (var r in rs) r.sharedMaterial = on ? bulbOn : bulbOff; }

    void Copy(string matName, List<Material> into, List<Color> glow)
    {
        var copies = new Dictionary<Material, Material>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null || ms[i].name != matName) continue;
                if (!copies.TryGetValue(ms[i], out var c))
                {
                    c = new Material(ms[i]); copies[ms[i]] = c; into.Add(c);
                    glow.Add(ms[i].GetColor("_EmissionColor"));
                }
                ms[i] = c; changed = true;
            }
            if (changed) r.sharedMaterials = ms;
        }
    }

    void Glow(List<Material> ms, List<Color> g, float k) { for (int i = 0; i < ms.Count; i++) ms[i].SetColor("_EmissionColor", g[i] * k); }
}
