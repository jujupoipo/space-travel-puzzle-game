using UnityEngine;

// data for nearby interactions, dispatched by the existing puzzle controller
public sealed class DreamInteraction : MonoBehaviour
{
    public enum Kind { Keeper, Petal, Astral, Inscription }
    public Kind kind;
    public int index;
    public string title;
    [TextArea(2, 6)] public string inscription;
    public Renderer indicator;
    private MaterialPropertyBlock block;

    public Vector3 Focus => transform.position + transform.up * 1.1f;

    public void Refresh(bool bloom)
    {
        if (indicator == null) return;
        bool lit = kind == Kind.Keeper ? DreamProgression.KeeperRemembered :
            kind == Kind.Petal ? DreamProgression.PetalSteps > index :
            kind == Kind.Astral && DreamProgression.AstralLit(index);
        bool available = kind != Kind.Petal || bloom == (index == 1);
        Color color = lit ? new Color(1f, .76f, .3f) :
            available ? new Color(.46f, .65f, .8f) : new Color(.22f, .18f, .3f);
        if (block == null) block = new MaterialPropertyBlock();
        indicator.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", color * (lit ? 2f : .12f));
        indicator.SetPropertyBlock(block);
    }
}
