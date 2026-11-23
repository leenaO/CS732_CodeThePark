using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Code the Park - editor helper written by us.
// Menu: CS732 > Build Code Screens
// Adds to each of the 4 stations: texts on the screen, a yellow slot (XR Socket Interactor),
// a desk with 3 code blocks (XR Grab Interactable) and one big button (I'M READY / RUN).
// Station 1 is set to test challenge 1 (until the LevelManager is added).
public static class CodeScreenBuilder
{
    static Material good, bad, idle, blockMat, slotMat, buttonMat, desk, iron;

    [MenuItem("CS732/Build Code Screens")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        Materials();
        int n = 0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith("Station_") && t.Find("ScreenBoard") != null) { Build(t.gameObject); n++; }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("CS732: code screens built on " + n + " stations. Station 1 tests challenge 1 - press Play.");
    }

    static void Build(GameObject st)
    {
        foreach (var name in new[] { "ScreenUI", "Desk" }) { var o = st.transform.Find(name); if (o != null) Object.DestroyImmediate(o.gameObject); }
        var old = st.GetComponent<CodeStation>(); if (old != null) Object.DestroyImmediate(old);

        // ---------- texts on the screen (same tilt as the board) ----------
        var ui = Child(st, "ScreenUI", new Vector3(0, 1.45f, 0));
        ui.transform.localRotation = Quaternion.Euler(-12, 0, 0);
        float z = -0.04f;
        var header  = Text(ui, "Header",   new Vector3(0, 0.43f, z),  new Vector2(1.7f, 0.09f), 0.42f, TextAlignmentOptions.Center, true);
        var body    = Text(ui, "Body",     new Vector3(0, 0.23f, z),  new Vector2(1.66f, 0.28f), 0.45f, TextAlignmentOptions.Left, true);
        var example = Text(ui, "Example",  new Vector3(0, 0.05f, z),  new Vector2(1.66f, 0.08f), 0.38f, TextAlignmentOptions.Left, true);
        var line    = Text(ui, "CodeLine", new Vector3(0, -0.1f, z),  new Vector2(1.7f, 0.14f), 0.7f, TextAlignmentOptions.Center, false);
        var fb      = Text(ui, "Feedback", new Vector3(0, -0.41f, z), new Vector2(1.66f, 0.15f), 0.4f, TextAlignmentOptions.Center, true);

        // ---------- the yellow slot (where a block goes) ----------
        var slotGo = Child(ui, "Slot", new Vector3(0, -0.25f, -0.09f));
        foreach (var (p, s) in new[] { (new Vector3(0, 0.055f, 0), new Vector3(0.36f, 0.012f, 0.012f)), (new Vector3(0, -0.055f, 0), new Vector3(0.36f, 0.012f, 0.012f)),
                                       (new Vector3(-0.18f, 0, 0), new Vector3(0.012f, 0.12f, 0.012f)), (new Vector3(0.18f, 0, 0), new Vector3(0.012f, 0.12f, 0.012f)) })
            Part(slotGo, PrimitiveType.Cube, p, s, slotMat);
        var hint = Text(slotGo, "Hint", new Vector3(0, 0, 0.02f), new Vector2(0.34f, 0.08f), 0.3f, TextAlignmentOptions.Center, false);
        hint.text = "<color=#FFD54A55>put a block here</color>";
        var trig = slotGo.AddComponent<SphereCollider>(); trig.isTrigger = true; trig.radius = 0.12f;
        var socket = slotGo.AddComponent<XRSocketInteractor>();
        socket.showInteractableHoverMeshes = true;
        socket.recycleDelayTime = 0.5f;

        // ---------- desk with the blocks and the big button ----------
        var deskGo = Child(st, "Desk", new Vector3(0, 0, -0.32f));
        Part(deskGo, PrimitiveType.Cube, new Vector3(0, 0.92f, 0), new Vector3(1.8f, 0.05f, 0.32f), desk);
        Part(deskGo, PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0.05f), new Vector3(0.08f, 0.45f, 0.08f), iron);
        var blocks = new CodeBlock[3]; var spots = new Transform[3];
        float[] xs = { -0.62f, -0.27f, 0.08f };
        for (int i = 0; i < 3; i++)
        {
            var spot = Child(deskGo, "Spot" + i, new Vector3(xs[i], 0.99f, 0.02f));
            spot.transform.localRotation = Quaternion.Euler(35, 0, 0);       // tilted towards the player's eyes
            spots[i] = spot.transform;
            blocks[i] = Block(deskGo, "CodeBlock" + i, spot.transform);
        }
        var btn = Child(deskGo, "BigButton", new Vector3(0.56f, 0.945f, -0.02f));
        Part(btn, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.24f, 0.015f, 0.24f), iron);
        var cap = Part(btn, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(0.18f, 0.025f, 0.18f), buttonMat);
        var bc = btn.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.04f, 0); bc.size = new Vector3(0.24f, 0.1f, 0.24f);
        btn.AddComponent<XRSimpleInteractable>();
        var pb = btn.AddComponent<PressButton>(); pb.cap = cap.transform;
        var lbl = Text(btn, "Label", new Vector3(0, 0.16f, 0), new Vector2(0.3f, 0.14f), 0.5f, TextAlignmentOptions.Center, false);
        lbl.transform.localRotation = Quaternion.Euler(20, 0, 0);

        // ---------- the station logic ----------
        var cs = st.AddComponent<CodeStation>();
        cs.header = header; cs.body = body; cs.example = example; cs.codeLine = line; cs.feedback = fb;
        cs.screen = st.transform.Find("ScreenBoard").GetComponent<Renderer>();
        cs.screenIdle = idle; cs.screenGood = good; cs.screenBad = bad;
        cs.slot = socket; cs.blocks = blocks; cs.blockSpots = spots;
        cs.bigButton = pb; cs.bigButtonLabel = lbl;
        cs.testChallenge = st.name.StartsWith("Station_1") ? 1 : 0;
    }

    static CodeBlock Block(GameObject parent, string name, Transform spot)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        g.transform.SetPositionAndRotation(spot.position, spot.rotation);
        Part(g, PrimitiveType.Cube, Vector3.zero, new Vector3(0.3f, 0.08f, 0.05f), blockMat);
        Part(g, PrimitiveType.Cube, new Vector3(0, 0.043f, 0), new Vector3(0.3f, 0.008f, 0.052f), slotMat);   // gold edge
        var t = Text(g, "Label", new Vector3(0, 0, -0.027f), new Vector2(0.28f, 0.07f), 0.42f, TextAlignmentOptions.Center, false);
        var col = g.AddComponent<BoxCollider>(); col.size = new Vector3(0.3f, 0.08f, 0.05f);
        var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var grab = g.AddComponent<XRGrabInteractable>(); grab.throwOnDetach = false;
        var cb = g.AddComponent<CodeBlock>(); cb.label = t;
        g.SetActive(false);
        return cb;
    }

    // ---------------- helpers ----------------
    static GameObject Child(GameObject parent, string name, Vector3 local)
    {
        var g = new GameObject(name); g.transform.SetParent(parent.transform, false); g.transform.localPosition = local; return g;
    }

    static GameObject Part(GameObject parent, PrimitiveType type, Vector3 local, Vector3 scale, Material m)
    {
        var p = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(parent.transform, false);
        p.transform.localPosition = local; p.transform.localScale = scale;
        p.GetComponent<Renderer>().sharedMaterial = m;
        return p;
    }

    static TextMeshPro Text(GameObject parent, string name, Vector3 local, Vector2 box, float maxSize, TextAlignmentOptions align, bool wrap)
    {
        var g = new GameObject(name); g.transform.SetParent(parent.transform, false); g.transform.localPosition = local;
        var t = g.AddComponent<TextMeshPro>();
        t.text = ""; t.color = Color.white; t.alignment = align;
        t.rectTransform.sizeDelta = box;
        t.enableAutoSizing = true; t.fontSizeMin = 0.15f; t.fontSizeMax = maxSize;
        t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.richText = true;
        return t;
    }

    static void Materials()
    {
        const string dir = "Assets/Materials/Stations";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Materials", "Stations");
        idle = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ScreenOff_Mat.mat");
        good = M(dir, "Screen_Good", new Color(0.03f, 0.16f, 0.06f), new Color(0.02f, 0.22f, 0.06f));
        bad = M(dir, "Screen_Bad", new Color(0.2f, 0.03f, 0.03f), new Color(0.28f, 0.02f, 0.02f));
        blockMat = M(dir, "Block_Navy", new Color(0.08f, 0.12f, 0.25f), null);
        slotMat = M(dir, "Slot_Gold", new Color(1f, 0.78f, 0.2f), new Color(1f, 0.7f, 0.15f) * 1.3f);
        buttonMat = M(dir, "Button_Red", new Color(0.9f, 0.18f, 0.12f), new Color(1f, 0.2f, 0.1f) * 1.2f);
        desk = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Teal.mat");
        iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Iron.mat");
    }

    static Material M(string dir, string name, Color c, Color? e)
    {
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.4f);
        if (e.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", e.Value); }
        EditorUtility.SetDirty(m);
        return m;
    }
}
