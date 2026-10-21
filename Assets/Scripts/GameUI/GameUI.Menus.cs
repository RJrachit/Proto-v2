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

    // a gold coin
    static void DrawCoinIcon(Rect coin)
    {
        float k = coin.width / 28f;
        UiTheme.Pill(coin, new Color(0.78f, 0.52f, 0.10f));
        UiTheme.Pill(new Rect(coin.x + 3f * k, coin.y + 2f * k, 22f * k, 22f * k), UiTheme.Gold);
    }

    // the logo: the newspaper emblem with the game's name; returns the y just below it
    float DrawLogo(float cx, float y, float titleSize)
    {
        float t = Time.unscaledTime;
        float k = titleSize / 92f;
        UiTheme.Newspaper(new Vector2(cx, y + 60f * k + Mathf.Sin(t * 1.7f) * 3f), 100f * k, -9f + Mathf.Sin(t * 1.3f) * 2.5f);
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
            y = DrawLogo(cx, 56f + insetT, 92f) + 14f;
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

        // three small stats
        float sy = card.y + 296f;
        float colw = (card.width - 44f) / 3f;
        string[] names = { "DISTANCE", "TIME", "COINS" };
        string[] vals = { Metres() + " m", Clock(gm.RunTime), "+" + gm.RunCoins };
        for (int i = 0; i < 3; i++)
        {
            var cell = new Rect(card.x + 22f + i * colw, sy, colw - 8f, 74f);
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
