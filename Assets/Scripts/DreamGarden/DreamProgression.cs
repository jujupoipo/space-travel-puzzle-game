public static class DreamProgression
{
    public static bool Planet1Solved { get; private set; }
    public static bool Planet2Solved { get; private set; }
    public static bool GameCompleted { get { return Planet1Solved && Planet2Solved; } }

    public static void MarkPlanet1Solved()
    {
        Planet1Solved = true;
    }

    public static void MarkPlanet2Solved()
    {
        Planet2Solved = true;
    }

    public static void ResetProgress()
    {
        Planet1Solved = false;
        Planet2Solved = false;
    }
}
