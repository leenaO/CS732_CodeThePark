using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Celebration
// Makes the materials for the fireworks and confetti (a soft glowing dot, made here in code),
// and connects the Celebration (fireworks, confetti, message, results, RESTART) to the LevelManager.
public static class CelebrationSetup
{
    const string Dir = "Assets/Materials/Celebration";

    [MenuItem("CS732/Setup Celebration")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first."); return; }
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Materials", "Celebration");

        // a soft round dot (white in the middle, fading out), our own texture
        string texPath = Dir + "/SoftDot.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                float a = Mathf.Clamp01(1f - d); a = a * a;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
        }
        var dot = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        var spark = Particle("Spark_Additive", dot, true, Color.white * 3f);      // brighter than white: it glows (Bloom)
        var confetti = Particle("Confetti", null, false, Color.white);
        var panel = Lit("Results_Panel", new Color(0.04f, 0.07f, 0.12f), Color.black);
        var button = Lit("Results_Button", new Color(0.9f, 0.2f, 0.12f), new Color(1f, 0.2f, 0.1f) * 1.2f);

        var sys = GameObject.Find("GameSystems");
        if (sys == null) { Debug.LogWarning("CS732: run Setup Game Flow first."); return; }
        var c = sys.GetComponent<Celebration>(); if (c == null) c = sys.AddComponent<Celebration>();
        c.sparkMat = spark; c.confettiMat = confetti; c.panelMat = panel; c.buttonMat = button;
        var lm = sys.GetComponent<LevelManager>(); if (lm != null) lm.celebration = c;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: celebration ready (fireworks, confetti, message, results + RESTART, 15-minute timer).");
    }

    static Material Particle(string name, Texture2D tex, bool additive, Color baseColor)
    {
        string path = Dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, path); }
        m.SetFloat("_Surface", 1f);                                   // transparent
        m.SetFloat("_Blend", additive ? 2f : 0f);                     // additive (glow) or normal
        m.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);                                      // both sides (confetti turns around)
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", baseColor);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Lit(string name, Color c, Color e)
    {
        string path = Dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        if (e != Color.black) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", e); }
        EditorUtility.SetDirty(m);
        return m;
    }
}
