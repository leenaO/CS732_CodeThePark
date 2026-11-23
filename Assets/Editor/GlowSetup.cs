using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Glow (Bloom)
// Why: an emissive material only makes a surface bright; the soft "glow" around a light
// comes from the Bloom post-processing effect (part of URP, Unity's render pipeline).
// This tool: 1) turns on HDR in the Quest (Mobile) render asset, so colours brighter than
//            white exist and only the lights glow, not white walls or text,
//         2) adds a Global Volume with Bloom set up cheaply for Quest 3,
//         3) turns on Post Processing on every camera in the scene.
public static class GlowSetup
{
    const string ProfilePath = "Assets/Settings/ParkGlowProfile.asset";

    [MenuItem("CS732/Setup Glow (Bloom)")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        // 1) HDR on in both render assets (Quest uses "Mobile", the editor may use "PC")
        foreach (var path in new[] { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" })
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (rp == null) continue;
            rp.supportsHDR = true;
            EditorUtility.SetDirty(rp);
        }

        // 2) volume profile with Bloom
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        profile.components.RemoveAll(c => c == null);     // clean a profile that lost its Bloom
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
            // IMPORTANT: save Bloom INSIDE the profile file, otherwise Unity forgets it after
            // recompiling (this was why the first version gave almost no glow)
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        bloom.active = true;
        bloom.threshold.Override(1.1f);            // only things brighter than white (our lights) glow
        bloom.intensity.Override(2.2f);
        bloom.scatter.Override(0.8f);              // how far the glow spreads
        bloom.tint.Override(new Color(1f, 0.92f, 0.8f));   // a little warm, like old light bulbs
        bloom.highQualityFiltering.Override(false);// cheaper, fine for VR
        bloom.downscale.Override(BloomDownscaleMode.Half);
        bloom.maxIterations.Override(6);

        // brighter bulbs: the glow grows with how much brighter than white a bulb is
        foreach (var (name, c) in new[] {
            ("BulbOn_Yellow_Mat", new Color(1f, 0.82f, 0.35f)), ("BulbOn_White_Mat", new Color(1f, 1f, 1f)),
            ("BulbOn_Pink_Mat", new Color(1f, 0.35f, 0.7f)),    ("BulbOn_Blue_Mat", new Color(0.3f, 0.6f, 1f)),
            ("BulbOn_Green_Mat", new Color(0.3f, 1f, 0.5f)) })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");
            if (m == null) continue;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 6f);
            EditorUtility.SetDirty(m);
        }
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(profile);

        var old = GameObject.Find("ParkGlowVolume");
        if (old != null) Object.DestroyImmediate(old);
        var go = new GameObject("ParkGlowVolume");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1;
        vol.sharedProfile = profile;

        // 3) post processing on for every camera (the headset camera is inside the XR Origin)
        int cams = 0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(data);
                cams++;
            }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: glow (Bloom) ready - HDR on, Global Volume added, post processing on " + cams + " camera(s).");
    }
}
