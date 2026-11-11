using UnityEngine;

// identifies and lights the final arch after the three guarded memories
public class DreamExitMarker : MonoBehaviour
{
    public DreamPuzzleController puzzle;
    public Renderer beaconRenderer;
    private bool unlocked;
    private MaterialPropertyBlock block;
    private void Update()
    {
        if (unlocked || puzzle == null || puzzle.MemoriesCollected != 3 || beaconRenderer == null) return;
        unlocked = true;
        block = new MaterialPropertyBlock();
        Color color = new Color(1f, .8f, .35f);
        block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", color * 3f);
        beaconRenderer.SetPropertyBlock(block);
    }
}
