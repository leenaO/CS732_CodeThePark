using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Add Wheel Light Ring
// The downloaded Ferris wheel has only a few tiny painted lights, so we add our own
// ring of bulbs on the rim and lines of bulbs on the spokes. They start OFF;
// the game turns them on (and spins them) when level 3 is solved.
public static class WheelLights
{
    [MenuItem("CS732/Add Wheel Light Ring")]
    public static void Run()
    {
        GameObject model = GameObject.Find("FerrisWheelModel");
        if (model == null) { Debug.LogWarning("Run CS732 > Setup Ride Models first."); return; }
        Transform area = model.transform.parent;
        GameObject oldGo = GameObject.Find("WheelLights");           // it may sit under the wheel bone
        Transform old = oldGo != null ? oldGo.transform : null;
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // measure the wheel from its real mesh points (not the box around it), because the
        // model is turned at an angle: a box around a turned wheel is too wide and too deep,
        // and that put the bulbs in the air around the wheel (the "strange dots").
        var pts = new System.Collections.Generic.List<Vector3>();
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var v = mf.sharedMesh.vertices;
            int step = Mathf.Max(1, v.Length / 4000);
            for (int i = 0; i < v.Length; i += step) pts.Add(mf.transform.TransformPoint(v[i]));
        }
        // the wheel model is animated, so its parts use SkinnedMeshRenderer: take a snapshot of their shape
        foreach (var sm in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var baked = new Mesh();
            sm.BakeMesh(baked, false);                      // points in the renderer's own space (no scale)
            var v = baked.vertices;
            int step = Mathf.Max(1, v.Length / 4000);
            for (int i = 0; i < v.Length; i += step) pts.Add(sm.transform.TransformPoint(v[i]));
            Object.DestroyImmediate(baked);
        }
        if (pts.Count == 0) { Debug.LogWarning("CS732: no wheel mesh found."); return; }
        // The snapshot of an animated (skinned) mesh can come out at the wrong size (the FBX was in
        // centimetres), so we fit the points to the real box Unity shows for the wheel:
        // same height and same centre. The direction of the wheel stays correct.
        var rends = model.GetComponentsInChildren<Renderer>();
        Bounds wb = rends[0].bounds; foreach (var r in rends) wb.Encapsulate(r.bounds);
        Vector3 pMin = pts[0], pMax = pts[0];
        foreach (var q in pts) { pMin = Vector3.Min(pMin, q); pMax = Vector3.Max(pMax, q); }
        float fit = wb.size.y / Mathf.Max(0.0001f, pMax.y - pMin.y);
        Vector3 pc = (pMin + pMax) * 0.5f;
        for (int i = 0; i < pts.Count; i++) pts[i] = wb.center + (pts[i] - pc) * fit;

        float top = float.MinValue, bottom = float.MaxValue;
        foreach (var q in pts) { top = Mathf.Max(top, q.y); bottom = Mathf.Min(bottom, q.y); }
        // only the upper half: that is wheel, not the base or stairs
        var up = pts.FindAll(q => q.y > bottom + (top - bottom) * 0.45f);
        Vector3 mean = Vector3.zero; foreach (var q in up) mean += q; mean /= up.Count;
        // the wheel's flat side = the horizontal direction where the points spread the most
        float sxx = 0, sxz = 0, szz = 0;
        foreach (var q in up) { float dx = q.x - mean.x, dz = q.z - mean.z; sxx += dx * dx; sxz += dx * dz; szz += dz * dz; }
        float ang = 0.5f * Mathf.Atan2(2f * sxz, sxx - szz);
        Vector3 along = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));   // in the wheel's plane
        Vector3 axle = Vector3.Cross(along, Vector3.up).normalized;         // the wheel's axle
        var pa = new System.Collections.Generic.List<float>(); var pz = new System.Collections.Generic.List<float>();
        foreach (var q in up) { pa.Add(Vector3.Dot(q - mean, along)); pz.Add(Vector3.Dot(q - mean, axle)); }
        pa.Sort(); pz.Sort();
        float aMin = pa[(int)(pa.Count * 0.005f)], aMax = pa[(int)(pa.Count * 0.995f)];
        float R = (aMax - aMin) * 0.5f * 0.97f;
        float zMin = pz[(int)(pz.Count * 0.05f)], zMax = pz[(int)(pz.Count * 0.95f)];
        float depth = zMax - zMin;
        Vector3 centre = mean + along * ((aMin + aMax) * 0.5f) + axle * ((zMin + zMax) * 0.5f);
        centre.y = top - R / 0.97f;

        GameObject lights = new GameObject("WheelLights");
        lights.transform.SetParent(area, true);
        lights.transform.position = centre;
        // local Z = the wheel's axle, local X/Y = the wheel's plane
        lights.transform.rotation = Quaternion.LookRotation(axle, Vector3.up);

        Material off = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulbOff_Mat.mat");
        float front = -depth * 0.5f - 0.05f;   // bulbs on the side that faces the plaza
        for (int side = 0; side < 2; side++)
        {
            float z = side == 0 ? front : -front;
            for (int i = 0; i < 36; i++)        // rim
            {
                float a = i * Mathf.PI * 2f / 36f;
                Bulb(lights, new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, z), 0.22f, off, "RimBulb");
            }
            for (int s = 0; s < 8; s++)         // spokes
            {
                float a = s * Mathf.PI * 2f / 8f;
                for (int k = 1; k <= 5; k++)
                {
                    float r = R * k / 6f;
                    Bulb(lights, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z), 0.16f, off, "SpokeBulb");
                }
            }
        }
        Bulb(lights, Vector3.zero, 0.6f, off, "HubBulb");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: wheel light ring added (radius " + R.ToString("0.0") + " m, depth " + depth.ToString("0.0") + " m, " + lights.transform.childCount + " bulbs).");
    }

    static void Bulb(GameObject parent, Vector3 local, float size, Material m, string name)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local;
        g.transform.localScale = Vector3.one * size;
        g.GetComponent<Renderer>().sharedMaterial = m;
    }
}
