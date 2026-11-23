using System.Collections.Generic;
using UnityEngine;

// Code the Park - written by us.
// Moves the roller coaster train around its track.
// The train has 4 cars (we split the model's cart mesh into carrinho_0..3), and EACH car
// follows the track on its own, at its own place on the rail, so the train bends smoothly
// through curves and loops like a real train (before, it moved as one stiff piece).
// The track path was taken from the model's middle rail (CoasterPathData.cs).
// Slow at the top, fast at the bottom. The player watches from the ground (no motion sickness).
public class RollerCoasterRide : MonoBehaviour
{
    [Tooltip("Tick to test in Play mode. In the game, the ride's RideController calls Play().")]
    public bool playOnStart = false;
    [Tooltip("Tick if the train drives backwards.")]
    public bool reverse = false;
    public float minSpeed = 2.5f;          // metres per second at the highest point
    public float speedBoost = 1.0f;        // more = faster at the bottom
    public float smoothing = 0.35f;        // how far ahead/behind we look on the rail to turn a car smoothly
    public float acceleration = 1.5f;      // speed changes gently (m/s per second)

    [Header("Track sounds (made by us, no music)")]
    public AudioClip rumble;               // heavy wheels on steel (loop), louder and higher when fast
    public AudioClip clack;                // wheels over a rail joint: "clack-clack"
    public AudioClip chain;                // tick-tick-tick while the train is pulled up slowly
    public float railJoint = 3f;           // a soft clack every this many metres

    Vector3[] pts, ups;
    float[] cum;
    float total, travelled, speed, maxY;
    readonly List<Transform> pivots = new List<Transform>();
    readonly List<float> startS = new List<float>();
    bool running;
    AudioSource rumbleSrc, clackSrc;
    float lastClack, lastChain, lastY;

    public bool IsRunning => running;

    void Start()
    {
        Setup();
        if (playOnStart) Play();
    }

    public void Play() { if (pivots.Count == 0) Setup(); running = pivots.Count > 0; }
    public void Stop() { running = false; if (rumbleSrc != null) rumbleSrc.Stop(); }

    void Setup()
    {
        // the 4 cars, front to back order does not matter: each one keeps its own place
        var cars = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name.ToLower().StartsWith("carrinho") && t.GetComponent<Renderer>() != null) cars.Add(t);
        if (cars.Count == 0) { Debug.LogWarning("RollerCoasterRide: cars not found"); return; }
        cars.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        // Unity mirrors OBJ files on X when it imports them: check which way the data fits
        Bounds all = cars[0].GetComponent<Renderer>().bounds;
        foreach (var c in cars) all.Encapsulate(c.GetComponent<Renderer>().bounds);
        Vector3 cLocal = transform.InverseTransformPoint(all.center);
        Vector3 cc = CoasterPathData.CartCentre;
        float sign = (cLocal - cc).sqrMagnitude <= (cLocal - new Vector3(-cc.x, cc.y, cc.z)).sqrMagnitude ? 1f : -1f;

