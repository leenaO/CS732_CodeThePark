using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us (team).
// Menu: CS732 > Place Lamps And Benches
// Uses two CC BY models (see CREDITS.md): "Low-Poly Lamp Post" by Memorie
// and "Bench_ Park" by adventurer (converted to OBJ by us, full detail).
public static class PropPlacer
{
    const string LampDir  = "Assets/Models/StreetLamp_Memorie/";
    const string BenchDir = "Assets/Models/Bench_adventurer/";

    [MenuItem("CS732/Place Lamps And Benches")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Props")) AssetDatabase.CreateFolder("Assets/Materials", "Props");

        Material lampMetal = Mat("Prop_Lamp", LampDir + "Lamp_baseColor.png", LampDir + "Lamp_normal.png", LampDir + "Lamp_MetallicSmoothness.png", LampDir + "Lamp_emissive.png", new Color(1f, 0.8f, 0.5f) * 1.2f);   // softer (was 3)
        Material bench     = Mat("Prop_Bench",     BenchDir + "Scene_-_Root_baseColor.png", BenchDir + "Scene_-_Root_normal.png", BenchDir + "Scene_-_Root_MetallicSmoothness.png", null, Color.black);

        Remap(LampDir + "LowPoly_LampPost.obj", ("Scene_-_Root", lampMetal));
        Remap(BenchDir + "Bench_Park.obj", ("Scene_-_Root", bench));

        GameObject env = GameObject.Find("Environment");
        if (env == null) { Debug.LogWarning("Run CS732 > Build Park Scene first."); return; }
        Transform oldLamps = env.transform.Find("LampPosts");
        if (oldLamps != null) oldLamps.gameObject.SetActive(false);    // our simple lamps, kept but hidden
        Transform old = env.transform.Find("Props");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject props = new GameObject("Props");
        props.transform.SetParent(env.transform);

        var lamp = AssetDatabase.LoadAssetAtPath<GameObject>(LampDir + "LowPoly_LampPost.obj");
        var seat = AssetDatabase.LoadAssetAtPath<GameObject>(BenchDir + "Bench_Park.obj");

        // 8 lamps along the main path (the model is about 3.8 m tall, its arm points over the path)
        for (int i = 0; i < 4; i++)
        {
            float z = -11f + i * 6f;
            Put(props, lamp, "StreetLamp", new Vector3(-2.4f, 0, z), 0);
            Put(props, lamp, "StreetLamp", new Vector3( 2.4f, 0, z), 0);
        }
        // 4 benches around the plaza, facing the centre
        Put(props, seat, "Bench", new Vector3(-4.6f, 0, 2.2f),  90);
        Put(props, seat, "Bench", new Vector3(-4.6f, 0, 5.8f),  90);
        Put(props, seat, "Bench", new Vector3( 4.6f, 0, 2.2f), -90);
        Put(props, seat, "Bench", new Vector3( 4.6f, 0, 5.8f), -90);

        foreach (Transform t in props.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: 8 street lamps and 4 benches placed.");
    }

    static void Put(GameObject parent, GameObject asset, string name, Vector3 pos, float yaw)
    {
        if (asset == null) return;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.transform);
        g.name = name;
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        // make sure the real size is right (lamp 3.6 m, bench 0.85 m), standing on the ground
        float targetH = name == "StreetLamp" ? 3.8f : 0.85f;
        var b = GetB(g);
        if (b.size.y > 0.0001f) g.transform.localScale = Vector3.one * (targetH / b.size.y);
        b = GetB(g);
        g.transform.position += new Vector3(0, pos.y - b.min.y, 0);
        b = GetB(g);
        // the lamp's arm must hang over the path (towards x = 0), not over the grass
        if (name == "StreetLamp" && Mathf.Abs(pos.x) > 0.1f && Mathf.Sign(b.center.x - pos.x) == Mathf.Sign(pos.x))
        {
            g.transform.Rotate(0, 180, 0, Space.World);
            b = GetB(g);
        }
        // a simple collider so the player cannot walk through it
        var box = g.AddComponent<BoxCollider>();
        box.center = g.transform.InverseTransformPoint(b.center);
        box.size = new Vector3(Mathf.Min(b.size.x, 1.6f), b.size.y, Mathf.Min(b.size.z, 1.6f));
        if (name == "StreetLamp") { box.size = new Vector3(0.4f, b.size.y, 0.4f); box.center = new Vector3(0, box.center.y, 0); }
    }

    static Bounds GetB(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Remap(string path, params (string, Material)[] pairs)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(path);
        if (mi == null) { Debug.LogWarning("Not found: " + path); return; }
        foreach (var (src, m) in pairs) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src), m);
        mi.SaveAndReimport();
    }

    static Material Mat(string name, string baseMap, string normal, string metalSmooth, string emission, Color emissionColor)
    {
        string path = "Assets/Materials/Props/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMap));
        m.SetColor("_BaseColor", Color.white);
        if (normal != null)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(normal);
            if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.EnableKeyword("_NORMALMAP");
        }
        if (metalSmooth != null)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(metalSmooth);
            if (ti != null && ti.sRGBTexture) { ti.sRGBTexture = false; ti.SaveAndReimport(); }
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metalSmooth));
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", name == "Prop_Lamp" ? 0.35f : 1f);   // lamp pole: less shiny (it reflected the light too much)
        }
        if (emission != null)
        {
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emission));
            m.SetColor("_EmissionColor", emissionColor);   // the street lamps are always on (comfort: the park is not pitch black)
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        m.SetFloat("_Cull", 0f);   // draw both sides: the simplified model has some thin open parts
        EditorUtility.SetDirty(m);
        return m;
    }
}
