using UnityEngine;

// GameUI (partial) - The in-run HUD: score card (with the streak multiplier), distance and baskets, the right column (pause button, lives and
// slow-mo meter), streak banner and toast. Built from the shared UiTheme pieces (translucent rounded cards, gold accents).
// The main part of the class is in GameUI.cs.
public partial class GameUI
{
    static readonly Color Ink = new Color(0.14f, 0.12f, 0.08f);
    static readonly Color Glass = new Color(0.07f, 0.09f, 0.22f, 0.80f);   // the dark translucent backing behind HUD text

    GUIStyle hudScore, hudCap, hudChip, hudBadge, hudBanner;
    int hudLastScore;
    float hudPulseAt = -10f;

    void EnsureHudStyles()
    {
        if (hudScore != null) return;
        hudScore = MakeText(label, 46, TextAnchor.MiddleLeft, UiTheme.Gold);
        hudCap = MakeText(label, 14, TextAnchor.MiddleLeft, new Color(0.62f, 0.68f, 0.88f));
        hudChip = MakeText(label, 23, TextAnchor.MiddleCenter, UiTheme.Cream);
        hudBadge = MakeText(label, 27, TextAnchor.MiddleCenter, Ink);
        hudBanner = MakeText(label, 27, TextAnchor.MiddleCenter, Ink);
        hudChip.richText = true;
        // the same rounded cartoon font as the buttons and the title (Lilita One has no bold of its own, so plain)
        Font f = UiTheme.ButtonFont;
        if (f != null)
            foreach (var s in new[] { hudScore, hudCap, hudChip, hudBadge, hudBanner, toastStyle })
            {
                s.font = f;
                s.fontStyle = FontStyle.Normal;
            }
    }

    const string NumHex = "#FFD447";   // the numbers in the chips are gold, the words dim

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
        GUIUtility.ScaleAroundPivot(Vector2.one * (1f + (0.16f + 0.22f * gm.Combo) * pulse), new Vector2(x + 18f, top + 46f));
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
            string trip = "<color=" + NumHex + ">" + Metres() + "</color> m    <color=" + NumHex + ">" + gm.RunBaskets + "</color>" + (gm.RunBaskets == 1 ? " basket" : " baskets");
            var chip = new Rect(x, top + 84f, hudChip.CalcSize(new GUIContent(trip)).x + 30f, 42f);
            UiTheme.Pill(chip, Glass);
            GUI.Label(chip, trip, hudChip);

