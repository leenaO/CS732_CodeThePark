using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Codey AI
// Adds CodeyAI (the 3 "Ask Codey" buttons + the connection to our local AI backend) and allows
// plain HTTP, so the Quest can talk to the backend on the laptop in the same Wi-Fi (no internet, no key).
public static class AISetup
{
    [MenuItem("CS732/Setup Codey AI")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first."); return; }
        var sys = GameObject.Find("GameSystems");
        if (sys == null) { Debug.LogWarning("CS732: run Setup Game Flow first."); return; }
        var ai = sys.GetComponent<CodeyAI>(); if (ai == null) ai = sys.AddComponent<CodeyAI>();
        ai.timeoutSeconds = 30;                                  // the small local model can need 10-20 s
        ai.buttonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Collectibles/Gem_Cyan.mat");   // glowing cyan buttons

        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;   // http://<laptop>:8000 on the local Wi-Fi

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: Codey AI ready. Backend URL: " + ai.backendUrl + "  (for the Quest build, put the laptop's Wi-Fi address in GameSystems > Codey AI).");
    }
}
