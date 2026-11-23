using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Code the Park - editor helper written by us.
// Menu: CS732 > Add Collectibles And Wrist Counter
// Builds our own coin, gem and Fix Tool (screwdriver) from simple shapes, places
// 3 coins + 1 Fix Tool + 1 hidden gem in each of the 4 levels, adds the Inventory,
// and puts the counter on the LEFT controller (wrist).
public static class CollectiblesSetup
{
    const string MatDir = "Assets/Materials/Collectibles";
    const string MeshPath = "Assets/Models/Collectibles/Gem.asset";
    static Material gold, gem, handle, metal, dark;
    static Mesh gemMesh;

    // positions on the ground (x, z) and height of each item. Level 1 is outside the gate.
    static readonly (int level, Inventory.Kind kind, Vector3 pos)[] Items = {
        (1, Inventory.Kind.Coin, new Vector3(-7.6f, 1.2f, -10.6f)),   // in front of the left ticket window
        (1, Inventory.Kind.Coin, new Vector3( 6.0f, 1.2f, -15.5f)),
        (1, Inventory.Kind.Coin, new Vector3(-2.8f, 1.2f, -19.0f)),
        (1, Inventory.Kind.Tool, new Vector3( 9.5f, 1.0f, -11.0f)),   // by the right tower
        (1, Inventory.Kind.Gem,  new Vector3(-13.5f, 0.5f, -10.5f)),  // hidden past the left tower

        (2, Inventory.Kind.Coin, new Vector3(-6.5f, 1.2f, 1.0f)),     // around the carousel
        (2, Inventory.Kind.Coin, new Vector3(-17.0f, 1.2f, 1.5f)),
        (2, Inventory.Kind.Coin, new Vector3(-9.0f, 1.2f, 14.5f)),
        (2, Inventory.Kind.Tool, new Vector3(-18.5f, 1.0f, 10.0f)),
        (2, Inventory.Kind.Gem,  new Vector3(-15.0f, 0.5f, 15.5f)),

        (3, Inventory.Kind.Coin, new Vector3( 6.5f, 1.2f, 2.5f)),     // around the Ferris wheel
        (3, Inventory.Kind.Coin, new Vector3(17.0f, 1.2f, 5.0f)),
        (3, Inventory.Kind.Coin, new Vector3(15.0f, 1.2f, 17.0f)),
        (3, Inventory.Kind.Tool, new Vector3( 6.5f, 1.0f, 15.0f)),
        (3, Inventory.Kind.Gem,  new Vector3(19.0f, 0.5f, 12.0f)),

        (4, Inventory.Kind.Coin, new Vector3(-8.0f, 1.2f, 15.0f)),    // around the roller coaster
        (4, Inventory.Kind.Coin, new Vector3( 8.0f, 1.2f, 15.5f)),
        (4, Inventory.Kind.Coin, new Vector3(-3.0f, 1.2f, 13.0f)),
        (4, Inventory.Kind.Tool, new Vector3(-12.5f, 1.0f, 20.0f)),
        (4, Inventory.Kind.Gem,  new Vector3(12.5f, 0.5f, 25.0f)),

        // ---- more items (so the player does not have to find every single one) ----
        (1, Inventory.Kind.Coin, new Vector3(12f, 1.2f, -20f)),
        (1, Inventory.Kind.Coin, new Vector3(-14f, 1.2f, -22f)),
        (1, Inventory.Kind.Coin, new Vector3(3f, 1.2f, -25f)),
        (1, Inventory.Kind.Tool, new Vector3(-18f, 1.0f, -14f)),
        (1, Inventory.Kind.Gem,  new Vector3(17f, 0.5f, -24f)),

        (2, Inventory.Kind.Coin, new Vector3(-22f, 1.2f, 8f)),
        (2, Inventory.Kind.Coin, new Vector3(-20f, 1.2f, 16f)),
        (2, Inventory.Kind.Coin, new Vector3(-6f, 1.2f, -3f)),
        (2, Inventory.Kind.Tool, new Vector3(-24f, 1.0f, 2f)),
        (2, Inventory.Kind.Gem,  new Vector3(-25f, 0.5f, 20f)),

        (3, Inventory.Kind.Coin, new Vector3(22f, 1.2f, 10f)),
        (3, Inventory.Kind.Coin, new Vector3(20f, 1.2f, 1f)),
        (3, Inventory.Kind.Coin, new Vector3(9f, 1.2f, -4f)),
        (3, Inventory.Kind.Tool, new Vector3(24f, 1.0f, 18f)),
        (3, Inventory.Kind.Gem,  new Vector3(25f, 0.5f, -3f)),

        (4, Inventory.Kind.Coin, new Vector3(-18f, 1.2f, 26f)),
        (4, Inventory.Kind.Coin, new Vector3(18f, 1.2f, 28f)),
        (4, Inventory.Kind.Coin, new Vector3(0f, 1.2f, 29f)),
        (4, Inventory.Kind.Tool, new Vector3(20f, 1.0f, 22f)),
        (4, Inventory.Kind.Gem,  new Vector3(-24f, 0.5f, 27f)),
    };

