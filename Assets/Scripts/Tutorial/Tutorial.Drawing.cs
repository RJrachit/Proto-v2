using UnityEngine;

// Tutorial (partial) - Drawing
// The main part of the class is in Tutorial.cs.
public partial class Tutorial
{
    // ---- drawing ----

    GUIStyle noteTitleS, noteBodyS;

    // A step card: a sticky note of torn paper with a strip of tape, a title in marker pen and the words in handwriting, in black.
    // bodyH: the height of the words' area.
    void DrawNote(Rect card, string title, string body, float bodyH)
    {
        UiTheme.Note(card);
        if (noteTitleS == null)
        {
            var ink = new Color(0.10f, 0.09f, 0.08f);
            noteTitleS = new GUIStyle(titleS) { fontSize = 32, fontStyle = FontStyle.Normal };
            noteBodyS = new GUIStyle(subS) { fontSize = 27, fontStyle = FontStyle.Normal };
            noteTitleS.normal.textColor = ink;
            noteBodyS.normal.textColor = ink;
            if (UiTheme.NoteTitleFont != null) noteTitleS.font = UiTheme.NoteTitleFont;
            if (UiTheme.NoteBodyFont != null) noteBodyS.font = UiTheme.NoteBodyFont;
        }
        GUI.Label(new Rect(card.x + 16f, card.y + 20f, card.width - 32f, 46f), title, noteTitleS);
        GUI.Label(new Rect(card.x + 28f, card.y + 68f, card.width - 56f, bodyH), body, noteBodyS);
    }

    void EnsureStyles()
    {
        if (titleS != null) return;
        titleS = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
        titleS.normal.textColor = UiTheme.Gold;
        skipS = new GUIStyle(titleS) { fontSize = 15, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleRight };
        skipS.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
        subS = new GUIStyle(titleS) { fontSize = 19, fontStyle = FontStyle.Normal, wordWrap = true, alignment = TextAnchor.UpperCenter };
        subS.normal.textColor = UiTheme.Cream;
        hintS = new GUIStyle(subS) { fontSize = 18, fontStyle = FontStyle.Bold };
        hintS.normal.textColor = new Color(0.08f, 0.40f, 0.22f);   // (dark green: it sits on the paper note)
        tileS = new GUIStyle(titleS) { fontSize = 18 };
        tileS.normal.textColor = UiTheme.Cream;
        tileDimS = new GUIStyle(tileS);
        tileDimS.normal.textColor = UiTheme.Dim;
        praiseS = new GUIStyle(titleS) { fontSize = 48 };
        praiseS.normal.textColor = UiTheme.Gold;
    }

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (!Active || gm == null || gm.State != GameManager.RunState.Playing || gm.Paused)
        {
            if (!Active || gm == null || gm.Paused) { cardRectScreen = new Rect(0f, 0f, 0f, 0f); extraRectScreen = new Rect(0f, 0f, 0f, 0f); }
            return;
        }
        UiTheme.Ensure();
        EnsureStyles();
        GUI.enabled = true;
        GUI.depth = -50;

        bool portrait = Screen.height > Screen.width;
        float scale = portrait ? Screen.width / 540f : Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;
        Rect safe = Screen.safeArea;
        float insetL = safe.x / scale, insetR = (Screen.width - safe.xMax) / scale, insetB = safe.y / scale, insetT = (Screen.height - safe.yMax) / scale;

        // the pull lesson comes first: the game stands still behind a dark overlay
        if (pullLesson) { DrawPullLesson(w, h, scale, insetT, insetB); DrawFadeOverlay(w, h); return; }
        if (nearLesson || longLesson) { DrawNearLesson(w, h, scale, insetT); DrawFadeOverlay(w, h); return; }
        if (floorsIntro) { DrawFloorsIntro(w, h, scale, insetT, insetB); DrawFadeOverlay(w, h); return; }
        if (hoopIntro) { DrawHoopIntro(w, h, scale, insetT, insetB); DrawFadeOverlay(w, h); return; }
        if (step == 2 && (afterLong || groundPhase)) { DrawLongAfter(w, h, scale, insetT); DrawFadeOverlay(w, h); return; }

        // the first card of the intro: scoring and non-scoring spots side by side, filling the screen
        if (step == 0 && introPhase == 0)
        {
            DrawIntroSlide(w, h, scale, insetT);
            return;
        }

