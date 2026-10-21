using UnityEngine;

// GameUI (partial) - The in-run HUD: score card (with the streak multiplier), distance and baskets, the right column (pause button, lives and
// slow-mo meter), streak banner and toast. Built from the shared UiTheme pieces (translucent rounded cards, gold accents).
// The main part of the class is in GameUI.cs.
public partial class GameUI
{
    static readonly Color Ink = new Color(0.14f, 0.12f, 0.08f);
    static readonly Color Glass = new Color(0.05f, 0.08f, 0.16f, 0.62f);   // the dark translucent backing behind HUD text

    GUIStyle hudScore, hudCap, hudChip, hudBadge, hudBanner;
    int hudLastScore;
    float hudPulseAt = -10f;

    void EnsureHudStyles()
    {
        if (hudScore != null) return;
        hudScore = MakeText(label, 42, TextAnchor.MiddleLeft, UiTheme.Gold);
        hudCap = MakeText(label, 13, TextAnchor.MiddleLeft, UiTheme.Dim);
        hudChip = MakeText(label, 20, TextAnchor.MiddleCenter, UiTheme.Cream);
        hudBadge = MakeText(label, 24, TextAnchor.MiddleCenter, Ink);
        hudBanner = MakeText(label, 24, TextAnchor.MiddleCenter, Ink);
    }

    void DrawHud(GameManager gm, float w, float h)
    {
        EnsureHudStyles();
        float x = 20f + insetL;
        float top = 12f + insetT;

        // ---- score card: the score, big, with the streak multiplier as a gold badge; the number pops when points arrive ----
        if (gm.Score != hudLastScore)
        {
            if (gm.Score > hudLastScore) hudPulseAt = Time.unscaledTime;
            hudLastScore = gm.Score;
        }
        float pulse = Mathf.Clamp01(1f - (Time.unscaledTime - hudPulseAt) / 0.3f);
        string scoreStr = gm.Score.ToString("N0");
        float sw = hudScore.CalcSize(new GUIContent(scoreStr)).x;
        bool hasMult = gm.Multiplier > 1;
        float badgeW = 62f;
        var card = new Rect(x, top, Mathf.Max(190f, sw + 40f + (hasMult ? badgeW + 8f : 0f)), 74f);
        UiTheme.Shadow(card, 5f);
        UiTheme.Pill(card, Glass);
        GUI.Label(new Rect(x + 18f, top + 8f, 100f, 16f), "SCORE", hudCap);
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * (1f + 0.16f * pulse), new Vector2(x + 18f, top + 46f));
        UiTheme.Label(new Rect(x + 18f, top + 22f, sw + 24f, 46f), scoreStr, hudScore);
        GUI.matrix = saved;
        if (hasMult)
        {
            var badge = new Rect(card.xMax - badgeW - 10f, top + 9f, badgeW, 40f);
            UiTheme.Pill(badge, UiTheme.Gold);
            GUI.Label(badge, "x" + gm.Multiplier, hudBadge);
        }

        // ---- distance and baskets, under the score ----
        if (!Tutorial.Active)
        {
            string trip = Metres() + " m   \u00B7   " + gm.RunBaskets + (gm.RunBaskets == 1 ? " basket" : " baskets");
            var chip = new Rect(x, top + 84f, hudChip.CalcSize(new GUIContent(trip)).x + 30f, 42f);
            UiTheme.Pill(chip, Glass);
            GUI.Label(chip, trip, hudChip);
        }

        // ---- the right column: the pause button, the lives just below it, and the slow-mo meter just below the lives ----
        int total = Mathf.Max(gm.startingLives, gm.Lives);
        float colW = Mathf.Max(total * 34f + 18f, 130f);
        float colX = w - 20f - insetR - colW;
        var heartsPill = new Rect(colX, top + 70f, colW, 42f);
        UiTheme.Pill(heartsPill, Glass);
        DrawHearts(gm, colX + (colW - (total * 34f - 6f)) * 0.5f, heartsPill.y + 7f);
        HeartsRectUi = new Rect(heartsPill.x - 4f, heartsPill.y - 4f, heartsPill.width + 8f, heartsPill.height + 8f);

