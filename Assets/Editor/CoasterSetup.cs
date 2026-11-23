using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Roller Coaster
// "Roller Coaster" by gaartt (CC BY 4.0, see CREDITS.md). We split its 727 small bulbs
// into 10 groups (Bulbs_0 ... Bulbs_9) so the for loop in level 4 can light them
// one group at a time: for (int i = 0; i < 10; i++) lightOn(i);
public static class CoasterSetup
{
    const string Dir = "Assets/Models/RollerCoaster_gaartt/";

    [MenuItem("CS732/Setup Roller Coaster")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Rides")) AssetDatabase.CreateFolder("Assets/Materials", "Rides");
        var mi = (ModelImporter)AssetImporter.GetAtPath(Dir + "RollerCoaster.obj");
        if (mi == null) { Debug.LogWarning("Roller coaster model not found."); return; }
        Remap(mi, "trilho_da_lateral", Mat("RC_SideRail",  "trilho_da_lateral"));
        Remap(mi, "trilho_do_meio",    Mat("RC_MiddleRail","trilho_do_meio"));
        Remap(mi, "trilhos",           Mat("RC_Rails",     "trilhos"));
        Remap(mi, "estrutura",         Mat("RC_Structure", "estrutura"));
        Remap(mi, "carrinho",          Mat("RC_Cart",      "carrinho"));
        Remap(mi, "bolhinhas", AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulbOff_Mat.mat"));   // bulbs start off
        mi.SaveAndReimport();

        GameObject area = GameObject.Find("Area4_LightTower");
        if (area == null) { Debug.LogWarning("Area4_LightTower not found."); return; }
        area.name = "Area4_RollerCoaster";
        foreach (Transform c in area.transform) c.gameObject.SetActive(false);   // hide our simple tower

        Transform old = area.transform.Find("RollerCoasterModel");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "RollerCoaster.obj");
        var g = (GameObject)PrefabUtility.InstantiatePrefab(asset, area.transform);
        g.name = "RollerCoasterModel";
        g.SetActive(true);
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.identity;

        // real size: about 19 m long, standing on the ground, a little behind station 4
        Bounds b = B(g);
        float len = Mathf.Max(b.size.x, b.size.z);
        if (len > 0.001f) g.transform.localScale = Vector3.one * (19f / len);
        b = B(g);
        Vector3 target = area.transform.position + new Vector3(0, 0, 2.5f);
        g.transform.position += new Vector3(target.x - b.center.x, -b.min.y, target.z - b.center.z);

        // our script that drives the cart around the track
        if (g.GetComponent<RollerCoasterRide>() == null) g.AddComponent<RollerCoasterRide>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: roller coaster placed (level 4). Bulb groups: Bulbs_0 ... Bulbs_9.");
    }

    static Bounds B(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Remap(ModelImporter mi, string name, Material m)
    {
        if (m != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), m);
    }

    static Material Mat(string name, string tex)
    {
        string path = "Assets/Materials/Rides/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + tex + "_baseColor.png"));
        m.SetColor("_BaseColor", Color.white);
        string ms = Dir + tex + "_MetallicSmoothness.png";
        var ti = (TextureImporter)AssetImporter.GetAtPath(ms);
        if (ti != null)
        {
            if (ti.sRGBTexture) { ti.sRGBTexture = false; ti.SaveAndReimport(); }
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ms));
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", 1f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
