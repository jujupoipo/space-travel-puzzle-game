using UnityEngine;

// cache the view once; labels stay legible when approaching from either corridor
public sealed class DreamWorldLabel : MonoBehaviour
{
    private Camera view;
    private void Start() => view = Camera.main;
    private void LateUpdate()
    {
        if (view == null) return;
        Vector3 forward = transform.position - view.transform.position;
        if (forward.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(forward, view.transform.up);
    }
}
