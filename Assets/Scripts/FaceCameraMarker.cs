using UnityEngine;

// Code the Park - written by us.
// Keeps a floating sign turned toward the player's head, so it is always readable.
public class FaceCameraMarker : MonoBehaviour
{
    Transform cam;

    void LateUpdate()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null) return;
        Vector3 dir = transform.position - cam.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
    }
}
