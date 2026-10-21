using UnityEngine;

// All on-screen UI (HUD, pause menu, main menu, shop, game over) drawn with OnGUI, so no Canvas setup is needed.
// Works in portrait (reference 540 wide) and landscape (reference 720 high), and keeps clear of notches (safe area).
// The menus use the shared look in UiTheme (rounded cards, chunky buttons, outlined titles over a darkening gradient).
public partial class GameUI : MonoBehaviour
{
    enum Panel { Main, Shop }

    Panel panel = Panel.Main;
    GUIStyle label, center, right, small, tiny, big, button, toastStyle, hintStyle;
    GUIStyle titleGold, titleCream, tagline, heading, rowText, rowSmall, rowSmallRight, chipText, statValue, statLabel;
    float scale = 1f;
    bool portrait;
    float insetL, insetR, insetT, insetB;   // safe-area insets in UI units

    // Screen-pixel rectangle (origin top-left) of the pause button, so throwing ignores presses on it.
    static Rect pauseRectScreen;

    // The lives on the HUD in UI units (the tutorial highlights them).
    public static Rect HeartsRectUi;

    public static bool BlocksInput(Vector2 mousePosition)
    {
        Vector2 p = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return pauseRectScreen.Contains(p) || DevMenu.BlocksInput(p) || OptionsMenu.Open || Tutorial.BlocksInput(p);
    }

    static GUIStyle MakeText(GUIStyle basis, int size, TextAnchor anchor, Color color)
    {
        var s = new GUIStyle(basis) { fontSize = size, alignment = anchor, fontStyle = FontStyle.Bold };
        s.normal.textColor = color;
        s.wordWrap = false;
        s.clipping = TextClipping.Overflow;
        return s;
    }

    // menu text styles are cached by size, alignment and colour, so the menus do not make garbage every frame
    readonly System.Collections.Generic.Dictionary<long, GUIStyle> styleCache = new System.Collections.Generic.Dictionary<long, GUIStyle>();

    GUIStyle T(int size, TextAnchor anchor, Color color)
    {
        Color32 c = color;
        long key = ((long)size << 40) | ((long)(int)anchor << 32) | ((long)c.r << 24) | ((long)c.g << 16) | ((long)c.b << 8) | c.a;
        GUIStyle s;
        if (!styleCache.TryGetValue(key, out s))
        {
            s = MakeText(label, size, anchor, color);
            styleCache[key] = s;
        }
        return s;
    }

    void EnsureStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
        label.normal.textColor = Color.white;
        center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        right = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
        small = new GUIStyle(label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        tiny = new GUIStyle(label) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        big = new GUIStyle(label) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
        button = new GUIStyle(GUI.skin.button) { fontSize = 32, fontStyle = FontStyle.Bold };
        toastStyle = new GUIStyle(center) { fontSize = 28, wordWrap = true, alignment = TextAnchor.UpperCenter };
        hintStyle = new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.LowerCenter };

        // menu text
        titleGold = MakeText(label, 92, TextAnchor.MiddleCenter, UiTheme.Gold);
        titleCream = MakeText(label, 74, TextAnchor.MiddleCenter, UiTheme.Cream);
        tagline = MakeText(label, 20, TextAnchor.MiddleCenter, UiTheme.Dim);
        heading = MakeText(label, 21, TextAnchor.MiddleLeft, UiTheme.Gold);
        rowText = MakeText(label, 21, TextAnchor.MiddleLeft, UiTheme.Cream);
        rowSmall = MakeText(label, 17, TextAnchor.MiddleCenter, UiTheme.Dim);
        rowSmallRight = MakeText(label, 17, TextAnchor.MiddleRight, UiTheme.Dim);
        chipText = MakeText(label, 22, TextAnchor.MiddleCenter, UiTheme.Cream);
        statValue = MakeText(label, 30, TextAnchor.MiddleCenter, UiTheme.Cream);
        statLabel = MakeText(label, 15, TextAnchor.MiddleCenter, UiTheme.Dim);
    }

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        EnsureStyles();
        UiTheme.Ensure();

        portrait = Screen.height > Screen.width;
        scale = portrait ? Screen.width / 540f : Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        Rect safe = Screen.safeArea;
        insetL = safe.x / scale;
        insetR = (Screen.width - safe.xMax) / scale;
        insetT = (Screen.height - safe.yMax) / scale;
        insetB = safe.y / scale;

        pauseRectScreen = new Rect(0f, 0f, 0f, 0f);

        GUI.enabled = !Tutorial.Modal;   // while the pull lesson is up, no button on the screen works (the pause button included)
        switch (gm.State)
        {
            case GameManager.RunState.Menu:
                DrawMenu(gm, w, h);
                break;
            case GameManager.RunState.Playing:
                DrawHud(gm, w, h);
                if (gm.Paused && !DevMenu.SeeThrough) DrawPause(gm, w, h);
                break;
            case GameManager.RunState.GameOver:
                DrawHud(gm, w, h);
                DrawGameOver(gm, w, h);
                break;
        }
        GUI.enabled = true;
    }

    // ---- in-run HUD ----

    static string Clock(float seconds)
    {
        int t = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (t / 60) + ":" + (t % 60).ToString("00");
    }

    // Distance run so far (the bike starts at z = 0).
    static int Metres()
    {
        var bike = BikeForward.Instance;
        return bike == null ? 0 : Mathf.Max(0, Mathf.FloorToInt(bike.transform.position.z));
    }
}
