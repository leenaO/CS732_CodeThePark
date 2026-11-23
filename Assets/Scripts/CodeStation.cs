using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Code the Park - written by us.
// The code screen at a station. One challenge at a time:
//   LEARN: Codey explains, the screen shows the idea and a small example; press the big button ("I'M READY").
//   TRY:   the code line with a blank; grab one of 3 code blocks, put it in the slot, press RUN.
//   UNDERSTAND: correct -> green, the output, Codey happy, the park changes;
//               wrong   -> red, why it is wrong, Codey sad, the block jumps out, try again.
// The slot is XRI's ready-made XR Socket Interactor; the blocks use XR Grab Interactable.
public class CodeStation : MonoBehaviour
{
    public enum Mode { Off, Learn, Try, Solved, Play }

    [Header("Screen texts")]
    public TextMeshPro header, body, example, codeLine, feedback;
    public Renderer screen;                       // the board: dark, green or red
    public Material screenIdle, screenGood, screenBad;

    [Header("Interaction")]
    public XRSocketInteractor slot;
    public CodeBlock[] blocks = new CodeBlock[3];
    public Transform[] blockSpots = new Transform[3];
    public PressButton bigButton;
    public TextMeshPro bigButtonLabel;

    [Header("Progress (made in Play if empty)")]
    public TextMeshPro progress;
    [HideInInspector] public bool managed;        // true: the LevelManager moves Codey (not this screen)

    [Header("Test without the LevelManager")]
    public int testChallenge = 0;                 // e.g. 1: this station starts with challenge 1 in Play

    public Mode mode { get; private set; } = Mode.Off;
    public Challenge current { get; private set; }
    public int wrongTries { get; private set; }
    public string lastWrong { get; private set; }          // the last wrong block (for "Why was I wrong?")

    // the LevelManager listens to these
    public event Action<CodeStation> ReadyPressed;
    public event Action<CodeStation, bool, string> RunFinished;     // correct?, the block used

    CodeBlock inSlot;

    void Awake()
    {
        if (progress == null)          // a line under the screen: how many steps are done and how many are left
        {
            var ui = header.transform.parent;
            var g = new GameObject("Progress"); g.transform.SetParent(ui, false);
            g.transform.localPosition = new Vector3(0, -0.54f, -0.05f);
            progress = g.AddComponent<TextMeshPro>();
            progress.alignment = TextAlignmentOptions.Center; progress.enableAutoSizing = true;
            progress.fontSizeMin = 0.1f; progress.fontSizeMax = 0.3f; progress.rectTransform.sizeDelta = new Vector2(1.8f, 0.07f);
            progress.color = new Color(0.05f, 0.25f, 0.22f); progress.fontStyle = FontStyles.Bold;
        }
        bigButton.onPress.AddListener(OnBigButton);
        slot.selectEntered.AddListener(a => { inSlot = a.interactableObject.transform.GetComponent<CodeBlock>(); ShowLineWith(inSlot != null ? inSlot.code : null); });
        slot.selectExited.AddListener(a => { if (mode == Mode.Try || mode == Mode.Play) { inSlot = null; ShowLineWith(null); } });
    }

    void Start()
    {
        if (testChallenge > 0) StartCoroutine(TestStart());
        else if (mode == Mode.Off) SetOff();
    }

#if UNITY_EDITOR
    // Testing in the Unity editor without a headset (only near this station):
    //   Enter = press the big button,  F1 / F2 / F3 = put block 1 / 2 / 3 into the slot.
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || Camera.main == null) return;
        if (Vector3.Distance(Camera.main.transform.position, transform.position) > 4f) return;
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) bigButton.Press();
        if (mode != Mode.Try && mode != Mode.Play) return;
        if (kb.f1Key.wasPressedThisFrame) PutInSlot(blocks[0]);
        if (kb.f2Key.wasPressedThisFrame) PutInSlot(blocks[1]);
        if (kb.f3Key.wasPressedThisFrame) PutInSlot(blocks[2]);
    }

    void PutInSlot(CodeBlock b)
    {
        var mgr = slot.interactionManager;
        if (mgr == null || b == null || !b.gameObject.activeSelf) return;
        if (inSlot != null && inSlot != b) mgr.SelectExit((IXRSelectInteractor)slot, (IXRSelectInteractable)inSlot.GetComponent<XRGrabInteractable>());
        mgr.SelectEnter((IXRSelectInteractor)slot, (IXRSelectInteractable)b.GetComponent<XRGrabInteractable>());
    }