    [MenuItem("CS732/Add Collectibles And Wrist Counter")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        Materials();
        gemMesh = GemMesh();

        // ---- the Inventory (keeps the counts) ----
        var sys = GameObject.Find("GameSystems") ?? new GameObject("GameSystems");
        if (sys.GetComponent<Inventory>() == null) sys.AddComponent<Inventory>();

        // ---- the items ----
        var old = GameObject.Find("Collectibles");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Collectibles");
        var levels = new Transform[5];
        for (int l = 1; l <= 4; l++) levels[l] = new GameObject("Level" + l).transform;
        for (int l = 1; l <= 4; l++) levels[l].SetParent(root.transform);
        int n = 0;
        foreach (var (level, kind, pos) in Items)
        {
            var g = kind == Inventory.Kind.Coin ? Coin(levels[level]) : kind == Inventory.Kind.Gem ? Gem(levels[level]) : Tool(levels[level]);
            g.transform.position = pos;
            Grabbable(g, kind, level);
            n++;
        }

        // ---- the wrist counter on the left controller ----
        string wrist = AddWristCounter();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("CS732: " + n + " collectibles placed (6 coins + 2 Fix Tools + 2 gems per level). " + wrist);
    }

    // ================= the three items (our own, from primitives) =================
    static GameObject Coin(Transform parent)
    {
        var g = new GameObject("Coin"); g.transform.SetParent(parent);
        var body = Part(g, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.24f, 0.012f, 0.24f), gold);
        body.transform.localRotation = Quaternion.Euler(90, 0, 0);           // stands up like a real coin
        Part(g, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.2f, 0.016f, 0.2f), gold).transform.localRotation = Quaternion.Euler(90, 0, 0);
        foreach (float side in new[] { -1f, 1f })                            // "C++" on both faces
        {
            var t = Label(g, "C++", new Vector3(0, 0, side * 0.018f), new Vector2(0.18f, 0.1f), new Color(0.45f, 0.28f, 0.02f), 1.2f);
            if (side > 0) t.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        return g;
    }

    static GameObject Gem(Transform parent)
    {
        var g = new GameObject("Gem"); g.transform.SetParent(parent);
        var p = new GameObject("Crystal"); p.transform.SetParent(g.transform, false);
        p.transform.localScale = Vector3.one * 0.2f;
        p.AddComponent<MeshFilter>().sharedMesh = gemMesh;
        p.AddComponent<MeshRenderer>().sharedMaterial = gem;
        return g;
    }

    static GameObject Tool(Transform parent)
    {
        var g = new GameObject("FixTool"); g.transform.SetParent(parent);
        var tilt = new GameObject("Screwdriver"); tilt.transform.SetParent(g.transform, false);
        tilt.transform.localRotation = Quaternion.Euler(0, 0, 35);           // leaning, easier to read as a tool
        Part(tilt, PrimitiveType.Cylinder, new Vector3(0, -0.07f, 0), new Vector3(0.06f, 0.07f, 0.06f), handle);
        Part(tilt, PrimitiveType.Sphere,   new Vector3(0, -0.14f, 0), new Vector3(0.06f, 0.03f, 0.06f), handle);
        Part(tilt, PrimitiveType.Cylinder, new Vector3(0, 0.07f, 0), new Vector3(0.016f, 0.08f, 0.016f), metal);
        Part(tilt, PrimitiveType.Cube,     new Vector3(0, 0.155f, 0), new Vector3(0.022f, 0.02f, 0.006f), metal);
        return g;
    }

    static void Grabbable(GameObject g, Inventory.Kind kind, int level)
    {
        var col = g.AddComponent<SphereCollider>();
        col.radius = kind == Inventory.Kind.Coin ? 0.16f : 0.18f;
        var rb = g.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        var grab = g.AddComponent<XRGrabInteractable>();                      // XRI ready-made component
        grab.throwOnDetach = false;
        var c = g.AddComponent<Collectible>();
        c.kind = kind; c.level = level;
        c.spinSpeed = kind == Inventory.Kind.Gem ? 60f : 90f;
    }

    // ================= wrist counter =================
    static string AddWristCounter()
    {
        Transform left = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Left Controller") { left = t; break; }
        if (left == null) return "Left Controller not found - wrist counter NOT added.";

        var oldC = left.Find("WristCounter");
        if (oldC != null) Object.DestroyImmediate(oldC.gameObject);

        var wc = new GameObject("WristCounter");
        wc.transform.SetParent(left, false);
        wc.transform.localPosition = new Vector3(0f, 0.05f, -0.08f);          // on top of the wrist
        wc.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);           // tilted towards the eyes
        wc.transform.localScale = Vector3.one * 0.55f;                        // small, like a watch (about 8 x 5 cm)

        var back = Part(wc, PrimitiveType.Cube, new Vector3(0, 0, 0.004f), new Vector3(0.14f, 0.09f, 0.004f), dark);
        Part(wc, PrimitiveType.Cube, new Vector3(0, 0.046f, 0.004f), new Vector3(0.14f, 0.004f, 0.006f), gold);   // gold top edge

        // icons view (level 1)
        var icons = new GameObject("Icons"); icons.transform.SetParent(wc.transform, false);
        var mc = Coin(icons.transform); mc.transform.localPosition = new Vector3(-0.045f, 0.013f, -0.01f); mc.transform.localScale = Vector3.one * 0.13f;
        var mg = Gem(icons.transform);  mg.transform.localPosition = new Vector3(0f, 0.013f, -0.01f);     mg.transform.localScale = Vector3.one * 0.14f;
        var mt = Tool(icons.transform); mt.transform.localPosition = new Vector3(0.045f, 0.015f, -0.01f); mt.transform.localScale = Vector3.one * 0.12f;
        foreach (var r in icons.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var coinN = Label(icons, "0", new Vector3(-0.045f, -0.022f, -0.002f), new Vector2(0.04f, 0.025f), Color.white, 0.25f);
        var gemN  = Label(icons, "0", new Vector3(0f, -0.022f, -0.002f),      new Vector2(0.04f, 0.025f), Color.white, 0.25f);
        var toolN = Label(icons, "0", new Vector3(0.045f, -0.022f, -0.002f),  new Vector2(0.04f, 0.025f), Color.white, 0.25f);

        // code view (after variables are learned)
        var code = Label(wc, "", new Vector3(0, 0, -0.002f), new Vector2(0.13f, 0.075f), Color.white, 0.2f);
        code.name = "CodeView";
        code.alignment = TextAlignmentOptions.Left;
        code.gameObject.SetActive(false);

        // the short "+1 coin" message appears low in front of the eyes for 1.5 s (easy to see, not in the way)
        TextMeshPro pop = null;
        Transform cam = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<Camera>(true))
                if (c.CompareTag("MainCamera")) { cam = c.transform; break; }
        if (cam != null)
        {
            var oldT = cam.Find("PickupToast");
            if (oldT != null) Object.DestroyImmediate(oldT.gameObject);
            pop = Label(cam.gameObject, "", new Vector3(0, -0.28f, 1.2f), new Vector2(0.6f, 0.12f), new Color(1f, 0.85f, 0.3f), 1.2f);
            pop.transform.localRotation = Quaternion.Euler(12f, 0, 0);      // a little tilted, like reading a book
            pop.name = "PickupToast";
        }

        var counter = wc.AddComponent<WristCounter>();
        counter.iconsGroup = icons; counter.coinText = coinN; counter.gemText = gemN; counter.toolText = toolN;
        counter.codeText = code; counter.popText = pop;
        return "Wrist counter added to " + left.name + ".";
    }

    // ================= helpers =================
    static GameObject Part(GameObject parent, PrimitiveType type, Vector3 local, Vector3 scale, Material m)
    {
        var p = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(parent.transform, false);
        p.transform.localPosition = local;
        p.transform.localScale = scale;
        p.GetComponent<Renderer>().sharedMaterial = m;
        return p;
    }

    static TextMeshPro Label(GameObject parent, string text, Vector3 local, Vector2 box, Color c, float maxSize)
    {
        var t = new GameObject("Text"); t.transform.SetParent(parent.transform, false);
        t.transform.localPosition = local;
        var tmp = t.AddComponent<TextMeshPro>();
        tmp.text = text; tmp.color = c; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = box;
        tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.05f; tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    // a simple cut diamond: flat top, 8-sided crown, point at the bottom (our own mesh)
    static Mesh GemMesh()
    {
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (m != null) return m;
        if (!AssetDatabase.IsValidFolder("Assets/Models/Collectibles")) AssetDatabase.CreateFolder("Assets/Models", "Collectibles");
        const int N = 8;
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        Vector3 top = new Vector3(0, 0.45f, 0), bottom = new Vector3(0, -0.6f, 0);
        for (int i = 0; i < N; i++)
        {
            float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * 0.3f, 0.45f, Mathf.Sin(a0) * 0.3f), t1 = new Vector3(Mathf.Cos(a1) * 0.3f, 0.45f, Mathf.Sin(a1) * 0.3f);
            Vector3 g0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0.15f, Mathf.Sin(a0) * 0.5f), g1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0.15f, Mathf.Sin(a1) * 0.5f);
            Tri(verts, tris, top, t1, t0);       // table
            Tri(verts, tris, t0, t1, g1);        // crown
            Tri(verts, tris, t0, g1, g0);
            Tri(verts, tris, g0, g1, bottom);    // pavilion
        }
        m = new Mesh { name = "Gem" };
        m.SetVertices(verts); m.SetTriangles(tris, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, MeshPath);
        return m;
    }

    static void Tri(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count; v.Add(a); v.Add(b); v.Add(c);    // separate vertices = sharp facets
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    static void Materials()
    {
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials", "Collectibles");
        gold   = M("Coin_Gold",    new Color(1f, 0.76f, 0.2f),  0.9f, 0.75f, new Color(1f, 0.7f, 0.15f) * 1.3f);
        gem    = M("Gem_Cyan",     new Color(0.3f, 0.95f, 1f),  0.1f, 0.95f, new Color(0.2f, 0.85f, 1f) * 2f);
        handle = M("Tool_Handle",  new Color(1f, 0.45f, 0.1f),  0f,   0.6f,  new Color(1f, 0.4f, 0.05f) * 1.2f);
        metal  = M("Tool_Metal",   new Color(0.8f, 0.82f, 0.85f), 1f, 0.8f, null);
        dark   = M("Wrist_Panel",  new Color(0.04f, 0.07f, 0.1f), 0f, 0.3f, null);
    }

    static Material M(string name, Color c, float metal, float smooth, Color? emission)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
        if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); }
        EditorUtility.SetDirty(m);
        return m;
    }
}
