using System.Collections.Generic;
using UnityEngine;

public class DreamPuzzleController : MonoBehaviour
{
    public Transform player;
    public GravitySource planetGravity;
    [Range(.4f, 1f)] public float ruinGravityMultiplier = .7f;
    public float interactionDistance = 2.8f;
    public string firstPlanetScene = "JuliaPlanet1";

    private readonly List<DreamLantern> lanterns = new List<DreamLantern>();
    private readonly List<DreamPhaseGate> gates = new List<DreamPhaseGate>();
    private readonly List<DreamAnchor> anchors = new List<DreamAnchor>();
    private readonly List<DreamInteraction> interactions = new List<DreamInteraction>();
    private DreamDialogue dialogue;
    private DreamGardenAtmosphere atmosphere;
    private bool checkpointBloom;
    public bool DialogueOpen => dialogue != null && dialogue.IsOpen;
    public string ChallengeHint => memoriesCollected == 0 ? "Find the Keeper before restoring Moon." :
        memoriesCollected == 1 ? "Garden seals: " + DreamProgression.PetalSteps + " / 3" :
        memoriesCollected == 2 ? "Astral sequence: " + DreamProgression.AstralSteps + " / 4" : "The final arch is awake.";
    private Vector3 exitPosition;
    private Vector3 checkpoint;
    private Quaternion checkpointRotation;
    private bool initialized;
    private bool checkpointReady;
    private readonly RaycastHit[] visibilityHits = new RaycastHit[48];
    private bool isBloom = true;
    private int memoriesCollected;
    private bool finished;
    private string notice;
    private float noticeUntil;
    public bool IsBloom => isBloom;
    public int MemoriesCollected => memoriesCollected;
    public int MemoryCount => lanterns.Count;
    public bool Finished => finished;
    public bool Ready => initialized;
    public string Notice => Time.unscaledTime < noticeUntil ? notice : "";
    public Vector3 ExitPosition => exitPosition;
    public bool CanShift => initialized && NearbyAnchor() != null;
    public bool PlayerInGarden => player != null && planetGravity != null
        && GravitySource.GetBest(player.position) == planetGravity;
    public string NextMemory => memoriesCollected < 3
        ? new[] { "Moon / Bloom", "Petal / Ruin", "Star / Bloom" }[memoriesCollected]
        : "Castle archway";

