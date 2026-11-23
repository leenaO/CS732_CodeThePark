using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us (team).
// Menu: CS732 > Dress Park With Nature
// Uses the free Quaternius "Stylized Nature MegaKit" (CC0) and the Poly Haven
// grass texture (CC0): makes URP materials, replaces our ball trees with real trees,
// and adds bushes, flowers, grass clumps and rocks around the park.
public static class NatureDresser
{
    const string Kit = "Assets/Models/NatureKit_Quaternius/";
    const string Mats = "Assets/Materials/Nature/";

    [MenuItem("CS732/Dress Park With Nature")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Nature")) AssetDatabase.CreateFolder("Assets/Materials", "Nature");

        // ---------- materials (the kit's FBX files name these materials)
        Material bark   = Mat("Nature_Bark",   Kit + "Textures/Bark_NormalTree.png", Kit + "Textures/Bark_NormalTree_Normal.png", false, Color.white);
        Material leaves = Mat("Nature_Leaves", Kit + "Textures/Leaves.png", null, true, Color.white);
        Material treeLeaves = Mat("Nature_TreeLeaves", Kit + "Textures/Leaves_GiantPine_C.png", null, true, new Color(0.85f, 1f, 0.85f));
        Material pine   = Mat("Nature_Pine",   Kit + "Textures/Leaf_Pine_C.png", null, true, Color.white);
        Material flower = Mat("Nature_Flowers",Kit + "Textures/Flowers.png", null, true, Color.white);
        Material grass  = Mat("Nature_Grass",  Kit + "Textures/Grass.png", null, true, Color.white);
        Material rock   = Mat("Nature_Rock",   null, null, false, new Color(0.45f, 0.45f, 0.47f));
        Material path   = Mat("Nature_PathRock", null, null, false, new Color(0.62f, 0.56f, 0.5f));

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models/NatureKit_Quaternius" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var mi = (ModelImporter)AssetImporter.GetAtPath(p);
            Map(mi, "Bark_NormalTree", bark);
            Map(mi, "Leaves_NormalTree", treeLeaves);
            Map(mi, "Leaves_TwistedTree", treeLeaves);
            Map(mi, "Leaves", leaves);
            Map(mi, "Material", treeLeaves);
            Map(mi, "Leaves_Pine", pine);
            Map(mi, "Flowers", flower);
            Map(mi, "Grass", grass);
            Map(mi, "Rocks", rock);
            Map(mi, "PathRocks", path);
            mi.SaveAndReimport();
        }

        // ---------- real grass on the ground (Poly Haven, tiled every 3 m)
        Material ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grass_Mat.mat");
        var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Ground/Grass_Lawn_1k.jpg");
        if (ground != null && grassTex != null)
        {
            ground.SetTexture("_BaseMap", grassTex);
            ground.SetTextureScale("_BaseMap", new Vector2(20, 20));
            ground.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(ground);
        }

        // ---------- place nature in the scene
        GameObject env = GameObject.Find("Environment");
        if (env == null) { Debug.LogWarning("Run CS732 > Build Park Scene first."); return; }
        Transform oldTrees = env.transform.Find("Trees");
        if (oldTrees != null) oldTrees.gameObject.SetActive(false);   // our ball trees, kept but hidden
        Transform oldNature = env.transform.Find("Nature");
        if (oldNature != null) Object.DestroyImmediate(oldNature.gameObject);
        GameObject nature = new GameObject("Nature");
        nature.transform.SetParent(env.transform);