        int n = CoasterPathData.Points.Length;
        pts = new Vector3[n]; ups = new Vector3[n]; cum = new float[n + 1];
        maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = CoasterPathData.Points[i], u = CoasterPathData.Ups[i];
            pts[i] = new Vector3(sign * p.x, p.y, p.z);
            ups[i] = new Vector3(sign * u.x, u.y, u.z);
            maxY = Mathf.Max(maxY, p.y);
        }
        for (int i = 0; i < n; i++) cum[i + 1] = cum[i] + Vector3.Distance(pts[i], pts[(i + 1) % n]);
        total = cum[n];

        // one pivot per car, placed on the rail at that car's own position
        for (int k = 0; k < cars.Count; k++)
        {
            float s0 = k < CoasterPathData.CarS.Length ? CoasterPathData.CarS[k] : CoasterPathData.CarS[0] - k * 0.8f;
            var pv = new GameObject("CarPivot_" + k).transform;
            pv.SetParent(transform, false);
            Place(pv, s0);
            cars[k].SetParent(pv, true);           // the car keeps its exact look, now hung on the pivot
            pivots.Add(pv); startS.Add(s0);
        }

        // the sounds ride ON the train (3D), so you hear it pass by
        var mid = pivots[pivots.Count / 2].gameObject;
        rumbleSrc = mid.AddComponent<AudioSource>();
        rumbleSrc.clip = rumble; rumbleSrc.loop = true; rumbleSrc.playOnAwake = false; rumbleSrc.volume = 0f;
        Make3D(rumbleSrc);
        clackSrc = pivots[0].gameObject.AddComponent<AudioSource>();
        clackSrc.playOnAwake = false;
        Make3D(clackSrc);
    }

    void Update()
    {
        if (!running || pivots.Count == 0) return;
        // speed from the height of the middle of the train: slow at the top, fast at the bottom
        float mid = 0f;
        foreach (var p in pivots) mid += p.localPosition.y;
        mid /= pivots.Count;
        float target = Mathf.Max(minSpeed, Mathf.Sqrt(2f * 9.81f * speedBoost * Mathf.Max(0f, maxY - mid)));
        speed = Mathf.MoveTowards(speed, target, acceleration * 4f * Time.deltaTime);
        travelled += (reverse ? -1f : 1f) * speed * Time.deltaTime;
        for (int k = 0; k < pivots.Count; k++) Place(pivots[k], startS[k] + travelled);
        TrackSounds();
    }

    void TrackSounds()
    {
        if (rumbleSrc != null && rumble != null)
        {
            if (!rumbleSrc.isPlaying) rumbleSrc.Play();
            float k = Mathf.Clamp01(speed / 14f);
            rumbleSrc.volume = 0.15f + 0.75f * k;           // quiet when slow, loud when fast
            rumbleSrc.pitch = 0.75f + 0.6f * k;
        }
        float y = pivots[0].localPosition.y;
        bool climbing = y > lastY + 0.0005f && speed < minSpeed * 1.4f;
        lastY = y;
        float d = Mathf.Abs(travelled);
        if (clackSrc == null) return;
        // (the chain "tick-tick" was removed: it sounded like annoying clicking)
        if (!climbing && clack != null && d - lastClack > railJoint)
        {
            clackSrc.pitch = 0.9f + Mathf.Clamp01(speed / 14f) * 0.3f;
            clackSrc.PlayOneShot(clack, 0.15f + 0.2f * Mathf.Clamp01(speed / 10f));   // soft, under the rumble
            lastClack = d;
        }
    }

    static void Make3D(AudioSource a)
    {
        a.spatialBlend = 1f; a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 3f; a.maxDistance = 35f; a.dopplerLevel = 0.5f;     // a little "whoosh" as it passes
    }

    // put a car pivot on the rail at distance "dist", facing along the rail (smoothed)
    void Place(Transform pv, float dist)
    {
        Vector3 pos = PointAt(dist, out Vector3 up);
        Vector3 fwd = (PointAt(dist + smoothing, out _) - PointAt(dist - smoothing, out _)).normalized;
        if (reverse) fwd = -fwd;
        pv.localPosition = pos;
        if (fwd.sqrMagnitude > 0.0001f) pv.localRotation = Quaternion.LookRotation(fwd, up);
    }

    Vector3 PointAt(float dist, out Vector3 up)
    {
        dist = Mathf.Repeat(dist, total);
        int lo = 0, hi = pts.Length - 1;                 // binary search on the distance table
        while (lo < hi) { int m = (lo + hi + 1) / 2; if (cum[m] <= dist) lo = m; else hi = m - 1; }
        int i = lo, j = (i + 1) % pts.Length;
        float seg = cum[i + 1] - cum[i];
        float t = seg > 0.0001f ? (dist - cum[i]) / seg : 0f;
        up = Vector3.Lerp(ups[i], ups[j], t).normalized;
        return Vector3.Lerp(pts[i], pts[j], t);
    }
}
