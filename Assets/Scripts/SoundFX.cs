using UnityEngine;

// Code the Park - written by us.
// One place for all short sounds. Every sound was made by us with code
// (Assets/Audio, see make_sounds.py), so there is no licence to worry about.
// 3D sounds come from where things happen (a coin, a ride); UI sounds are 2D.
public class SoundFX : MonoBehaviour
{
    public static SoundFX I { get; private set; }

    [Tooltip("Played when the player picks up a coin, gem or Fix Tool (magic sparkle).")]
    public AudioClip pickup;
    [Header("Music and night sounds (also put in place here when the game starts, in case a scene link was lost)")]
    public AudioClip ambience, musicCarousel, musicWheel;
    public AudioClip coin, gem, tool, correct, wrong, click, run, powerOn, gateOpen,
                     fireworkLaunch, fireworkBoom, celebrate;
    [Tooltip("Long fireworks recording for the final celebration (Pixabay, see CREDITS.md).")]
    public AudioClip fireworksShow;
    [Range(0f, 1f)] public float volume = 0.9f;
    [Tooltip("Volume of the WHOLE game (all sounds and music). 1 = full.")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    AudioSource ui;

    void Awake()
    {
        I = this;
        AudioListener.volume = masterVolume;                 // the whole game is softer
        ui = gameObject.AddComponent<AudioSource>();
        ui.playOnAwake = false; ui.spatialBlend = 0f;        // 2D, for buttons and the code screen
    }

    void Start()
    {
        // make sure the player can hear: one active AudioListener on the headset camera, not paused, full volume
        AudioListener.pause = false;
        AudioListener.volume = Mathf.Max(0.05f, masterVolume);
        var cam = Camera.main;
        if (cam != null)
        {
            var l = cam.GetComponent<AudioListener>();
            if (l == null) l = cam.gameObject.AddComponent<AudioListener>();
            l.enabled = true;
        }
        // load the short sounds now, so the first click / coin / Codey beep is not silent
        foreach (var c in new[] { pickup, coin, gem, tool, correct, wrong, click, run, powerOn, gateOpen, fireworkLaunch, fireworkBoom, celebrate })
            if (c != null && c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Debug.Log("CS732 audio: listeners=" + listeners.Length + " volume=" + AudioListener.volume +
                  " output=" + AudioSettings.outputSampleRate + "Hz " + AudioSettings.speakerMode +
                  " coin clip=" + (coin != null ? coin.loadState.ToString() : "missing"));
        // night ambience: everywhere, quiet
        AudioSource amb = null;
        var ambGo = transform.Find("NightAmbience");
        if (ambGo != null) amb = ambGo.GetComponent<AudioSource>();
        if (amb == null) { amb = new GameObject("NightAmbience").AddComponent<AudioSource>(); amb.transform.SetParent(transform, false); amb.volume = 0.12f; }
        if (amb.clip == null && ambience != null) { amb.clip = ambience; amb.loop = true; amb.spatialBlend = 0f; }
        if (amb.clip != null && !amb.isPlaying) amb.Play();
        // ride music (the RideController starts it when the ride is fixed)
        Music("Area2_Carousel", musicCarousel);
        Music("Area3_FerrisWheel", musicWheel);
    }

    static void Music(string area, AudioClip clip)
    {
        var a = GameObject.Find(area);
        if (a == null || clip == null) return;
        var m = a.transform.Find("RideMusic");
        var src = m != null ? m.GetComponent<AudioSource>() : null;
        if (src != null && src.clip == null) src.clip = clip;
    }

    // a short sound at a place in the park (3D: louder when close)
    public static void At(AudioClip clip, Vector3 pos, float vol = 1f)
    {
        if (I == null || clip == null) return;
        var go = new GameObject("SFX_" + clip.name);
        go.transform.position = pos;
        var s = go.AddComponent<AudioSource>();
        s.clip = clip; s.volume = vol * I.volume;
        s.spatialBlend = 1f; s.minDistance = 2f; s.maxDistance = 40f; s.rolloffMode = AudioRolloffMode.Linear;
        s.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // a sound that is not in one place (button click, correct / wrong)
    public static void UI(AudioClip clip, float vol = 1f)
    {
        if (I == null || clip == null) return;
        I.ui.PlayOneShot(clip, vol * I.volume);
    }
}
