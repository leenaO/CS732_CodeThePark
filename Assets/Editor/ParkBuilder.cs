using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Code the Park - editor helper written by us (team).
// Menu: CS732 > Build Park Scene
// Builds the night park from simple shapes: ground, paths, gate, carousel,
// Ferris wheel, light tower, lamp posts, trees, invisible borders,
// night sky and lighting, the XR rig and the four code stations.
// All rides start "off" (dark bulbs). Game scripts turn them on later.
public static class ParkBuilder
{
    const string ScenePath = "Assets/Scenes/CodeThePark.unity";
    const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

    static Material grass, path, stone, wood, white, red, yellow, blue, purple, dark, bulbOff, leaves, trunk, screenMat;

    [MenuItem("CS732/Build Park Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeMaterials();
        NightLighting();

        // ---------- ground and borders
        GameObject env = new GameObject("Environment");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(env.transform);
        ground.transform.position = new Vector3(0, 0, 2);
        ground.transform.localScale = new Vector3(6, 1, 6);            // 60 x 60 m
        ground.GetComponent<Renderer>().sharedMaterial = grass;
        var area = ground.AddComponent<TeleportationArea>();
        area.interactionLayers = InteractionLayerMask.GetMask("Teleport");

        GameObject borders = Child("Borders", env);
        Wall(borders, new Vector3(0, 1.5f, 32), new Vector3(60, 3, 0.5f));
        Wall(borders, new Vector3(0, 1.5f, -28), new Vector3(60, 3, 0.5f));
        Wall(borders, new Vector3(30, 1.5f, 2), new Vector3(0.5f, 3, 60));
        Wall(borders, new Vector3(-30, 1.5f, 2), new Vector3(0.5f, 3, 60));

        // ---------- paths (no colliders, so the teleport ray lands on the ground)
        GameObject paths = Child("Paths", env);
        Flat(paths, "Path_Main",  new Vector3(0, 0.01f, 1),  new Vector3(3, 0.02f, 30), path);
        Flat(paths, "Path_Cross", new Vector3(0, 0.012f, 4), new Vector3(20, 0.02f, 3), path);
        Flat(paths, "Plaza",      new Vector3(0, 0.014f, 4), new Vector3(7, 0.02f, 7), stone);

        // ---------- lamp posts along the main path
        GameObject lamps = Child("LampPosts", env);
        for (int i = 0; i < 5; i++)
        {
            float z = -11 + i * 6f;
            LampPost(lamps, new Vector3(-2.3f, 0, z));
            LampPost(lamps, new Vector3( 2.3f, 0, z));
        }

        // ---------- trees around the park
        GameObject trees = Child("Trees", env);
        Random.InitState(732);
        for (int i = 0; i < 28; i++)
        {
            float a = i * Mathf.PI * 2f / 28f;
            float r = 21f + Random.Range(0f, 5f);
            Tree(trees, new Vector3(Mathf.Cos(a) * r, 0, 2 + Mathf.Sin(a) * r), Random.Range(0.8f, 1.4f));
        }

        // ---------- the four areas
        BuildGate();
        BuildCarousel(new Vector3(-9, 0, 7));
        BuildFerrisWheel(new Vector3(9, 0, 10));
        BuildLightTower(new Vector3(0, 0, 17));

        // ---------- code stations (the code screens are added in the next step)
        GameObject stations = new GameObject("CodeStations");
        Station(stations, "Station_1_Gate",       new Vector3(-7.5f, 0, -16f), 0);   // beside the path, not in front of the doors
        Station(stations, "Station_2_Carousel",   new Vector3(-5, 0, 4.5f), -60);
        Station(stations, "Station_3_FerrisWheel",new Vector3(5, 0, 6),     55);
        Station(stations, "Station_4_LightTower", new Vector3(0, 0, 12.5f), 0);

        // ---------- the player (XR rig from the XRI Starter Assets)
        var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (rigAsset != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset);
            rig.transform.position = new Vector3(0, 0, -13);
            rig.transform.rotation = Quaternion.identity;
        }
        else Debug.LogWarning("XR Origin (XR Rig) prefab not found at " + RigPath);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("CS732: Code the Park scene built and saved (" + ScenePath + ").");
    }

