using UnityEngine;

// progress lasts for this play session, across scene changes;
public static class DreamProgression
{
    public static bool Planet1Solved { get; private set; }
    public static bool Planet2Solved { get; private set; }
    public static int Memories { get; private set; }
    public static bool KeeperRemembered { get; private set; }
    public static int PetalSteps { get; private set; }
    public static int AstralSteps { get; private set; }
    private static readonly int[] astralOrder = { 2, 0, 3, 1 };
    public static bool GameCompleted => Planet1Solved && Planet2Solved;
    public static void MarkPlanet1Solved() => Planet1Solved = true;
    public static bool ChallengeComplete(int index) => index == 0 ? KeeperRemembered :
        index == 1 ? PetalSteps == 3 : index == 2 && AstralSteps == 4;

    public static string NormalizeAnswer(string answer) =>
        string.Join(" ", (answer ?? "").Trim().ToLowerInvariant().Split(
            (char[])null, System.StringSplitOptions.RemoveEmptyEntries));

    public static bool AnswerKeeper(string answer)
    {
        string normalized = NormalizeAnswer(answer);
        if (normalized != "silence" && normalized != "silencio" && normalized != "silêncio") return false;
        KeeperRemembered = true;
        return true;
    }

    public static bool PressPetal(int index, bool bloom)
    {
        if (Memories != 1 || index != PetalSteps || index < 0 || index > 2) return false;
        if (bloom != (index == 1)) return false;
        PetalSteps++;
        return true;
    }

    public static bool PressAstral(int symbol, bool bloom)
    {
        if (Memories != 2 || !bloom || AstralSteps == 4) return false;
        if (symbol != astralOrder[AstralSteps]) { AstralSteps = 0; return false; }
        AstralSteps++;
        return true;
    }

    public static bool AstralLit(int symbol)
    {
        for (int i = 0; i < AstralSteps; i++) if (astralOrder[i] == symbol) return true;
        return false;
    }

    // all callers share the same completion guard
    public static void RestoreMemory(int count)
    {
        if (count == Memories + 1 && ChallengeComplete(Memories)) Memories = Mathf.Clamp(count, 0, 3);
    }
    public static void MarkPlanet2Solved() { if (Memories == 3) Planet2Solved = true; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetProgress()
    {
        Planet1Solved = false;
        Planet2Solved = false;
        Memories = 0;
        KeeperRemembered = false;
        PetalSteps = 0;
        AstralSteps = 0;
    }
}