    public void Initialize(List<DreamLantern> createdLanterns, List<DreamPhaseGate> createdGates, Vector3 exit)
    {
        initialized = false;
        lanterns.Clear();
        gates.Clear();
        anchors.Clear();
        if (createdLanterns != null)
            foreach (DreamLantern lantern in createdLanterns)
                if (lantern != null) lanterns.Add(lantern);
        if (createdGates != null)
            foreach (DreamPhaseGate gate in createdGates)
                if (gate != null) gates.Add(gate);
        lanterns.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));
        if (lanterns.Count != 3)
        {
            Debug.LogError("The garden requires exactly three memories.", this);
            return;
        }
        Transform garden = lanterns[0].transform.parent;
        interactions.Clear();
        if (garden != null)
        {
            anchors.AddRange(garden.GetComponentsInChildren<DreamAnchor>(true));
            interactions.AddRange(garden.GetComponentsInChildren<DreamInteraction>(true));
            atmosphere = garden.GetComponent<DreamGardenAtmosphere>();
        }
        dialogue = GetComponent<DreamDialogue>();
        if (dialogue == null) dialogue = gameObject.AddComponent<DreamDialogue>();
        foreach (DreamPhaseGate gate in gates) gate.player = player;
        exitPosition = exit;
        memoriesCollected = DreamProgression.Memories;
        finished = DreamProgression.Planet2Solved;
        for (int i = 0; i < lanterns.Count; i++) lanterns[i].SetRestored(i < memoriesCollected);
        initialized = true;
        checkpointReady = false;
        UpdateInitialCheckpoint();
        SetPhase(true);
        ShowNotice("Moon awakens in Bloom. Petal dreams in Ruin. Star shines in Bloom once more.", 8);
        if (finished && DreamProgression.GameCompleted) LockAtEnding();
    }

    private void Update()
    {
        if (!initialized || player == null || DreamSceneFlow.Busy || DialogueOpen || !PlayerInGarden) return;
        UpdateInitialCheckpoint();
        if (finished) return;
        if (Input.GetKeyDown(KeyCode.Q)) TryShift();
        if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        if (Input.GetKeyDown(KeyCode.R)) ReturnToCheckpoint();
    }
    public bool TryShift()
    {
        if (!initialized || finished || DreamSceneFlow.Busy || DialogueOpen || !PlayerInGarden) return false;
        if (!CanShift) { ShowNotice("Approach a pedestal to shift between Bloom and Ruin.", 3); return false; }
        SetPhase(!isBloom);
        ShowNotice(isBloom ? "Bloom: the rose-colored paths awaken." : "Ruin: the violet passages awaken.", 3);
        return true;
    }
    private void SetPhase(bool bloom)
    {
        isBloom = bloom;
        if (planetGravity != null)
        {
            planetGravity.rule = bloom ? GravitySource.Rule.Normal : GravitySource.Rule.Low;
            planetGravity.lowGravityMultiplier = ruinGravityMultiplier;
        }
        foreach (DreamPhaseGate gate in gates) if (gate != null) gate.ApplyPhase(bloom);
        foreach (DreamInteraction interaction in interactions) if (interaction != null) interaction.Refresh(bloom);
        if (atmosphere != null) atmosphere.SetPhase(bloom);
    }
    public bool TryInteract()
    {
        if (!initialized || finished || player == null || DreamSceneFlow.Busy || DialogueOpen || !PlayerInGarden) return false;
        if (memoriesCollected == 3 && AtExit())
        {
            finished = true;
            DreamProgression.MarkPlanet2Solved();
            if (DreamProgression.GameCompleted) LockAtEnding();
            else ShowNotice("Planet 2 complete! Return to Planet 1 to finish your journey.", 6);
            return true;
        }
        DreamInteraction mechanism = GetNearbyInteraction();
        if (mechanism != null) return UseMechanism(mechanism);
        DreamLantern nearby = GetNearbyLantern();
        if (nearby == null) return false;
        if (nearby.Activated) { ShowNotice("This memory is already restored. Press Q near a pedestal to shift the dream.", 3); return false; }
        if (nearby.orderIndex != memoriesCollected) { ShowNotice("Restore the memories in order: Moon > Petal > Star. Your progress is safe.", 4); return false; }
        bool requiredBloom = nearby.orderIndex != 1;
        if (requiredBloom != isBloom)
        {
            ShowNotice(requiredBloom ? "This memory belongs to Bloom. Press Q near a pedestal." : "This memory belongs to Ruin. Press Q near a pedestal.", 4);
            return false;
        }
        if (!DreamProgression.ChallengeComplete(nearby.orderIndex))
        {
            ShowNotice(nearby.orderIndex == 0 ? "Speak to the Forgotten Keeper and name what the moon has lost." :
                nearby.orderIndex == 1 ? "Wake Root in Ruin, Dew in Bloom, then Blossom in Ruin." :
                "Read the pilgrim's inscription. Light the four astral symbols in order, in Bloom.", 6);
            return false;
        }
        nearby.Activate();
        memoriesCollected++;
        DreamProgression.RestoreMemory(memoriesCollected);
        // keep the checkpoint at the actual player position, not inside a wall
        checkpoint = player.position + player.up * 0.15f;
        checkpointRotation = player.rotation;
        checkpointBloom = isBloom;
        checkpointReady = true;
        ShowNotice(memoriesCollected == 3 ? "All three memories are restored. Find the castle archway." :
            nearby.memoryName + " restored. Next: " + NextMemory + ".", 5);
        return true;
    }
    private void LockAtEnding()
    {
        if (player != null)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true; }
        }
        // do not disable cameras belonging to other players in the same scene
        foreach (PlanetCamera camera in FindObjectsByType<PlanetCamera>())
            if (camera.target == player) camera.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void ReturnToCheckpoint()
    {
        if (player == null || finished || DialogueOpen || DreamSceneFlow.Busy) return;
        if (!PlayerInGarden || !checkpointReady)
        {
            ShowNotice("Enter the Garden of Memories before using a checkpoint.", 3);
            return;
        }
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = checkpoint;
            body.rotation = checkpointRotation;
        }
        player.SetPositionAndRotation(checkpoint, checkpointRotation);
        Physics.SyncTransforms();
        SetPhase(checkpointBloom);
        ShowNotice("Returned to the last checkpoint. Your memories are preserved.", 3);
    }
    private void UpdateInitialCheckpoint()
    {
        if (checkpointReady || !PlayerInGarden) return;
        // do not save a checkpoint in empty space far from the planet surface
        float radius = 45f;
        CubeSphereGenerator sphere = planetGravity.GetComponent<CubeSphereGenerator>();
        if (sphere != null) radius = sphere.radius;
        float distance = Vector3.Distance(player.position, planetGravity.transform.position);
        if (Mathf.Abs(distance - radius) > 12f) return;
        checkpoint = player.position + player.up * 0.15f;
        checkpointRotation = player.rotation;
        checkpointBloom = isBloom;
        checkpointReady = true;
    }

    private bool Visible(Transform item, Vector3 point)
    {
        if (player == null) return false;
        Vector3 origin = player.position + player.up * .25f;
        Vector3 direction = point - origin;
        float distance = direction.magnitude;
        if (distance < .05f) return true;

        // test every layer, but ignore colliders belonging to the player
        int count = Physics.RaycastNonAlloc(origin, direction / distance,
            visibilityHits, distance, ~0, QueryTriggerInteraction.Ignore);
        if (count == visibilityHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform obstacle = visibilityHits[i].transform;
            if (obstacle == null || obstacle == player || obstacle.IsChildOf(player)) continue;
            if (item != null && (obstacle == item || obstacle.IsChildOf(item))) continue;
            return false;
        }
        return true;
    }
    private bool AtExit()
    {
        // the arch's own colliders should not block interaction with the exit
        return player != null && PlayerInGarden
            && Vector3.Distance(player.position, exitPosition) < interactionDistance;
    }
    private DreamAnchor NearbyAnchor()
    {
        if (!initialized || player == null || !PlayerInGarden) return null;
        foreach (DreamAnchor anchor in anchors)
            if (anchor != null && Vector3.Distance(player.position, anchor.transform.position) < interactionDistance &&
                Visible(anchor.transform, anchor.transform.position + anchor.transform.up * 1.6f)) return anchor;
        return null;
    }
    private DreamLantern GetNearbyLantern()
    {
        DreamLantern nearest = null;
        float best = interactionDistance * interactionDistance;
        foreach (DreamLantern lantern in lanterns)
        {
            if (lantern == null) continue;
            float distance = (lantern.transform.position - player.position).sqrMagnitude;
            Vector3 point = lantern.orbRenderer != null ? lantern.orbRenderer.bounds.center : lantern.transform.position + lantern.transform.up;
            if (distance < best && Visible(lantern.transform, point)) { best = distance; nearest = lantern; }
        }
        return nearest;
    }
    public string GetInteractionHint()
    {
        if (!initialized || finished || player == null || DialogueOpen || !PlayerInGarden) return "";
        DreamInteraction mechanism = GetNearbyInteraction();
        if (mechanism != null) return "E - " + mechanism.title + "     Q - Bloom / Ruin";
        if (memoriesCollected == 3 && AtExit()) return "E - Restore the starlight beacon";
        DreamLantern lantern = GetNearbyLantern();
        if (lantern != null) return "E - " + lantern.memoryName + "     Q - Shift the dream";
        return CanShift ? "Q - Shift between Bloom and Ruin" : "";
    }
    private DreamInteraction GetNearbyInteraction()
    {
        DreamInteraction nearest = null;
        float best = interactionDistance * interactionDistance;
        foreach (DreamInteraction interaction in interactions)
        {
            if (interaction == null) continue;
            float distance = (player.position - interaction.Focus).sqrMagnitude;
            if (distance < best && Visible(interaction.transform, interaction.Focus))
            { best = distance; nearest = interaction; }
        }
        return nearest;
    }

    private bool UseMechanism(DreamInteraction mechanism)
    {
        if (mechanism.kind == DreamInteraction.Kind.Inscription)
        {
            dialogue.Open(player, mechanism.title, new[] { mechanism.inscription });
            return true;
        }
        if (mechanism.kind == DreamInteraction.Kind.Keeper)
        {
            if (!isBloom) { ShowNotice("The Keeper is an echo in Ruin. Return in Bloom.", 4); return false; }
            if (DreamProgression.KeeperRemembered)
                dialogue.Open(player, "The Forgotten Keeper", new[] { "You have given the quiet its name. Carry it to the Moon pedestal. Some things return only when we cease calling them." });
            else
                dialogue.Open(player, "The Forgotten Keeper", new[] {
                    "I kept the names of those who slept beneath these branches. Each spring, a petal was laid upon my door. Each winter, one name faded from the stone.",
                    "When the last house emptied, the moon waited in our well. I promised to wake her when the garden spoke again. I have forgotten what she asked me to keep.",
                    "I enter when the last voice leaves. A single spoken word breaks me. I fill an empty hall, yet weigh less than its dust. What am I? Type one word."
                }, DreamProgression.AnswerKeeper, () => {
                    mechanism.Refresh(isBloom);
                    ShowNotice("The Keeper remembers. Restore the Moon at its pedestal [E].", 7);
                });
            return true;
        }
        bool accepted;
        if (mechanism.kind == DreamInteraction.Kind.Petal)
        {
            accepted = DreamProgression.PressPetal(mechanism.index, isBloom);
            ShowNotice(accepted ? "The garden remembers: " + DreamProgression.PetalSteps + " / 3 seals."
                : "Moon must be restored first. Follow Root / Ruin > Dew / Bloom > Blossom / Ruin. Lit seals stay awake.", 5);
        }
        else
        {
            accepted = DreamProgression.PressAstral(mechanism.index, isBloom);
            ShowNotice(accepted ? "The constellation answers: " + DreamProgression.AstralSteps + " / 4."
                : memoriesCollected != 2 ? "Restore Petal before waking the constellation."
                : !isBloom ? "The stars answer only in Bloom."
                : "The constellation falls silent. Read the pilgrim's story and begin again.", 5);
        }
        foreach (DreamInteraction interaction in interactions) if (interaction != null) interaction.Refresh(isBloom);
        return accepted;
    }

    private void ShowNotice(string value, float duration) { notice = value; noticeUntil = Time.unscaledTime + duration; }
}
