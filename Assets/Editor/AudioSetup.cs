using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Sounds
// Puts our own sounds (Assets/Audio, all made with code by us) into the game:
// SoundFX (short sounds), music for each ride (3D, starts when the ride is fixed),
// and a quiet night ambience (crickets + soft wind) for the whole park.
public static class AudioSetup
{
    const string Dir = "Assets/Audio/";
    static AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + n + ".wav");

    [MenuItem("CS732/Setup Sounds")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }

        // music: long files stream from disk, short ones load into memory (good for Quest)
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))   // includes Audio/Robot
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (AudioImporter)AssetImporter.GetAtPath(path);
            var st = imp.defaultSampleSettings;
            bool longClip = path.Contains("music_") || path.Contains("ambience_") || path.Contains("Celebration");
            st.loadType = longClip ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            st.quality = 0.7f;
            st.preloadAudioData = !longClip;                 // short sounds are ready before the game starts
            imp.defaultSampleSettings = st;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }

        var sys = GameObject.Find("GameSystems");
        if (sys == null) sys = new GameObject("GameSystems");
        var fx = sys.GetComponent<SoundFX>();
        if (fx == null) fx = sys.AddComponent<SoundFX>();
        fx.pickup = C("item_magic");
        fx.ambience = C("ambience_night"); fx.musicCarousel = C("music_carousel"); fx.musicWheel = C("music_ferriswheel");
        foreach (var (n, clip) in new[] { ("ambience_night", fx.ambience), ("music_carousel", fx.musicCarousel), ("music_ferriswheel", fx.musicWheel), ("sfx_coin", fx.coin) })
            if (clip == null) Debug.LogError("CS732: sound not found: Assets/Audio/" + n + ".wav");                           // ... and play the magic sparkle when picked up
        fx.coin = C("sfx_coin"); fx.gem = C("sfx_gem"); fx.tool = C("sfx_tool");
        fx.correct = C("sfx_correct"); fx.wrong = C("sfx_wrong"); fx.click = C("sfx_click"); fx.run = C("sfx_run");
        fx.powerOn = C("sfx_power_on"); fx.gateOpen = C("sfx_gate_open");
        fx.fireworkLaunch = C("sfx_firework_launch"); fx.fireworkBoom = C("sfx_firework_boom"); fx.celebrate = C("sfx_celebrate"); fx.fireworksShow = C("Celebration/fireworks_celebration");

        // Codey's voice: cute beeps while talking, happy / sad sounds, soft hover hum
        var codey = Object.FindAnyObjectByType<RobotTeacher>();
        if (codey != null)
        {
            var beeps = new AudioClip[8];
            for (int i = 0; i < 8; i++) beeps[i] = C("Robot/robot_talk_" + i);
            codey.talkBeeps = beeps;
            codey.happyClip = C("Robot/robot_happy"); codey.sadClip = C("Robot/robot_sad"); codey.hoverClip = C("Robot/robot_hover");
            EditorUtility.SetDirty(codey);
        }

        // soft twinkle on every coin, gem and Fix Tool (heard only when close)
        AudioClip shimmer = null;                              // items are quiet while they float ...
        int items = 0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<Collectible>(true)) { c.shimmer = shimmer; EditorUtility.SetDirty(c); items++; }

        // ride music (the RideMusic sources were made by Setup Rides Come Alive)
        int rides = 0;
        rides += Music("Area2_Carousel", "music_carousel");
        rides += Music("Area3_FerrisWheel", "music_ferriswheel");
        // roller coaster: no music - the sound of the train on the track instead
        var coasterArea = GameObject.Find("Area4_RollerCoaster");
        if (coasterArea != null)
        {
            var m = coasterArea.transform.Find("RideMusic");
            if (m != null && m.GetComponent<AudioSource>() != null) m.GetComponent<AudioSource>().clip = null;
            foreach (var ride in coasterArea.GetComponentsInChildren<RollerCoasterRide>(true))
            {
                ride.rumble = C("Coaster/coaster_rumble"); ride.clack = C("Coaster/coaster_clack"); ride.chain = null; ride.railJoint = 3f;
                EditorUtility.SetDirty(ride);
            }
        }

        // night ambience, quiet, everywhere (2D)
        var amb = sys.transform.Find("NightAmbience");
        if (amb == null) { amb = new GameObject("NightAmbience").transform; amb.SetParent(sys.transform, false); }
        var a = amb.GetComponent<AudioSource>();
        if (a == null) a = amb.gameObject.AddComponent<AudioSource>();
        a.clip = C("ambience_night"); a.loop = true; a.playOnAwake = true; a.spatialBlend = 0f; a.volume = 0.12f;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: sounds ready - SoundFX, Codey's voice, night ambience, twinkle on " + items + " items, music on " + rides + " rides.");
    }

    static int Music(string area, string clip)
    {
        var go = GameObject.Find(area);
        if (go == null) return 0;
        var m = go.transform.Find("RideMusic");
        if (m == null) return 0;
        var src = m.GetComponent<AudioSource>();
        if (src == null) return 0;
        src.clip = C(clip);
        return 1;
    }
}
