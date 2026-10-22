using UnityEngine;

public class DreamPhaseGate : MonoBehaviour
{
    public bool openInBloom;
    public GameObject blockingWall;
    public Transform player;

    private bool shouldBlock;

    public void ApplyPhase(bool isBloom)
    {
        shouldBlock = openInBloom != isBloom;
        RefreshWall();
    }

    private void Update()
    {
        if (blockingWall != null && blockingWall.activeSelf != shouldBlock)
            RefreshWall();
    }

    private void RefreshWall()
    {
        if (blockingWall == null) return;

        // do not close a wall while the player is inside it
        if (shouldBlock && player != null &&
            Vector3.Distance(player.position, blockingWall.transform.position) < 2.3f)
            return;

        blockingWall.SetActive(shouldBlock);
    }
}