    // ================= areas =================
    static void BuildGate()
    {
        GameObject gate = new GameObject("Area1_Gate");
        gate.transform.position = new Vector3(0, 0, -8);
        Box(gate, "Pillar_L", new Vector3(-3.2f, 2.2f, 0), new Vector3(0.8f, 4.4f, 0.8f), red);
        Box(gate, "Pillar_R", new Vector3( 3.2f, 2.2f, 0), new Vector3(0.8f, 4.4f, 0.8f), red);
        Box(gate, "Arch",     new Vector3(0, 4.6f, 0),     new Vector3(7.6f, 0.6f, 0.8f), white);
        Box(gate, "SignBoard",new Vector3(0, 5.6f, 0),     new Vector3(5.5f, 1.4f, 0.2f), dark);
        Text3D(gate, "GateSignText", new Vector3(0, 5.6f, -0.12f), "", 1.6f, new Vector2(5.2f, 1.2f), Color.white);
        GameObject doors = Child("GateDoors", gate);
        Box(doors, "GateDoor_L", new Vector3(-1.4f, 1.5f, 0), new Vector3(2.8f, 3f, 0.15f), wood);
        Box(doors, "GateDoor_R", new Vector3( 1.4f, 1.5f, 0), new Vector3(2.8f, 3f, 0.15f), wood);
        GameObject bulbs = Child("GateBulbs", gate);
        for (int i = 0; i < 9; i++)
            Bulb(bulbs, new Vector3(-3.6f + i * 0.9f, 5.0f, -0.45f), 0.2f);
    }

