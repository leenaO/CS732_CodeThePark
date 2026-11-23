using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Code the Park - written by us (based on our Lab 7 LLM client).
// The AI part of Codey. Next to every code screen there are 3 buttons:
//    "Explain again"  -  "Give me a hint"  -  "Why was I wrong?"
// Unity sends the current challenge (idea, example, line, last wrong block, progress) to OUR backend
// (Backend/codey_backend.py), which asks a LOCAL model (Ollama, llama3.2) - no API key anywhere.
// FALLBACK: if the backend does not answer in time (or the answer gives away the solution),
// Codey uses the prepared text from ChallengeData (explanation, hint, why it is wrong).
public class CodeyAI : MonoBehaviour
{
    [Header("Backend (Quest 3: use the laptop's Wi-Fi address, e.g. http://192.168.1.20:8000/ask)")]
    public string backendUrl = "http://127.0.0.1:8000/ask";
    public int timeoutSeconds = 30;
    public Material buttonMat, buttonOffMat;

    [Serializable] class Req { public string mode, idea, learn, example, line, lastAnswer, reason, progress, question, correct; public int wrongTries; }
    [Serializable] class Res { public string answer; }

    RobotTeacher codey;
    bool busy;
    public int aiAnswers, fallbacks;     // for the report: how often the AI answered, how often the fallback was used

    void Start()
    {
        codey = FindAnyObjectByType<RobotTeacher>();
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var st in root.GetComponentsInChildren<CodeStation>(true)) MakeButtons(st);
    }

#if UNITY_EDITOR
    // testing in the editor near a screen:  F10 = Explain again,  F11 = Give me a hint,  F12 = Why was I wrong?
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || Camera.main == null) return;
        string mode = kb.f10Key.wasPressedThisFrame ? "explain" : kb.f11Key.wasPressedThisFrame ? "hint" : kb.f12Key.wasPressedThisFrame ? "why" : null;
        if (mode == null) return;
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var st in root.GetComponentsInChildren<CodeStation>())
                if (Vector3.Distance(Camera.main.transform.position, st.transform.position) < 4f) { Ask(st, mode); return; }
    }
#endif

    // ---------------- the 3 buttons beside each screen (on Codey's side) ----------------
    void MakeButtons(CodeStation st)
    {
        var holder = new GameObject("AskCodey");
        holder.transform.SetParent(st.transform, false);
        float side = st.codeyOnRight ? 1f : -1f;
        holder.transform.localPosition = new Vector3(side * 1.12f, 0, -0.12f);
        string[] labels = { "Explain\nagain", "Give me\na hint", "Why was\nI wrong?" };
        string[] modes = { "explain", "hint", "why" };
        for (int i = 0; i < 3; i++)
        {
            string mode = modes[i];
            var b = new GameObject("Ask_" + mode);
            b.transform.SetParent(holder.transform, false);
            b.transform.localPosition = new Vector3(0, 1.62f - i * 0.22f, 0);
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(cap.GetComponent<Collider>());
            cap.transform.SetParent(b.transform, false);
            cap.transform.localRotation = Quaternion.Euler(90, 0, 0);          // the round face looks at the player
            cap.transform.localScale = new Vector3(0.11f, 0.015f, 0.11f);
            cap.GetComponent<Renderer>().sharedMaterial = buttonMat;
            var t = new GameObject("Label").AddComponent<TextMeshPro>();
            t.transform.SetParent(b.transform, false);
            t.transform.localPosition = new Vector3(side * 0.17f, 0, -0.01f);
            t.text = labels[i]; t.fontSize = 0.32f; t.color = Color.white;
            t.alignment = side > 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            t.rectTransform.sizeDelta = new Vector2(0.22f, 0.14f);
            t.rectTransform.pivot = new Vector2(side > 0 ? 0f : 1f, 0.5f);
            t.transform.localPosition = new Vector3(side * 0.07f, 0, -0.01f);
            var col = b.AddComponent<SphereCollider>(); col.radius = 0.07f;
            b.AddComponent<XRSimpleInteractable>();
            var pb = b.AddComponent<PressButton>(); pb.cap = cap.transform; pb.cooldown = 1.5f;
            pb.onPress.AddListener(() => Ask(st, mode));
        }
    }

    // ---------------- ask ----------------
    public void Ask(CodeStation st, string mode)
    {
        var c = st.current;
        if (c == null || st.mode == CodeStation.Mode.Off || st.mode == CodeStation.Mode.Play)
        { Say("First let's start a challenge on this screen!"); return; }
        if (mode == "why" && string.IsNullOrEmpty(st.lastWrong))
        { Say("You have not made a mistake yet. Try a block first!"); return; }
        if (busy) return;
        if (mode == "hint" && LevelManager.I != null) LevelManager.I.hintsUsed++;

        string reason = st.lastWrong == c.wrongA ? c.whyA : st.lastWrong == c.wrongB ? c.whyB : "";
        string fallback = mode == "explain" ? c.learn + " " + c.exampleSays
                        : mode == "hint" ? "Hint: " + c.hint
                        : reason;
        var req = new Req {
            mode = mode, idea = c.idea, learn = c.learn, example = c.example, line = c.line,
            lastAnswer = st.lastWrong ?? "", reason = reason, correct = c.correct, wrongTries = st.wrongTries,
            progress = "Level " + c.level + ", challenge " + c.id + " of 20", question = ""
        };
        StartCoroutine(Send(req, fallback, c, st));
    }

    IEnumerator Send(Req req, string fallback, Challenge c, CodeStation st)
    {
        busy = true;
        if (codey != null) codey.Thinking(true);              // moving dots in Codey's bubble ...
        st.ShowThinking(true);                                 // ... and on the screen, until the answer comes
        string answer = null;
        if (!string.IsNullOrWhiteSpace(backendUrl))
        {
            using (var r = new UnityWebRequest(backendUrl, "POST"))
            {
                r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(req)));
                r.downloadHandler = new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                r.timeout = timeoutSeconds;
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success)
                {
                    try { var res = JsonUtility.FromJson<Res>(r.downloadHandler.text); if (res != null && !string.IsNullOrWhiteSpace(res.answer)) answer = res.answer; }
                    catch (Exception) { answer = null; }
                }
                else Debug.Log("CodeyAI: backend not reached (" + r.error + ") - using the prepared answer.");
            }
        }
        if (codey != null) codey.Thinking(false);
        st.ShowThinking(false);
        if (answer != null) aiAnswers++; else { fallbacks++; answer = fallback; }
        Say(ChallengeData.Highlight(answer, c));
        busy = false;
    }

    void Say(string text) { if (codey != null) codey.SayLines(new[] { text }); }
}
