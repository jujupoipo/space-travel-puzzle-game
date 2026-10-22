using System.Collections.Generic;
using UnityEngine;

public class DreamPuzzleController : MonoBehaviour
{
    public Transform player;
    public GravitySource planetGravity;
    [Range(0.4f, 1f)] public float ruinGravityMultiplier = 0.7f;
    public float interactionDistance = 2.8f;

    private readonly List<DreamLantern> lanterns = new List<DreamLantern>();
    private readonly List<DreamPhaseGate> gates = new List<DreamPhaseGate>();
    private Vector3 exitPosition;
    private bool initialized;
    private bool isBloom = true;
    private int memoriesCollected;
    private bool finished;
    private string notice = "find the moon, petal and star memories";
    private float noticeUntil;

    public bool IsBloom { get { return isBloom; } }
    public int MemoriesCollected { get { return memoriesCollected; } }
    public int MemoryCount { get { return lanterns.Count; } }
    public bool Finished { get { return finished; } }
    public bool Ready { get { return initialized; } }
    public string Notice { get { return Time.time < noticeUntil ? notice : ""; } }

    public void Initialize(List<DreamLantern> createdLanterns, List<DreamPhaseGate> createdGates, Vector3 exit)
    {
        lanterns.Clear();
        lanterns.AddRange(createdLanterns);
        lanterns.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));
        gates.Clear();
        gates.AddRange(createdGates);
        exitPosition = exit;
        initialized = true;
        SetPhase(true);
        ShowNotice("the inscription reads: moon > petal > star", 7f);
    }

    private void Update()
    {
        if (!initialized || finished || player == null) return;

        // q changes which dream paths are open
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetPhase(!isBloom);
            ShowNotice(isBloom ? "the garden is blooming" : "the ruins are awake", 2.5f);
        }

        if (Input.GetKeyDown(KeyCode.E)) TryInteract();
    }

    private void SetPhase(bool bloom)
    {
        isBloom = bloom;
        if (planetGravity != null)
        {
            // the ruin phase has softer gravity and longer jumps
            planetGravity.rule = isBloom ? GravitySource.Rule.Normal : GravitySource.Rule.Low;
            planetGravity.lowGravityMultiplier = ruinGravityMultiplier;
        }
        foreach (DreamPhaseGate gate in gates)
            if (gate != null) gate.ApplyPhase(isBloom);
    }

    private void TryInteract()
    {
        if (memoriesCollected == lanterns.Count)
        {
            if (Vector3.Distance(player.position, exitPosition) < interactionDistance + 1f)
            {
                finished = true;
                DreamProgression.MarkPlanet2Solved();
                ShowNotice("all the memories have returned", 10000f);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
        }

        DreamLantern nearby = GetNearbyLantern();
        if (nearby == null) return;
        if (nearby.Activated)
        {
            ShowNotice("this memory is already restored", 2f);
            return;
        }

        if (nearby.orderIndex != memoriesCollected)
        {
            ShowNotice("wrong order. remember: moon > petal > star", 3f);
            return;
        }

        // each memory belongs to a particular dream phase
        bool requiredBloom = nearby.orderIndex != 1;
        if (requiredBloom != isBloom)
        {
            ShowNotice(requiredBloom ? "return to the bloom dream (q)" : "enter the ruin dream (q)", 3f);
            return;
        }

        nearby.Activate();
        memoriesCollected++;
        ShowNotice(memoriesCollected == lanterns.Count
            ? "the castle portal is now open. return to the glowing arch"
            : nearby.memoryName + " memory restored!", 5f);
    }

    private DreamLantern GetNearbyLantern()
    {
        DreamLantern nearest = null;
        float best = interactionDistance * interactionDistance;
        foreach (DreamLantern lantern in lanterns)
        {
            if (lantern == null) continue;
            float d = (lantern.transform.position - player.position).sqrMagnitude;
            if (d < best) { best = d; nearest = lantern; }
        }
        return nearest;
    }

    public string GetInteractionHint()
    {
        if (!initialized || finished || player == null) return "";
        if (memoriesCollected == lanterns.Count &&
            Vector3.Distance(player.position, exitPosition) < interactionDistance + 1f)
            return "press e to finish the dream";
        DreamLantern lantern = GetNearbyLantern();
        if (lantern != null) return "press e: " + lantern.memoryName + " memory";
        return "";
    }

    private void ShowNotice(string value, float duration)
    {
        notice = value;
        noticeUntil = Time.time + duration;
    }
}
