using UnityEngine;

// Player options: the colours of the delivery areas and of the grass. Opened from the Options button on the main menu and the
// pause menu (GameUI calls OptionsMenu.Show). Choices apply at once and are saved on the phone.
public class OptionsMenu : MonoBehaviour
{
    static bool open;
    static bool pendingClose;
    static bool pendingTutorial;
    public static bool Open { get { return open; } }
    public static void Show() { open = true; }

    int tab;                       // 0 = delivery areas, 1 = mailbox, 2 = path, 3 = grass, 4 = time of day
    GUIStyle lab, val, btn, tabBtn, note, slider, thumb, swatch, titleStyle;

    static readonly string[] Tabs = { "Pads", "Mailbox", "Path", "Grass", "Time" };
    static readonly int[] TabColor = { 0, 4, 5, 1, 0 };   // GameConfig colour index behind each tab
    static readonly string[] TabHint =
    {
        "Colour of the lawn pad and balcony of the houses you deliver to",
        "Colour of the mailbox of the houses you deliver to",
        "Colour of the path and doorstep mat of the houses you deliver to (other houses keep a dark path)",
        "Colour of the grass and the park lawns",
        "",
    };

    // DayCycle.ForcePhase: -2 follows the phone's clock, -1 follows the ride, 0..4 holds one time of day (3 = dusk is the default)
    static readonly string[] TimeNames = { "Clock", "Cycle", "Dawn", "Day", "Rain", "Dusk", "Night" };
    static readonly string[] TimeHints =
    {
        "The game uses your phone's time: night in the evening and at night, day in the daytime",
        "The time of day changes as you ride: dawn, day, rain, dusk, then night",
        "Always dawn",
        "Always day",
        "Always rain",
        "Always dusk",
        "Always night: stars, lit lamps and lit windows",
    };

    static readonly Color[] PadPresets =
    {
        new Color(0.25f, 0.80f, 0.35f), new Color(0.55f, 0.95f, 0.20f), new Color(0.20f, 0.60f, 1.00f),
        new Color(1.00f, 0.55f, 0.15f), new Color(1.00f, 0.40f, 0.70f), new Color(1.00f, 0.90f, 0.20f),
    };
    static readonly Color[] MailPresets =
    {
        new Color(1.00f, 0.35f, 0.15f), new Color(0.90f, 0.10f, 0.10f), new Color(0.20f, 0.45f, 0.95f),
        new Color(1.00f, 0.85f, 0.10f), new Color(0.65f, 0.30f, 0.85f), new Color(0.95f, 0.95f, 0.95f),
    };
    static readonly Color[] PathPresets =
    {
        new Color(0.93f, 0.85f, 0.62f), new Color(0.95f, 0.95f, 0.92f), new Color(0.95f, 0.65f, 0.30f),
        new Color(0.60f, 0.80f, 0.95f), new Color(0.95f, 0.60f, 0.75f), new Color(1.00f, 0.92f, 0.40f),
    };
    static readonly Color[] GrassPresets =
    {
        new Color(0.38f, 0.70f, 0.32f), new Color(0.45f, 0.60f, 0.37f), new Color(0.25f, 0.50f, 0.25f),
        new Color(0.60f, 0.55f, 0.25f), new Color(0.80f, 0.72f, 0.50f), new Color(0.88f, 0.92f, 0.95f),
    };

    void Awake()
    {
        GameConfig.LoadColors();   // saved choices, before the road is built
        DayCycle.ForcePhase = Mathf.Clamp(PlayerPrefs.GetInt("TimeOfDay", 3), -2, 4);
    }

