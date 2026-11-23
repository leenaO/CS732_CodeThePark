using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Start Screen
// Adds the StartPanel (game name, goal, controls, START button) to GameSystems and gives it its materials.
public static class StartPanelSetup
{
    [MenuItem("CS732/Setup Start Screen")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first."); return; }
        var sys = GameObject.Find("GameSystems");
        if (sys == null) { Debug.LogWarning("CS732: run Setup Game Flow first."); return; }
        var sp = sys.GetComponent<StartPanel>(); if (sp == null) sp = sys.AddComponent<StartPanel>();
        sp.boardMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ScreenOff_Mat.mat");
        sp.frameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Teal.mat");
        sp.buttonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stations/Button_Red.mat");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: Start screen ready (" + (sp.boardMat != null) + "/" + (sp.frameMat != null) + "/" + (sp.buttonMat != null) + ").");
    }
}
