using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Game Flow
// Adds the LevelManager (runs the 4 levels) and GateEffects (the gate starts dark and locked),
// and connects the 4 stations and the 3 rides to it.
public static class GameFlowSetup
{
    [MenuItem("CS732/Setup Game Flow")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + n + ".mat");

        var gateGo = GameObject.Find("Area1_Gate");
        GateEffects gate = null;
        if (gateGo != null)
        {
            gate = gateGo.GetComponent<GateEffects>();
            if (gate == null) gate = gateGo.AddComponent<GateEffects>();
            gate.bulbOff = M("BulbOff_Mat"); gate.bulbOn = M("BulbOn_Yellow_Mat");
        }

        var sys = GameObject.Find("GameSystems");
        if (sys == null) sys = new GameObject("GameSystems");
        var lm = sys.GetComponent<LevelManager>();
        if (lm == null) lm = sys.AddComponent<LevelManager>();
        lm.gate = gate;
        var cel = sys.GetComponent<Celebration>(); if (cel != null) lm.celebration = cel;
        string[] names = { "Station_1", "Station_2", "Station_3", "Station_4" };
        int found = 0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var cs in root.GetComponentsInChildren<CodeStation>(true))
                for (int i = 0; i < 4; i++) if (cs.name.StartsWith(names[i])) { lm.stations[i] = cs; found++; }
        lm.carousel = Ride("Area2_Carousel"); lm.wheel = Ride("Area3_FerrisWheel"); lm.coaster = Ride("Area4_RollerCoaster");
        lm.bulbBlue = M("BulbOn_Blue_Mat"); lm.bulbGreen = M("BulbOn_Green_Mat"); lm.bulbPink = M("BulbOn_Pink_Mat");
        lm.bulbYellow = M("BulbOn_Yellow_Mat"); lm.bulbWhite = M("BulbOn_White_Mat");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: game flow ready - " + found + " stations, gate " + (gate != null ? "OK" : "missing") +
                  ", rides " + (lm.carousel != null) + "/" + (lm.wheel != null) + "/" + (lm.coaster != null) + ". Press Play.");
    }

    static RideController Ride(string area)
    {
        var g = GameObject.Find(area);
        return g != null ? g.GetComponent<RideController>() : null;
    }
}
