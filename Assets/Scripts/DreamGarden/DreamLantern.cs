using UnityEngine;

public class DreamLantern : MonoBehaviour
{
    public int orderIndex;
    public string memoryName;
    public Renderer orbRenderer;

    private bool activated;
    public bool Activated { get { return activated; } }

    public void Activate()
    {
        activated = true;
        if (orbRenderer != null)
        {
            Material material = orbRenderer.material;
            Color color = new Color(1f, 0.88f, 0.55f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.7f);
            }
        }
    }
}