        Random.InitState(7322);
        string[] trees = { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5", "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" };

        // a ring of trees around the park
        for (int i = 0; i < 34; i++)
        {
            float a = i * Mathf.PI * 2f / 34f + Random.Range(-0.05f, 0.05f);
            float r = 20.5f + Random.Range(0f, 6f);
            Vector3 tp = new Vector3(Mathf.Cos(a) * r, 0, 2 + Mathf.Sin(a) * r);
            if (OnRide(tp)) continue;
            Put(nature, trees[Random.Range(0, trees.Length)], tp, Random.Range(6f, 9.5f));
        }

        // flowering bushes along the main path
        for (int i = 0; i < 6; i++)
        {
            float z = -8f + i * 4.2f;
            Put(nature, i % 2 == 0 ? "Bush_Common_Flowers" : "Bush_Common", new Vector3(-3.3f, 0, z), Random.Range(0.9f, 1.3f));
            Put(nature, i % 2 == 0 ? "Bush_Common" : "Bush_Common_Flowers", new Vector3( 3.3f, 0, z), Random.Range(0.9f, 1.3f));
        }

        // flower patches near the plaza and the rides
        Vector3[] patches = { new Vector3(-4.5f,0,1), new Vector3(4.5f,0,1), new Vector3(-4,0,8), new Vector3(4,0,8),
                              new Vector3(-13,0,3), new Vector3(-13,0,11), new Vector3(13,0,6), new Vector3(14,0,14),
                              new Vector3(-3,0,15), new Vector3(3,0,15), new Vector3(-6,0,-7), new Vector3(6,0,-7) };
        foreach (var c in patches)
            if (!OnRide(c))
            for (int k = 0; k < 3; k++)
                Put(nature, Random.value < 0.5f ? "Flower_3_Group" : "Flower_4_Group", c + new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f)), Random.Range(0.35f, 0.55f));

        // grass clumps, plants and clover scattered on the lawn (not on paths or rides)
        string[] small = { "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Grass_Wispy_Tall", "Clover_1", "Clover_2", "Fern_1", "Plant_1_Big", "Plant_7_Big" };
        int placed = 0, tries = 0;
        while (placed < 110 && tries < 2000)
        {
            tries++;
            Vector3 p = new Vector3(Random.Range(-19f, 19f), 0, Random.Range(-15f, 21f));
            if (Mathf.Abs(p.x) < 2.6f) continue;                                  // main path
            if (Mathf.Abs(p.z - 4f) < 2.2f && Mathf.Abs(p.x) < 10.5f) continue;   // cross path
            if (OnRide(p) || Near(p, new Vector3(0, 0, -8), 4.5f)) continue;  // rides and gate
            Put(nature, small[Random.Range(0, small.Length)], p, Random.Range(0.25f, 0.6f));
            placed++;
        }

        // a few rocks near the edges
        for (int i = 0; i < 10; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(17f, 20f);
            Put(nature, "Rock_Medium_" + Random.Range(1, 4), new Vector3(Mathf.Cos(a) * r, 0, 2 + Mathf.Sin(a) * r), Random.Range(0.5f, 1.1f));
        }

        // static objects can be drawn together (faster on Quest)
        foreach (Transform t in nature.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: park dressed with nature (" + nature.transform.childCount + " objects).");
    }

    // keep nature away from the rides (carousel, Ferris wheel, roller coaster)
    static bool OnRide(Vector3 p) =>
        Near(p, new Vector3(-12, 0, 8), 8.5f) || Near(p, new Vector3(11, 0, 11), 7.5f) ||
        (Mathf.Abs(p.x) < 13f && p.z > 14.5f && p.z < 28f);

    static bool Near(Vector3 a, Vector3 b, float d) => (new Vector2(a.x - b.x, a.z - b.z)).magnitude < d;

    // places a model, scaled so its height is "targetHeight" metres, standing on the ground
    static void Put(GameObject parent, string model, Vector3 pos, float targetHeight)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".fbx");
        if (asset == null) return;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.transform);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        Bounds b = GetBounds(g);
        if (b.size.y > 0.001f) g.transform.localScale = Vector3.one * (targetHeight / b.size.y);
        b = GetBounds(g);
        g.transform.position += new Vector3(0, pos.y - b.min.y, 0);
    }

    static Bounds GetBounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Map(ModelImporter mi, string name, Material m)
    {
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), m);
    }

    static Material Mat(string name, string tex, string normal, bool cutout, Color c)
    {
        string path = Mats + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        if (tex != null)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(tex);
            if (ti != null && ti.maxTextureSize > 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
        }
        if (normal != null)
        {
            var ni = (TextureImporter)AssetImporter.GetAtPath(normal);
            if (ni != null && ni.textureType != TextureImporterType.NormalMap) { ni.textureType = TextureImporterType.NormalMap; ni.maxTextureSize = 1024; ni.SaveAndReimport(); }
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.EnableKeyword("_NORMALMAP");
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.1f);
        if (cutout)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.45f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