        GUI.Label(new Rect(colX + 4f, top + 118f, colW, 14f), "SLOW-MO", hudCap);
        UiTheme.Bar(new Rect(colX, top + 134f, colW, 12f), gm.SlowMoMeter, new Color(1f, 0.85f, 0.2f));

        // ---- streak banner (a gold pill that beats; orange for the super streak) ----
        if (gm.HotStreak)
        {
            string txt = (gm.SuperStreak ? "SUPER HOT STREAK  x" : "HOT STREAK  x") + gm.Multiplier;
            float bw = Mathf.Min(w - 40f, hudBanner.CalcSize(new GUIContent(txt)).x + 52f);
            var banner = new Rect((w - bw) * 0.5f, top + 164f, bw, 42f);
            saved = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 8f)), banner.center);
            UiTheme.Pill(banner, gm.SuperStreak ? new Color(1f, 0.50f, 0.18f) : UiTheme.Gold);
            GUI.Label(banner, txt, hudBanner);
            GUI.matrix = saved;
        }

        if (gm.Playing && gm.RunTime < 15f && !Tutorial.Active)
            UiTheme.Label(new Rect(20, h - 110 - insetB, w - 40, 90),
                      "Pull back and release: the paper flies the opposite way. Longer pull = further.   Tap = bell", hintStyle);

        // ---- toast: the latest bonus or message on a dark pill, fading out ----
        if (!string.IsNullOrEmpty(gm.Toast) && Time.unscaledTime < gm.ToastUntil)
        {
            float fade = Mathf.Clamp01((gm.ToastUntil - Time.unscaledTime) / 0.35f);
            var content = new GUIContent(gm.Toast);
            float tw = Mathf.Min(w - 40f, toastStyle.CalcSize(content).x + 56f);
            float th = toastStyle.CalcHeight(content, tw - 40f) + 18f;
            var pill = new Rect((w - tw) * 0.5f, top + 222f, tw, th);
            Color keep = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);
            UiTheme.Pill(pill, new Color(Glass.r, Glass.g, Glass.b, Glass.a * fade));
            UiTheme.Label(new Rect(pill.x + 20f, pill.y + 9f, tw - 40f, th), gm.Toast, toastStyle);
            GUI.color = keep;
        }

        // ---- pause button (top right) ----
        if (gm.Playing)
        {
            var rect = new Rect(w - 80f - insetR, top, 60f, 60f);
            pauseRectScreen = new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale);
            if (UiTheme.Secondary(rect, "II", 24)) gm.Pause();
        }
    }

    static Texture2D heartTexture;

    // A heart drawn once into a small texture (the default GUI font has no heart symbol).
    static Texture2D HeartTexture()
    {
        if (heartTexture != null) return heartTexture;
        const int n = 48;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int py = 0; py < n; py++)
        {
            for (int px = 0; px < n; px++)
            {
                float u = (px / (n - 1f) * 2f - 1f) * 1.3f;
                float v = (py / (n - 1f) * 2f - 1f) * 1.3f + 0.15f;
                float a = u * u + v * v - 1f;
                bool inside = a * a * a - u * u * v * v * v <= 0f;
                tex.SetPixel(px, py, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();
        heartTexture = tex;
        return tex;
    }

    void DrawHearts(GameManager gm, float x, float y)
    {
        var tex = HeartTexture();
        int total = Mathf.Max(gm.startingLives, gm.Lives);
        for (int i = 0; i < total; i++)
        {
            GUI.color = i < gm.Lives ? new Color(1f, 0.25f, 0.3f) : new Color(0.15f, 0.15f, 0.15f, 0.6f);
            GUI.DrawTexture(new Rect(x + i * 34f, y, 28f, 28f), tex);
        }
        GUI.color = Color.white;
    }
}