        bool caption = step == 0 || step >= 4;
        float ch = caption ? 270f : 250f;   // the paper note: room for the title, up to four lines of handwriting and the hint or the button
        float cw = Mathf.Min(w - 32f - insetL - insetR, portrait ? 480f : 560f);
        var card = new Rect((w - cw) * 0.5f, h - ch - 22f - insetB, cw, ch);
        if (step == 0)
        {
            ch = 250f;
            card = new Rect((w - cw) * 0.5f, 160f + insetT, cw, ch);   // at the top, so it never covers the glowing spots down the road
        }

        // the ghost hand shows the drag on the first steps, until the player has tried
        if (ghost && IsHouseStep(step) && !Input.GetMouseButton(0)) DrawGhost(w, h, SideOfStep(step - 1));

        // names on the circles of the intro step
        if (step == 0) { DrawDim(w, h); DrawLabels(scale); }

        // highlight the lives on the lives step
        if (step == 5 && GameUI.HeartsRectUi.width > 0f)
        {
            Rect r = GameUI.HeartsRectUi;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            GUI.color = new Color(1f, 0.82f, 0.25f, 0.55f + 0.45f * pulse);
            float t = 3f;
            GUI.DrawTexture(new Rect(r.x - t, r.y - t, r.width + 2f * t, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x - t, r.yMax, r.width + 2f * t, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x - t, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // the card: a sticky note of torn paper, like the pull lesson (no progress dots)
        cardRectScreen = new Rect(card.x * scale, card.y * scale, card.width * scale, card.height * scale);
        DrawNote(card, Titles[step], ForControls(Subs[step]), 125f);
        if (IsHouseStep(step) && throws >= 3)
            GUI.Label(new Rect(card.x + 24f, card.yMax - 52f, card.width - 48f, 40f), ForControls(Hints[step - 1]), hintS);

        // buttons on the caption steps
        if (caption)
        {
            var b = new Rect(card.center.x - 110f, card.yMax - 70f, 220f, 56f);
            string text = step == StepCount - 1 ? "Start riding" : "Got it";
            if (UiTheme.Primary(b, text, 28))
            {
                if (step == StepCount - 1) Leave();
                else if (step == 0) Advance(null);
                else Advance(null);
            }
        }

        // skip (not on the last card, which already ends the tutorial)
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);
        if (step < StepCount - 1)
        {
            if (SkipButton(new Rect(card.xMax - 70f, card.y + 6f, 56f, 20f), true)) { Leave(); return; }
        }

        // the quick fade when the bike is put back
        float since = Time.unscaledTime - fadeStart;
        if (since >= 0f && since < 0.8f)
        {
            float a = since < 0.25f ? since / 0.25f : (since < 0.4f ? 1f : 1f - (since - 0.4f) / 0.4f);
            GUI.depth = -60;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    // ---- the intro slide: what scores and what does not, side by side ----

    // a rounded tile with a glow behind it (glow 0 = none)
    static void Tile(Rect r, Color fill, float glow)
    {
        if (glow > 0.01f)
        {
            Rect halo = new Rect(r.x - 6f, r.y - 6f, r.width + 12f, r.height + 12f);
            UiTheme.Pill(halo, new Color(1f, 0.88f, 0.25f, 0.55f * glow));
        }
        UiTheme.Pill(r, fill);
    }

    static Color Dull(Color c) { Color g = new Color(0.42f, 0.43f, 0.46f); return Color.Lerp(c, g, 0.62f); }

    void DrawIntroSlide(float w, float h, float scale, float insetT)
    {
        // darken the whole picture and take every press
        GUI.color = new Color(0.02f, 0.03f, 0.08f, 0.86f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        cardRectScreen = new Rect(0f, 0f, Screen.width, Screen.height);
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);

        float pw = Mathf.Min(w - 24f, 520f);
        float ph = 560f;
        var panel = new Rect((w - pw) * 0.5f, Mathf.Max(insetT + 10f, (h - ph) * 0.5f), pw, ph);
        UiTheme.Card(panel);

        float t = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);

        UiTheme.Label(new Rect(panel.x, panel.y + 14f, panel.width, 44f), "Which spots score?", titleS, 1.5f);

        float colW = (pw - 48f - 16f) * 0.5f;
        float xL = panel.x + 24f, xR = xL + colW + 16f;
        float y0 = panel.y + 70f;

        // column headers
        var hl = new Rect(xL, y0, colW, 34f);
        var hr = new Rect(xR, y0, colW, 34f);
        UiTheme.Pill(hl, UiTheme.Mint);
        UiTheme.Pill(hr, new Color(0.30f, 0.32f, 0.38f));
        GUI.Label(hl, "SCORES", tileHeadDark());
        GUI.Label(hr, "NO SCORE", tileDimS);

        float rowH = 124f, tileH = 66f;
        float y = y0 + 48f;

        // row 1: lawn | house wall
        var a = new Rect(xL, y, colW, tileH);
        Tile(a, Color.Lerp(GameConfig.PadColor, Color.white, 0.18f + 0.35f * pulse), 0.35f + 0.65f * pulse);
        Sparkles(a, t);
        GUI.Label(new Rect(xL, y + tileH + 6f, colW, 26f), "Lawn  +" + GameConfig.PointsGarden, tileS);
        var b = new Rect(xR, y, colW, tileH);
        Tile(b, Dull(new Color(0.98f, 0.85f, 0.70f)), 0f);
        UiTheme.Pill(new Rect(b.x + 18f, b.y + 14f, 34f, 38f), Dull(new Color(0.55f, 0.75f, 0.9f)) * 0.9f);
        UiTheme.Pill(new Rect(b.xMax - 52f, b.y + 14f, 34f, 38f), Dull(new Color(0.55f, 0.75f, 0.9f)) * 0.9f);
        GUI.Label(new Rect(xR, y + tileH + 6f, colW, 26f), "House wall  +0", tileDimS);
        y += rowH;

        // row 2: doormat | window
        a = new Rect(xL, y, colW, tileH);
        Tile(a, Color.Lerp(new Color(0.80f, 0.62f, 0.40f), Color.white, 0.18f + 0.35f * pulse), 0.35f + 0.65f * pulse);
        UiTheme.Pill(new Rect(a.x + 12f, a.center.y - 4f, a.width - 24f, 8f), new Color(1f, 1f, 1f, 0.35f));
        Sparkles(a, t + 0.4f);
        GUI.Label(new Rect(xL, y + tileH + 6f, colW, 26f), "Doormat  +" + GameConfig.PointsDoorstep, tileS);
        b = new Rect(xR, y, colW, tileH);
        Tile(b, Dull(new Color(0.55f, 0.75f, 0.9f)), 0f);
        UiTheme.Pill(new Rect(b.center.x - 2f, b.y + 8f, 4f, b.height - 16f), new Color(0.2f, 0.22f, 0.26f, 0.9f));
        UiTheme.Pill(new Rect(b.x + 14f, b.center.y - 2f, b.width - 28f, 4f), new Color(0.2f, 0.22f, 0.26f, 0.9f));
        GUI.Label(new Rect(xR, y + tileH + 6f, colW, 26f), "Window  +0", tileDimS);
        y += rowH;

        // row 3: mailbox | road
        a = new Rect(xL, y, colW, tileH);
        Tile(a, new Color(0.22f, 0.28f, 0.40f), 0.35f + 0.65f * pulse);
        Color mb = Color.Lerp(GameConfig.MailboxColor, Color.white, 0.12f + 0.40f * pulse);
        UiTheme.Pill(new Rect(a.center.x - 4f, a.center.y - 4f, 8f, 28f), new Color(0.35f, 0.25f, 0.18f));
        UiTheme.Pill(new Rect(a.center.x - 24f, a.center.y - 26f, 48f, 28f), mb);
        UiTheme.Pill(new Rect(a.center.x + 18f, a.center.y - 34f, 6f, 18f), mb);
        Sparkles(a, t + 0.8f);
        GUI.Label(new Rect(xL, y + tileH + 6f, colW, 26f), "Mailbox  +" + GameConfig.PointsMailbox, tileS);
        b = new Rect(xR, y, colW, tileH);
        Tile(b, new Color(0.24f, 0.24f, 0.27f), 0f);
        for (int i = 0; i < 4; i++) UiTheme.Pill(new Rect(b.x + 20f + i * 48f, b.center.y - 3f, 26f, 6f), new Color(0.6f, 0.6f, 0.6f, 0.7f));
        GUI.Label(new Rect(xR, y + tileH + 6f, colW, 26f), "Road  +0", tileDimS);

        // the divider between the columns
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(xL + colW + 7f, y0, 2f, rowH * 3f + 40f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // buttons
        if (UiTheme.Primary(new Rect(panel.center.x - 110f, panel.yMax - 74f, 220f, 56f), "Got it", 28))
        {
            introPhase = 1;
            stepTime = 0f;
        }
        if (SkipButton(new Rect(panel.xMax - 70f, panel.y + 6f, 56f, 20f))) Leave();
    }

    // A small, flat "Skip" chip that sits fully inside the card's top-right corner (the themed button's shadow and rim stuck out of the card).
    bool SkipButton(Rect r, bool onPaper = false)
    {
        // plain text, no shape: nothing to overlap the card edge or the title
        bool hot = r.Contains(Event.current.mousePosition);
        skipS.normal.textColor = onPaper ? new Color(0.10f, 0.09f, 0.08f, hot ? 1f : 0.6f) : new Color(1f, 1f, 1f, hot ? 1f : 0.7f);
        GUI.Label(r, "Skip", skipS);
        return GUI.Button(new Rect(r.x - 10f, r.y - 6f, r.width + 20f, r.height + 12f), GUIContent.none, GUIStyle.none);   // a roomier tap area than the text
    }

    GUIStyle tileHead;
    GUIStyle tileHeadDark()
    {
        if (tileHead == null)
        {
            tileHead = new GUIStyle(tileS);
            tileHead.normal.textColor = UiTheme.Navy;
        }
        return tileHead;
    }

    // a few white glints crossing a glowing tile
    static void Sparkles(Rect r, float t)
    {
        for (int i = 0; i < 3; i++)
        {
            float p = (t * 0.7f + i * 0.33f) % 1f;
            float x = Mathf.Lerp(r.x + 8f, r.xMax - 14f, p);
            float y = r.y + 10f + (i * 17f) % (r.height - 22f);
            float s = 8f * Mathf.Sin(p * Mathf.PI);
            UiTheme.Pill(new Rect(x - s * 0.5f, y - s * 0.5f, s, s), new Color(1f, 1f, 1f, 0.9f));
        }
    }

    GUIStyle labelS, labelHitS, labelDimS;
    Texture2D dimTex;
    Color32[] dimPx;
    const int DW = 48, DH = 48;
    readonly Vector2[] holePx = new Vector2[MaxRings];
    readonly float[] holeRad = new float[MaxRings];

    // The intro step darkens the whole picture except around the hit zones (a soft spotlight on each), so the zones that score stand out from everything that does not.
    void DrawDim(float w, float h)
    {
        var cam = Camera.main;
        if (cam == null || holeCount == 0) return;
        if (dimTex == null)
        {
            dimTex = new Texture2D(DW, DH, TextureFormat.RGBA32, false);
            dimTex.wrapMode = TextureWrapMode.Clamp;
            dimTex.filterMode = FilterMode.Bilinear;
            dimTex.hideFlags = HideFlags.HideAndDontSave;
            dimPx = new Color32[DW * DH];
        }
        Vector3 cr = cam.transform.right;
        cr.y = 0f;
        cr = cr.sqrMagnitude > 0.0001f ? cr.normalized : Vector3.right;
        float minRad = Screen.height * 0.05f;
        for (int i = 0; i < holeCount; i++)
        {
            Vector3 s = cam.WorldToScreenPoint(holeC[i]);
            Vector3 s2 = cam.WorldToScreenPoint(holeC[i] + cr * holeR[i] * 1.6f);
            holePx[i] = new Vector2(s.x, s.y);
            holeRad[i] = s.z > 0f ? Mathf.Max(minRad, Mathf.Abs(s2.x - s.x)) : 0.0001f;
        }
        float fade = Mathf.Clamp01(stepTime / 0.7f);
        for (int y = 0; y < DH; y++)
        {
            for (int x = 0; x < DW; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / DW * Screen.width, (y + 0.5f) / DH * Screen.height);
                float m = 9f;
                for (int i = 0; i < holeCount; i++)
                {
                    float d = (p - holePx[i]).magnitude / holeRad[i];
                    if (d < m) m = d;
                }
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1.8f, m)) * 0.62f * fade;
                dimPx[y * DW + x] = new Color32(6, 8, 20, (byte)(a * 255f));
            }
        }
        dimTex.SetPixels32(dimPx);
        dimTex.Apply(false);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0f, 0f, w, h), dimTex, ScaleMode.StretchToFill);
    }

    // Small name tags over the circles (the intro step), placed from the circles' world positions.
    void DrawLabels(float scale)
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (labelS == null)
        {
            labelS = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            labelS.normal.textColor = UiTheme.Gold;
        }
        if (labelHitS == null)
        {
            labelHitS = new GUIStyle(labelS);
            labelHitS.normal.textColor = UiTheme.Navy;
            labelDimS = new GUIStyle(labelS) { fontSize = 16 };
            labelDimS.normal.textColor = UiTheme.Dim;
        }
        float beat = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
        for (int i = 0; i < labelCount; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(labelPos[i]);
            if (sp.z <= 0f) continue;
            float x = sp.x / scale, y = (Screen.height - sp.y) / scale;
            if (labelHit[i])
            {
                var r = new Rect(x - 56f * beat, y - 36f, 112f * beat, 30f);
                UiTheme.Pill(r, UiTheme.Gold);
                GUI.Label(r, labelText[i], labelHitS);
            }
            else
            {
                var r = new Rect(x - 80f, y - 18f, 160f, 28f);
                UiTheme.Pill(r, new Color(0.12f, 0.13f, 0.17f, 0.9f));
                GUI.Label(r, labelText[i], labelDimS);
            }
        }
    }

    PaperThrower ghostThrower;

    // The drag that sends the paper at this step's house, in screen units (y down): the throw heading is worked out exactly the way
    // PaperThrower.ComputeVelocity does it (the pull's screen axes turned by the camera's yaw), so the hand drags back from the target
    // whatever the camera. The length grows with the distance, like a real pull.
    bool GhostPull(out Vector2 pull)
    {
        pull = Vector2.zero;
        var gm = GameManager.Instance;
        var bike = BikeForward.Instance;
        if (gm == null || gm.Spawner == null || bike == null) return false;
        House h = gm.Spawner.TutorialHouse(Mathf.Max(1, step));
        if (h == null || h.gardenTarget == null) return false;
        if (ghostThrower == null) ghostThrower = Object.FindFirstObjectByType<PaperThrower>();

        Vector3 to = h.gardenTarget.transform.position - bike.transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.5f) return false;
        float yaw = 0f;
        var view = CameraFollow.Instance;
        if (view != null && !view.firstPerson) yaw = view.yaw;
        Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * (to / dist);                 // undo the camera's turn
        Transform axes = ghostThrower != null ? ghostThrower.transform : bike.transform;   // (the thrower's own axes, as in ComputeVelocity)
        Vector2 aim = new Vector2(Vector3.Dot(local, axes.right), Vector3.Dot(local, axes.forward));   // the throw, in screen axes (y up)
        if (aim.sqrMagnitude < 0.0001f) return false;
        aim.Normalize();
        bool sling = ghostThrower == null || ghostThrower.slingshotStyle;
        Vector2 drag = sling ? -aim : aim;
        float len = Mathf.Lerp(70f, 150f, Mathf.InverseLerp(GameConfig.MinThrowDistance, GameConfig.MaxThrowDistance, dist));
        pull = new Vector2(drag.x, -drag.y) * len;                                    // GUI y runs down
        return true;
    }

    // A hand-shaped dot drags back (away from the house), the paper line follows, then it lets go.
    void DrawGhost(float w, float h, int side)
    {
        const float period = 2.4f;
        float p = (Time.unscaledTime % period) / period;
        Vector2 from = new Vector2(w * 0.5f, h * 0.50f);
        // the pull points away from the target (the paper flies the opposite way); without a target, a house on the right means pulling left and down
        Vector2 to = from + new Vector2(-side * 90f, 120f);
        Vector2 pull;
        if (GhostPull(out pull)) to = from + pull;

        float move = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.62f, p));
        float alpha = p < 0.1f ? p / 0.1f : (p > 0.86f ? Mathf.Clamp01((1f - p) / 0.14f) : 1f);
        Vector2 pos = Vector2.Lerp(from, to, move);

        Matrix4x4 saved = GUI.matrix;
        float len = (pos - from).magnitude;
        if (len > 6f)
        {
            float angle = Mathf.Atan2(pos.y - from.y, pos.x - from.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.color = new Color(1f, 1f, 1f, 0.55f * alpha);
            GUI.DrawTexture(new Rect(from.x, from.y - 3f, len, 6f), Texture2D.whiteTexture);
            GUI.matrix = saved;
        }
        UiTheme.Pill(new Rect(from.x - 9f, from.y - 9f, 18f, 18f), new Color(1f, 1f, 1f, 0.7f * alpha));
        float press = p > 0.1f && p < 0.8f ? 1f : 0.8f;
        float size = 54f * press;
        UiTheme.Pill(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), new Color(1f, 1f, 1f, 0.55f * alpha));
        UiTheme.Pill(new Rect(pos.x - size * 0.3f, pos.y - size * 0.3f, size * 0.6f, size * 0.6f), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.9f * alpha));
        GUI.color = Color.white;
    }
}