    static void BuildCarousel(Vector3 pos)
    {
        GameObject root = new GameObject("Area2_Carousel");
        root.transform.position = pos;
        Box(root, "Base", new Vector3(0, 0.1f, 0), new Vector3(6.6f, 0.2f, 6.6f), stone, PrimitiveType.Cylinder);
        GameObject spin = Child("CarouselSpin", root);
        Box(spin, "Platform", new Vector3(0, 0.3f, 0), new Vector3(6f, 0.1f, 6f), wood, PrimitiveType.Cylinder);
        Box(spin, "CenterPole", new Vector3(0, 2.1f, 0), new Vector3(0.6f, 1.8f, 0.6f), yellow, PrimitiveType.Cylinder);
        Box(spin, "Roof_1", new Vector3(0, 3.95f, 0), new Vector3(6.6f, 0.15f, 6.6f), red, PrimitiveType.Cylinder);
        Box(spin, "Roof_2", new Vector3(0, 4.35f, 0), new Vector3(4.2f, 0.3f, 4.2f), white, PrimitiveType.Cylinder);
        Box(spin, "Roof_Top", new Vector3(0, 4.9f, 0), new Vector3(1.2f, 0.3f, 1.2f), red, PrimitiveType.Cylinder);
        GameObject horses = Child("Horses", spin);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 2.2f, 0, Mathf.Sin(a) * 2.2f);
            Box(spin, "HorsePole_" + (i + 1), p + new Vector3(0, 2.1f, 0), new Vector3(0.08f, 1.8f, 0.08f), yellow, PrimitiveType.Cylinder);
            GameObject h = Box(horses, "Horse_" + (i + 1), p + new Vector3(0, 1.3f, 0), new Vector3(0.35f, 0.45f, 0.9f), i % 2 == 0 ? white : purple);
            h.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            h.SetActive(false);   // challenge 6 (int horses = 6;) shows them
        }
        GameObject bulbs = Child("CarouselBulbs", spin);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            Bulb(bulbs, new Vector3(Mathf.Cos(a) * 3.3f, 3.85f, Mathf.Sin(a) * 3.3f), 0.22f);
        }
        Text3D(root, "CarouselNameSign", new Vector3(0, 5.7f, 0), "", 0.9f, new Vector2(5, 0.8f), new Color(1f, 0.85f, 0.3f), true);
    }

    static void BuildFerrisWheel(Vector3 pos)
    {
        GameObject root = new GameObject("Area3_FerrisWheel");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0, -30, 0);
        foreach (float z in new[] { -0.8f, 0.8f })
        {
            Leg(root, new Vector3(-1.6f, 3.2f, z), 14f);
            Leg(root, new Vector3( 1.6f, 3.2f, z), -14f);
        }
        GameObject hub = Box(root, "Hub", new Vector3(0, 6.5f, 0), new Vector3(0.7f, 1f, 0.7f), dark, PrimitiveType.Cylinder);
        hub.transform.localRotation = Quaternion.Euler(90, 0, 0);
        GameObject spin = Child("WheelSpin", root);
        spin.transform.localPosition = new Vector3(0, 6.5f, 0);
        const float R = 5f;
        for (int i = 0; i < 24; i++)
        {
            float a = i * 360f / 24f;
            GameObject seg = Box(spin, "Rim_" + i, Polar(R, a), new Vector3(0.18f, 1.35f, 0.18f), white);
            seg.transform.localRotation = Quaternion.Euler(0, 0, a);
        }
        GameObject cabins = Child("Cabins", spin);
        GameObject bulbs = Child("WheelBulbs", spin);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f;
            GameObject sp = Box(spin, "Spoke_" + i, Polar(R / 2f, a), new Vector3(0.1f, R, 0.1f), white);
            sp.transform.localRotation = Quaternion.Euler(0, 0, a - 90f);
            Box(cabins, "Cabin_" + (i + 1), Polar(R, a) + new Vector3(0, -0.7f, 0), new Vector3(0.9f, 0.9f, 0.9f), i % 2 == 0 ? blue : yellow);
        }
        for (int i = 0; i < 16; i++)
            Bulb(bulbs, Polar(R + 0.2f, i * 22.5f) + new Vector3(0, 0, -0.15f), 0.22f);
    }

    static void BuildLightTower(Vector3 pos)
    {
        GameObject root = new GameObject("Area4_LightTower");
        root.transform.position = pos;
        Box(root, "Base", new Vector3(0, 0.15f, 0), new Vector3(2f, 0.3f, 2f), stone, PrimitiveType.Cylinder);
        Box(root, "Column", new Vector3(0, 4.3f, 0), new Vector3(0.9f, 4f, 0.9f), dark, PrimitiveType.Cylinder);
        GameObject lights = Child("TowerLights", root);
        for (int i = 0; i < 10; i++)
            Bulb(lights, new Vector3(0, 0.9f + i * 0.75f, -0.5f), 0.3f);   // for (i < 10) lightOn(i)
        Bulb(root, new Vector3(0, 8.8f, 0), 0.9f).name = "TowerBeacon";
        Bulb(root, new Vector3(0.55f, 0.6f, -0.3f), 0.25f).name = "TowerClimber";   // while (height < 10) height++
    }

    // ================= small builders =================
    static void Station(GameObject parent, string name, Vector3 pos, float yaw)
    {
        GameObject s = new GameObject(name);
        s.transform.SetParent(parent.transform);
        s.transform.position = pos;
        s.transform.rotation = Quaternion.Euler(0, yaw, 0);
        Box(s, "Stand", new Vector3(0, 0.55f, 0), new Vector3(0.12f, 1.1f, 0.12f), dark);
        GameObject board = Box(s, "ScreenBoard", new Vector3(0, 1.45f, 0), new Vector3(1.8f, 1.0f, 0.06f), screenMat);
        board.transform.localRotation = Quaternion.Euler(-12, 0, 0);
    }

    static void LampPost(GameObject parent, Vector3 pos)
    {
        GameObject l = new GameObject("LampPost");
        l.transform.SetParent(parent.transform);
        l.transform.position = pos;
        Box(l, "Pole", new Vector3(0, 1.5f, 0), new Vector3(0.12f, 1.5f, 0.12f), dark, PrimitiveType.Cylinder);
        GameObject lamp = Box(l, "Lamp", new Vector3(0, 3.1f, 0), new Vector3(0.4f, 0.4f, 0.4f), Emissive("Lamp_Warm_Mat", new Color(1f, 0.8f, 0.45f), 1.5f), PrimitiveType.Sphere);
        Object.DestroyImmediate(lamp.GetComponent<Collider>());
    }

    static void Tree(GameObject parent, Vector3 pos, float s)
    {
        GameObject t = new GameObject("Tree");
        t.transform.SetParent(parent.transform);
        t.transform.position = pos;
        t.transform.localScale = Vector3.one * s;
        Box(t, "Trunk", new Vector3(0, 1f, 0), new Vector3(0.35f, 1f, 0.35f), trunk, PrimitiveType.Cylinder);
        Box(t, "Leaves", new Vector3(0, 2.6f, 0), new Vector3(2f, 2.2f, 2f), leaves, PrimitiveType.Sphere);
    }

    static void Leg(GameObject parent, Vector3 localPos, float tilt)
    {
        GameObject g = Box(parent, "Leg", localPos, new Vector3(0.3f, 6.8f, 0.3f), white);
        g.transform.localRotation = Quaternion.Euler(0, 0, tilt);
    }

    static GameObject Bulb(GameObject parent, Vector3 localPos, float size)
    {
        GameObject b = Box(parent, "Bulb", localPos, Vector3.one * size, bulbOff, PrimitiveType.Sphere);
        Object.DestroyImmediate(b.GetComponent<Collider>());
        return b;
    }

    static Vector3 Polar(float r, float deg)
    {
        float a = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
    }

    static void Wall(GameObject parent, Vector3 pos, Vector3 size)
    {
        GameObject w = new GameObject("InvisibleWall");
        w.transform.SetParent(parent.transform);
        w.transform.position = pos;
        w.AddComponent<BoxCollider>().size = size;
    }

    static void Flat(GameObject parent, string name, Vector3 pos, Vector3 size, Material m)
    {
        GameObject g = Box(parent, name, pos, size, m);
        g.transform.position = pos;
        Object.DestroyImmediate(g.GetComponent<Collider>());
    }

    static GameObject Child(string name, GameObject parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        return g;
    }

    static GameObject Box(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material m, PrimitiveType type = PrimitiveType.Cube)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static void Text3D(GameObject parent, string name, Vector3 localPos, string text, float size, Vector2 box, Color c, bool faceBoth = false)
    {
        GameObject t = new GameObject(name);
        t.transform.SetParent(parent.transform, false);
        t.transform.localPosition = localPos;
        var tmp = t.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = c;
        tmp.rectTransform.sizeDelta = box;
        if (faceBoth) t.AddComponent<FaceCameraMarker>();
    }

    // ================= look =================
    static void NightLighting()
    {
        GameObject moon = new GameObject("Moonlight");
        var l = moon.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.55f, 0.65f, 1f);
        l.intensity = 0.35f;
        l.shadows = LightShadows.Soft;
        moon.transform.rotation = Quaternion.Euler(40, -30, 0);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.28f);

        Material sky = LoadOrCreate("NightSky_Mat", "Skybox/Procedural");
        sky.SetFloat("_SunSize", 0.02f);
        sky.SetFloat("_AtmosphereThickness", 0.4f);
        sky.SetColor("_SkyTint", new Color(0.1f, 0.12f, 0.3f));
        sky.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.08f));
        sky.SetFloat("_Exposure", 0.25f);
        RenderSettings.skybox = sky;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.07f, 0.08f, 0.16f);
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 70f;
    }

    static void MakeMaterials()
    {
        grass  = Lit("Grass_Mat",  new Color(0.16f, 0.32f, 0.16f));
        path   = Lit("Path_Mat",   new Color(0.55f, 0.45f, 0.35f));
        stone  = Lit("Stone_Mat",  new Color(0.6f, 0.6f, 0.62f));
        wood   = Lit("Wood_Mat",   new Color(0.55f, 0.35f, 0.2f));
        white  = Lit("White_Mat",  new Color(0.92f, 0.92f, 0.9f));
        red    = Lit("Red_Mat",    new Color(0.8f, 0.15f, 0.18f));
        yellow = Lit("Yellow_Mat", new Color(0.95f, 0.75f, 0.2f));
        blue   = Lit("Blue_Mat",   new Color(0.2f, 0.45f, 0.9f));
        purple = Lit("Purple_Mat", new Color(0.55f, 0.3f, 0.8f));
        dark   = Lit("Dark_Mat",   new Color(0.12f, 0.12f, 0.15f));
        leaves = Lit("Leaves_Mat", new Color(0.12f, 0.4f, 0.18f));
        trunk  = Lit("Trunk_Mat",  new Color(0.35f, 0.22f, 0.12f));
        screenMat = Lit("ScreenOff_Mat", new Color(0.05f, 0.06f, 0.1f));
        bulbOff = Lit("BulbOff_Mat", new Color(0.3f, 0.3f, 0.32f));
        // "on" colours used later by the game scripts
        Emissive("BulbOn_Yellow_Mat", new Color(1f, 0.85f, 0.3f), 2f);
        Emissive("BulbOn_Pink_Mat",   new Color(1f, 0.35f, 0.7f), 2f);
        Emissive("BulbOn_Blue_Mat",   new Color(0.3f, 0.6f, 1f), 2f);
        Emissive("BulbOn_Green_Mat",  new Color(0.3f, 1f, 0.5f), 2f);
        Emissive("BulbOn_White_Mat",  new Color(1f, 1f, 1f), 2f);
    }

    static Material Lit(string name, Color c)
    {
        Material m = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Emissive(string name, Color c, float intensity)
    {
        Material m = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material LoadOrCreate(string name, string shader)
    {
        string p = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, p);
        }
        return m;
    }
}
