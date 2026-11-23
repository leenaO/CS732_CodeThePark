using System;
using UnityEngine;

// Code the Park - written by us.
// Keeps count of what the player collected: coins, gems and Fix Tools.
// It also remembers which C++ ideas the player has already learned, so the wrist
// counter and the ride locks only show code AFTER the idea was taught.
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public enum Kind { Coin, Gem, Tool }

    [Header("Counts")]
    public int coins, gems, tools;
    public int coinsThisLevel;          // coins found in the current level (3 open the next ride)

    [Header("What the player has learned (set by the challenge system)")]
    public bool learnedVariables;       // after int and ++ (level 2): counter shows code
    public bool learnedIf;              // after if (level 3): ride locks show if (...)
    public bool learnedFor;             // after for (level 4): results use a for loop

    [Header("Level")]
    public int currentLevel = 1;

    public event Action<Kind> Collected;    // the wrist counter and Codey listen to this
    public event Action Changed;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowLevelItems(currentLevel);
    }

    readonly int[] coinsByLevel = new int[6];
    public int CoinsOfLevel(int level) => level >= 0 && level < coinsByLevel.Length ? coinsByLevel[level] : 0;

    public void Add(Kind k, int level = 0)
    {
        if (k == Kind.Coin) { coins++; coinsThisLevel++; if (level >= 0 && level < coinsByLevel.Length) coinsByLevel[level]++; }
        else if (k == Kind.Gem) gems++;
        else tools++;
        Collected?.Invoke(k);
        Changed?.Invoke();
    }

    // (not used now: Fix Tools are kept - each ride needs one to open its machine)
    public bool UseTool()
    {
        if (tools <= 0) return false;
        tools--;
        Changed?.Invoke();
        return true;
    }

    public bool HasCoinsForNextRide => coinsThisLevel >= 3;

    public void StartLevel(int level)
    {
        currentLevel = level;
        coinsThisLevel = 0;
        ShowLevelItems(level);
        Changed?.Invoke();
    }

    public void SetLearned(bool variables, bool ifs, bool loops)
    {
        learnedVariables |= variables; learnedIf |= ifs; learnedFor |= loops;
        Changed?.Invoke();
    }

    // only the items of the levels the player has reached are visible
    void ShowLevelItems(int level)
    {
        // look through the whole scene, including hidden items
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<Collectible>(true))
                if (!c.taken) c.gameObject.SetActive(c.level <= level);
    }
}
