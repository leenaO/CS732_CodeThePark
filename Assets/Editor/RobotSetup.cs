using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Add Robot Teacher (Codey)
public static class RobotSetup
{
    const string ModelPath = "Assets/Models/Robot_BotNeil/scene.gltf";

    [MenuItem("CS732/Add Robot Teacher (Codey)")]
    public static void Run()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (asset == null) { Debug.LogWarning("Robot model not imported yet. Install glTFast (com.unity.cloud.gltfast) first."); return; }

        var old = GameObject.Find("Codey");
        if (old != null) Object.DestroyImmediate(old);

        var codey = new GameObject("Codey");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, codey.transform);
        model.name = "RobotModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // size: about 0.75 m tall, centred on the "Codey" object
        Bounds b = B(model);
        if (b.size.y > 0.0001f) model.transform.localScale = Vector3.one * (0.75f / b.size.y);
        b = B(model);
        model.transform.position += codey.transform.position - b.center;

        // speech bubble above the robot: dark panel + white text
        var bubble = new GameObject("SpeechBubble");
        bubble.transform.SetParent(codey.transform, false);
        bubble.transform.localPosition = new Vector3(0, 0.85f, 0);
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.transform.SetParent(bubble.transform, false);
        panel.transform.localPosition = new Vector3(0, 0, 0.01f);
        panel.transform.localScale = new Vector3(1.5f, 0.55f, 0.01f);
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Dark_Mat.mat");
        if (mat != null) panel.GetComponent<Renderer>().sharedMaterial = mat;
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(bubble.transform, false);
        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.text = "";
        tmp.fontSize = 1.1f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(1.4f, 0.5f);
        tmp.textWrappingMode = TextWrappingModes.Normal;

        var rt = codey.AddComponent<RobotTeacher>();
        rt.model = model.transform;
        rt.bubble = bubble.transform;
        rt.bubbleText = tmp;

        // start next to the first code station, beside the path
        codey.transform.position = new Vector3(-4.7f, 1.25f, -9.4f);   // beside station 1
        rt.FlyTo(new Vector3(-4.7f, 0, -9.4f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = codey;
        Debug.Log("CS732: Codey the robot teacher added. Tick 'Demo Mode' and press Play to see all his moves.");
    }

    static Bounds B(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
