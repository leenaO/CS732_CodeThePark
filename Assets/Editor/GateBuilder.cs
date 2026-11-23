using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us (our own design, built from Unity primitives).
// Menu: CS732 > Build Park Entrance
// Replaces the simple gate with a classic amusement-park entrance:
// three green arches, ticket booths, two towers with domes, a half "sun wheel"
// and the game name "CODE THE PARK" in coloured glowing letter tiles.
// The object names GateDoors (with hinges), GateSignText and GateBulbs are kept for the game code.
public static class GateBuilder
{
    const string Dir = "Assets/Materials/Gate";
    static Material teal, cream, roof, dome, iron, sun, bulbOn, lantern;
    static readonly Color[] TileColors = {
        new Color(0.95f, 0.15f, 0.15f), new Color(1f, 0.5f, 0.05f), new Color(1f, 0.82f, 0.05f),
        new Color(0.2f, 0.8f, 0.25f),   new Color(0.15f, 0.45f, 1f), new Color(0.6f, 0.25f, 0.95f) };

    [MenuItem("CS732/Build Park Entrance")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        Materials();
        var old = GameObject.Find("Area1_Gate");
        Vector3 pos = old != null ? old.transform.position : new Vector3(0, 0, -8);
        if (old != null) Object.DestroyImmediate(old);

        var gate = new GameObject("Area1_Gate");
        gate.transform.position = pos;

        // ---- three arches on green columns ----
        foreach (float x in new[] { -5.4f, -2.2f, 2.2f, 5.4f })
        {
            P(gate, "Column", new Vector3(x, 1.5f, 0), new Vector3(0.4f, 1.5f, 0.4f), teal, PrimitiveType.Cylinder, true);
            P(gate, "ColumnBase", new Vector3(x, 0.15f, 0), new Vector3(0.6f, 0.3f, 0.6f), cream);
        }
        P(gate, "Beam", new Vector3(0, 3.2f, 0), new Vector3(11.6f, 0.45f, 0.6f), teal);
        P(gate, "BeamTrim", new Vector3(0, 3.46f, 0), new Vector3(11.8f, 0.08f, 0.66f), cream);
        Arc(gate, "Arch_Middle", 0f, 3.5f, 2.2f, 16, cream);
        Arc(gate, "Arch_Left", -3.8f, 3.5f, 1.6f, 12, cream);
        Arc(gate, "Arch_Right", 3.8f, 3.5f, 1.6f, 12, cream);
        // subtitle on the beam (the game can change this text)
        Text(gate, "GateSignText", new Vector3(0, 3.2f, -0.32f), "LEARN  C++  IN  VR", new Vector2(6f, 0.4f), Color.white, 3.2f);

        // low railings in the side arches, so the visitor walks through the middle
        foreach (float x in new[] { -3.8f, 3.8f })
        {
            P(gate, "Rail", new Vector3(x, 1.0f, 0), new Vector3(3.0f, 0.08f, 0.08f), iron, PrimitiveType.Cube, true);
            for (int i = 0; i < 7; i++)
                P(gate, "RailBar", new Vector3(x - 1.35f + i * 0.45f, 0.5f, 0), new Vector3(0.05f, 0.5f, 0.05f), iron, PrimitiveType.Cylinder);
        }

        // ---- doors (each door turns on its hinge when the gate opens) ----
        var doors = Child(gate, "GateDoors", Vector3.zero);
        Door(doors, "GateDoor_L", -2.2f, 1f);
        Door(doors, "GateDoor_R", 2.2f, -1f);

        // ---- ticket booths and towers on both sides ----
        foreach (int s in new[] { -1, 1 })
        {
            string side = s < 0 ? "L" : "R";
            var booth = Child(gate, "TicketBooth_" + side, new Vector3(7.6f * s, 0, 0.6f));
            P(booth, "Body", new Vector3(0, 1.4f, 0), new Vector3(4f, 2.8f, 2.6f), cream, PrimitiveType.Cube, true);
            P(booth, "Window", new Vector3(0, 1.5f, -1.31f), new Vector3(1.6f, 1f, 0.04f), iron);
            P(booth, "WindowFrame", new Vector3(0, 0.95f, -1.35f), new Vector3(1.9f, 0.1f, 0.15f), teal);
            Text(booth, "Label", new Vector3(0, 2.35f, -1.33f), "TICKETS", new Vector2(2.5f, 0.45f), new Color(1f, 0.78f, 0.15f), 3.5f);   // gold, like the sun wheel
            var r = P(booth, "Roof", new Vector3(0, 3.05f, -0.1f), new Vector3(4.4f, 0.15f, 3.2f), roof);
            r.transform.localRotation = Quaternion.Euler(-12f, 0, 0);

            var tower = Child(gate, "Tower_" + side, new Vector3(10.8f * s, 0, 0.6f));
            P(tower, "Base", new Vector3(0, 1.7f, 0), new Vector3(2.4f, 3.4f, 2.4f), cream, PrimitiveType.Cube, true);
            P(tower, "Floor", new Vector3(0, 3.46f, 0), new Vector3(2.7f, 0.12f, 2.7f), roof);
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 1) })
                P(tower, "Post", new Vector3(c.x * 1.1f, 4.25f, c.y * 1.1f), new Vector3(0.14f, 0.8f, 0.14f), teal, PrimitiveType.Cylinder);
            P(tower, "Lantern", new Vector3(0, 4.28f, 0), new Vector3(1.8f, 1.55f, 1.8f), lantern);   // glowing room under the dome
            P(tower, "Ledge", new Vector3(0, 5.1f, 0), new Vector3(2.8f, 0.2f, 2.8f), teal);
            P(tower, "Dome", new Vector3(0, 5.2f, 0), new Vector3(2.2f, 2.2f, 2.2f), dome, PrimitiveType.Sphere);
            P(tower, "Tip", new Vector3(0, 6.45f, 0), new Vector3(0.3f, 0.3f, 0.3f), sun, PrimitiveType.Sphere);
            var tb = Child(tower, "GateBulbs_Tower", Vector3.zero);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Bulb(tb, new Vector3(Mathf.Cos(a) * 1.45f, 5.0f, Mathf.Sin(a) * 1.45f), 0.14f);
            }
        }

        // ---- half sun wheel behind the name ----
        var wheel = Child(gate, "SunWheel", new Vector3(0, 4.4f, 0.5f));
        Arc(wheel, "Rim", 0, 0, 4.4f, 22, sun, 0.22f);
        Arc(wheel, "Hub", 0, 0, 1.3f, 10, sun, 0.18f);
        for (int i = 0; i <= 8; i++)
        {
            float a = i * Mathf.PI / 8f;
            var sp = P(wheel, "Spoke", new Vector3(Mathf.Cos(a) * 2.85f, Mathf.Sin(a) * 2.85f, 0), new Vector3(3.1f, 0.12f, 0.12f), sun);
            sp.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
        }

        // ---- the game name in glowing neon letters (each letter its own colour) ----
        var sign = Child(gate, "GateSign", new Vector3(0, 6.3f, -0.35f));
        P(sign, "Panel", new Vector3(0, 0, 0.05f), new Vector3(10.0f, 1.15f, 0.12f), iron);
        P(sign, "PanelFrame", new Vector3(0, 0, 0.1f), new Vector3(10.3f, 1.35f, 0.08f), cream);
        string name = "CODE THE PARK";
        float step = 0.74f, gap = 0.4f, width = 0;
        foreach (char ch in name) width += ch == ' ' ? gap : step;
        float xx = -width / 2f;
        int k = 0;
        foreach (char ch in name)
        {
            if (ch == ' ') { xx += gap; continue; }
            var letter = Text(sign, "Neon_" + ch, new Vector3(xx + step / 2f, 0, -0.05f), ch.ToString(),
                              new Vector2(0.8f, 1.0f), TileColors[k % TileColors.Length], 9f);
            letter.fontSharedMaterial = NeonMat(letter, k % TileColors.Length);
            xx += step; k++;
        }

        // ---- light bulbs ----
        var bulbs = Child(gate, "GateBulbs", Vector3.zero);
        for (int i = 0; i <= 14; i++)                       // under the beam
            Bulb(bulbs, new Vector3(-5.6f + i * 0.8f, 2.92f, -0.32f), 0.13f);
        ArcBulbs(bulbs, 0, 3.5f, 2.2f, 0.2f, 13);            // middle arch
        ArcBulbs(bulbs, -3.8f, 3.5f, 1.6f, 0.2f, 9);
        ArcBulbs(bulbs, 3.8f, 3.5f, 1.6f, 0.2f, 9);
        ArcBulbs(bulbs, 0, 4.4f, 4.4f, 0.32f, 19);           // sun wheel rim

        StyleStations();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = gate;
        Debug.Log("CS732: new park entrance built.");
    }

    // Station 1 stood in the middle of the path, right in front of the doors, and hid the gate.
    // Move it to the left side of the path, turned towards the visitor; Codey floats beside it.
    [MenuItem("CS732/Move Gate Station Aside")]
    public static void MoveGateStation()
    {
        var st = GameObject.Find("Station_1_Gate");
        if (st != null)
        {
            st.transform.position = new Vector3(-3.3f, 0f, -9.8f);
            st.transform.rotation = Quaternion.Euler(0f, -30f, 0f);   // screen faces the path
        }
        var codey = GameObject.Find("Codey");
        if (codey != null)
        {
            codey.transform.position = new Vector3(-4.7f, 1.25f, -9.4f);
            var rt = codey.GetComponent<RobotTeacher>();
            if (rt != null) rt.FlyTo(new Vector3(-4.7f, 0f, -9.4f));
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: gate station moved to the side of the path (Codey next to it).");
    }

    // The 4 code stations get the same park colours as the gate:
    // green-teal stand, cream frame around the screen, gold top trim, cream base.
    // (Green / red are kept only for "correct" / "wrong" on the screens.)
    static void StyleStations()
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            if (t == null || !t.name.StartsWith("Station_")) continue;
            var st = t.gameObject;
            var stand = t.Find("Stand");
            if (stand != null) stand.GetComponent<Renderer>().sharedMaterial = teal;
            var oldFrame = t.Find("BoardFrame");
            if (oldFrame != null) Object.DestroyImmediate(oldFrame.gameObject);
            var oldBase = t.Find("StandBase");
            if (oldBase != null) Object.DestroyImmediate(oldBase.gameObject);

            var frame = Child(st, "BoardFrame", new Vector3(0, 1.45f, 0));
            frame.transform.localRotation = Quaternion.Euler(-12, 0, 0);       // same tilt as the screen
            P(frame, "Frame", new Vector3(0, 0, 0.045f), new Vector3(1.96f, 1.16f, 0.04f), cream);
            P(frame, "TopTrim", new Vector3(0, 0.6f, 0), new Vector3(2.02f, 0.07f, 0.1f), sun);
            P(st, "StandBase", new Vector3(0, 0.03f, 0), new Vector3(0.6f, 0.03f, 0.6f), cream, PrimitiveType.Cylinder);
        }
    }

    // ================= helpers =================
    static void Door(GameObject parent, string name, float hingeX, float dir)
    {
        var hinge = Child(parent, name, new Vector3(hingeX, 0, 0));      // pivot = hinge, so it can swing open
        P(hinge, "Top", new Vector3(dir * 1.1f, 2.75f, 0), new Vector3(2.2f, 0.1f, 0.08f), iron);
        P(hinge, "Mid", new Vector3(dir * 1.1f, 1.4f, 0), new Vector3(2.2f, 0.08f, 0.08f), iron);
        P(hinge, "Bottom", new Vector3(dir * 1.1f, 0.15f, 0), new Vector3(2.2f, 0.1f, 0.08f), iron);
        for (int i = 0; i < 8; i++)
            P(hinge, "Bar", new Vector3(dir * (0.12f + i * 0.28f), 1.45f, 0), new Vector3(0.05f, 1.3f, 0.05f), iron, PrimitiveType.Cylinder);
        P(hinge, "Badge", new Vector3(dir * 1.1f, 1.4f, -0.05f), new Vector3(0.45f, 0.45f, 0.06f), sun, PrimitiveType.Cylinder)
            .transform.localRotation = Quaternion.Euler(90, 0, 0);
        var col = hinge.AddComponent<BoxCollider>();                     // blocks walking until it opens
        col.center = new Vector3(dir * 1.1f, 1.4f, 0);
        col.size = new Vector3(2.2f, 2.8f, 0.15f);
    }

    static void Arc(GameObject parent, string name, float cx, float cy, float r, int n, Material m, float thick = 0.25f)
    {
        var arc = Child(parent, name, Vector3.zero);
        float d = Mathf.PI / n, len = 2f * r * Mathf.Sin(d / 2f) * 1.08f;
        for (int i = 0; i < n; i++)
        {
            float a = d * (i + 0.5f);
            var s = P(arc, "Seg", new Vector3(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, 0), new Vector3(len, thick, 0.4f), m);
            s.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f);
        }
    }

    static void ArcBulbs(GameObject parent, float cx, float cy, float r, float z, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.PI * i / (n - 1);
            Bulb(parent, new Vector3(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, -z), 0.14f);
        }
    }

    static void Bulb(GameObject parent, Vector3 p, float size)
    {
        P(parent, "GateBulb", p, Vector3.one * size, bulbOn, PrimitiveType.Sphere);
    }

    static GameObject Child(GameObject parent, string name, Vector3 local)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local;
        return g;
    }

    static GameObject P(GameObject parent, string name, Vector3 local, Vector3 scale, Material m,
                        PrimitiveType type = PrimitiveType.Cube, bool keepCollider = false)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        if (!keepCollider) Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
        return g;
    }

    static TextMeshPro Text(GameObject parent, string name, Vector3 local, string text, Vector2 box, Color c, float size)
    {
        var t = new GameObject(name);
        t.transform.SetParent(parent.transform, false);
        t.transform.localPosition = local;
        var tmp = t.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = c;
        tmp.rectTransform.sizeDelta = box;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.5f;
        tmp.fontSizeMax = size;
        return tmp;
    }

    // ================= materials =================
    static void Materials()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Materials", "Gate");
        teal  = M("Gate_Teal",  new Color(0.07f, 0.45f, 0.38f), 0.1f, 0.5f);
        cream = M("Gate_Cream", new Color(0.93f, 0.87f, 0.72f), 0f, 0.3f);
        roof  = M("Gate_Roof",  new Color(0.78f, 0.38f, 0.18f), 0f, 0.35f);
        dome  = M("Gate_Dome",  new Color(0.25f, 0.55f, 0.48f), 0.4f, 0.6f);
        iron  = M("Gate_Iron",  new Color(0.08f, 0.12f, 0.12f), 0.7f, 0.5f);
        sun   = M("Gate_Sun",   new Color(1f, 0.8f, 0.1f), 0.2f, 0.5f, new Color(1f, 0.72f, 0.1f) * 2f);
        lantern = M("Gate_Lantern", new Color(1f, 0.85f, 0.55f), 0f, 0.4f, new Color(1f, 0.72f, 0.3f) * 1.2f);
        bulbOn = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulbOn_Yellow_Mat.mat");
    }

    // a copy of the font material with TextMeshPro's Glow turned on, in the letter's colour
    static Material NeonMat(TextMeshPro tmp, int i)
    {
        string path = Dir + "/Gate_Neon_" + i + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(tmp.font.material);
            AssetDatabase.CreateAsset(m, path);
        }
        m.EnableKeyword("GLOW_ON");
        m.SetColor("_GlowColor", TileColors[i] * 1.5f);
        m.SetFloat("_GlowOffset", 0f);
        m.SetFloat("_GlowInner", 0.1f);
        m.SetFloat("_GlowOuter", 0.6f);
        m.SetFloat("_GlowPower", 1f);
        m.SetFloat("_FaceDilate", 0.15f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material TileMat(int i)
    {
        Color c = TileColors[i % TileColors.Length];
        return M("Gate_Tile_" + (i % TileColors.Length), c, 0f, 0.6f, c * 1.6f);
    }

    static Material M(string name, Color c, float metal, float smooth, Color? emission = null)
    {
        string path = Dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Smoothness", smooth);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission.Value);
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
