using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Fix Ride Sizes
// Makes the rides match real-world sizes next to a 1.6 m tall player:
//   carousel horse about 1.5 m long, Ferris wheel 16 m tall, roller coaster 22 m long.
// Each ride also stays on the ground and is moved so it does not cover the paths.
public static class RideSizeFixer
{
    [MenuItem("CS732/Fix Ride Sizes")]
    public static void Run()
    {
        // ---------- carousel: measure one horse
        var car = GameObject.Find("CarouselModel");
        if (car != null)
        {
            var horses = car.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterials.Any(m => m != null && m.name.StartsWith("Car_Horse"))).ToList();
            float horseLen = 0f;
            if (horses.Count > 0)
            {
                var sizes = horses.Select(r => Mathf.Max(r.bounds.size.x, r.bounds.size.z)).OrderBy(v => v).ToList();
                horseLen = sizes[sizes.Count / 2];
            }
            float factor;
            if (horseLen > 0.05f && horseLen < 3f) factor = 1.5f / horseLen;      // one horse per mesh
            else factor = 14f / Width(car);                                      // horses joined: use a 14 m wide carousel
            car.transform.localScale *= factor;
            Ground(car, new Vector3(-12f, 0, 8f));
            Debug.Log("CS732: carousel horse was " + horseLen.ToString("0.00") + " m (" + horses.Count + " horse meshes), carousel now " + Width(car).ToString("0.0") + " m wide.");
        }

        // ---------- Ferris wheel: 16 m tall
        var wheel = GameObject.Find("FerrisWheelModel");
        if (wheel != null)
        {
            wheel.transform.localScale *= 16f / B(wheel).size.y;
            Ground(wheel, new Vector3(11f, 0, 11f));
            var lightsGo = GameObject.Find("WheelLights");
            var lights = lightsGo != null ? lightsGo.transform : null;
            if (lights != null) { Object.DestroyImmediate(lights.gameObject); Debug.Log("CS732: wheel lights removed - run CS732 > Add Wheel Light Ring again."); }
            Debug.Log("CS732: Ferris wheel now 16 m tall.");
        }

        // ---------- roller coaster: 22 m long, behind station 4
        var rc = GameObject.Find("RollerCoasterModel");
        if (rc != null)
        {
            rc.transform.localScale *= 22f / Width(rc);
            Ground(rc, new Vector3(0, 0, 21f));
            Debug.Log("CS732: roller coaster now 22 m long.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    static float Width(GameObject g) { var b = B(g); return Mathf.Max(b.size.x, b.size.z); }

    // stand on the ground with the centre of the model at "centre"
    static void Ground(GameObject g, Vector3 centre)
    {
        var b = B(g);
        g.transform.position += new Vector3(centre.x - b.center.x, -b.min.y, centre.z - b.center.z);
    }

    static Bounds B(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
