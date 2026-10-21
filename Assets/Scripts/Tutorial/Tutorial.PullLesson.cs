using UnityEngine;

// Tutorial (partial) - Pull lesson
// The very first thing a new player sees: how to throw. The game stands still behind a dark overlay, a yellow circle marks the bike (where the
// drag starts), and a looped animation shows a finger pulling back from it and letting go.
// It loops until the player taps "Got it". While it is open nothing else on the screen works (the pause button, the DEV button, throwing).
public partial class Tutorial
{
    bool pullLesson;

    // True while the pull lesson or the floors overlay is on screen (GameUI and DevMenu switch their buttons off, PaperThrower ignores presses).
    public static bool Modal { get { return Active && inst != null && (inst.pullLesson || inst.floorsIntro || inst.hoopIntro); } }

    static Texture2D ringTex, spotTex;
    GUIStyle gotItS;

    // a soft white ring (tint it with GUI.color)
    static Texture2D RingTexture()
    {
        if (ringTex != null) return ringTex;
        const int n = 128;
        ringTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        ringTex.wrapMode = TextureWrapMode.Clamp;
        ringTex.filterMode = FilterMode.Bilinear;
        ringTex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.88f) / 0.1f);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        ringTex.SetPixels32(px);
        ringTex.Apply(false);
        return ringTex;
    }

    // the dark overlay's hole: clear in the middle, fully dark at the edge and in the corners
    static Texture2D SpotTexture()
    {
        if (spotTex != null) return spotTex;
        const int n = 128;
        spotTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        spotTex.wrapMode = TextureWrapMode.Clamp;
        spotTex.filterMode = FilterMode.Bilinear;
        spotTex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.78f, d)) * DimAlpha;
                px[y * n + x] = new Color32(6, 8, 20, (byte)(a * 255f));
            }
        spotTex.SetPixels32(px);
        spotTex.Apply(false);
        return spotTex;
    }

    const float DimAlpha = 0.86f;
    static readonly Color RingGold = new Color(1f, 0.86f, 0.16f);

    // where the bike is on the screen, in the UI units of the scaled GUI
    static Vector2 BikeGuiPos(float w, float h, float scale)
    {
        var cam = Camera.main;
        var bike = BikeForward.Instance;
        Vector2 p = new Vector2(w * 0.5f, h * 0.60f);
        if (cam != null && bike != null)
        {
            Vector3 s = cam.WorldToScreenPoint(bike.transform.position + Vector3.up * 1.0f);
            if (s.z > 0f) p = new Vector2(s.x / scale, (Screen.height - s.y) / scale);
        }
        p.x = Mathf.Clamp(p.x, 150f, w - 150f);
        p.y = Mathf.Clamp(p.y, 150f, h - 220f);
        return p;
    }

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawPullLesson(float w, float h, float scale, float insetT, float insetB)
    {
        // every press belongs to this screen
        cardRectScreen = new Rect(0f, 0f, Screen.width, Screen.height);
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);

        float t = Time.unscaledTime;
        Vector2 c = BikeGuiPos(w, h, scale);

        // ---- the dark overlay with a clear spot over the bike ----
        const float S = 300f;
        Rect sq = new Rect(c.x - S * 0.5f, c.y - S * 0.5f, S, S);
        Color dim = new Color(6f / 255f, 8f / 255f, 20f / 255f, DimAlpha);
        Fill(new Rect(0f, 0f, w, Mathf.Max(0f, sq.y)), dim);
        Fill(new Rect(0f, sq.yMax, w, Mathf.Max(0f, h - sq.yMax)), dim);
        Fill(new Rect(0f, sq.y, Mathf.Max(0f, sq.x), S), dim);
        Fill(new Rect(sq.xMax, sq.y, Mathf.Max(0f, w - sq.xMax), S), dim);
        GUI.color = Color.white;
        GUI.DrawTexture(sq, SpotTexture());

        // ---- the yellow circle on the bike: where the drag starts ----
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
        float rr = 64f * (1f + 0.07f * pulse);
        GUI.color = RingGold;
        GUI.DrawTexture(new Rect(c.x - rr, c.y - rr, rr * 2f, rr * 2f), RingTexture());
        float q = (t * 0.9f) % 1f;                      // a second ring that spreads out and fades
        float r2 = 64f * (1f + 0.9f * q);
        GUI.color = new Color(RingGold.r, RingGold.g, RingGold.b, 1f - q);
        GUI.DrawTexture(new Rect(c.x - r2, c.y - r2, r2 * 2f, r2 * 2f), RingTexture());
        GUI.color = Color.white;

        // ---- the looped pull ----
        // (the lesson always teaches the pull: see UsePullControls)
        Vector2 pull;
        if (!GhostPull(out pull)) pull = new Vector2(-SideOfStep(0) * 90f, 120f);   // a house on the right: pull left and down
        else if (!GameConfig.SlingshotStyle) pull = -pull;                           // (GhostPull follows the control setting: turn it into the pull)
        Vector2 pdir = pull.sqrMagnitude > 0.01f ? pull.normalized : new Vector2(-0.6f, 0.8f);
        const float pullLen = 130f;
        // keep the finger on the screen
        if (c.x + pdir.x * pullLen < 40f || c.x + pdir.x * pullLen > w - 40f) pdir.x = -pdir.x * 0.4f;

        const float period = 2.6f;
        float p = (t % period) / period;
        float appear = Mathf.Clamp01(p / 0.08f);
        float move = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.10f, 0.50f, p));
        float fingerFade = p < 0.62f ? appear : Mathf.Clamp01(1f - (p - 0.62f) / 0.06f);
        Vector2 pos = c + pdir * (pullLen * move);

        if (fingerFade > 0.01f)
        {
            // the band from where the drag began to the finger
            float len = (pos - c).magnitude;
            if (len > 6f)
            {
                Matrix4x4 saved = GUI.matrix;
                float angle = Mathf.Atan2(pos.y - c.y, pos.x - c.x) * Mathf.Rad2Deg;
                GUIUtility.RotateAroundPivot(angle, c);
                GUI.color = new Color(1f, 1f, 1f, 0.75f * fingerFade);
                GUI.DrawTexture(new Rect(c.x, c.y - 3f, len, 6f), Texture2D.whiteTexture);
                GUI.matrix = saved;
            }
            UiTheme.Pill(new Rect(c.x - 9f, c.y - 9f, 18f, 18f), new Color(1f, 1f, 1f, 0.8f * fingerFade));
            float size = 62f * (p > 0.08f && p < 0.62f ? 1f : 0.85f);
            UiTheme.Pill(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), new Color(1f, 1f, 1f, 0.6f * fingerFade));
            UiTheme.Pill(new Rect(pos.x - size * 0.3f, pos.y - size * 0.3f, size * 0.6f, size * 0.6f), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.95f * fingerFade));
        }

        GUI.color = Color.white;

        // ---- the words and the button (at the top, away from the bike and the animation) ----
        float cw = Mathf.Min(w - 32f, portraitWidthCap(w));
        float ch = 196f;
        Rect card = new Rect((w - cw) * 0.5f, Mathf.Max(insetT + 30f, h * 0.10f), cw, ch);
        UiTheme.Card(card);
        UiTheme.Label(new Rect(card.x + 12f, card.y + 14f, card.width - 24f, 40f), "Pull back to throw", titleS, 1.5f);
        GUI.Label(new Rect(card.x + 24f, card.y + 60f, card.width - 48f, 56f),
            "Press, pull away from the house, then let go. The paper flies toward it.", subS);
        // a bright green button with bold black text
        var b = new Rect(card.center.x - 110f, card.yMax - 68f, 220f, 54f);
        UiTheme.Shadow(b);
        bool down = Input.GetMouseButton(0) && b.Contains(Event.current.mousePosition);
        UiTheme.Pill(b, down ? new Color(0.13f, 0.66f, 0.22f) : new Color(0.20f, 0.88f, 0.32f));
        if (gotItS == null)
        {
            gotItS = new GUIStyle(titleS) { fontSize = 30, fontStyle = FontStyle.Bold };
            gotItS.normal.textColor = Color.black;
        }
        GUI.Label(b, "Got it", gotItS);
        if (GUI.Button(b, GUIContent.none, GUIStyle.none))
        {
            pullLesson = false;
            stepTime = 0f;
            cardRectScreen = new Rect(0f, 0f, 0f, 0f);
            nearLesson = true;                 // step 2: a near shot, hands on, with the game slowed
            nearCached = false;
            GameTime.Lesson = LessonSlow;
            GameTime.Apply();
        }
    }

    static float portraitWidthCap(float w) { return 480f; }

    // A new tutorial teaches the pull, so the throw controls are put on the pull (a choice made earlier in the dev menu is reset).
    static void UsePullControls()
    {
        GameConfig.SlingshotStyle = true;
        try { PlayerPrefs.SetInt("ctl_sling", 1); } catch (System.Exception) { }
        var th = Object.FindFirstObjectByType<PaperThrower>();
        if (th != null) th.slingshotStyle = true;
    }

    // ---- step 2: the near shot ----
    // The overlay is gone and the game runs slowed. Two circles are marked: where the drag starts (on the bike) and where it must be let go
    // (a short pull away from the house). Pressing outside the start circle does nothing, and letting go outside the release circle throws nothing.
    // The first real throw ends the lesson and the game speed goes back to normal.

    bool nearLesson, lessonPlaced;
    const float LessonSlow = 0.3f;        // game speed during the lesson
    const float StartRadiusUi = 46f, ReleaseRadiusUi = 38f;
    const float CircleGapUi = 22f;        // the two circles never touch: this much clear space between them
    bool afterNear;                       // the near throw has been let go and is being judged (a miss starts the lesson again)
    float lessonBikeZ;                    // where the bike stood at the start of the lesson

    static void EndLessonTime()
    {
        GameTime.Lesson = 1f;
        GameTime.Apply();
    }

    void FinishNearLesson()
    {
        nearLesson = false;
        afterNear = true;
        EndLessonTime();
        ghost = false;      // (the old step's ghost hand is not needed: the player has just thrown)
        throws = 1;
    }

    // The bike stands a short way from the first house (a near target) while the lessons run. Done once, as soon as the house exists.
    void PlaceLessonBike(GameManager gm, BikeForward bike)
    {
        if (lessonPlaced || (!pullLesson && !nearLesson) || gm == null || gm.Spawner == null || bike == null) return;
        House h = gm.Spawner.TutorialHouse(1);
        if (h == null || h.gardenTarget == null) return;
        Vector3 tp = h.gardenTarget.transform.position;
        float lateral = Mathf.Abs(tp.x - bike.transform.position.x);
        if (ghostThrower == null) ghostThrower = Object.FindFirstObjectByType<PaperThrower>();
        float want = Mathf.Max(8.5f, lateral * 1.25f);
        if (ghostThrower != null)
        {
            // far enough that the pull to the lawn is longer than the two circles side by side (so they never overlap), still a near target
            var th = ghostThrower;
            bool portrait = Screen.height > Screen.width;
            float scale = portrait ? Screen.width / 540f : Screen.height / 720f;
            float uiRef = Mathf.Min(Screen.width, Screen.height) / scale;
            float needFrac = (StartRadiusUi + ReleaseRadiusUi + CircleGapUi) / uiRef;
            float tt = Mathf.Clamp01((needFrac - th.deadZone) / Mathf.Max(0.01f, th.fullPowerDrag - th.deadZone));
            float dMin = th.minDistance + Mathf.Pow(tt, th.powerCurve) * (th.maxDistance - th.minDistance);
            want = Mathf.Max(want, dMin + 0.8f);
            want = Mathf.Min(want, th.maxDistance - 3f);
        }
        float dz = Mathf.Sqrt(Mathf.Max(0.5f, want * want - lateral * lateral));
        lessonBikeZ = tp.z - dz;
        bike.MoveTo(lessonBikeZ);
        lessonPlaced = true;
    }

    // Where the two circles are, in screen pixels with the origin at the bottom left (the space of Input.mousePosition).
    // The circles are worked out once per attempt and then stay put: the aim zoom (and anything else that moves the camera's projection)
    // must not drag them around while the player pulls.
    bool nearCached;
    Vector2 nearStartPx, nearReleasePx;
    float nearStartR, nearReleaseR;

    bool NearGeometry(out Vector2 startPx, out Vector2 releasePx, out float startR, out float releaseR)
    {
        if (!circleLesson) nearCached = false;
        if (nearCached)
        {
            startPx = nearStartPx; releasePx = nearReleasePx; startR = nearStartR; releaseR = nearReleaseR;
            return true;
        }
        bool ok = longLesson ? ComputeLongGeometry(out startPx, out releasePx, out startR, out releaseR)
                             : ComputeNearGeometry(out startPx, out releasePx, out startR, out releaseR);
        if (ok && circleLesson && (longLesson || lessonPlaced))
        {
            nearCached = true;
            nearStartPx = startPx; nearReleasePx = releasePx; nearStartR = startR; nearReleaseR = releaseR;
        }
        return ok;
    }

    bool ComputeNearGeometry(out Vector2 startPx, out Vector2 releasePx, out float startR, out float releaseR)
    {
        startPx = releasePx = Vector2.zero;
        startR = releaseR = 0f;
        var gm = GameManager.Instance;
        if (gm == null || gm.Spawner == null) return false;
        House h = gm.Spawner.TutorialHouse(1);
        var bike = BikeForward.Instance;
        if (h == null || h.gardenTarget == null || bike == null) return false;
        if (ghostThrower == null) ghostThrower = Object.FindFirstObjectByType<PaperThrower>();
        if (ghostThrower == null) return false;

        bool portrait = Screen.height > Screen.width;
        float scale = portrait ? Screen.width / 540f : Screen.height / 720f;
        float w = Screen.width / scale, hh = Screen.height / scale;
        Vector2 cGui = BikeGuiPos(w, hh, scale);

        // how long the pull has to be for a throw that lands on the lawn (the inverse of PaperThrower.ComputeVelocity)
        Vector3 to = h.gardenTarget.transform.position - bike.transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        var th = ghostThrower;
        float power = Mathf.Clamp01((dist - th.minDistance) / Mathf.Max(0.1f, th.maxDistance - th.minDistance));
        float t = Mathf.Pow(power, 1f / Mathf.Max(0.1f, th.powerCurve));
        float lenFrac = th.deadZone + t * (th.fullPowerDrag - th.deadZone);
        float refPx = Mathf.Min(Screen.width, Screen.height);
        float lenGui = lenFrac * refPx / scale;

        Vector2 pull;
        if (!GhostPull(out pull)) pull = new Vector2(-SideOfStep(0) * 90f, 120f);
        else if (!GameConfig.SlingshotStyle) pull = -pull;
        Vector2 pdir = pull.sqrMagnitude > 0.01f ? pull.normalized : new Vector2(-0.6f, 0.8f);
        Vector2 rGui = cGui + pdir * lenGui;

        startPx = new Vector2(cGui.x * scale, Screen.height - cGui.y * scale);
        releasePx = new Vector2(rGui.x * scale, Screen.height - rGui.y * scale);
        startR = StartRadiusUi * scale;
        releaseR = ReleaseRadiusUi * scale;
        return true;
    }

    // Throwing: a press only starts in the start circle (a little roomy) during the lesson. p: top-left origin, as the other input checks.
    static bool NearBlocks(Vector2 pTopLeft)
    {
        if (!Active || inst == null || !inst.circleLesson) return false;
        Vector2 s, r; float sr, rr;
        if (!inst.NearGeometry(out s, out r, out sr, out rr)) return false;
        Vector2 m = new Vector2(pTopLeft.x, Screen.height - pTopLeft.y);
        return (m - s).magnitude > sr * 1.3f;
    }

    // Letting go: only inside the release circle (a little roomy). m: Input.mousePosition.
    public static bool AllowRelease(Vector2 m)
    {
        if (!Active || inst == null || !inst.circleLesson) return true;
        Vector2 s, r; float sr, rr;
        if (!inst.NearGeometry(out s, out r, out sr, out rr)) return true;
        if ((m - r).magnitude <= rr * 1.3f) return true;
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast("Release in the green circle", 1.6f);
        return false;
    }

    void DrawNearLesson(float w, float h, float scale, float insetT)
    {
        cardRectScreen = new Rect(0f, 0f, 0f, 0f);
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);
        if (hoopMode) DrawHoopDim(w, h, scale);   // (the basket shot: everything dimmed but the board and the ring)
        Vector2 sPx, rPx; float sR, rR;
        if (!NearGeometry(out sPx, out rPx, out sR, out rR)) return;
        Vector2 s = new Vector2(sPx.x / scale, (Screen.height - sPx.y) / scale);
        Vector2 r = new Vector2(rPx.x / scale, (Screen.height - rPx.y) / scale);

        float time = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 5f);
        Vector2 mouse = Input.mousePosition;
        bool pressing = Input.GetMouseButton(0);
        bool inside = pressing && (mouse - rPx).magnitude <= rR;

        // the start circle on the bike (gold)
        float rs = StartRadiusUi * (1f + 0.06f * pulse);
        GUI.color = RingGold;
        GUI.DrawTexture(new Rect(s.x - rs, s.y - rs, rs * 2f, rs * 2f), RingTexture());
        // the release circle (green; it fills in when the finger is inside)
        Color green = new Color(0.25f, 0.95f, 0.42f);
        float rr2 = ReleaseRadiusUi * (1f + 0.06f * (1f - pulse));
        if (inside) UiTheme.Pill(new Rect(r.x - ReleaseRadiusUi, r.y - ReleaseRadiusUi, ReleaseRadiusUi * 2f, ReleaseRadiusUi * 2f), new Color(green.r, green.g, green.b, 0.35f));
        GUI.color = green;
        GUI.DrawTexture(new Rect(r.x - rr2, r.y - rr2, rr2 * 2f, rr2 * 2f), RingTexture());
        float q = (time * 0.9f) % 1f;
        float r3 = ReleaseRadiusUi * (1f + 0.8f * q);
        GUI.color = new Color(green.r, green.g, green.b, 1f - q);
        GUI.DrawTexture(new Rect(r.x - r3, r.y - r3, r3 * 2f, r3 * 2f), RingTexture());
        GUI.color = Color.white;

        // the looped pull from the first circle to the second, until the player starts to press
        if (!pressing)
        {
            const float period = 2.6f;
            float p = (time % period) / period;
            float appear = Mathf.Clamp01(p / 0.08f);
            float move = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.10f, 0.50f, p));
            float fade = p < 0.62f ? appear : Mathf.Clamp01(1f - (p - 0.62f) / 0.06f);
            Vector2 pos = Vector2.Lerp(s, r, move);
            if (fade > 0.01f)
            {
                float len = (pos - s).magnitude;
                if (len > 6f)
                {
                    Matrix4x4 saved = GUI.matrix;
                    float angle = Mathf.Atan2(pos.y - s.y, pos.x - s.x) * Mathf.Rad2Deg;
                    GUIUtility.RotateAroundPivot(angle, s);
                    GUI.color = new Color(1f, 1f, 1f, 0.75f * fade);
                    GUI.DrawTexture(new Rect(s.x, s.y - 3f, len, 6f), Texture2D.whiteTexture);
                    GUI.matrix = saved;
                }
                UiTheme.Pill(new Rect(s.x - 9f, s.y - 9f, 18f, 18f), new Color(1f, 1f, 1f, 0.8f * fade));
                float size = 62f * (p > 0.08f && p < 0.62f ? 1f : 0.85f);
                UiTheme.Pill(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), new Color(1f, 1f, 1f, 0.6f * fade));
                UiTheme.Pill(new Rect(pos.x - size * 0.3f, pos.y - size * 0.3f, size * 0.6f, size * 0.6f), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.95f * fade));
            }
        }
        GUI.color = Color.white;

        // the words, at the top
        float cw = Mathf.Min(w - 32f, 480f);
        Rect card = new Rect((w - cw) * 0.5f, Mathf.Max(insetT + 36f, h * 0.10f), cw, 158f);
        DrawNote(card, hoopMode ? "Basket shot" : (longLesson ? "Long shot" : "Near shot"),
                 hoopMode ? "Pull to the green circle. Release into the basket." :
                 longLesson ? "Pull farther to reach the top balcony." : "Pull back to the green circle. Release.", 68f);
    }

    // A paper came to rest without scoring (called by Paper): during the near-shot lesson that starts it again.
    public static void OnPaperFailed()
    {
        if (Active && inst != null && inst.afterNear && inst.moveKind == 0 && !inst.transitioning) inst.RetryNear();
        else if (Active && inst != null) inst.LongPaperFailed();
    }

    void RetryNear()
    {
        afterNear = false;
        StartFade(3);
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast("Missed. Try again.", 2.2f);
    }

    // The quick black fade when the lesson starts again (the same fade the other steps draw at the end of OnGUI).
    void DrawFadeOverlay(float w, float h)
    {
        float since = Time.unscaledTime - fadeStart;
        if (since < 0f || since >= 0.8f) return;
        float a = since < 0.25f ? since / 0.25f : (since < 0.4f ? 1f : 1f - (since - 0.4f) / 0.4f);
        GUI.depth = -60;
        GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
