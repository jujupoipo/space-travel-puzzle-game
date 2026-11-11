using UnityEngine;

public class DreamPhaseGate : MonoBehaviour
{
    public bool openInBloom;
    public GameObject blockingWall;
    public Transform player;
    private bool shouldBlock;
    public void ApplyPhase(bool isBloom) { shouldBlock = openInBloom != isBloom; RefreshWall(); }
    private void Update()
    {
        if (blockingWall != null && blockingWall.activeSelf != shouldBlock) RefreshWall();
    }
    private void RefreshWall()
    {
        if (blockingWall == null) return;
        if (shouldBlock && player != null)
        {
            // Works while the wall is disabled;
            BoxCollider box = blockingWall.GetComponent<BoxCollider>();
            if (box != null)
            {
                Vector3 p = box.transform.InverseTransformPoint(player.position) - box.center;
                Vector3 scale = box.transform.lossyScale;
                Vector3 margin = new Vector3(.85f / Mathf.Abs(scale.x), 1.3f / Mathf.Abs(scale.y), .85f / Mathf.Abs(scale.z));
                Vector3 half = box.size * .5f + margin;
                if (Mathf.Abs(p.x) < half.x && Mathf.Abs(p.y) < half.y && Mathf.Abs(p.z) < half.z) return;
            }
        }
        blockingWall.SetActive(shouldBlock);
    }
}
