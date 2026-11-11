using UnityEngine;

public class DreamLantern : MonoBehaviour
{
    public int orderIndex;
    public string memoryName;
    public Renderer orbRenderer;
    public Light memoryLight;
    private MaterialPropertyBlock block;
    private bool activated;
    public bool Activated => activated;
    public void Activate() => SetRestored(true);
    public void SetRestored(bool restored)
    {
        activated = restored;
        if (orbRenderer == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        Color color = restored ? new Color(1, .88f, .55f) :
            (orderIndex == 1 ? new Color(.92f, .62f, .78f) : new Color(.65f, .62f, .9f));
        orbRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", color * (restored ? 2.4f : .12f));
        orbRenderer.SetPropertyBlock(block);
        if (memoryLight != null) { memoryLight.color = color; memoryLight.intensity = restored ? 2.2f : .15f; }
    }
}
