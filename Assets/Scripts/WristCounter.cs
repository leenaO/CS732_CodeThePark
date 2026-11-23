using System.Collections;
using TMPro;
using UnityEngine;

// Code the Park - written by us.
// The counter on the LEFT wrist (not in front of the eyes, for comfort).
// Level 1: small icons + numbers. After the player learns variables (int, ++),
// the same counter is shown as C++ code, and a pickup shows "coins++;".
public class WristCounter : MonoBehaviour
{
    public TextMeshPro coinText, gemText, toolText;   // numbers next to the 3 mini icons
    public GameObject iconsGroup;                      // icons + numbers
    public TextMeshPro codeText;                       // the C++ view
    public TextMeshPro popText;                        // short "+1" / "coins++;" message

    Inventory inv;
    bool lastCodeMode;
    TextMeshPro timeText;             // time played so far, made in Play

    void Update()
    {
        if (timeText == null)
        {
            var g = new GameObject("TimeLeft"); g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0, -0.06f, -0.002f);
            timeText = g.AddComponent<TextMeshPro>();
            timeText.alignment = TextAlignmentOptions.Center; timeText.fontSize = 0.22f; timeText.rectTransform.sizeDelta = new Vector2(0.14f, 0.03f);
        }
        var lm = LevelManager.I;
        if (lm == null) { timeText.text = ""; return; }
        float t = lm.TimePlayed;                                // time played so far (no time limit)
        timeText.text = $"<color=#BBBBBB>time  {(int)t / 60:00}:{(int)t % 60:00}</color>";
    }

    void Start()
    {
        inv = Inventory.Instance;
        if (inv != null) { inv.Changed += Refresh; inv.Collected += Pop; }
        if (popText != null) popText.text = "";
        Refresh();
    }

    void OnDestroy()
    {
        if (inv != null) { inv.Changed -= Refresh; inv.Collected -= Pop; }
    }

    void Refresh()
    {
        if (inv == null) return;
        bool code = inv.learnedVariables;
        iconsGroup.SetActive(!code);
        codeText.gameObject.SetActive(code);
        coinText.text = inv.coins.ToString();
        gemText.text = inv.gems.ToString();
        toolText.text = inv.tools.ToString();
        codeText.text =
            "<color=#7FD7FF>int</color> coins = <color=#FFD54A>" + inv.coins + "</color>;\n" +
            "<color=#7FD7FF>int</color> gems  = <color=#FFD54A>" + inv.gems + "</color>;\n" +
            "<color=#7FD7FF>int</color> tools = <color=#FFD54A>" + inv.tools + "</color>;";
        if (code && !lastCodeMode)
        {
            var codey = FindAnyObjectByType<RobotTeacher>();
            if (codey != null) codey.Say("Look at your wrist! Your coins are now a variable, like you learned!", 5f);
        }
        lastCodeMode = code;
    }

    void Pop(Inventory.Kind k)
    {
        if (popText == null) return;
        string name = k == Inventory.Kind.Coin ? "coins" : k == Inventory.Kind.Gem ? "gems" : "tools";
        string label = k == Inventory.Kind.Coin ? "+1 coin" : k == Inventory.Kind.Gem ? "+1 gem!" : "+1 Fix Tool";
        popText.text = inv.learnedVariables ? name + "++;" : label;
        StopAllCoroutines();
        StartCoroutine(HidePop());
    }

    IEnumerator HidePop()
    {
        yield return new WaitForSeconds(1.5f);
        popText.text = "";
    }
}
