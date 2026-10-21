using System.Collections.Generic;
using UnityEngine;

// Shows errors and the frame rate on screen, so problems on a phone are visible without a computer.
// Added first by PrototypeBootstrap. Turn `show` off (or remove the line in the bootstrap) for a release build.
public class DebugOverlay : MonoBehaviour
{
    public bool show = true;
    public bool showOdds = false;   // the spawn-odds list down the left side (for tuning only)

    readonly List<string> lines = new List<string>();
    GUIStyle style;
    float fps;
    SegmentSpawner spawner;
    GUIStyle oddsStyle;

    void Awake()
    {
        Application.logMessageReceived += OnLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        bool shaderWarning = type == LogType.Warning && condition.ToLower().Contains("shader");
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && !shaderWarning) return;
        string first = stackTrace;
        int nl = first == null ? -1 : first.IndexOf('\n');
        if (nl > 0) first = first.Substring(0, nl);
        string text = condition.Length > 200 ? condition.Substring(0, 200) : condition;
        lines.Add(text + (string.IsNullOrEmpty(first) ? "" : "\n   at " + first));
        if (lines.Count > 5) lines.RemoveAt(0);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.05f);
    }

    void OnGUI()
    {
        if (!show) return;
        GUI.depth = -1000;
        float scale = Screen.width < Screen.height ? Screen.width / 540f : Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        }

        style.normal.textColor = Color.white;
        GUI.Label(new Rect(w / 2f - 40f, 2f + Screen.safeArea.y * 0f, 120f, 20f), Mathf.RoundToInt(fps) + " fps", style);

        if (showOdds) DrawOdds();

        if (lines.Count == 0) return;
        string all = "ERRORS:\n" + string.Join("\n", lines.ToArray());
        float boxH = Mathf.Min(h * 0.5f, 40f + lines.Count * 52f);
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0f, h - boxH, w, boxH), Texture2D.whiteTexture);
        GUI.color = Color.white;
        style.normal.textColor = new Color(1f, 0.4f, 0.4f);
        GUI.Label(new Rect(6f, h - boxH + 2f, w - 12f, boxH - 4f), all, style);
    }

    // Live spawn odds for the stretch of road the bike is passing now (for tuning the ramps; remove for release).
    // A row that changes flashes for a second: yellow for a small step, orange for a bigger one, red for a big jump.
    readonly Dictionary<string, int> lastPct = new Dictionary<string, int>();
    readonly Dictionary<string, float> flashUntil = new Dictionary<string, float>();
    readonly Dictionary<string, Color> flashColor = new Dictionary<string, Color>();
    float rowY;

    void DrawOdds()
    {
        if (spawner == null) spawner = FindFirstObjectByType<SegmentSpawner>();
        if (spawner == null) return;
        if (oddsStyle == null) oddsStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = false, padding = new RectOffset(0, 0, 0, 0) };

        // The odds are set by how far the bike will have ridden when it reaches a stretch of road, and the road is built about 150 m
        // ahead of the bike, so the odds shown are for the stretch the bike is passing now (the road being built is already at the end of the ramp).
        var bikeNow = BikeForward.Instance;
        float d = bikeNow != null ? Mathf.Max(0f, bikeNow.transform.position.z) : 0f;
        float hz = GameConfig.HazardStart, tg = GameConfig.TargetStart;
        var gm = GameManager.Instance;
        float t = gm != null ? gm.RunTime : 0f;
        bool low = gm != null && gm.Lives <= 1;
        rowY = 214f;

        Text("BIKE AT " + Mathf.RoundToInt(d) + " m   (road built to " + Mathf.RoundToInt(spawner.FrontierZ) + ")   speed " + GameConfig.SpeedAt(t).ToString("0.0") + " m/s", Color.white);
        Row("scenery ramp", GameConfig.Ramp(d, GameConfig.SceneryRampSeconds));
        Row("gameplay ramp", GameConfig.Ramp(d, GameConfig.GameplayRampSeconds));

        Text("-- scenery (per lot) --", new Color(0.7f, 0.9f, 1f));
        Row("trees", GameConfig.SceneryOdds(d, GameConfig.TreeChance));
        Row("porches", GameConfig.SceneryOdds(d, GameConfig.PorchChance));
        Row("decorated yards", GameConfig.SceneryOdds(d, GameConfig.YardChance));
        Row("lawn tables", GameConfig.SceneryOdds(d, GameConfig.TableChance));
        Row("parks", GameConfig.SceneryOdds(d, GameConfig.ParkChance));
        Row("  courts (of lots)", GameConfig.SceneryOdds(d, GameConfig.ParkChance) * GameConfig.CourtShare);
        Row("crate / can stacks", GameConfig.SceneryOdds(d, GameConfig.DestructibleChance));
        Row("shops / restaurants", GameConfig.SceneryOdds(d, GameConfig.ShopChance));
        Row("trampolines", GameConfig.SceneryOdds(d, GameConfig.TrampolineChance));
        Row("reverse baskets", GameConfig.SceneryOdds(d, GameConfig.ReverseHoopChance));
        Row("low fences", GameConfig.FenceChance);
        Row("wall bushes", GameConfig.WallBushChance);
        Row("chimneys", GameConfig.ChimneyChance);
        Row("street lamps", GameConfig.StreetLamps ? 1f : 0f);

        Text("-- gameplay --", new Color(1f, 0.85f, 0.6f));
        Row("mailbox targets", 1f);
        Row("windows", 1f);
        Row("balcony per upper floor (scenery)", GameConfig.BalconyChance);
        Row("customer: first floor", GameConfig.GameplayOdds(d, GameConfig.BalconyTargetChance, tg));
        Row("customer: second floor (of those)", GameConfig.GameplayOdds(d, GameConfig.SecondFloorChance, 0f));
        Row("extra balcony on a floor", GameConfig.GameplayOdds(d, GameConfig.ExtraBalconyChance, 0f));
        Row("cows (per stretch)", GameConfig.GameplayOdds(d, GameConfig.CowChance, hz));
        Row("walking residents", GameConfig.GameplayOdds(d, GameConfig.WalkerChance, hz));
        Row("blockers", GameConfig.GameplayOdds(d, GameConfig.BlockerChance, hz));
        Row("walled houses", GameConfig.GameplayOdds(d, GameConfig.WalledChance, hz));
        Row("building sites", GameConfig.GameplayOdds(d, GameConfig.ConstructionChance, hz));
        Row("beaches (per stretch)", d >= GameConfig.BeachFromNow ? GameConfig.BeachChanceNow : 0f);
        Row("heart pickups", d >= GameConfig.HeartFrom ? (low ? GameConfig.HeartChanceLowLives : GameConfig.HeartChance) : 0f);
        Text("hazards per stretch: max " + GameConfig.HazardBudget, Color.white);
    }

    void Text(string text, Color c)
    {
        oddsStyle.normal.textColor = c;
        GUI.Label(new Rect(4f, rowY, 220f, 14f), text, oddsStyle);
        rowY += 11.5f;
    }

    void Row(string name, float v)
    {
        int pct = Mathf.RoundToInt(v * 100f);
        int before;
        if (lastPct.TryGetValue(name, out before) && before != pct)
        {
            int step = Mathf.Abs(pct - before);
            flashColor[name] = step <= 1 ? new Color(1f, 0.92f, 0.2f) : step <= 4 ? new Color(1f, 0.6f, 0.1f) : new Color(1f, 0.25f, 0.2f);
            flashUntil[name] = Time.unscaledTime + 1f;
        }
        lastPct[name] = pct;

        Color c = new Color(1f, 1f, 1f, 0.85f);
        float until;
        if (flashUntil.TryGetValue(name, out until) && Time.unscaledTime < until) c = flashColor[name];
        Text(name + "  " + pct + "%", c);
    }
}
