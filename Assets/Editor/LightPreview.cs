using UnityEditor;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Preview Ride Lights ON / OFF
// The ride lights start OFF (the park is dark until the player fixes the code).
// This lets us look at the lights in the editor. Turn them OFF again before building.
public static class LightPreview
{
    static readonly (string path, Color on)[] Mats = {
        ("Assets/Materials/Rides/FW_Atlas.mat",    new Color(1f, 0.8f, 0.45f) * 4f),
        ("Assets/Materials/Rides/Car_Tent.mat",    new Color(1f, 0.85f, 0.5f) * 1.2f),
        ("Assets/Materials/Rides/Car_Palette.mat", new Color(1f, 0.85f, 0.4f) * 1.2f),
        // park entrance (GateBuilder): sun wheel and tower lanterns
        ("Assets/Materials/Gate/Gate_Sun.mat",    new Color(1f, 0.72f, 0.1f) * 2f),
        ("Assets/Materials/Gate/Gate_Lantern.mat", new Color(1f, 0.72f, 0.3f) * 1.2f),
    };

    [MenuItem("CS732/Preview Ride Lights ON")]
    public static void On()  { Set(true); }

    [MenuItem("CS732/Preview Ride Lights OFF")]
    public static void Off() { Set(false); }

    static void Set(bool on)
    {
        foreach (var (path, c) in Mats)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) continue;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", on ? c : Color.black);
            EditorUtility.SetDirty(m);
        }
        // neon name letters on the gate: TextMeshPro glow on / off
        for (int i = 0; i < 6; i++)
        {
            var n = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Neon_" + i + ".mat");
            if (n == null) continue;
            if (on) n.EnableKeyword("GLOW_ON"); else n.DisableKeyword("GLOW_ON");
            n.SetFloat("_GlowPower", on ? 1f : 0f);
            EditorUtility.SetDirty(n);
        }
        // our own bulbs (wheel ring): swap between the "off" and "on" materials
        var offM = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulbOff_Mat.mat");
        var onM  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulbOn_Yellow_Mat.mat");
        foreach (string holder in new[] { "WheelLights", "RollerCoasterModel", "Area1_Gate", "CarouselLights" })
        {
            var ring = GameObject.Find(holder);
            if (ring == null) continue;
            foreach (var r in ring.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial == offM || r.sharedMaterial == onM || r.name.StartsWith("Bulbs_"))
                    r.sharedMaterial = on ? onM : offM;
        }
        if (!Application.isPlaying)   // in Play mode the scene cannot be saved (the preview still shows)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("CS732: ride lights preview " + (on ? "ON" : "OFF"));
    }
}
