using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Setup Rides Come Alive
// Gives the carousel, the Ferris wheel and the roller coaster a RideController:
//  - the carousel and the Ferris wheel use their OWN animation from the downloaded model
//    (we only set it to loop and made an Animator Controller for it),
//  - our wheel bulbs are attached to the wheel bone so they turn with the wheel,
//  - one warm light and a 3D music source per ride.
// Menu: CS732 > Test: Power On All Rides (Play mode only)
public static class RideLifeSetup
{
    const string AnimDir = "Assets/Animations";
    static string carouselSpinner;

    [MenuItem("CS732/Setup Rides Come Alive")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        if (!AssetDatabase.IsValidFolder(AnimDir)) AssetDatabase.CreateFolder("Assets", "Animations");
        var off = Mat("Assets/Materials/BulbOff_Mat.mat");
        var yellow = Mat("Assets/Materials/BulbOn_Yellow_Mat.mat");
        var pink = Mat("Assets/Materials/BulbOn_Pink_Mat.mat");
        var white = Mat("Assets/Materials/BulbOn_White_Mat.mat");
        string log = "";

        // ---------- carousel ----------
        var car = GameObject.Find("CarouselModel");
        if (car != null)
        {
            var anim = Animate(car, "Assets/Models/Carousel_Enkarra/Carousel_Enkarra.fbx", "Carousel");
            // softer glow on the model itself (the strong glow looked harsh) ...
            var rc = Controller(car.transform.parent.gameObject, new[] { "Car_Tent", "Car_Palette" }, anim, null, off, yellow, white, 1.2f);
            // ... and real carousel lighting: a ring of our bulbs under the roof edge
            // and one warm light under the roof that shines on the horses
            var ring = CarouselBulbs(car, anim);
            if (ring != null)
            {
                rc.bulbHolders = new[] { ring.transform };
                rc.attachToBone = carouselSpinner;
                rc.attach = new[] { ring.transform };
                var tent = TentBounds(car);
                rc.rideLight.transform.position = new Vector3(tent.center.x, tent.min.y - 0.4f, tent.center.z);
                rc.rideLight.range = Mathf.Max(tent.size.x, tent.size.z) * 0.9f;
                rc.lightIntensity = 4f;
            }
            log += "carousel (" + (anim != null && anim.runtimeAnimatorController != null ? "animated" : "no animation") + "), ";
        }

        // ---------- Ferris wheel ----------
        var wheel = GameObject.Find("FerrisWheelModel");
        if (wheel != null)
        {
            var anim = Animate(wheel, "Assets/Models/FerrisWheel_Lenda/FerrisWheel_Lenda.fbx", "FerrisWheel");
            var lights = GameObject.Find("WheelLights");
            var bone = wheel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DEF.WHEEL");
            // the bulbs stay under the ride in the scene; RideController puts them on the wheel bone in Play
            if (lights != null) lights.transform.SetParent(wheel.transform.parent, true);
            var rc = Controller(wheel.transform.parent.gameObject, new[] { "FW_Atlas" }, anim, null, off, yellow, white, 4f);
            rc.attachToBone = bone != null ? bone.name : null;
            rc.attach = lights != null ? new[] { lights.transform } : new Transform[0];
            rc.bulbHolders = lights != null ? new[] { lights.transform } : new Transform[0];
            log += "Ferris wheel (" + (bone != null ? "bulbs on wheel bone" : "bone not found") + "), ";
        }

        // ---------- roller coaster ----------
        var coasterModel = GameObject.Find("RollerCoasterModel");
        if (coasterModel != null)
        {
            var ride = coasterModel.GetComponentInParent<RollerCoasterRide>();
            if (ride == null) ride = coasterModel.GetComponentInChildren<RollerCoasterRide>();
            if (ride == null) ride = Object.FindAnyObjectByType<RollerCoasterRide>();
            if (ride != null) ride.playOnStart = false;                                   // waits for the player's code
            var area = coasterModel.transform.parent.gameObject;
            var rc = Controller(area, new string[0], null, ride, off, yellow, pink, 4f);
            rc.bulbHolders = new[] { coasterModel.transform };
            log += "roller coaster";
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("CS732: rides ready to come alive: " + log + ". Press Play, then CS732 > Test: Power On All Rides.");
    }

    [MenuItem("CS732/Test: Power On All Rides")]
    public static void TestPowerOn()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Press Play first, then use this menu."); return; }
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var rc in root.GetComponentsInChildren<RideController>()) rc.PowerOn();
    }

    // ---------- helpers ----------
    static Animator Animate(GameObject model, string fbx, string name)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        if (mi == null) return null;
        var clips = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
        if (clips.Length == 0) return null;
        if (clips.Any(c => !c.loopTime))                                             // only re-import once
        {
            foreach (var c in clips) c.loopTime = true;                                // play again and again
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }

        var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview"));
        if (clip == null) return null;
        string path = AnimDir + "/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);

        var anim = model.GetComponent<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        anim.enabled = true;
        return anim;
    }

    static RideController Controller(GameObject area, string[] glow, Animator anim, RollerCoasterRide coaster,
                                     Material off, Material a, Material b, float glowStrength)
    {
        var rc = area.GetComponent<RideController>();
        if (rc == null) rc = area.AddComponent<RideController>();
        rc.glowMaterials = glow;
        rc.glowStrength = glowStrength;
        rc.animator = anim;
        rc.coaster = coaster;
        rc.bulbOff = off; rc.bulbOnA = a; rc.bulbOnB = b;

        // size of the ride, to place the light and the sound in its middle
        var rs = area.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        Bounds bb = rs.Length > 0 ? rs[0].bounds : new Bounds(area.transform.position, Vector3.one);
        foreach (var r in rs) bb.Encapsulate(r.bounds);

        var lt = area.transform.Find("RideLight");
        if (lt == null) { lt = new GameObject("RideLight").transform; lt.SetParent(area.transform, false); }
        lt.position = new Vector3(bb.center.x, bb.min.y + bb.size.y * 0.6f, bb.center.z);
        var light = lt.GetComponent<Light>();
        if (light == null) light = lt.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.55f);
        light.range = Mathf.Max(bb.size.x, bb.size.z) * 0.9f;
        light.shadows = LightShadows.None;                      // cheap for Quest 3
        light.intensity = 0f;
        rc.rideLight = light;
        rc.lightIntensity = 3f;

        var snd = area.transform.Find("RideMusic");
        if (snd == null) { snd = new GameObject("RideMusic").transform; snd.SetParent(area.transform, false); }
        snd.position = new Vector3(bb.center.x, 1.5f, bb.center.z);
        var src = snd.GetComponent<AudioSource>();
        if (src == null) src = snd.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false; src.loop = true;
        src.spatialBlend = 1f;                                  // 3D: louder near the ride
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 4f; src.maxDistance = 30f;
        rc.music = src;
        return rc;
    }

    static Bounds TentBounds(GameObject car)
    {
        Bounds? b = null;
        foreach (var r in car.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
                if (m != null && m.name == "Car_Tent") { if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; } }
        if (b != null) return b.Value;
        var all = car.GetComponentsInChildren<Renderer>(); Bounds a = all[0].bounds; foreach (var r in all) a.Encapsulate(r.bounds);
        return new Bounds(new Vector3(a.center.x, a.min.y + a.size.y * 0.7f, a.center.z), a.size);
    }

    // ring of bulbs under the edge of the carousel roof (+ a smaller inner ring), ours, from spheres
    static GameObject CarouselBulbs(GameObject car, Animator anim)
    {
        var old = GameObject.Find("CarouselLights");
        if (old != null) Object.DestroyImmediate(old);
        var tent = TentBounds(car);
        float R = Mathf.Min(tent.size.x, tent.size.z) * 0.5f * 0.96f;
        var ring = new GameObject("CarouselLights");
        ring.transform.position = new Vector3(tent.center.x, tent.min.y - 0.08f, tent.center.z);
        var off = Mat("Assets/Materials/BulbOff_Mat.mat");
        for (int i = 0; i < 40; i++)
        {
            float a = i * Mathf.PI * 2f / 40f;
            Bulb(ring, new Vector3(Mathf.Cos(a) * R, 0, Mathf.Sin(a) * R), 0.16f, off);
        }
        for (int i = 0; i < 20; i++)
        {
            float a = i * Mathf.PI * 2f / 20f;
            Bulb(ring, new Vector3(Mathf.Cos(a) * R * 0.55f, 0.35f, Mathf.Sin(a) * R * 0.55f), 0.13f, off);
        }
        // put the ring on the part of the model that turns, so the bulbs turn with the roof
        Transform spinner = FindSpinner(car, anim);
        ring.transform.SetParent(car.transform.parent, true);      // saved under the ride ...
        carouselSpinner = spinner != null ? spinner.name : null;   // ... and put on the turning part in Play
        return ring;
    }

    static void Bulb(GameObject parent, Vector3 local, float size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = "CarouselBulb";
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local;
        g.transform.localScale = Vector3.one * size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // which transform does the model's animation turn? (sample the clip twice and compare)
    static Transform FindSpinner(GameObject car, Animator anim)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return null;
        var clip = anim.runtimeAnimatorController.animationClips.FirstOrDefault();
        if (clip == null) return null;
        var ts = car.GetComponentsInChildren<Transform>(true);
        clip.SampleAnimation(car, 0f);
        var before = ts.Select(t => t.rotation).ToArray();
        clip.SampleAnimation(car, clip.length * 0.25f);
        Transform best = null; int bestCount = 0;
        for (int i = 0; i < ts.Length; i++)
        {
            if (Quaternion.Angle(before[i], ts[i].rotation) < 5f) continue;
            int pi = ts[i].parent != null ? System.Array.IndexOf(ts, ts[i].parent) : -1;
            if (pi >= 0 && Quaternion.Angle(before[pi], ts[pi].rotation) >= 5f) continue;   // only the top turning part
            int c = ts[i].GetComponentsInChildren<Renderer>(true).Length;
            if (c > bestCount) { bestCount = c; best = ts[i]; }
        }
        clip.SampleAnimation(car, 0f);
        return best;
    }

    static Material Mat(string p) => AssetDatabase.LoadAssetAtPath<Material>(p);
}
