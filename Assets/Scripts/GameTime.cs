using UnityEngine;

// Combines the different reasons time can be slowed or stopped (aiming, hit-stop, pause) into one timeScale.
public static class GameTime
{
    public static float Aim = 1f;
    public static float Hit = 1f;
    public static bool Paused = false;

    public static void Apply()
    {
        float s = Mathf.Min(Aim, Hit);
        Time.timeScale = Paused ? 0f : s;
        Time.fixedDeltaTime = 0.02f * Mathf.Max(s, 0.05f);   // keeps physics smooth in slow motion
        AudioListener.pause = Paused;
    }

    public static void Reset()
    {
        Aim = 1f;
        Hit = 1f;
        Paused = false;
        Apply();
    }
}
