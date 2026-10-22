using UnityEngine;

// GameUI (partial) - Menus
// The main part of the class is in GameUI.cs.
public partial class GameUI
{
    // ---- shared pieces of the menus ----

    // the coins chip: a gold coin and the total, in a dark pill (right edge at rightX)
    void DrawCoins(float rightX, float y)
    {
        var r = new Rect(rightX - 150f, y, 150f, 44f);
        UiTheme.Shadow(r, 4f);
        UiTheme.Pill(r, new Color(0.05f, 0.07f, 0.14f, 0.88f));
        DrawCoinIcon(new Rect(r.x + 8f, r.y + 8f, 28f, 28f));
        GUI.Label(new Rect(r.x + 40f, r.y, r.width - 50f, r.height), Progress.Coins.ToString(), chipText);
    }

    // a gold coin: a soft shadow, a dark amber rim, a bright bevel, a groove, the face with a glint, and a "$" stamped in the middle.
    // One drawing for every place a coin shows (the HUD chip, the menu's wallet, the game-over screen), so they all match at any size.
    // The discs are a small round texture tinted with GUI.color (the rounded "pill" box turns into a plain rectangle when it is drawn this small).
    static GUIStyle coinDollar;
    static Texture2D coinDisc;

    static Texture2D CoinDisc()
    {
        if (coinDisc != null) return coinDisc;
        const int n = 64;
        coinDisc = new Texture2D(n, n, TextureFormat.RGBA32, false);
        coinDisc.wrapMode = TextureWrapMode.Clamp;
        coinDisc.filterMode = FilterMode.Bilinear;
        coinDisc.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - n * 0.5f, dy = y + 0.5f - n * 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((n * 0.5f - 0.5f - dist) / 1.3f);   // a soft edge about a pixel wide
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        coinDisc.SetPixels(px);
        coinDisc.Apply();
        return coinDisc;
    }

