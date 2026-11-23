using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Code the Park - written by us.
// Turns on FIXED FOVEATED RENDERING on the Quest: the centre of each eye is drawn at full quality,
// only the far edges of the view (which the eye sees blurry anyway) are drawn with fewer pixels.
// This lowers the work of the graphics chip (GPU). The OpenXR "Foveated Rendering" feature must be on
// (menu CS732 > Optimize For Quest). Level 0 = off, 1 = strongest. We use a medium level.
public class QuestPerformance : MonoBehaviour
{
    [Range(0f, 1f)] public float foveationLevel = 0.5f;
    float lastSet = -1f;

    void Update()
    {
        if (Mathf.Approximately(lastSet, foveationLevel)) return;   // the XR display may start a moment after the scene
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        if (displays.Count == 0) return;
        foreach (var d in displays)
        {
            d.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;  // fixed (Quest 3 has no eye tracking)
            d.foveatedRenderingLevel = foveationLevel;
        }
        lastSet = foveationLevel;
        Debug.Log("CS732 performance: foveated rendering level " + foveationLevel);
    }
}
