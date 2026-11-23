using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us (team).
// Menu: CS732 > Setup Ride Models
// 1) Makes URP materials for the two downloaded rides (CC BY models, see CREDITS.md)
//    and connects their textures. Textures are limited to 2048 px for Quest 3.
// 2) Replaces our simple carousel and Ferris wheel shapes with the real models.
public static class RideModelSetup
{
    const string CarDir = "Assets/Models/Carousel_Enkarra/";
    const string FwDir  = "Assets/Models/FerrisWheel_Lenda/";
    const string MatDir = "Assets/Materials/Rides/";

    [MenuItem("CS732/Setup Ride Models")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Rides"))
            AssetDatabase.CreateFolder("Assets/Materials", "Rides");

        FixTextures(CarDir + "Textures", null);
        FixTextures(FwDir + "Textures", "normal");

        // ---------- carousel materials (names come from the FBX file)
        Remap(CarDir + "Carousel_Enkarra.fbx", new[] {
            ("Ground",        Mat("Car_Ground",   CarDir + "Textures/new_text.png", null, null, false)),
            ("Grass",         Mat("Car_Grass",    CarDir + "Textures/Flowers_3.png", null, null, true)),
            ("horse_white",   Mat("Car_HorseWhite", CarDir + "Textures/Horse_texture.png", null, null, false)),
            ("Horse_black",   Mat("Car_HorseBlack", CarDir + "Textures/Horse_texture_black.png", null, null, false)),
            ("Tent",          Mat("Car_Tent",     CarDir + "Textures/Tent_Basecolor.png", null, CarDir + "Textures/Tent_emission.png", false)),
            ("surfaceShader3",Mat("Car_Palette",  CarDir + "Textures/Pallete.png", null, CarDir + "Textures/lixt_emissiv.png", false)),
        });

        // ---------- Ferris wheel materials
        Remap(FwDir + "FerrisWheel_Lenda.fbx", new[] {
            ("Bake_Atlas",        Mat("FW_Atlas",  FwDir + "Textures/Bake_Atlas_4K_diffuse_and_alpha.png", FwDir + "Textures/Bake_Atlas_4K_normal2.png", FwDir + "Textures/Bake_Atlas_4K_emiss.png", true)),
            ("Bake_Atlas_cabine", Mat("FW_Cabin",  FwDir + "Textures/Bake_cabine_atlas_color_alpha.png", FwDir + "Textures/Bake_cabine_atlas_normal8.png", null, true)),
        });

        // ---------- put the models in the scene
        Place("Area2_Carousel",   CarDir + "Carousel_Enkarra.fbx", "CarouselModel", 8.5f, false);
        Place("Area3_FerrisWheel", FwDir + "FerrisWheel_Lenda.fbx", "FerrisWheelModel", 13f, true);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: ride models set up (materials, textures 2048, placed in the park).");
    }

    static void FixTextures(string folder, string normalKeyword)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            bool changed = false;
            if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
            if (normalKeyword != null && p.ToLower().Contains(normalKeyword) && ti.textureType != TextureImporterType.NormalMap)
            { ti.textureType = TextureImporterType.NormalMap; changed = true; }
            if (changed) ti.SaveAndReimport();
        }
    }

    static Material Mat(string name, string baseMap, string normal, string emission, bool cutout)
    {
        string path = MatDir + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMap));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.3f);
        if (normal != null)
        {
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.EnableKeyword("_NORMALMAP");
        }
        if (emission != null)
        {
            // starts OFF (dark park); the game turns the glow on when the code is fixed
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emission));
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        if (cutout)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.4f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f);   // draw both sides (leaves, thin parts)
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Remap(string fbx, (string, Material)[] pairs)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        if (mi == null) { Debug.LogWarning("Model not found: " + fbx); return; }
        foreach (var (src, mat) in pairs)
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src), mat);
        mi.SaveAndReimport();
    }

    static void Place(string areaName, string fbx, string instName, float targetSize, bool useHeight)
    {
        GameObject area = GameObject.Find(areaName);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (area == null || asset == null) { Debug.LogWarning("Missing " + areaName + " or " + fbx); return; }

        // hide our simple shapes (kept in the scene, just turned off)
        foreach (Transform child in area.transform)
            if (child.name != instName) child.gameObject.SetActive(false);

        Transform old = area.transform.Find(instName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, area.transform);
        inst.name = instName;
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        // scale the model to a sensible real size
        Bounds b = WorldBounds(inst);
        float size = useHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
        if (size > 0.001f) inst.transform.localScale = Vector3.one * (targetSize / size);

        // stand it on the ground and centre it on the area
        b = WorldBounds(inst);
        Vector3 shift = new Vector3(area.transform.position.x - b.center.x, -b.min.y, area.transform.position.z - b.center.z);
        inst.transform.position += shift;

        var anim = inst.GetComponent<Animator>();
        if (anim != null) anim.enabled = false;   // the ride stays still until its code is fixed
    }

    static Bounds WorldBounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
