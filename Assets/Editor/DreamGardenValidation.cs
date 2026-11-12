using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// batch entry point for the isolated validation project
public static class DreamGardenValidation
{
    private static int assertions;
    private static readonly List<string> report = new List<string>();

    public static void Run()
    {
        try
        {
            DreamGardenVisualUpgrade.BakeBoth();
            CheckProgression();
            CheckMazeSeeds();
            foreach (string path in new[] { DreamGardenVisualUpgrade.MainScene, DreamGardenVisualUpgrade.StandaloneScene })
                CheckScene(path);
            File.WriteAllLines("validation-results.txt", report);
            Debug.Log("DREAM_GARDEN_VALIDATION_PASS assertions=" + assertions);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-dreamGardenPlay") >= 0) DreamGardenPlayValidation.Run();
            else EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText("validation-results.txt", string.Join("\n", report) + "\nFAIL: " + error);
            EditorApplication.Exit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void CheckProgression()
    {
        DreamProgression.ResetProgress();
        DreamProgression.RestoreMemory(3); DreamProgression.MarkPlanet2Solved();
        Require(DreamProgression.Memories == 0 && !DreamProgression.Planet2Solved, "cannot skip challenges");
        Require(!DreamProgression.AnswerKeeper("moon"), "wrong keeper answer");
        Require(DreamProgression.AnswerKeeper("  SiLeNcE \t "), "normalized keeper answer");
        DreamProgression.RestoreMemory(1);
        Require(DreamProgression.Memories == 1, "moon restored");
        Require(!DreamProgression.PressPetal(1, true), "petal order protected");
        Require(!DreamProgression.PressPetal(0, true), "petal phase protected");
        Require(DreamProgression.PressPetal(0, false), "root accepted");
        Require(!DreamProgression.PressPetal(0, false), "duplicate root rejected");
        Require(DreamProgression.PressPetal(1, true), "dew accepted");
        Require(DreamProgression.PressPetal(2, false), "blossom accepted");
        DreamProgression.RestoreMemory(2);
        Require(DreamProgression.Memories == 2, "petal restored");
        Require(!DreamProgression.PressAstral(2, false), "astral phase protected");
        Require(DreamProgression.PressAstral(2, true), "seed accepted");
        Require(!DreamProgression.PressAstral(1, true) && DreamProgression.AstralSteps == 0, "wrong sequence resets");
        foreach (int symbol in new[] { 2, 0, 3, 1 }) Require(DreamProgression.PressAstral(symbol, true), "astral step");
        DreamProgression.RestoreMemory(3); DreamProgression.MarkPlanet2Solved();
        Require(DreamProgression.Planet2Solved && !DreamProgression.GameCompleted, "garden does not complete planet 1");
        DreamProgression.MarkPlanet1Solved();
        Require(DreamProgression.GameCompleted, "campaign completes after both planets");
        DreamProgression.ResetProgress();
        Require(!DreamProgression.KeeperRemembered && DreamProgression.PetalSteps == 0 && DreamProgression.AstralSteps == 0, "new session resets challenges");
        report.Add("PASS progression: riddle normalization, mandatory challenges, phases, order, reset and campaign guard");
    }

    private static void CheckMazeSeeds()
    {
        for (int size = 9; size <= 13; size += 2)
            for (int seed = 0; seed < 32; seed++)
            {
                var maze = new DreamMazeLayout(size, seed);
                var repeat = new DreamMazeLayout(size, seed);
                Require(maze.Route.Count >= 24, "route supports mechanisms");
                Require(maze.Route.Count == repeat.Route.Count, "deterministic route");
                foreach (DreamMazeLayout.Cell cell in maze.Route)
                    Require(cell.Row >= 0 && cell.Row < size && cell.Col >= 0 && cell.Col < size, "route bounds");
                for (int i = 1; i < maze.Route.Count; i++)
                {
                    var a = maze.Route[i - 1]; var b = maze.Route[i];
                    Require(Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col) == 1, "adjacent route cells");
                }
            }
        report.Add("PASS deterministic maze: 96 size/seed combinations");
    }

    private static void CheckScene(string path)
    {
        DreamProgression.ResetProgress();
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        DreamGardenVisualUpgrade.ValidateScene(scene);
        DreamGardenGenerator garden = DreamGardenVisualUpgrade.Find<DreamGardenGenerator>(scene);
        DreamPuzzleController puzzle = DreamGardenVisualUpgrade.Find<DreamPuzzleController>(scene);
        PlayerController player = DreamGardenVisualUpgrade.Find<PlayerController>(scene);
        garden.EnsureInitialized();
        Require(puzzle.Ready && puzzle.MemoryCount == 3, "baked garden initializes");
        Require(!garden.GetComponentInChildren<CubeSphereGenerator>().enabled, "baked planet does not regenerate");
        DreamGardenTerrain terrain = garden.GetComponentInChildren<DreamGardenTerrain>();
        Require(Mathf.Abs(terrain.SampleHeight(new Vector3(.3f, .9f, .3f).normalized, 24, 24)) < .001f, "square plateau includes corners");
        foreach (DreamPhaseGate gate in garden.GetComponentsInChildren<DreamPhaseGate>()) gate.blockingWall.SetActive(false);
        player.transform.position = garden.transform.position + Vector3.down * 200f;
        Physics.SyncTransforms();
        var maze = new DreamMazeLayout(garden.mazeCells, garden.seed);
        foreach (DreamMazeLayout.Cell cell in maze.Route)
        {
            Vector3 ground = garden.transform.TransformPoint(garden.CellCenter(cell.Row, cell.Col, .18f));
            Vector3 up = (ground - garden.transform.position).normalized;
            foreach (Collider hit in Physics.OverlapCapsule(ground + up * .45f, ground + up * 1.65f, .32f, ~0, QueryTriggerInteraction.Ignore))
                Require(hit.transform.IsChildOf(player.transform), "blocked route cell " + cell.Row + "," + cell.Col + ": " + hit.name);
            Require(Physics.Raycast(ground + up * 2f, -up, out RaycastHit floor, 4f, ~0, QueryTriggerInteraction.Ignore), "route has ground");
            Require(floor.collider.GetComponent<DreamGardenTerrain>() != null, "ground is the planet mesh");
        }
        for (int i = 1; i < maze.Route.Count; i++)
        {
            var a = maze.Route[i - 1]; var b = maze.Route[i];
            for (int sample = 1; sample < 5; sample++)
            {
                Vector3 local = Vector3.Lerp(garden.CellCenter(a.Row, a.Col, .2f), garden.CellCenter(b.Row, b.Col, .2f), sample / 5f);
                Vector3 ground = garden.transform.TransformPoint(local);
                Vector3 up = (ground - garden.transform.position).normalized;
                foreach (Collider hit in Physics.OverlapCapsule(ground + up * .45f, ground + up * 1.65f, .32f, ~0, QueryTriggerInteraction.Ignore))
                    Require(false, "blocked passage " + i + " sample " + sample + " by " + hit.name);
            }
        }
        DreamLantern moon = Array.Find(garden.GetComponentsInChildren<DreamLantern>(), l => l.orderIndex == 0);
        player.transform.position = moon.transform.position + moon.transform.up * 1.4f - moon.transform.forward * .8f;
        Physics.SyncTransforms();
        puzzle.TryInteract();
        Require(puzzle.MemoriesCollected == 0, "pedestal cannot bypass keeper");
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/PlanetDesign/DreamGarden/Shaders/DreamGardenTriplanar.shader");
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "triplanar shader imported");
        int renderers = garden.GetComponentsInChildren<Renderer>(true).Length;
        int colliders = garden.GetComponentsInChildren<Collider>(true).Length;
        report.Add("PASS " + path + ": references, serialized terrain, route ground/clearance, pedestal guard; renderers=" + renderers + ", colliders=" + colliders);
        report.Add("NOTE: edit mode physics queries do not replace a full playthrough or GPU performance measurement.");
    }
}
