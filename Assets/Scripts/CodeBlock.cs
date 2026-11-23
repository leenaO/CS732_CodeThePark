using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Code the Park - written by us.
// One code block (a piece of code the player can grab and put in the blank).
// If the player lets it go anywhere except the slot, it flies back to its place on the tray.
[RequireComponent(typeof(XRGrabInteractable))]
public class CodeBlock : MonoBehaviour
{
    public TextMeshPro label;
    [HideInInspector] public string code;
    Vector3 homePos; Quaternion homeRot;
    XRGrabInteractable grab;
    float freeTime;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        homePos = transform.position; homeRot = transform.rotation;
    }

    public void Set(string text, Vector3 pos, Quaternion rot)
    {
        code = text;
        if (label != null) label.text = "<mspace=0.55em>" + CodeColors.Color(text) + "</mspace>";   // coloured like a code editor
        homePos = pos; homeRot = rot;
        transform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);
        grab.enabled = true;
    }

    public void Lock() { grab.enabled = false; }      // the correct block stays in the slot

    void Update()
    {
        if (!grab.enabled || grab.isSelected) { freeTime = 0; return; }   // held by a hand or in the slot
        if ((transform.position - homePos).sqrMagnitude < 0.0004f) return;
        freeTime += Time.deltaTime;
        if (freeTime > 0.8f)                                              // dropped somewhere: go home
        {
            transform.position = Vector3.Lerp(transform.position, homePos, 8f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, homeRot, 8f * Time.deltaTime);
        }
    }
}
