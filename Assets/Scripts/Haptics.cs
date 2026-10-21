using UnityEngine;

// Short vibration pulses on Android. Does nothing in the editor or on other platforms.
public static class Haptics
{
    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt("Haptics", 1) == 1; }
        set { PlayerPrefs.SetInt("Haptics", value ? 1 : 0); }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
    static bool initialised;
#endif

    // Never true at runtime. Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the Android build.
    static bool never;
    static void KeepVibratePermission() { if (never) Handheld.Vibrate(); }

    public static void Pulse(long milliseconds)
    {
        if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!initialised)
            {
                initialised = true;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            if (vibrator != null) vibrator.Call("vibrate", milliseconds);
        }
        catch (System.Exception) { }
#endif
    }
}
