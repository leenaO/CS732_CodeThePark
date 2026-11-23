using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Arrange Park (fence, ride barriers, stations)
//  - an iron fence around the park (it joins the gate towers), so the park has a real border,
//  - a low railing around each ride, so the player cannot walk through the rides,
//  - stations moved: the screen is beside the ride (ride on the right), so the player sees the changes,
//  - coins that ended up inside a railing are moved just outside it.
// Menu: CS732 > Update Park (do everything)  - runs all the setup tools in the right order.
public static class ParkArranger
{
    static Material iron, teal, gold;

    [MenuItem("CS732/Update Park (do everything)")]
    public static void All()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first."); return; }
        WheelLights.Run();
        RideLifeSetup.Run();
        CollectiblesSetup.Run();          // coins, gems, Fix Tools (then Run() moves any that sit inside a railing)
        Run();
        AudioSetup.Run();
        GameFlowSetup.Run();
        CelebrationSetup.Run();
        AISetup.Run();
        StartPanelSetup.Run();
        LightPreview.Off();
        Debug.Log("CS732: park fully updated. Press Play (or Build And Run).");
    }

    [MenuItem("CS732/Arrange Park (fence, ride barriers, stations)")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Iron.mat");
        teal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Teal.mat");
        gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate/Gate_Sun.mat");

        // ---------- stations: beside each ride, the ride on the player's right ----------
        // station 1 further from the gate: the player sees the sign and the doors without looking up too much
        Move("Station_1", new Vector3(-7.5f, 0, -16f), 0);     // straight, facing the start; the gate is seen on its right
        foreach (var cs in Object.FindObjectsByType<CodeStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { cs.codeyOnRight = cs.name.StartsWith("Station_1"); EditorUtility.SetDirty(cs); }   // gate screen: Codey on the right
        var rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig != null) { rig.transform.position = new Vector3(0, 0, -20f); rig.transform.rotation = Quaternion.identity; }   // start behind it
        Move("Station_2", new Vector3(-17f, 0, 0f), 0);
        Move("Station_3", new Vector3(6.5f, 0, -1f), 0);
        Move("Station_4", new Vector3(-3f, 0, 10f), 0);

        var old = GameObject.Find("ParkFences"); if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("ParkFences");

        // ---------- outer fence: from the gate towers around the park ----------
        float x0 = 12.1f, X = 29f, zF = -7.4f, zB = 31f;
        Fence(root, new Vector3(-x0, 0, zF), new Vector3(-X, 0, zF), 2.2f, true);
        Fence(root, new Vector3(x0, 0, zF), new Vector3(X, 0, zF), 2.2f, true);
        Fence(root, new Vector3(-X, 0, zF), new Vector3(-X, 0, zB), 2.2f, true);
        Fence(root, new Vector3(X, 0, zF), new Vector3(X, 0, zB), 2.2f, true);
        Fence(root, new Vector3(-X, 0, zB), new Vector3(X, 0, zB), 2.2f, true);

        // ---------- low railing around each ride ----------
        var barriers = new List<(Vector3 c, Vector3 half, bool circle)>();
        string report = "";
        foreach (var (area, model, circle) in new[] { ("Area2_Carousel", "CarouselModel", true), ("Area3_FerrisWheel", "FerrisWheelModel", false), ("Area4_RollerCoaster", "RollerCoasterModel", false) })
        {
            var m = GameObject.Find(model);
            if (m == null) continue;
            var rs = m.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var g = new GameObject("Railing_" + area); g.transform.SetParent(root.transform);
            Vector3 c = new Vector3(b.center.x, 0, b.center.z);
            if (circle)
            {
                float R = Mathf.Max(b.extents.x, b.extents.z) + 0.8f;
                int n = 28;
                for (int i = 0; i < n; i++)
                {
                    float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                    Fence(g, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R, 1.0f, false);
                }
                barriers.Add((c, new Vector3(R, 0, R), true));
            }
            else
            {
                float hx = b.extents.x + 1f, hz = b.extents.z + 1f;
                Vector3 a = c + new Vector3(-hx, 0, -hz), bb = c + new Vector3(hx, 0, -hz), cc = c + new Vector3(hx, 0, hz), d = c + new Vector3(-hx, 0, hz);
                Fence(g, a, bb, 1.0f, false); Fence(g, bb, cc, 1.0f, false); Fence(g, cc, d, 1.0f, false); Fence(g, d, a, 1.0f, false);
                barriers.Add((c, new Vector3(hx, 0, hz), false));
            }
        }

        // ---------- no trees right in front of the roller coaster (they hid the train) ----------
        var nat = GameObject.Find("Nature");
        if (nat != null)
            foreach (var bar in barriers)
            {
                if (bar.circle) continue;
                var near = new List<GameObject>();
                foreach (Transform t in nat.transform)
                {
                    string n = t.name.ToLower();
                    if (!(n.Contains("tree") || n.Contains("pine"))) continue;
                    Vector3 d = t.position - bar.c;
                    if (Mathf.Abs(d.x) < bar.half.x + 3.5f && Mathf.Abs(d.z) < bar.half.z + 3.5f && bar.c.z > 16f) near.Add(t.gameObject);   // only the roller coaster (it is the one at the back of the park)
                }
                foreach (var g in near) { report += "tree " + g.name + " removed (in front of the coaster). "; Object.DestroyImmediate(g); }
            }

        // ---------- no rocks on the way to the gate (they stood in the middle of the entrance) ----------
        if (nat != null)
        {
            var rocks = new List<GameObject>();
            foreach (Transform t in nat.transform)
                if (t.name.ToLower().Contains("rock") && Mathf.Abs(t.position.x) < 9f && t.position.z < -7f) rocks.Add(t.gameObject);
            foreach (var g in rocks) { report += "rock " + g.name + " removed (entrance). "; Object.DestroyImmediate(g); }
        }

        // ---------- keep stations and coins outside the railings ----------
        foreach (var rootGo in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var st in rootGo.GetComponentsInChildren<CodeStation>(true))
                foreach (var bar in barriers) if (Inside(bar, st.transform.position, 0.6f)) report += st.name + " is inside a railing! ";
            foreach (var col in rootGo.GetComponentsInChildren<Collectible>(true))
                foreach (var bar in barriers)
                    if (Inside(bar, col.transform.position, 0.5f))
                    {
                        Vector3 dir = col.transform.position - bar.c; dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.back;
                        float out_ = bar.circle ? bar.half.x : Mathf.Max(bar.half.x, bar.half.z);
                        Vector3 p = bar.c + dir * (out_ + 1.0f); p.y = col.transform.position.y;
                        col.transform.position = p;
                        report += col.name + " (level " + col.level + ") moved out of a railing. ";
                    }
        }

        report += NatureColliders();
        foreach (var r in root.GetComponentsInChildren<Renderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: park arranged (fence, railings, stations). " + report);
    }

    // trees, bushes and rocks get a collider, so the player (and Codey) cannot walk through them
    static string NatureColliders()
    {
        var nature = GameObject.Find("Nature");
        if (nature == null) return "";
        int n = 0;
        foreach (Transform t in nature.transform)
        {
            string name = t.name.ToLower();
            bool tree = name.Contains("tree") || name.Contains("pine");
            bool bush = name.Contains("bush");
            bool rock = name.Contains("rock");
            if (!tree && !bush && !rock) continue;                      // grass and flowers: walk through them
            foreach (var old in t.GetComponents<Collider>()) Object.DestroyImmediate(old);
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var cap = t.gameObject.AddComponent<CapsuleCollider>();
            float s = Mathf.Max(0.001f, t.lossyScale.x);
            float radius = tree ? 0.45f : Mathf.Min(b.extents.x, b.extents.z) * 0.8f;   // a tree: only its trunk
            cap.radius = radius / s;
            cap.height = Mathf.Max(b.size.y, radius * 2f) / s;
            Vector3 mid = tree ? new Vector3(t.position.x, b.min.y + b.size.y / 2f, t.position.z)   // the trunk is at the tree's origin
                               : new Vector3(b.center.x, b.min.y + b.size.y / 2f, b.center.z);
            cap.center = t.InverseTransformPoint(mid);
            n++;
        }
        return n + " trees/bushes/rocks got colliders. ";
    }

    static bool Inside((Vector3 c, Vector3 half, bool circle) b, Vector3 p, float margin)
    {
        Vector3 d = p - b.c; d.y = 0;
        if (b.circle) return d.magnitude < b.half.x + margin;
        return Mathf.Abs(d.x) < b.half.x + margin && Mathf.Abs(d.z) < b.half.z + margin;
    }

    static void Move(string prefix, Vector3 pos, float yaw)
    {
        foreach (var rootGo in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in rootGo.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(prefix + "_") && t.GetComponent<CodeStation>() != null)
                { t.position = pos; t.rotation = Quaternion.Euler(0, yaw, 0); return; }
    }

    // one straight piece of fence: posts, rails and (tall fence) pickets; one collider for the whole piece
    static void Fence(GameObject parent, Vector3 a, Vector3 b, float height, bool tall)
    {
        var seg = new GameObject("Fence"); seg.transform.SetParent(parent.transform);
        Vector3 d = b - a; float len = d.magnitude;
        seg.transform.position = (a + b) * 0.5f;
        seg.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        float postGap = tall ? 2.5f : 1.6f;
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / postGap));
        for (int i = 0; i <= posts; i++)
        {
            float z = -len / 2 + len * i / posts;
            P(seg, PrimitiveType.Cylinder, new Vector3(0, height / 2, z), new Vector3(0.09f, height / 2, 0.09f), tall ? iron : teal);
            P(seg, PrimitiveType.Sphere, new Vector3(0, height + 0.04f, z), Vector3.one * 0.14f, gold);
        }
        foreach (float y in tall ? new[] { 0.25f, height - 0.25f } : new[] { 0.35f, height - 0.08f })
            P(seg, PrimitiveType.Cube, new Vector3(0, y, 0), new Vector3(0.05f, 0.05f, len), tall ? iron : teal);
        if (tall)
            for (float z = -len / 2 + 0.3f; z < len / 2; z += 0.3f)
                P(seg, PrimitiveType.Cube, new Vector3(0, height / 2, z), new Vector3(0.025f, height - 0.3f, 0.025f), iron);
        var col = seg.AddComponent<BoxCollider>();
        col.center = new Vector3(0, height / 2, 0); col.size = new Vector3(0.2f, height + 0.6f, len);
    }

    static void P(GameObject parent, PrimitiveType t, Vector3 local, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(t);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local; g.transform.localScale = scale;
        var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