    static void CoinDot(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, CoinDisc(), ScaleMode.StretchToFill, true);
        GUI.color = old;
    }

    static Rect CoinInset(Rect r, float d) { return new Rect(r.x + d, r.y + d, r.width - 2f * d, r.height - 2f * d); }

    static void DrawCoinIcon(Rect coin)
    {
        float h = Mathf.Min(coin.width, coin.height);
        coin = new Rect(coin.center.x - h * 0.5f, coin.center.y - h * 0.5f, h, h);

        CoinDot(new Rect(coin.x, coin.y + h * 0.07f, h, h), new Color(0f, 0f, 0f, 0.35f));            // shadow
        CoinDot(coin, new Color(0.60f, 0.36f, 0.04f));                                                  // rim
        CoinDot(CoinInset(coin, h * 0.07f), new Color(1.00f, 0.80f, 0.24f));                            // bevel (bright)
        CoinDot(CoinInset(coin, h * 0.17f), new Color(0.84f, 0.56f, 0.09f));                            // groove (darker ring)
        CoinDot(CoinInset(coin, h * 0.21f), UiTheme.Gold);                                              // face
        CoinDot(new Rect(coin.x + h * 0.20f, coin.y + h * 0.13f, h * 0.30f, h * 0.15f), new Color(1f, 1f, 1f, 0.55f));   // glint

        if (coinDollar == null)
        {
            coinDollar = new GUIStyle(GUI.skin.label);
            coinDollar.alignment = TextAnchor.MiddleCenter;
            coinDollar.fontStyle = FontStyle.Bold;
            coinDollar.wordWrap = false;
            coinDollar.clipping = TextClipping.Overflow;
            if (UiTheme.ButtonFont != null) coinDollar.font = UiTheme.ButtonFont;
        }
        coinDollar.fontSize = Mathf.Max(8, Mathf.RoundToInt(h * 0.64f));
        Rect tr = new Rect(coin.x, coin.y + h * 0.02f, h, h);
        Color keep = coinDollar.normal.textColor;
        coinDollar.normal.textColor = new Color(1f, 0.95f, 0.65f, 0.85f);                               // a light edge below the stamp
        GUI.Label(new Rect(tr.x, tr.y + h * 0.05f, tr.width, tr.height), "$", coinDollar);
        coinDollar.normal.textColor = new Color(0.50f, 0.28f, 0.02f);                                   // the stamp
        GUI.Label(tr, "$", coinDollar);
        coinDollar.normal.textColor = keep;
    }

    // the logo: the newspaper-bundle emblem above the game's name; returns the y just below it
    float DrawLogo(float cx, float y, float titleSize)
    {
        float t = Time.unscaledTime;
        float k = titleSize / 92f;
        ParcelLogo.Draw(new Vector2(cx, y + 62f * k + Mathf.Sin(t * 1.7f) * 3f), 130f * k, -7f + Mathf.Sin(t * 1.3f) * 2.5f);   // the low-poly newspaper bundle (the old folded newspaper is UiTheme.Newspaper)
        titleGold.fontSize = Mathf.RoundToInt(92f * k);
        titleCream.fontSize = Mathf.RoundToInt(74f * k);
        float w = 700f;
        UiTheme.Label(new Rect(cx - w * 0.5f, y + 128f * k, w, 100f * k), "POLY", titleGold, 3f);
        UiTheme.Label(new Rect(cx - w * 0.5f, y + 208f * k, w, 84f * k), "PARCEL", titleCream, 3f);
        return y + 300f * k;
    }

    static float Pulse(float amount, float speed) { return 1f + amount * Mathf.Sin(Time.unscaledTime * speed); }

    static Rect Grow(Rect r, float k)
    {
        float w = r.width * k, h = r.height * k;
        return new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
    }

    // ---- pause menu ----

    void DrawPause(GameManager gm, float w, float h)
    {
        // un-pausing: no countdown, the pause screen just fades away and the game is back
        float fade = gm.Resuming ? Mathf.Clamp01(gm.ResumeIn / Mathf.Max(0.05f, GameConfig.ResumeCountdown)) : 1f;
        UiTheme.Scrim(w, h, fade);
        GUI.color = new Color(0f, 0f, 0f, 0.28f * fade);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (gm.Resuming) return;

        float cw = Mathf.Min(380f, w - 40f);
        float bh = 70f, gap = 12f;
        float cardH = 112f + 4 * (bh + gap) + 14f;
        var card = new Rect((w - cw) * 0.5f, Mathf.Max(30f + insetT, (h - cardH) * 0.5f), cw, cardH);
        UiTheme.Card(card);
        var title = T(56, TextAnchor.MiddleCenter, UiTheme.Gold);
        UiTheme.Label(new Rect(card.x, card.y + 14f, card.width, 80f), "PAUSED", title, 2.5f);

        float bx = card.x + 22f, bw = card.width - 44f;
        float y = card.y + 112f;
        if (UiTheme.Tinted(new Rect(bx, y, bw, bh + 6f), "Resume", 36, UiTheme.Tone.Green)) gm.Resume();
        y += bh + 6f + gap;
        if (UiTheme.Tinted(new Rect(bx, y, bw, bh), "Restart", 30, UiTheme.Tone.Yellow)) gm.StartRun();
        y += bh + gap;
        if (UiTheme.Tinted(new Rect(bx, y, bw, bh), "Home", 30, UiTheme.Tone.Orange)) gm.QuitToMenu();
        y += bh + gap;
        if (UiTheme.Tinted(new Rect(bx, y, bw, bh), "Options", 30, UiTheme.Tone.Yellow)) OptionsMenu.Show();
    }

    // ---- main menu ----

    void DrawMenu(GameManager gm, float w, float h)
    {
        UiTheme.Scrim(w, h, 1f);
        float topPad = 16f + insetT;
        DrawCoins(w - 22f - insetR, topPad);

        float colW = Mathf.Min(380f, w - 40f);
        float cx, y;
        if (portrait)
        {
            cx = w * 0.5f;
            // the logo and the three buttons are one block, centred in the free height (not stuck to the top with an empty bottom)
            const float blockH = 300f + 14f + 96f + 16f + 66f + 12f + 66f;
            float free = h - insetT - insetB;
            float top = Mathf.Max(56f + insetT, insetT + (free - blockH) * 0.5f);
            y = DrawLogo(cx, top, 92f) + 14f;
        }
        else
        {
            // landscape: the logo on the left, the buttons on the right
            DrawLogo(w * 0.27f, Mathf.Max(70f, (h - 330f) * 0.5f), 92f);
            cx = Mathf.Min(w * 0.72f, w - colW * 0.5f - 24f - insetR);
            y = Mathf.Max(topPad + 52f, 54f + insetT);
        }

        float x0 = cx - colW * 0.5f;

        // Play
        var playRect = new Rect(x0, y, colW, 96f);
        if (UiTheme.Tinted(Grow(playRect, Pulse(0.018f, 3f)), "PLAY", 48, UiTheme.Tone.Green)) gm.StartRun();
        y += 96f + 16f;

        // How to play (the home page only): plays the tutorial again
        if (UiTheme.Tinted(new Rect(x0, y, colW, 66f), "How to play", 28, UiTheme.Tone.Yellow)) gm.StartTutorial();
        y += 66f + 12f;

        // Options
        if (UiTheme.Tinted(new Rect(x0, y, colW, 66f), "Options", 28, UiTheme.Tone.Orange)) OptionsMenu.Show();

        // best score: top-left in portrait (the coins are top-right), under the logo in landscape
        float by = portrait ? topPad : h - 62f - insetB;
        float bx = portrait ? 22f + insetL + 105f : w * 0.27f;
        var best = new Rect(bx - 105f, by, 210f, 44f);
        UiTheme.Shadow(best, 4f);
        UiTheme.Pill(best, new Color(0.05f, 0.07f, 0.14f, 0.88f));
        GUI.Label(new Rect(best.x, best.y, best.width, best.height), "Best Score  " + gm.HighScore, T(22, TextAnchor.MiddleCenter, UiTheme.Gold));
    }

    // ---- game over ----

    void DrawGameOver(GameManager gm, float w, float h)
    {
        UiTheme.Scrim(w, h, 1f);
        GUI.color = new Color(0f, 0f, 0f, 0.3f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cw = Mathf.Min(portrait ? 440f : 600f, w - 32f);
        float cardH = portrait ? 600f : 520f;
        var card = new Rect((w - cw) * 0.5f, Mathf.Max(24f + insetT, (h - cardH) * 0.5f), cw, cardH);
        UiTheme.Card(card);

        UiTheme.Label(new Rect(card.x, card.y + 14f, card.width, 70f), "You're FIRED", T(52, TextAnchor.MiddleCenter, UiTheme.Cream), 2.5f);
        if (!string.IsNullOrEmpty(gm.GameOverReason))
            GUI.Label(new Rect(card.x + 16f, card.y + 82f, card.width - 32f, 28f), gm.GameOverReason, T(19, TextAnchor.MiddleCenter, UiTheme.Dim));

        // the score, big, with a badge when it is a new best
        GUI.Label(new Rect(card.x, card.y + 118f, card.width, 24f), "SCORE", statLabel);
        UiTheme.Label(new Rect(card.x, card.y + 138f, card.width, 100f), gm.Score.ToString(), T(88, TextAnchor.MiddleCenter, UiTheme.Gold), 2.5f);
        var badge = new Rect(card.center.x - 90f, card.y + 242f, 180f, 36f);
        if (gm.Score > 0 && gm.Score >= gm.HighScore)
        {
            UiTheme.Pill(badge, UiTheme.Mint);
            GUI.Label(badge, "NEW BEST!", T(20, TextAnchor.MiddleCenter, UiTheme.Navy));
        }
        else
        {
            UiTheme.Pill(badge, new Color(1f, 1f, 1f, 0.12f));
            GUI.Label(badge, "Best Score " + gm.HighScore, T(20, TextAnchor.MiddleCenter, UiTheme.Cream));
        }

        // four small stats
        float sy = card.y + 296f;
        float colw = (card.width - 44f) / 4f;
        string[] names = { "DISTANCE", "BASKETS", "TIME", "COINS" };
        string[] vals = { Metres() + " m", gm.RunBaskets.ToString(), Clock(gm.RunTime), "+" + gm.RunCoins };
        for (int i = 0; i < 4; i++)
        {
            var cell = new Rect(card.x + 22f + i * colw, sy, colw - 6f, 74f);
            UiTheme.Pill(cell, new Color(1f, 1f, 1f, 0.07f));
            GUI.Label(new Rect(cell.x, cell.y + 8f, cell.width, 18f), names[i], statLabel);
            GUI.Label(new Rect(cell.x, cell.y + 26f, cell.width, 40f), vals[i], statValue);
        }
        // the wallet: the total, then a coin
        string wallet = Progress.Coins.ToString();
        float walletW = rowSmall.CalcSize(new GUIContent(wallet)).x + 4f;
        float walletX = card.center.x - (walletW + 6f + 22f) * 0.5f;
        GUI.Label(new Rect(walletX, sy + 78f, walletW, 24f), wallet, rowSmall);
        DrawCoinIcon(new Rect(walletX + walletW + 6f, sy + 79f, 22f, 22f));

        // buttons
        float by = card.yMax - 98f;
        if (portrait)
        {
            // stacked
            float bw = card.width - 44f;
            if (UiTheme.Tinted(new Rect(card.x + 22f, by - 82f, bw, 74f), "Play again", 34, UiTheme.Tone.Green)) gm.StartRun();
            if (UiTheme.Tinted(new Rect(card.x + 22f, by, bw, 66f), "Home", 28, UiTheme.Tone.Orange)) gm.GoToMenu();
        }
        else
        {
            float bw = (card.width - 44f - 14f) * 0.5f;
            if (UiTheme.Tinted(new Rect(card.x + 22f, by - 8f, bw, 76f), "Play again", 32, UiTheme.Tone.Green)) gm.StartRun();
            if (UiTheme.Tinted(new Rect(card.x + 22f + bw + 14f, by - 8f, bw, 76f), "Home", 30, UiTheme.Tone.Orange)) gm.GoToMenu();
        }
    }
}
