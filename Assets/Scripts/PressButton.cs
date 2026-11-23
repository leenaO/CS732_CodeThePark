using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Code the Park - written by us.
// A big round 3D button. Press it with your finger (poke) or point the ray at it and pull the Trigger.
// Uses XRI's ready-made XR Simple Interactable. The cap moves down a little and clicks.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PressButton : MonoBehaviour
{
    public Transform cap;                 // the part that moves down
    public UnityEvent onPress = new UnityEvent();
    public float cooldown = 0.6f;
    float last = -10f;
    Vector3 capRest;

    void Awake()
    {
        if (cap != null) capRest = cap.localPosition;
        GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => Press());
    }

    public void Press()
    {
        if (Time.time - last < cooldown) return;
        last = Time.time;
        if (SoundFX.I != null) SoundFX.At(SoundFX.I.click, transform.position);
        if (cap != null) { cap.localPosition = capRest - Vector3.up * 0.015f; Invoke(nameof(Up), 0.15f); }
        onPress.Invoke();
    }

    void Up() { if (cap != null) cap.localPosition = capRest; }
}
