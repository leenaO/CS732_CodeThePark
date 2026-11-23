using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Code the Park - written by us.
// The START screen: game name, the goal in one sentence, the controls, and a big START button.
// It stands in front of the player when the game opens. Nothing happens until START is pressed:
// then the panel folds away, the clock starts and Codey introduces himself (LevelManager waits for it).
// Press it with the ray + Trigger, or touch it with the hand. In the Unity editor: Enter also works.
public class StartPanel : MonoBehaviour
{
    public Material boardMat, frameMat, buttonMat;
    public float distance = 2.0f, height = 1.45f;
    public bool Started { get; private set; }
    GameObject panel;

    void Start() { StartCoroutine(Build()); }

    IEnumerator Build()
    {
        yield return null;                                       // let the XR rig find its start pose
        var rig = FindAnyObjectByType<XROrigin>();
        Transform basis = rig != null ? rig.transform : (Camera.main != null ? Camera.main.transform : transform);
        Vector3 fwd = Vector3.ProjectOnPlane(basis.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;

        panel = new GameObject("StartPanel");
        panel.transform.position = new Vector3(basis.position.x, 0, basis.position.z) + fwd * distance + Vector3.up * height;
        panel.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);   // text faces the player

        Box("Frame", new Vector3(0, 0, 0.03f), new Vector3(1.78f, 1.18f, 0.04f), frameMat);
        Box("Board", new Vector3(0, 0, 0.005f), new Vector3(1.7f, 1.1f, 0.02f), boardMat);

        Text("<color=#FFD54A><b>CODE THE PARK</b></color>", new Vector3(0, 0.44f, 0), 1.0f, 1.6f, TextAlignmentOptions.Center);
        Text("Learn C++ in VR with Codey, the robot teacher", new Vector3(0, 0.33f, 0), 0.42f, 1.6f, TextAlignmentOptions.Center);
        Text("The park is dark because its code is broken. Fix <b>20 lines of C++</b> in 4 levels " +
             "and bring every ride back to life! Collect <color=#FFD54A>coins</color>, <color=#4DE3FF>gems</color> and " +
             "<color=#FFA040>Fix Tools</color> - each ride needs some of them.",
             new Vector3(0, 0.17f, 0), 0.36f, 1.5f, TextAlignmentOptions.Center);
        Text("<color=#FFD54A><b>Controls</b></color>\n" +
             "<b>Left stick</b>  -  walk\n" +
             "<b>Right stick</b>  -  turn (left / right), teleport (push forward)\n" +
             "<b>Grip</b> (side button)  -  grab a code block, a coin, a gem or a Fix Tool\n" +
             "<b>Trigger</b>  -  press buttons with the ray (or touch them with your hand)\n" +
             "<b>Left wrist</b>  -  look at it to see what you collected",
             new Vector3(-0.05f, -0.15f, 0), 0.34f, 1.45f, TextAlignmentOptions.Left);

        // the big START button (same kind of button as RUN)
        var b = new GameObject("StartButton");
        b.transform.SetParent(panel.transform, false);
        b.transform.localPosition = new Vector3(0, -0.47f, -0.04f);
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(cap.GetComponent<Collider>());
        cap.transform.SetParent(b.transform, false);
        cap.transform.localRotation = Quaternion.Euler(90, 0, 0);
        cap.transform.localScale = new Vector3(0.36f, 0.025f, 0.13f);
        if (buttonMat != null) cap.GetComponent<Renderer>().sharedMaterial = buttonMat;
        var label = Text("<b>START</b>", Vector3.zero, 0.6f, 0.4f, TextAlignmentOptions.Center);
        label.transform.SetParent(b.transform, false);
        label.transform.localPosition = new Vector3(0, 0, -0.03f);
        var col = b.AddComponent<BoxCollider>(); col.size = new Vector3(0.4f, 0.16f, 0.08f);
        b.AddComponent<XRSimpleInteractable>();
        var pb = b.AddComponent<PressButton>(); pb.cap = cap.transform;
        pb.onPress.AddListener(Go);
    }

    void Box(string name, Vector3 local, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(panel.transform, false);
        g.transform.localPosition = local; g.transform.localScale = scale;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
    }

    TextMeshPro Text(string s, Vector3 local, float size, float width, TextAlignmentOptions align)
    {
        var t = new GameObject("Text").AddComponent<TextMeshPro>();
        t.transform.SetParent(panel.transform, false);
        t.transform.localPosition = local + new Vector3(0, 0, -0.01f);
        t.rectTransform.sizeDelta = new Vector2(width, 0.3f);
        t.text = s; t.fontSize = size; t.color = Color.white; t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

#if UNITY_EDITOR
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (!Started && kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) Go();
    }
#endif

    void Go()
    {
        if (Started) return;
        Started = true;
        if (SoundFX.I != null && SoundFX.I.powerOn != null) SoundFX.At(SoundFX.I.powerOn, panel != null ? panel.transform.position : transform.position);
        StartCoroutine(Fold());
    }

    IEnumerator Fold()                                           // the panel shrinks and disappears
    {
        if (panel == null) yield break;
        Vector3 s0 = panel.transform.localScale;
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            panel.transform.localScale = Vector3.Lerp(s0, new Vector3(s0.x, 0.02f, s0.z), t);
            yield return null;
        }
        Destroy(panel);
    }

    public IEnumerator WaitForStart()
    {
        while (!Started) yield return null;
    }
}
