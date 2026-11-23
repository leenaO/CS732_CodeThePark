using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Code the Park - editor helper written by us.
// Menu: CS732 > Turn On Street Lights
// The street lamps stay ON for the whole game (only the rides are broken), so the player
// can always see the path. Each lamp gets one warm Point Light under its lamp head.
// Cheap for Quest 3: no shadows, small range.
public static class StreetLights
{
    static Material CoreMat()
    {
        const string path = "Assets/Materials/Props/Lamp_Core.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", new Color(1f, 0.85f, 0.6f));
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.42f) * 2.2f);   // warm, gentle glow
        EditorUtility.SetDirty(m);
        return m;
    }

    [MenuItem("CS732/Turn On Street Lights")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("CS732: press Stop first - this tool changes the scene and cannot run in Play mode."); return; }
        int n = 0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.name != "StreetLamp") continue;
                var old = t.Find("LampLight");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                // find the lamp head from the real mesh: the pole stands at the lamp's origin, and the
                // lantern hangs at the end of the arm = the points farthest from the pole sideways
                var pts = new System.Collections.Generic.List<Vector3>();
                foreach (var mf in t.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var v = mf.sharedMesh.vertices;
                    int step = Mathf.Max(1, v.Length / 3000);
                    for (int i = 0; i < v.Length; i += step) pts.Add(mf.transform.TransformPoint(v[i]));
                }
                if (pts.Count == 0) continue;
                Vector2 pole = new Vector2(t.position.x, t.position.z);
                float far = 0f;
                foreach (var q in pts) far = Mathf.Max(far, Vector2.Distance(new Vector2(q.x, q.z), pole));
                Vector3 head = Vector3.zero; float lowY = float.MaxValue; int hn = 0;
                foreach (var q in pts)
                    if (Vector2.Distance(new Vector2(q.x, q.z), pole) > far * 0.7f) { head += q; hn++; lowY = Mathf.Min(lowY, q.y); }
                head /= Mathf.Max(1, hn);

                var go = new GameObject("LampLight");
                go.transform.SetParent(t, true);
                Vector3 inside = new Vector3(head.x, (head.y + lowY) * 0.5f, head.z);   // middle of the lantern glass

                // 1) a small glowing "bulb" INSIDE the lantern box: with Bloom the glass looks softly lit
                var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.name = "LampCore";
                Object.DestroyImmediate(core.GetComponent<Collider>());
                core.transform.SetParent(go.transform, false);
                core.transform.position = inside;
                core.transform.localScale = Vector3.one * 0.11f;
                core.GetComponent<Renderer>().sharedMaterial = CoreMat();
                core.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // 2) the light itself a little BELOW the lantern, so it does not make a hard bright spot
                //    on the lantern frame, and softly lights the path under it
                go.transform.position = inside - Vector3.up * 0.45f;
                core.transform.position = inside;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.78f, 0.5f);     // warm, like old street lamps
                l.range = 7f;
                l.intensity = 1.3f;                         // soft, just enough to see the path
                l.shadows = LightShadows.None;
                n++;
            }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CS732: " + n + " street lamps now give light.");
    }
}