    void Update()
    {
        if (pendingClose) { pendingClose = false; open = false; PlayerPrefs.Save(); }
        if (pendingTutorial)
        {
            pendingTutorial = false;
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameManager.RunState.Menu) gm.StartTutorial();
        }
    }

    // ---- colours ----

    // The grass colour also sets the park lawns and the far ground, keeping their usual difference from it.
    static void SetGrass(Color c)
    {
        GameConfig.SetColor(1, c);
        GameConfig.SetColor(2, new Color(Mathf.Clamp01(c.r * 0.895f), Mathf.Clamp01(c.g * 1.114f), Mathf.Clamp01(c.b * 1.125f)));
        GameConfig.SetColor(3, new Color(Mathf.Clamp01(c.r * 0.92f), Mathf.Clamp01(c.g * 0.943f), Mathf.Clamp01(c.b * 0.9375f)));
    }

    void EnsureStyles()
    {
        UiTheme.Ensure();
        if (lab != null) return;
        lab = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        lab.normal.textColor = UiTheme.Cream;
        val = new GUIStyle(lab) { alignment = TextAnchor.MiddleRight };
        val.normal.textColor = UiTheme.Gold;
        note = new GUIStyle(lab) { fontSize = 17, fontStyle = FontStyle.Normal, wordWrap = true, alignment = TextAnchor.UpperLeft };
        note.normal.textColor = UiTheme.Dim;
        titleStyle = new GUIStyle(lab) { fontSize = 36 };
        titleStyle.normal.textColor = UiTheme.Gold;
        btn = new GUIStyle(UiTheme.SecondaryStyle) { fontSize = 24 };
        tabBtn = new GUIStyle(UiTheme.TabStyle) { fontSize = 20 };
        slider = UiTheme.SliderStyle;
        thumb = UiTheme.ThumbStyle;
        swatch = new GUIStyle(UiTheme.PillStyle);   // a rounded colour chip: the button's background colour tints it
    }

    float Slider(string name, float v)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(46));
        GUILayout.Label(name, lab, GUILayout.Width(110));
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        v = GUILayout.HorizontalSlider(v, 0f, 1f, slider, thumb);
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.Label(v.ToString("0.00"), val, GUILayout.Width(64));
        GUILayout.EndHorizontal();
        return v;
    }

    void OnGUI()
    {
        if (!open) return;
        EnsureStyles();
        GUI.depth = -500;

        float scale = Screen.width < Screen.height ? Screen.width / 540f : Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float pw = Mathf.Min(w - 24f, 600f);
        float ph = Mathf.Min(h - 60f, 620f);
        var panel = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
        UiTheme.Card(panel);

        GUILayout.BeginArea(new Rect(panel.x + 22f, panel.y + 16f, panel.width - 44f, panel.height - 32f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("OPTIONS", titleStyle);
        GUILayout.FlexibleSpace();
        var gmo = GameManager.Instance;
        if (gmo != null && gmo.State == GameManager.RunState.Menu)
        {
            bool howTo = GUILayout.Button("How to play", tabBtn, GUILayout.Width(170), GUILayout.Height(44));
            UiTheme.ShineLast(false);
            if (howTo)
            {
                pendingClose = true;
                pendingTutorial = true;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        tab = GUILayout.Toolbar(tab, Tabs, tabBtn, GUILayout.Height(46));
        GUILayout.Space(6f);

        if (tab == 4)
        {
            int ti = GUILayout.Toolbar(DayCycle.ForcePhase + 2, TimeNames, tabBtn, GUILayout.Height(54));
            if (ti - 2 != DayCycle.ForcePhase)
            {
                DayCycle.ForcePhase = ti - 2;
                PlayerPrefs.SetInt("TimeOfDay", DayCycle.ForcePhase);
            }
            GUILayout.Space(8f);
            GUILayout.Label(TimeHints[DayCycle.ForcePhase + 2], note);
        }
        else
        {
            int ci = TabColor[tab];
            bool grass = ci == 1;
            GUILayout.Label(TabHint[tab], note);
            Color[] presets = tab == 0 ? PadPresets : (tab == 1 ? MailPresets : (tab == 2 ? PathPresets : GrassPresets));

            GUILayout.BeginHorizontal();
            Color prevBg = GUI.backgroundColor;
            for (int i = 0; i < presets.Length; i++)
            {
                GUI.backgroundColor = presets[i];
                if (GUILayout.Button(GUIContent.none, swatch, GUILayout.Height(50)))
                {
                    if (grass) SetGrass(presets[i]); else GameConfig.SetColor(ci, presets[i]);
                }
            }
            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            Color c = GameConfig.GetColor(ci);
            Rect sw = GUILayoutUtility.GetRect(1f, 38f, GUILayout.ExpandWidth(true));
            UiTheme.Pill(sw, c);
            GUILayout.Label("Or mix your own:", note);
            float r = Slider("Red", c.r);
            float g = Slider("Green", c.g);
            float b = Slider("Blue", c.b);
            if (r != c.r || g != c.g || b != c.b)
            {
                var nc = new Color(r, g, b);
                if (grass) SetGrass(nc); else GameConfig.SetColor(ci, nc);
            }

        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (tab != 4)
        {
            bool reset = GUILayout.Button("Reset colours", btn, GUILayout.Height(54));
            UiTheme.ShineLast(false);
            if (reset) GameConfig.ApplyColorPreset(0);
        }
        var doneStyle = new GUIStyle(UiTheme.PrimaryStyle) { fontSize = 26 };
        bool done = GUILayout.Button("Done", doneStyle, GUILayout.Height(58));
        UiTheme.ShineLast(true);
        if (done) pendingClose = true;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        // Swallows any press outside the panel's controls, so nothing underneath (menu, throw) reacts.
        GUI.Button(new Rect(0f, 0f, w, h), GUIContent.none, GUIStyle.none);
    }
}
