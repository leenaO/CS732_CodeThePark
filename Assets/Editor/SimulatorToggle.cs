using UnityEditor;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Device Simulator ON / OFF
// ON: test with keyboard and mouse.  OFF: test with the real Quest 3 over Quest Link (the simulator
// would fight with the real headset and controllers).
public static class SimulatorToggle
{
    const string Path = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

    [MenuItem("CS732/Device Simulator OFF (use Quest Link)")]
    static void Off() => Set(false);

    [MenuItem("CS732/Device Simulator ON (keyboard + mouse)")]
    static void On() => Set(true);

    static void Set(bool on)
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Path);
        if (asset == null) { Debug.LogWarning("CS732: simulator settings not found."); return; }
        var so = new SerializedObject(asset);
        so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = on;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("CS732: XR Device Simulator " + (on ? "ON - keyboard and mouse." : "OFF - put on the Quest with Quest Link and press Play."));
    }
}
