using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Code the Park - written by us.
// A coin, gem or Fix Tool floating in the park. It spins and bobs so the player notices it.
// When the player grabs it (Grip, uses XRI's ready-made XR Grab Interactable) it is collected:
// small vibration, it shrinks into the air and the Inventory counts it.
[RequireComponent(typeof(XRGrabInteractable))]
public class Collectible : MonoBehaviour
{
    public Inventory.Kind kind = Inventory.Kind.Coin;
    public int level = 1;               // which level this item belongs to
    public float spinSpeed = 90f;
    [Tooltip("Soft twinkle sound heard only near the item (made by us).")]
    public AudioClip shimmer;
    public float nearDistance = 1.8f;   // closer than this: the item "calls" the player
    [HideInInspector] public bool taken;

    Vector3 startPos;
    XRGrabInteractable grab;
    AudioSource hum;
    Transform head;
    Vector3 baseScale;
    bool wasNear;

    void Awake()
    {
        startPos = transform.position;
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        baseScale = transform.localScale;

        // a quiet 3D twinkle: you only hear it within a few metres, so it helps you find the item
        if (shimmer != null)
        {
            hum = gameObject.AddComponent<AudioSource>();
            hum.clip = shimmer; hum.loop = true; hum.playOnAwake = false;
            hum.spatialBlend = 1f; hum.rolloffMode = AudioRolloffMode.Linear;
            hum.minDistance = 0.5f; hum.maxDistance = 3.5f; hum.volume = 0.15f;   // quiet, only heard close by
            hum.pitch = kind == Inventory.Kind.Gem ? 1.25f : kind == Inventory.Kind.Tool ? 0.8f : 1f;
            hum.time = Random.Range(0f, shimmer.length * 0.9f);   // not all items in step
        }
    }

    void OnEnable()  { if (hum != null) hum.Play(); }

    void OnDestroy()
    {
        if (grab != null) grab.selectEntered.RemoveListener(OnGrab);
    }

    void Update()
    {
        if (taken) return;
        if (head == null && Camera.main != null) head = Camera.main.transform;
        bool near = head != null && Vector3.Distance(head.position, transform.position) < nearDistance;

        // when the player is close: spin faster and gently "breathe" bigger, plus a small ping
        float spin = near ? spinSpeed * 2.5f : spinSpeed;
        float pulse = near ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 5f)) : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * pulse, 10f * Time.deltaTime);
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * 2f + startPos.x) * 0.06f;
        transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
        if (hum != null) hum.volume = Mathf.Lerp(hum.volume, near ? 0.35f : 0.15f, 3f * Time.deltaTime);
        // (the 'tick' when you come close was removed)
        wasNear = near;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (taken) return;
        if (args.interactorObject is XRBaseInputInteractor hand) hand.SendHapticImpulse(0.6f, 0.12f);
        CollectNow();
    }

    // collect this item (by hand, or with the test keys in the editor): it counts, sparkles and disappears
    public void CollectNow()
    {
        if (taken) return;
        taken = true;
        if (Inventory.Instance != null) Inventory.Instance.Add(kind, level);
        if (SoundFX.I != null)
        {
            if (SoundFX.I.pickup != null) SoundFX.At(SoundFX.I.pickup, transform.position, 0.45f);   // magic sparkle, soft
            else SoundFX.At(kind == Inventory.Kind.Coin ? SoundFX.I.coin : kind == Inventory.Kind.Gem ? SoundFX.I.gem : SoundFX.I.tool, transform.position);
        }
        grab.enabled = false;           // let go of the hand straight away
        if (hum != null) hum.Stop();
        StartCoroutine(Vanish());
    }

    IEnumerator Vanish()
    {
        Vector3 s = baseScale, p = transform.position;
        float t = 0;
        while (t < 0.35f)
        {
            float k = t / 0.35f;
            transform.localScale = s * (1f - k);
            transform.position = p + Vector3.up * k * 0.4f;
            transform.Rotate(0, 720f * Time.deltaTime, 0, Space.World);
            t += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
