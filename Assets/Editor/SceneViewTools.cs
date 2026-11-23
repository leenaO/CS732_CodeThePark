using UnityEditor;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Toggle Scene View Lighting  (Ctrl+Alt+L)
// Turns the night lighting on/off in the Scene view only, so we can see the real
// colours while building. It does not change the game or the saved scene.
public static class SceneViewTools
{
    [MenuItem("CS732/Toggle Scene View Lighting %&l")]
    public static void ToggleLighting()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) { Debug.Log("Open the Scene view first."); return; }
        sv.sceneLighting = !sv.sceneLighting;
        sv.Repaint();
        Debug.Log("Scene view lighting: " + (sv.sceneLighting ? "ON (night)" : "OFF (see real colours)"));
    }
}