#endif

    // ---------------- called by the LevelManager ----------------
    public void SetOff(string message = "This ride's code is broken.\nCome back later!")
    {
        mode = Mode.Off;
        Paint(screenIdle);
        header.text = ""; body.text = message; example.text = ""; codeLine.text = ""; feedback.text = ""; progress.text = "";
        foreach (var b in blocks) b.gameObject.SetActive(false);
        bigButton.gameObject.SetActive(false);
        slot.socketActive = false;
    }

    public void Show(Challenge c)
    {
        current = c; wrongTries = 0; lastWrong = null; mode = Mode.Learn;
        Paint(screenIdle);
        header.text = $"<color=#FFD54A>Level {c.level}</color>  ·  Challenge {c.id} of 20  ·  <b>{c.idea}</b>";
        SetProgress(c, false);
        body.text = "<color=#9EE493><b>The idea:</b></color>  " + ChallengeData.Highlight(c.learn, c);
        example.text = "<color=#9EE493>Example:</color>  " + Code(c.example) + "   <color=#BBBBBB>- </color>" + ChallengeData.Highlight(c.exampleSays, c);
        codeLine.text = "<size=60%><color=#FFD54A>Your task:</color> " + ChallengeData.Highlight(c.task, c) + "</size>";
        feedback.text = "<color=#AAAAAA>Listen to Codey, then press  I'M READY.</color>";
        foreach (var b in blocks) b.gameObject.SetActive(false);
        slot.socketActive = false;
        bigButton.gameObject.SetActive(true);
        bigButtonLabel.text = "I'M\nREADY";
        var codey = managed ? null : FindAnyObjectByType<RobotTeacher>();
        if (codey != null)
        {
            // Codey flies to the LEFT side of the screen, a little in front of it, so the player sees him
            codey.FlyTo(CodeyPoint());
            codey.Wave();
            codey.SayLines(ChallengeData.Lesson(c));     // idea -> example -> your task
        }
    }

    public void StartTry()
    {
        mode = Mode.Try;
        body.text = "<color=#FFD54A>Your task:</color> " + current.task + "\nPick the right block, put it in the <color=#FFD54A>yellow slot</color>, then press <b>RUN</b>.";
        example.text = "<color=#9EE493>Example:</color>  " + Code(current.example);
        ShowLineWith(null);
        feedback.text = "";
        // the three blocks in a random order, so the answer is not always in the same place
        string[] codes = { current.correct, current.wrongA, current.wrongB };
        for (int i = 0; i < 3; i++) { int j = UnityEngine.Random.Range(i, 3); (codes[i], codes[j]) = (codes[j], codes[i]); }
        for (int i = 0; i < 3; i++) blocks[i].Set(codes[i], blockSpots[i].position, blockSpots[i].rotation);
        slot.socketActive = true;
        bigButtonLabel.text = "RUN";
    }

    // ---------------- playground: no right or wrong, the player tries values and sees the result ----------------
    string playLine; Action<string> playRun;
    public void StartPlayground(string title, string text, string line, string[] options, Action<string> onRun)
    {
        mode = Mode.Play; playLine = line; playRun = onRun;
        Paint(screenGood);
        header.text = "<color=#FFD54A>Playground</color>  ·  <b>" + title + "</b>";
        body.text = text; example.text = ""; feedback.text = ""; progress.text = "";
        ShowLineWith(null);
        for (int i = 0; i < 3; i++) blocks[i].Set(options[i], blockSpots[i].position, blockSpots[i].rotation);
        slot.socketActive = true;
        bigButton.gameObject.SetActive(true);
        bigButtonLabel.text = "RUN";
    }

    // under the screen: ■■■□□  3 of 5 - 2 more steps until the ride works
    public void SetProgress(Challenge c, bool solvedThis)
    {
        if (progress == null || c == null) return;
        int inLevel = (c.id - 1) % 5;                    // challenges already done before this one
        int done = inLevel + (solvedThis ? 1 : 0);
        string bar = new string('■', done) + new string('□', 5 - done);
        int left = 5 - done;
        string what = c.level == 1 ? "the gate opens" : c.level == 4 ? "the big celebration" : ChallengeData.LevelNames[c.level].Replace("The ", "the ") + " works";
        progress.text = $"Level {c.level}:  {bar}   {done} of 5 done   -   " + (left == 0 ? "complete!" : $"{left} more step{(left > 1 ? "s" : "")} until {what}");
    }

    // the line under the code says that Codey (the AI) is preparing an answer
    Coroutine thinkLine;
    public void ShowThinking(bool on)
    {
        if (thinkLine != null) { StopCoroutine(thinkLine); thinkLine = null; }
        if (on) thinkLine = StartCoroutine(ThinkLine());
        else if (feedback.text.Contains("Codey is thinking")) feedback.text = "";
    }

    IEnumerator ThinkLine()
    {
        for (int n = 0; ; n++)
        {
            feedback.text = "<color=#7FE8FF>Codey is thinking " + new string('●', n % 4 + 1) + new string('○', 3 - n % 4) + "</color>";
            yield return new WaitForSeconds(0.35f);
        }
    }

    // ---------------- buttons ----------------
    void OnBigButton()
    {
        if (mode == Mode.Learn)
        {
            if (ReadyPressed != null) ReadyPressed(this); else StartTry();
        }
        else if (mode == Mode.Try || mode == Mode.Play) Run();
    }

    void Run()
    {
        if (mode == Mode.Play)
        {
            if (inSlot == null) { Feedback("Put a block in the yellow slot first.", false, false); return; }
            if (SoundFX.I != null) SoundFX.UI(SoundFX.I.run);
            playRun?.Invoke(inSlot.code);
            Feedback("You ran:  " + Code(playLine.Replace("___", inSlot.code)) + "   - try another one!", true, true);
            StartCoroutine(EjectSoon(inSlot));
            return;
        }
        if (inSlot == null)
        {
            Feedback("Put a code block in the yellow slot first.", false, false);
            return;
        }
        if (SoundFX.I != null) SoundFX.UI(SoundFX.I.run);
        bool ok = inSlot.code == current.correct;
        var codey = FindAnyObjectByType<RobotTeacher>();
        if (ok)
        {
            mode = Mode.Solved;
            inSlot.Lock();
            foreach (var b in blocks) if (b != inSlot) b.gameObject.SetActive(false);
            slot.socketActive = true;
            Paint(screenGood);
            Feedback("<b>Correct!</b>  " + Output(), true, true);
            SetProgress(current, true);
            bigButtonLabel.text = "DONE";
            if (codey != null) { codey.Happy(); codey.Say(wrongTries == 0 ? "Perfect, first try! Look at the park!" : "Yes! You fixed it! Look at the park!", 4f); }
        }
        else
        {
            wrongTries++;
            lastWrong = inSlot.code;
            Paint(screenBad);
            string why = inSlot.code == current.wrongA ? current.whyA : current.whyB;
            Feedback("<b>Not yet.</b>  " + ChallengeData.Highlight(why, current), false, true);
            if (codey != null) { codey.Sad(); codey.Say(ChallengeData.Highlight(why, current) + " Try again!", 6f); }
            StartCoroutine(EjectSoon(inSlot));
        }
        if (RunFinished != null) RunFinished(this, ok, inSlot != null ? inSlot.code : "");
        else if (ok) StartCoroutine(TestNext());
    }

    IEnumerator EjectSoon(CodeBlock b)
    {
        yield return new WaitForSeconds(1.2f);
        if ((mode != Mode.Try && mode != Mode.Play) || b == null) yield break;
        var grab = b.GetComponent<XRGrabInteractable>();
        if (grab.isSelected && slot.interactionManager != null)
            slot.interactionManager.SelectExit((IXRSelectInteractor)slot, (IXRSelectInteractable)grab);
        Paint(screenIdle);
    }

    // when testing without a LevelManager: Codey introduces himself first (challenge 1 only)
    IEnumerator TestStart()
    {
        SetOff("Codey is coming...");
        var codey = FindAnyObjectByType<RobotTeacher>();
        if (codey != null && testChallenge == 1) yield return codey.Introduce(CodeyPoint());
        Show(ChallengeData.Get(testChallenge));
    }

    [Tooltip("Codey stands on the RIGHT of this screen (otherwise on the left).")]
    public bool codeyOnRight;
    public Vector3 CodeyPoint() => transform.TransformPoint(new Vector3(codeyOnRight ? 2.3f : -2.3f, 0f, -0.3f));   // beside the screen (left), not in the player's face

    // when testing without a LevelManager: go to the next challenge of the same level
    IEnumerator TestNext()
    {
        yield return new WaitForSeconds(4f);
        var next = ChallengeData.Get(current.id + 1);
        if (next != null && next.level == current.level) Show(next);
        else SetOff("Level " + current.level + " fixed! Well done!");
    }

    // ---------------- helpers ----------------
    void ShowLineWith(string block)
    {
        if (mode == Mode.Play) { codeLine.text = Code(playLine).Replace("___", block == null ? "<color=#FFD54A><u>   ?   </u></color>" : "<b><u>" + CodeColors.Color(block) + "</u></b>"); return; }
        if (current == null || mode == Mode.Off || mode == Mode.Learn) return;
        string fill = block == null ? "<color=#FFD54A><u>      ?      </u></color>" : "<b><u>" + CodeColors.Color(block) + "</u></b>";
        codeLine.text = Code(current.line).Replace("___", fill);
    }

    // what the program prints, for the output lines (so the player sees the result)
    string Output()
    {
        switch (current.id)
        {
            case 1: return "Output:  <color=#FFFFFF>CODE THE PARK</color>";
            case 2: return "Output:  <color=#FFFFFF>2026</color>";
            case 3: return "Output:  <color=#FFFFFF>Code</color>  (new line)  <color=#FFFFFF>Park</color>";
            default: return "";
        }
    }

    void Feedback(string text, bool good, bool sound)
    {
        feedback.text = (good ? "<color=#7CFF8A>" : "<color=#FF8A80>") + text + "</color>";
        if (sound && SoundFX.I != null) SoundFX.UI(good ? SoundFX.I.correct : SoundFX.I.wrong);
    }

    void Paint(Material m) { if (screen != null && m != null) screen.sharedMaterial = m; }

    // a little colour for code, like a code editor
    static string Code(string s) => "<mspace=0.55em>" + CodeColors.Color(s) + "</mspace>";
    static string Esc(string s) => s.Replace("<", "<noparse><</noparse>");
}
