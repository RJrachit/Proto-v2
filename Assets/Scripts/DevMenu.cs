using UnityEngine;

// In-game developer menu for playtesting on the phone. Tap the small DEV button at the top centre.
// Opening it pauses a run in progress. Throw / bike / camera settings apply straight away; world (lamps, fences, parks...)
// settings (the Chances tab) apply to lots spawned after the change, or press "Restart run" to rebuild the whole road.
// Added by PrototypeBootstrap. Remove that line (or the file) for a release build.
public class DevMenu : MonoBehaviour
{
    static bool open;
    public static bool SeeThrough { get; private set; }   // the Camera tab is showing: the pause screen hides so the whole view is visible
    static Rect blockRect;   // screen pixels, origin top-left: the DEV button, or the whole screen while the menu is open

    public static bool BlocksInput(Vector2 p)
    {
        return open || pendingOpen || blockRect.Contains(p);
    }

    // OnGUI only sets these; Update applies them, so the control layout never changes between a Layout and a Repaint event.
    static bool pendingOpen, pendingClose, pendingRestart;
    int tab, pendingTab;
    static readonly string[] Tabs = { "Throw", "Bike", "Camera", "Look", "Chances", "Cheats" };
    const int CameraTabIndex = 2;

    PaperThrower thrower;
    BikeForward bike;
    CameraFollow cam;
    DebugOverlay dbg;

    GUIStyle lab, val, btn, tabBtn, devBtn, note, slider, thumb, vbar;
    Vector2 scroll;
    int scrollTab = -1;

    void Update()
    {
        if (pendingOpen)
        {
            pendingOpen = false;
            open = true;
            thrower = FindFirstObjectByType<PaperThrower>();
            bike = BikeForward.Instance;
            cam = CameraFollow.Instance;
            dbg = FindFirstObjectByType<DebugOverlay>();
            var gm = GameManager.Instance;
            if (gm != null && gm.Playing) gm.Pause();
        }
        if (pendingRestart)
        {
            pendingRestart = false;
            var gm = GameManager.Instance;
            if (gm != null) { open = false; gm.StartRun(); }
        }
        if (pendingClose) { pendingClose = false; open = false; }
    }

