using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// tests the serialized scenes through their real start and update lifecycle
[InitializeOnLoad]
public static class DreamGardenPlayValidation
{
    private const string Key = "dream garden play validation";
    private static double readyAt;
    private static int stage;
    private static readonly List<string> results = new List<string>();

    static DreamGardenPlayValidation()
    {
        if (SessionState.GetBool(Key, false))
        { readyAt = EditorApplication.timeSinceStartup + 6; EditorApplication.update += Tick; }
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(DreamGardenVisualUpgrade.StandaloneScene);
        SessionState.SetBool(Key, true);
        stage = 0; results.Clear();
        readyAt = EditorApplication.timeSinceStartup + 6;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < readyAt) return;
        try
        {
            CheckRuntime();
            if (stage++ == 0)
            {
                SceneManager.LoadScene("two_planets");
                readyAt = EditorApplication.timeSinceStartup + 6;
                return;
            }
            File.WriteAllLines("play-validation-results.txt", results);
            Debug.Log("DREAM_GARDEN_PLAY_VALIDATION_PASS");
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText("play-validation-results.txt", string.Join("\n", results) + "\nFAIL " + error);
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            EditorApplication.Exit(1);
        }
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static void CheckRuntime()
    {
        Scene scene = SceneManager.GetActiveScene();
        DreamGardenGenerator garden = DreamGardenVisualUpgrade.Find<DreamGardenGenerator>(scene);
        DreamPuzzleController puzzle = garden.puzzle;
        Transform actor = puzzle.player;
        PlayerController controller = actor.GetComponent<PlayerController>();
        Rigidbody body = actor.GetComponent<Rigidbody>();
        Assert(puzzle.Ready && puzzle.MemoryCount == 3, "runtime initialization");
        DreamProgression.ResetProgress();
        // initialize from the same saved geometry to verify idempotence and session progress
        DreamLantern[] memories = garden.GetComponentsInChildren<DreamLantern>();
        Array.Sort(memories, (a, b) => a.orderIndex.CompareTo(b.orderIndex));
        puzzle.Initialize(new List<DreamLantern>(memories), new List<DreamPhaseGate>(garden.GetComponentsInChildren<DreamPhaseGate>()),
            garden.GetComponentInChildren<DreamExitMarker>().transform.position);
        foreach (TriggerPortal portal in UnityEngine.Object.FindObjectsByType<TriggerPortal>()) portal.gameObject.SetActive(false);
        void Place(Transform target)
        {
            Vector3 position = target.position + target.up * (controller.halfHeight + .25f) - target.forward * .65f;
            actor.SetPositionAndRotation(position, target.rotation);
            body.position = position; body.rotation = target.rotation;
            body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
        }
        DreamInteraction keeper = Array.Find(garden.GetComponentsInChildren<DreamInteraction>(), m => m.kind == DreamInteraction.Kind.Keeper);
        Place(keeper.transform);
        Assert(puzzle.PlayerInGarden, "radial gravity selects garden");
        bool movement = controller.enabled, kinematic = body.isKinematic;
        CursorLockMode cursor = Cursor.lockState;
        Assert(puzzle.TryInteract() && puzzle.DialogueOpen, "keeper interaction opens dialogue");
        Assert(!controller.enabled && body.isKinematic, "dialogue suspends actor");
        Assert(!puzzle.TryShift(), "dialogue blocks world shift");
        puzzle.GetComponent<DreamDialogue>().Close();
        Assert(controller.enabled == movement && body.isKinematic == kinematic && Cursor.lockState == cursor, "dialogue restores actor and cursor");
        Place(memories[0].transform);
        Assert(!puzzle.TryInteract() && puzzle.MemoriesCollected == 0, "moon cannot bypass riddle");
        Assert(DreamProgression.AnswerKeeper("  SILENCE  "), "keeper answer");
        Assert(puzzle.TryInteract() && puzzle.MemoriesCollected == 1, "moon pedestal restores");
        for (int i = 0; i < 3; i++)
        {
            int sealIndex = i;
            DreamInteraction seal = Array.Find(garden.GetComponentsInChildren<DreamInteraction>(), m => m.kind == DreamInteraction.Kind.Petal && m.index == sealIndex);
            Place(seal.transform);
            if (puzzle.IsBloom != (i == 1)) Assert(puzzle.TryShift(), "petal anchor shift");
            Assert(puzzle.TryInteract(), "petal mechanism " + i);
        }
        Place(memories[1].transform);
        Assert(puzzle.TryInteract() && puzzle.MemoriesCollected == 2, "petal pedestal restores");
        Assert(puzzle.TryShift(), "shift at petal");
        foreach (int symbolIndex in new[] { 2, 0, 3, 1 })
        {
            DreamInteraction symbol = Array.Find(garden.GetComponentsInChildren<DreamInteraction>(), m => m.kind == DreamInteraction.Kind.Astral && m.index == symbolIndex);
            Place(symbol.transform); Assert(puzzle.TryInteract(), "astral mechanism " + symbolIndex);
        }
        Place(memories[2].transform);
        Assert(puzzle.TryInteract() && puzzle.MemoriesCollected == 3, "star pedestal restores");
        Assert(!puzzle.Finished, "final arch still required");
        Vector3 checkpoint = actor.position;
        actor.position += actor.up * 8f;
        puzzle.ReturnToCheckpoint();
        Assert(Vector3.Distance(actor.position, checkpoint) < .3f && puzzle.IsBloom, "checkpoint and phase restore");
        Place(garden.GetComponentInChildren<DreamExitMarker>().transform);
        Assert(puzzle.TryInteract() && DreamProgression.Planet2Solved, "final arch completes garden");
        Assert(!DreamProgression.Planet1Solved, "planet 1 progress unchanged");
        results.Add("PASS " + scene.name + ": runtime initialization, radial gravity, dialogue lock/restore, all 3 guarded challenges, checkpoint/phase and final arch.");
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-dreamGardenPreview") >= 0) DreamGardenPreview.CaptureGarden(garden);
    }
}