            // the coins earned in this run: a coin and +N
            string coinTxt = "<color=" + NumHex + ">+" + gm.RunCoins + "</color>";
            float coinTw = hudChip.CalcSize(new GUIContent(coinTxt)).x;
            var coinChip = new Rect(x, top + 134f, coinTw + 62f, 42f);
            UiTheme.Pill(coinChip, Glass);
            coinTarget = new Vector2(coinChip.x + 23f, coinChip.y + 21f);
            float cp = Mathf.Clamp01(1f - (Time.unscaledTime - coinPulseAt) / 0.28f);   // the icon pops each time a bit of dust arrives
            if (cp > 0f) DrawDot(coinTarget, 44f + 20f * cp, new Color(1f, 0.85f, 0.25f, 0.5f * cp));
            DrawCoinIcon(Grow(new Rect(coinChip.x + 10f, coinChip.y + 8f, 26f, 26f), 1f + 0.28f * cp));
            GUI.Label(new Rect(coinChip.x + 40f, coinChip.y, coinTw + 14f, coinChip.height), coinTxt, hudChip);
        }
        else coinTarget = Vector2.zero;

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
            string txt = gm.SuperStreak ? "SUPER HOT" : "HOT STREAK";   // (the flames come after the words)
            GameManager.ToastIcon flames = gm.SuperStreak ? GameManager.ToastIcon.FireDouble : GameManager.ToastIcon.Fire;
            const float flameSize = 30f;
            float textW = hudBanner.CalcSize(new GUIContent(txt)).x;
            float contentW = textW + 6f + IconsWidth(flames, flameSize);
            float bw = Mathf.Min(w - 40f, contentW + 44f);
            var banner = new Rect((w - bw) * 0.5f, top + 164f, bw, 42f);
            saved = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 8f)), banner.center);
            UiTheme.Pill(banner, gm.SuperStreak ? new Color(1f, 0.50f, 0.18f) : UiTheme.Gold);
            float bx0 = banner.center.x - contentW * 0.5f;
            GUI.Label(new Rect(bx0, banner.y, textW + 4f, banner.height), txt, hudBanner);
            DrawIcons(flames, bx0 + textW + 6f, banner.center.y - flameSize * 0.5f - 1f, flameSize, 1f, true);
            GUI.matrix = saved;
        }

        // ---- toast: the latest bonus or message on a dark pill, fading out ----
        if (!string.IsNullOrEmpty(gm.Toast) && Time.unscaledTime < gm.ToastUntil)
        {
            float fade = Mathf.Clamp01((gm.ToastUntil - Time.unscaledTime) / 0.35f);
            var content = new GUIContent(gm.Toast);
            const float iconSize = 34f;
            float iconW = IconsWidth(gm.ToastIconKind, iconSize);
            float extra = iconW > 0f ? iconW + 10f : 0f;   // (room for the icon(s) after the words)
            float tw = Mathf.Min(w - 40f, toastStyle.CalcSize(content).x + extra + 56f);
            float th = toastStyle.CalcHeight(content, tw - 40f - extra) + 18f;
            var pill = new Rect((w - tw) * 0.5f, top + 222f, tw, th);
            Color keep = GUI.color;
            // the popup punches in (bigger, then settles) and stays bigger the hotter the streak is
            float pop = Mathf.Clamp01(1f - (Time.unscaledTime - gm.ToastAt) / 0.25f);
            float popScale = (1f + 0.30f * gm.Combo) * (1f + (0.20f + 0.35f * gm.Combo) * pop * pop);
            Matrix4x4 toastSaved = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * popScale, pill.center);
            GUI.color = new Color(1f, 1f, 1f, fade);
            UiTheme.Pill(pill, new Color(Glass.r, Glass.g, Glass.b, Glass.a * fade));
            float textW = Mathf.Min(tw - 40f - extra, toastStyle.CalcSize(content).x + 4f);
            float startX = pill.center.x - (textW + extra) * 0.5f;
            UiTheme.Label(new Rect(startX, pill.y + 9f, textW, th), gm.Toast, toastStyle);
            if (iconW > 0f) DrawIcons(gm.ToastIconKind, startX + textW + 10f, pill.center.y - iconSize * 0.5f, iconSize, fade);
            GUI.color = keep;
            GUI.matrix = toastSaved;
        }

        DrawCoinDust(w, h);

        // ---- pause button (top right) ----
        if (gm.Playing)
        {
            var rect = new Rect(w - 80f - insetR, top, 60f, 60f);
            pauseRectScreen = new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale);
            if (UiTheme.Tinted(rect, "II", 24, UiTheme.Tone.Green)) gm.Pause();
        }
    }

    // ---- coin dust: tiny golden specks that fly from where coins were earned up to the coin counter (no coin is drawn) ----

    struct Speck { public float t0, dur, sx, sy, cx, cy, size, seed; }
    struct DustReq { public Vector3 world; public int n; }
    static readonly System.Collections.Generic.List<DustReq> dustReqs = new System.Collections.Generic.List<DustReq>();
    static readonly System.Collections.Generic.List<Speck> specks = new System.Collections.Generic.List<Speck>();
    Vector2 coinTarget;          // the centre of the coin icon in the HUD (zero when it is not shown)
    float coinPulseAt = -10f;
    static Texture2D dotTex;

    // Called when coins are earned: the dust starts at the world position (turned into a screen spot on the next HUD draw).
    public static void AddCoinDust(Vector3 world, int coins)
    {
        if (coins <= 0 || dustReqs.Count > 12) return;
        dustReqs.Add(new DustReq { world = world, n = Mathf.Clamp(10 + coins * 4, 12, 40) });
    }

    // a soft round dot, drawn once
    static Texture2D DotTexture()
    {
        if (dotTex != null) return dotTex;
        const int n = 32;
        dotTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        dotTex.wrapMode = TextureWrapMode.Clamp;
        dotTex.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                dotTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
        dotTex.Apply();
        return dotTex;
    }

    static void DrawDot(Vector2 c, float size, Color col)
    {
        Color keep = GUI.color;
        GUI.color = col;
        GUI.DrawTexture(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), DotTexture());
        GUI.color = keep;
    }

    void DrawCoinDust(float w, float h)
    {
        if (coinTarget == Vector2.zero) { dustReqs.Clear(); specks.Clear(); return; }   // (no coin counter shown: the tutorial earns no coins)
        float now = Time.unscaledTime;

        // new requests become specks: they start round the target's spot on the screen and each flies along its own gentle curve
        if (dustReqs.Count > 0)
        {
            var cam = Camera.main;
            foreach (var rq in dustReqs)
            {
                Vector2 s0 = new Vector2(w * 0.5f, h * 0.45f);
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(rq.world + Vector3.up * 0.6f);
                    if (sp.z > 0.1f) s0 = new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
                }
                for (int i = 0; i < rq.n; i++)
                {
                    var sk = new Speck();
                    sk.t0 = now + Random.Range(0f, 0.30f);
                    sk.dur = Random.Range(0.55f, 0.95f);
                    sk.sx = s0.x + Random.Range(-16f, 16f);
                    sk.sy = s0.y + Random.Range(-14f, 14f);
                    Vector2 mid = (new Vector2(sk.sx, sk.sy) + coinTarget) * 0.5f;
                    Vector2 dir = (coinTarget - new Vector2(sk.sx, sk.sy));
                    Vector2 perp = dir.sqrMagnitude > 1f ? new Vector2(-dir.y, dir.x).normalized : Vector2.right;
                    Vector2 ctrl = mid + perp * Random.Range(-70f, 70f) + new Vector2(0f, -Random.Range(10f, 60f));   // a gentle curve, bowing upwards
                    sk.cx = ctrl.x; sk.cy = ctrl.y;
                    sk.size = Random.Range(2.5f, 4.5f);
                    sk.seed = Random.value * 6.28f;
                    specks.Add(sk);
                }
            }
            dustReqs.Clear();
        }

        for (int i = specks.Count - 1; i >= 0; i--)
        {
            Speck sk = specks[i];
            float u = (now - sk.t0) / sk.dur;
            if (u < 0f) continue;
            if (u >= 1f) { coinPulseAt = now; specks.RemoveAt(i); continue; }
            float e = u * u * (3f - 2f * u);                            // eases out of the target and into the counter
            for (int tr = 2; tr >= 0; tr--)                             // a short fading trail behind the speck
            {
                float et = Mathf.Max(0f, e - tr * 0.035f);
                float om = 1f - et;
                float px = om * om * sk.sx + 2f * om * et * sk.cx + et * et * coinTarget.x;
                float py = om * om * sk.sy + 2f * om * et * sk.cy + et * et * coinTarget.y;
                float fade = Mathf.Clamp01(u * 8f) * Mathf.Clamp01((1f - u) * 6f);
                float tw = 0.75f + 0.35f * Mathf.Sin(now * 22f + sk.seed);   // a quick twinkle
                float a = fade * (tr == 0 ? 1f : (tr == 1 ? 0.45f : 0.2f));
                float sz = sk.size * tw * (1f - 0.35f * tr * 0.5f);
                DrawDot(new Vector2(px, py), sz * 3.2f, new Color(1f, 0.78f, 0.15f, 0.30f * a));    // the glow
                DrawDot(new Vector2(px, py), sz, new Color(1f, 0.96f, 0.62f, a));                  // the bright core
            }
        }
    }

    // ---- icons drawn after toast / banner text: one flame, two flames, or a yellow alert triangle (the default GUI font has none of these) ----
    static float IconsWidth(GameManager.ToastIcon icon, float size)
    {
        if (icon == GameManager.ToastIcon.None) return 0f;
        int n = icon == GameManager.ToastIcon.FireDouble ? 2 : 1;
        return n * size - (n - 1) * size * 0.2f;
    }

    static void DrawIcons(GameManager.ToastIcon icon, float x, float y, float size, float alpha, bool onBright = false)
    {
        Color keep = GUI.color;
        if (icon == GameManager.ToastIcon.Alert) DrawAlert(new Rect(x, y, size, size), alpha);
        else if (icon == GameManager.ToastIcon.Fire) DrawFlame(new Rect(x, y, size, size), alpha, onBright);
        else if (icon == GameManager.ToastIcon.FireDouble)
        {
            DrawFlame(new Rect(x, y, size, size), alpha, onBright);
            DrawFlame(new Rect(x + size * 0.8f, y, size, size), alpha, onBright);
        }
        GUI.color = keep;
    }

    // onBright: the flame sits on the gold / orange banner, so it is drawn deep red there (an orange flame would vanish)
    static void DrawFlame(Rect r, float alpha, bool onBright)
    {
        var tex = FlameTexture();
        GUI.color = onBright ? new Color(0.72f, 0.12f, 0.03f, alpha) : new Color(1f, 0.38f, 0.06f, alpha);
        GUI.DrawTexture(r, tex);
        GUI.color = onBright ? new Color(1f, 0.90f, 0.40f, alpha) : new Color(1f, 0.86f, 0.25f, alpha);   // the bright core
        GUI.DrawTexture(new Rect(r.x + r.width * 0.25f, r.y + r.height * 0.40f, r.width * 0.5f, r.height * 0.58f), tex);
    }

    static void DrawAlert(Rect r, float alpha)
    {
        GUI.color = new Color(1f, 0.80f, 0.10f, alpha);
        GUI.DrawTexture(r, AlertTexture());
        GUI.color = new Color(0.14f, 0.12f, 0.08f, alpha);   // the "!"
        float s = r.width;
        GUI.DrawTexture(new Rect(r.center.x - s * 0.06f, r.y + s * 0.38f, s * 0.12f, s * 0.28f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.center.x - s * 0.07f, r.y + s * 0.72f, s * 0.14f, s * 0.12f), Texture2D.whiteTexture);
    }

    static Texture2D flameTexture, alertTexture;

    // A flame (round at the bottom, a leaning tip), drawn once into a small texture.
    static Texture2D FlameTexture()
    {
        if (flameTexture != null) return flameTexture;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int py = 0; py < n; py++)
        {
            for (int px = 0; px < n; px++)
            {
                float x = px / (n - 1f) * 2f - 1f;    // -1..1
                float y = py / (n - 1f) * 2f;         // 0..2, upwards
                bool inside;
                if (y < 0.72f) inside = x * x + (y - 0.72f) * (y - 0.72f) <= 0.68f * 0.68f;
                else
                {
                    float t = Mathf.Clamp01((y - 0.72f) / 1.28f);
                    float hw = 0.68f * Mathf.Pow(1f - t, 1.5f);
                    float c = 0.22f * t * t;           // the tip leans to the right
                    inside = Mathf.Abs(x - c) <= hw;
                }
                tex.SetPixel(px, py, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();
        flameTexture = tex;
        return tex;
    }

    // A triangle (point up) for the alert icon.
    static Texture2D AlertTexture()
    {
        if (alertTexture != null) return alertTexture;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int py = 0; py < n; py++)
        {
            for (int px = 0; px < n; px++)
            {
                float u = px / (n - 1f);
                float v = py / (n - 1f);   // 0 at the bottom
                // apex (0.5, 0.94), base from (0.03, 0.08) to (0.97, 0.08)
                float half = 0.47f * Mathf.Clamp01((0.94f - v) / 0.86f);
                bool inside = v >= 0.08f && v <= 0.94f && Mathf.Abs(u - 0.5f) <= half;
                tex.SetPixel(px, py, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();
        alertTexture = tex;
        return tex;
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