    void EnsureStyles()
    {
        if (lab != null) return;
        lab = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        lab.normal.textColor = Color.white;
        val = new GUIStyle(lab) { alignment = TextAnchor.MiddleRight };
        val.normal.textColor = new Color(1f, 0.9f, 0.4f);
        note = new GUIStyle(lab) { fontSize = 16, fontStyle = FontStyle.Normal, wordWrap = true, alignment = TextAnchor.UpperLeft };
        note.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        btn = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        tabBtn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        devBtn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        slider = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 14 };
        thumb = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 34, fixedHeight = 34 };
        // a fat scrollbar, so the long tabs can be dragged with a finger
        vbar = new GUIStyle(GUI.skin.verticalScrollbar) { fixedWidth = 34 };
        GUI.skin.verticalScrollbarThumb.fixedWidth = 34;
        GUI.skin.verticalScrollbarThumb.stretchHeight = true;
    }

    // ---- row helpers ----

    float Slider(string name, float v, float min, float max, string fmt)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(44));
        GUILayout.Label(name, lab, GUILayout.Width(200));
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        v = GUILayout.HorizontalSlider(v, min, max, slider, thumb);
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.Label(v.ToString(fmt), val, GUILayout.Width(74));
        GUILayout.EndHorizontal();
        return v;
    }

    bool Toggle(string name, bool v)
    {
        if (GUILayout.Button(name + ":  " + (v ? "ON" : "OFF"), btn, GUILayout.Height(44))) v = !v;
        return v;
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.Layout) tab = pendingTab;
        EnsureStyles();
        GUI.depth = -900;
        SeeThrough = open && tab == CameraTabIndex;

        float scale = Screen.width < Screen.height ? Screen.width / 540f : Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        if (!open)
        {
            if (Tutorial.Modal) { blockRect = new Rect(0f, 0f, 0f, 0f); return; }   // (no DEV button over the pull lesson)
            float insetT = (Screen.height - Screen.safeArea.yMax) / scale;
            var r = new Rect(w * 0.5f - 40f, 24f + insetT, 80f, 34f);
            blockRect = new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale);
            if (GUI.Button(r, "DEV", devBtn)) pendingOpen = true;
            return;
        }

        blockRect = new Rect(0f, 0f, Screen.width, Screen.height);

        // on the Camera tab the menu turns see-through so you can watch the view change behind it
        bool seeThrough = tab == CameraTabIndex;
        GUI.color = new Color(0f, 0f, 0f, seeThrough ? 0f : 0.65f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        float pw = Mathf.Min(w - 24f, 640f);
        float ph = Mathf.Min(h - 40f, 640f);
        var panel = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
        GUI.color = new Color(0.10f, 0.11f, 0.15f, seeThrough ? camMenuOpacity : 0.97f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, panel.height - 20f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("DEV MENU", lab);
        if (GUILayout.Button("Beach test: " + (GameConfig.BeachTest ? "ON" : "OFF"), devBtn, GUILayout.Width(190), GUILayout.Height(32)))
            GameConfig.BeachTest = !GameConfig.BeachTest;   // (needs Restart run)
        GUILayout.EndHorizontal();
        pendingTab = GUILayout.Toolbar(pendingTab, Tabs, tabBtn, GUILayout.Height(44));
        GUILayout.Space(6f);
        if (scrollTab != tab) { scrollTab = tab; scroll = Vector2.zero; }
        scroll = GUILayout.BeginScrollView(scroll, false, true, GUI.skin.horizontalScrollbar, vbar, GUILayout.ExpandHeight(true));

        switch (tab)
        {
            case 0: ThrowTab(); break;
            case 1: BikeTab(); break;
            case 2: CameraTab(); break;
            case 3: LookTab(); break;
            case 4: ChancesTab(); break;
            default: CheatsTab(); break;
        }
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart run", btn, GUILayout.Height(50))) pendingRestart = true;
        if (GUILayout.Button("Close", btn, GUILayout.Height(50))) pendingClose = true;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        // Swallows any press outside the panel's controls, so nothing underneath (pause menu, throw) reacts.
        GUI.Button(new Rect(0f, 0f, w, h), GUIContent.none, GUIStyle.none);
    }

    // ---- tabs ----

    void ThrowTab()
    {
        if (thrower == null) { GUILayout.Label("No thrower yet", note); return; }
        thrower.assist = Toggle("Aim assist", thrower.assist);
        GameConfig.AimAssist = thrower.assist;
        thrower.assistRange = GameConfig.AssistRange = Slider("Assist range", thrower.assistRange, 0.5f, 8f, "0.0");
        thrower.assistStrength = GameConfig.AssistStrength = Slider("Assist strength", thrower.assistStrength, 0f, 1f, "0.00");
        GameConfig.RainThrowPenalty = Slider("Rain throw penalty", GameConfig.RainThrowPenalty, 0f, 0.3f, "0.00");   // share of the distance lost in the rain; the aim guide does not change
        GameConfig.BoardBounce = Slider("Backboard bounce", GameConfig.BoardBounce, 0f, 0.6f, "0.00");   // applies to houses placed after the change
        GameConfig.BoardSlideKeep = Slider("Backboard slide kept", GameConfig.BoardSlideKeep, 0f, 1f, "0.00");
        GameConfig.RimBounce = Slider("Hoop ring bounce", GameConfig.RimBounce, 0f, 0.4f, "0.00");
        GameConfig.RimSlideKeep = Slider("Hoop ring slide kept", GameConfig.RimSlideKeep, 0f, 1f, "0.00");
        GameConfig.ChuteTilt = Slider("Roof tilt (degrees)", GameConfig.ChuteTilt, 0f, 30f, "0");   // applies to roofs set up after the change
        GameConfig.RoofFriction = Slider("Roof friction (low = slippery)", GameConfig.RoofFriction, 0f, 0.8f, "0.00");
        thrower.maxDistance = GameConfig.MaxThrowDistance = Slider("Max throw dist", thrower.maxDistance, 8f, 45f, "0.0");
        thrower.minDistance = GameConfig.MinThrowDistance = Slider("Min throw dist", thrower.minDistance, 1f, 10f, "0.0");
        thrower.launchAngleLight = GameConfig.LaunchAngleLight = Slider("Angle, light pull", thrower.launchAngleLight, 20f, 75f, "0");
        thrower.launchAngleFull = GameConfig.LaunchAngleFull = Slider("Angle, full pull", thrower.launchAngleFull, 30f, 80f, "0");
        thrower.fullPowerDrag = GameConfig.FullPowerDrag = Slider("Full-power pull", thrower.fullPowerDrag, 0.15f, 0.6f, "0.00");
        thrower.powerCurve = GameConfig.PowerCurve = Slider("Power curve", thrower.powerCurve, 0.5f, 2f, "0.00");
        bool sling = Toggle("Controls: pull back to throw (off = drag toward the target)", thrower.slingshotStyle);
        if (sling != thrower.slingshotStyle)
        {
            thrower.slingshotStyle = GameConfig.SlingshotStyle = sling;
            try { PlayerPrefs.SetInt("ctl_sling", sling ? 1 : 0); PlayerPrefs.Save(); } catch (System.Exception) { }   // remembered between runs
        }
        GameConfig.TutorialPreviewShare = Slider("Aim guide shown in how to play", GameConfig.TutorialPreviewShare, 0.1f, 1f, "0.00");
        GameConfig.PreviewShare = Slider("Aim guide shown", GameConfig.PreviewShare, 0.1f, 1f, "0.00");   // share of the arc drawn: 0.60 = 60%
        GameConfig.ThrowBoost = Slider("Throw speed-up at start (1 = off)", GameConfig.ThrowBoost, 1f, 2.5f, "0.00");   // same path, flown faster at first
        GameConfig.ThrowBoostShare = Slider("Speed-up share of the flight", GameConfig.ThrowBoostShare, 0.2f, 0.9f, "0.00");
        GameConfig.AirDrag = Slider("Air resistance (0 = none, higher = arc drops quicker)", GameConfig.AirDrag, 0f, 1.5f, "0.00");
    }

    void BikeTab()
    {
        if (bike == null) { GUILayout.Label("No bike yet", note); return; }
        bike.startSpeed = GameConfig.BikeStartSpeed = Slider("Start speed", GameConfig.BikeStartSpeed, 1f, 15f, "0.0");
        bike.maxSpeed = GameConfig.BikeMaxSpeed = Slider("Max speed", GameConfig.BikeMaxSpeed, 1f, 20f, "0.0");
        GameConfig.BikePlateauSpeed = Slider("Plateau speed", GameConfig.BikePlateauSpeed, 1f, 20f, "0.00");
        GameConfig.SpeedHoldStart = Slider("Hold at start speed (s)", GameConfig.SpeedHoldStart, 0f, 300f, "0");
        GameConfig.SpeedRise1 = Slider("Rise to plateau (s)", GameConfig.SpeedRise1, 1f, 400f, "0");
        GameConfig.SpeedHold = Slider("Hold at plateau (s)", GameConfig.SpeedHold, 0f, 300f, "0");
        GameConfig.SpeedRise2 = Slider("Rise to max (s)", GameConfig.SpeedRise2, 1f, 400f, "0");
        GameConfig.SceneryRampSeconds = Slider("Scenery ramp (s)", GameConfig.SceneryRampSeconds, 5f, 300f, "0");
        GameConfig.GameplayRampSeconds = Slider("Gameplay ramp (s)", GameConfig.GameplayRampSeconds, 10f, 600f, "0");
        GUILayout.Label("Max speed applies at once. Start speed applies from the next run (Restart run).", note);
    }

    void CameraTab()
    {
        if (cam == null) { GUILayout.Label("No camera rig yet", note); return; }
        Vector3 o = cam.offset;
        o.x = Slider("Sideways", o.x, -15f, 15f, "0.00");
        o.y = Slider("Height", o.y, 1f, 25f, "0.00");
        o.z = Slider("Ahead (+) / back (-)", o.z, -30f, 15f, "0.00");
        cam.offset = o;
        cam.pitch = Slider("Pitch (look down)", cam.pitch, -20f, 80f, "0.0");
        cam.yaw = Slider("Turn (sideways)", cam.yaw, -90f, 90f, "0.0");
        cam.baseFov = Slider("Field of view", cam.baseFov, 30f, 90f, "0");
        cam.minHorizontalFov = Slider("Min view across", cam.minHorizontalFov, 20f, 130f, "0");
        camMenuOpacity = Slider("Menu opacity", camMenuOpacity, 0.1f, 0.95f, "0.00");
        cam.aimZoomLight = Slider("Aim zoom, light pull", cam.aimZoomLight, 0f, 0.25f, "0.00");
        cam.aimZoomFull = Slider("Aim zoom, full pull", cam.aimZoomFull, 0f, 0.25f, "0.00");
        cam.flightFollow = Slider("Follow the paper (side view)", cam.flightFollow, 0f, 1f, "0.00");
        cam.flightMaxAhead = Slider("Follow limit (m)", cam.flightMaxAhead, 0f, 25f, "0");
        GUILayout.Label("x " + o.x.ToString("0.00") + "   y " + o.y.ToString("0.00") + "   z " + o.z.ToString("0.00") + "   pitch " + cam.pitch.ToString("0.0")
                        + "   turn " + cam.yaw.ToString("0.0") + "   fov " + cam.baseFov.ToString("0") + "   min view " + cam.minHorizontalFov.ToString("0"), note);
        bool chase = Toggle("Chase view (off = side view)", GameConfig.ChaseView);
        if (chase != GameConfig.ChaseView)
        {
            cam.SetChase(chase);
            pendingRestart = true;   // the road is rebuilt: the side view leaves out the right-hand houses
            camMsg = chase ? "Chase view: both sides built." : "Side view: right-hand houses left out.";
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save camera", btn, GUILayout.Height(50))) { cam.SaveView(); camMsg = "Saved: it loads every time the game starts."; }
        if (GUILayout.Button("Reset camera", btn, GUILayout.Height(50))) { cam.ResetView(); camMsg = "Back to the built-in view."; }
        GUILayout.EndHorizontal();
        GUILayout.Label(!string.IsNullOrEmpty(camMsg) ? camMsg : (cam.HasSavedView ? "A saved view is in use." : "Using the built-in view (nothing saved)."), note);
    }

    static float camMenuOpacity = 0.10f;     // how solid the menu is on the Camera tab (low = see the game behind it)
    string camMsg;

    // Everything that is decided by a dice roll while the road is built: how often each kind of lot, prop, house feature and hazard turns up.
    // Changes apply to lots spawned after the change; Restart run rebuilds the whole road.
    void ChancesTab()
    {
        var sp = GameManager.Instance != null ? GameManager.Instance.Spawner : null;

        GUILayout.Label("Lots", lab);
        GameConfig.ParkChance = Slider("Parks", GameConfig.ParkChance, 0f, 1f, "0.00");
        GameConfig.ParkingShare = Slider("  of parks: car parks", GameConfig.ParkingShare, 0f, 1f, "0.00");
        GameConfig.CourtShare = Slider("  of parks: courts", GameConfig.CourtShare, 0f, 1f, "0.00");
        GameConfig.ShopChance = Slider("Shops and restaurants", GameConfig.ShopChance, 0f, 1f, "0.00");
        GameConfig.BeachChance = Slider("Beaches (per stretch)", GameConfig.BeachChance, 0f, 0.5f, "0.00");
        GameConfig.BeachBothShare = Slider("  beaches on both sides", GameConfig.BeachBothShare, 0f, 1f, "0.00");
        GameConfig.BeachFrom = Slider("  beaches from (m)", GameConfig.BeachFrom, 0f, 2000f, "0");

        GUILayout.Label("Yards and props", lab);
        GameConfig.TreeChance = Slider("Trees", GameConfig.TreeChance, 0f, 1f, "0.00");
        GameConfig.PorchChance = Slider("Porches", GameConfig.PorchChance, 0f, 1f, "0.00");
        GameConfig.YardChance = Slider("Decorated yards (cars, stalls...)", GameConfig.YardChance, 0f, 1f, "0.00");
        if (sp != null) { sp.treeChance = GameConfig.TreeChance; sp.porchChance = GameConfig.PorchChance; sp.yardChance = GameConfig.YardChance; }   // (the spawner keeps its own copies)
        GameConfig.TableChance = Slider("Lawn tables", GameConfig.TableChance, 0f, 1f, "0.00");
        GameConfig.TableItemChance = Slider("  tables with dishes", GameConfig.TableItemChance, 0f, 1f, "0.00");
        GameConfig.WallBushChance = Slider("Wall bushes", GameConfig.WallBushChance, 0f, 1f, "0.00");
        GameConfig.FenceChance = Slider("Low fences", GameConfig.FenceChance, 0f, 1f, "0.00");
        GameConfig.ChimneyChance = Slider("Chimneys", GameConfig.ChimneyChance, 0f, 1f, "0.00");
        GameConfig.DestructibleChance = Slider("Crate and can stacks", GameConfig.DestructibleChance, 0f, 1f, "0.00");
        GameConfig.CrateChain = Slider("  crate hits a neighbour", GameConfig.CrateChain, 0f, 1f, "0.00");
        GameConfig.TrampolineChance = Slider("Trampolines (full)", GameConfig.TrampolineChance, 0f, 1f, "0.00");
        GameConfig.ReverseHoopChance = Slider("Reverse baskets (full)", GameConfig.ReverseHoopChance, 0f, 1f, "0.00");
        GameConfig.WallPadChance = Slider("Wall trampolines", GameConfig.WallPadChance, 0f, 1f, "0.00");
        GameConfig.BalconyChance = Slider("Balconies per floor (scenery)", GameConfig.BalconyChance, 0f, 1f, "0.00");

        GUILayout.Label("Customers' houses", lab);
        GameConfig.BalconyTargetChance = Slider("First floor (full)", GameConfig.BalconyTargetChance, 0f, 1f, "0.00");
        GameConfig.SecondFloorChance = Slider("Second floor (full)", GameConfig.SecondFloorChance, 0f, 1f, "0.00");
        GameConfig.ExtraBalconyChance = Slider("Extra balconies (full)", GameConfig.ExtraBalconyChance, 0f, 1f, "0.00");
        GameConfig.WalledChance = Slider("Walled houses (full)", GameConfig.WalledChance, 0f, 1f, "0.00");
        GameConfig.ConstructionChance = Slider("Building sites (full)", GameConfig.ConstructionChance, 0f, 1f, "0.00");
        GameConfig.BlockerChance = Slider("Blockers (full)", GameConfig.BlockerChance, 0f, 1f, "0.00");
        GameConfig.WalkerChance = Slider("Walkers (full)", GameConfig.WalkerChance, 0f, 1f, "0.00");

        GUILayout.Label("Road", lab);
        GameConfig.CowChance = Slider("Cows (full)", GameConfig.CowChance, 0f, 1f, "0.00");
        GameConfig.HazardBudget = Mathf.RoundToInt(Slider("Hazards / stretch", GameConfig.HazardBudget, 0f, 5f, "0"));
        GameConfig.HeartChance = Slider("Hearts", GameConfig.HeartChance, 0f, 1f, "0.00");
        GameConfig.HeartChanceLowLives = Slider("Hearts (low lives)", GameConfig.HeartChanceLowLives, 0f, 1f, "0.00");
        GameConfig.CustomerGapStart = Slider("Customer gap, start (m)", GameConfig.CustomerGapStart, 4f, 60f, "0");
        GameConfig.CustomerGapEnd = Slider("Customer gap, end (m)", GameConfig.CustomerGapEnd, 4f, 60f, "0");
        if (sp != null) { sp.customerGapStart = GameConfig.CustomerGapStart; sp.customerGapEnd = GameConfig.CustomerGapEnd; }
        GameConfig.FeaturesPerSide = Mathf.RoundToInt(Slider("Features / side (0 = no limit)", GameConfig.FeaturesPerSide, 0f, 4f, "0"));
        GameConfig.MaxNonDeliveryRun = Mathf.RoundToInt(Slider("Non-delivery lots in a row (0 = no limit)", GameConfig.MaxNonDeliveryRun, 0f, 6f, "0"));
        GameConfig.OpeningNonDelivery = Mathf.RoundToInt(Slider("Non-delivery lots in the opening", GameConfig.OpeningNonDelivery, 0f, 6f, "0"));
        GameConfig.EasyOpening = Toggle("Easy opening (only customers' houses at first)", GameConfig.EasyOpening);
        GameConfig.EasyOpeningSeconds = Slider("Easy opening lasts (seconds)", GameConfig.EasyOpeningSeconds, 0f, 60f, "0");
        GameConfig.OpeningFeatures = Mathf.RoundToInt(Slider("Parks / courts allowed in the opening", GameConfig.OpeningFeatures, 0f, 3f, "0"));
        GameConfig.EasyFirstCustomer = Slider("First customer this far ahead (metres)", GameConfig.EasyFirstCustomer, 4f, 40f, "0");
        GameConfig.NoTreesSeconds = Slider("No lawn trees for (seconds, 0 = off)", GameConfig.NoTreesSeconds, 0f, 300f, "0");
        GameConfig.TreeFadeSeconds = Slider("Lawn trees fade in over (seconds)", GameConfig.TreeFadeSeconds, 0f, 120f, "0");

        GUILayout.Label("How the odds rise with distance", lab);
        GameConfig.PlainStart = Slider("Plain start (m)", GameConfig.PlainStart, 0f, 200f, "0");
        GameConfig.SceneryStart = Slider("Scenery start share", GameConfig.SceneryStart, 0f, 1f, "0.00");
        GameConfig.TargetStart = Slider("Targets start share", GameConfig.TargetStart, 0f, 1f, "0.00");
        GameConfig.HazardStart = Slider("Hazards start share", GameConfig.HazardStart, 0f, 1f, "0.00");
        GUILayout.Label("A chance marked (full) is the odds at the end of the ramp; it starts at the start share of that. Features / side caps parks, car parks, courts, pyramids and decorated yards per side of a stretch.", note);
    }

    static readonly string[] TimeNames = { "Clock", "Cycle", "Dawn", "Day", "Rain", "Dusk", "Night" };

    void LookTab()
    {
        GUILayout.Label("Time of day", lab);
        int ti = GUILayout.Toolbar(DayCycle.ForcePhase + 2, TimeNames, btn, GUILayout.Height(44));
        DayCycle.ForcePhase = ti - 2;
        GameConfig.LampLight = Slider("Lamp light", GameConfig.LampLight, 0f, 3f, "0.00");
        GameConfig.NightDarkness = Slider("Night darkness", GameConfig.NightDarkness, 0f, 1f, "0.00");
        GameConfig.PitchedRoofs = Toggle("Pitched roofs", GameConfig.PitchedRoofs);
        GameConfig.ChimneyChance = Slider("Chimneys", GameConfig.ChimneyChance, 0f, 1f, "0.00");
        GameConfig.BlobShadows = Toggle("Soft blob shadows", GameConfig.BlobShadows);
        GameConfig.AmbientGradient = Toggle("Ambient light gradient", GameConfig.AmbientGradient);
        GameConfig.VignetteStrength = Slider("Vignette", GameConfig.VignetteStrength, 0f, 0.8f, "0.00");
        GameConfig.ParcelGlow = Slider("Parcel glow (0 = off)", GameConfig.ParcelGlow, 0f, 1f, "0.00");
        GameConfig.ParcelSeeThrough = Toggle("Parcel light shows through obstacles", GameConfig.ParcelSeeThrough);
        GameConfig.BoardGlowStrength = Slider("Backboard glow (0 = off)", GameConfig.BoardGlowStrength, 0f, 1f, "0.00");
        GameConfig.HoopXRay = Toggle("Hoop rings show through obstacles", GameConfig.HoopXRay);
        GameConfig.HoopXRayAlpha = Slider("Ring brightness through obstacles", GameConfig.HoopXRayAlpha, 0f, 1f, "0.00");
        GameConfig.SunShadows = Toggle("Sun shadows (daytime)", GameConfig.SunShadows);
        GUILayout.Label("Light, vignette and sun shadows apply at once. Roofs, chimneys and blob shadows apply to lots spawned after the change: Restart run rebuilds the road. If sun shadows show speckles, switch them off.", note);
    }

    void CheatsTab()
    {
        GameConfig.GodMode = Toggle("God mode (no lives lost)", GameConfig.GodMode);
        GameConfig.InfinitePapers = Toggle("Infinite papers", GameConfig.InfinitePapers);
        if (dbg != null) dbg.show = Toggle("FPS and error overlay", dbg.show);
        var gm = GameManager.Instance;
        if (GUILayout.Button("+10 papers", btn, GUILayout.Height(44)) && gm != null) gm.AddPapers(10);
    }
}
