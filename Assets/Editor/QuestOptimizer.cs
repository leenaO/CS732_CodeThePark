using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

// Code the Park - editor helper written by us.
// Menu: CS732 > Optimize For Quest
// After our first FPS test (average 66 FPS, GPU 88-96 % busy) we lower the GPU work WITHOUT changing how the game looks:
//  1. Fixed foveated rendering (OpenXR feature + QuestPerformance.cs at level 0.5): only the edges of the view get fewer pixels.
//  2. Shadow distance 50 m -> 25 m on the Quest (Mobile) render asset: the park is at night, far shadows are not visible.
public static class QuestOptimizer
{
    [MenuItem("CS732/Optimize For Quest")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first."); return; }

        // 1a. OpenXR feature for Android
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var fov = settings != null ? settings.GetFeature<FoveatedRenderingFeature>() : null;
        if (fov != null)
        {
            fov.enabled = true;
            var so = new SerializedObject(fov);
            var eye = so.FindProperty("useEyeTracking");
            if (eye != null) { eye.boolValue = false; so.ApplyModifiedProperties(); }   // Quest 3: no eye tracking, no extra permission
            EditorUtility.SetDirty(fov);
        }
        else Debug.LogWarning("CS732: Foveated Rendering feature not found in OpenXR settings (Android).");

        // 1b. runtime script
        var sys = GameObject.Find("GameSystems");
        if (sys != null && sys.GetComponent<QuestPerformance>() == null) sys.AddComponent<QuestPerformance>();

        // 2. shadows on the Quest render asset
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (rp != null) { rp.shadowDistance = 25f; EditorUtility.SetDirty(rp); }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: Quest optimized - foveation " + (fov != null && fov.enabled) + ", runtime script " + (sys != null) + ", shadow distance " + (rp != null ? rp.shadowDistance.ToString() : "?") + " m.");
    }

    // We tested it in the Quest 3: with foveated rendering the text became blocky and the picture looked worse.
    // So we went back to our first settings. This menu undoes everything that "Optimize For Quest" changed.
    [MenuItem("CS732/Undo Optimize For Quest (back to our first settings)")]
    public static void UndoOptimize()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first."); return; }
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var fov = settings != null ? settings.GetFeature<FoveatedRenderingFeature>() : null;
        if (fov != null) { fov.enabled = false; EditorUtility.SetDirty(fov); }
        var sys = GameObject.Find("GameSystems");
        var qp = sys != null ? sys.GetComponent<QuestPerformance>() : null;
        bool removed = qp != null;
        if (qp != null) Object.DestroyImmediate(qp);
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (rp != null) { rp.shadowDistance = 50f; EditorUtility.SetDirty(rp); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: back to the first settings - foveation " + (fov != null && fov.enabled) + ", runtime script removed " + removed + ", shadow distance " + (rp != null ? rp.shadowDistance.ToString() : "?") + " m.");
    }
}

// Runs the undo ONE time by itself after Unity compiles this file (so nobody has to click the menu).
[InitializeOnLoad]
static class QuestOptimizerAutoUndo
{
    const string Flag = "CS732_QuestOptimizer_Undone_v1";
    static QuestOptimizerAutoUndo()
    {
        if (EditorPrefs.GetBool(Flag, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isCompiling) return;
            EditorPrefs.SetBool(Flag, true);
            QuestOptimizer.UndoOptimize();
        };
    }
}
