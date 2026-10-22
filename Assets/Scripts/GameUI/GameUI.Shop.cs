using UnityEngine;

// GameUI (partial) - Shop
// Opened from the Shop button on the main menu only. One row per power-up: its picture, a bar with a part per upgrade (one lit per level bought),
// how long it lasts now and after the next upgrade, and a button that buys the next level with coins.
public partial class GameUI
{
    bool shopOpen;

    static readonly string[] PowerTitles = { "Paper Stack", "Hoop Magnet", "Slow-mo + Zoom" };

    void DrawShop(GameManager gm, float w, float h)
    {
        UiTheme.Scrim(w, h, 1f);
        GUI.color = new Color(0f, 0f, 0f, 0.3f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        const float rowH = 124f, rowGap = 10f;
        float cw = Mathf.Min(470f, w - 28f);
        float cardH = 92f + 3f * (rowH + rowGap) + 78f;
        var card = new Rect((w - cw) * 0.5f, Mathf.Max(14f + insetT, (h - cardH) * 0.5f), cw, cardH);
        UiTheme.Card(card);

        UiTheme.Label(new Rect(card.x, card.y + 10f, card.width, 72f), "SHOP", T(54, TextAnchor.MiddleCenter, UiTheme.Gold), 2.5f);
        DrawCoins(card.xMax - 14f, card.y + 22f);

        float y = card.y + 92f;
        for (int k = 0; k < 3; k++)
        {
            DrawShopRow(new Rect(card.x + 14f, y, card.width - 28f, rowH), (PowerKind)k);
            y += rowH + rowGap;
        }

        if (UiTheme.Tinted(new Rect(card.center.x - 100f, y + 2f, 200f, 60f), "Back", 30, UiTheme.Tone.Orange)) shopOpen = false;
    }

    void DrawShopRow(Rect r, PowerKind kind)
    {
        UiTheme.Pill(r, new Color(0.05f, 0.07f, 0.16f, 0.62f));

        int lv = Progress.PowerLevel(kind);
        int maxLv = Progress.MaxLevel(kind);
        bool maxed = lv >= maxLv;
        Color col = PowerUps.KindColor(kind);

        DrawPowerIcon(new Rect(r.x + 12f, r.y + 14f, 62f, 62f), kind, true);

        float tx = r.x + 88f;
        float btnW = 124f;
        float rightX = r.xMax - btnW - 22f;   // where the text and the bar end

        UiTheme.Label(new Rect(tx, r.y + 8f, rightX - tx, 36f), PowerTitles[(int)kind], T(26, TextAnchor.MiddleLeft, UiTheme.Cream), 1.5f);

        // the bar: 5 parts, one lit per level bought
        const float segGap = 6f;
        float segW = (rightX - tx - segGap * (maxLv - 1)) / maxLv;
        for (int i = 0; i < maxLv; i++)
        {
            var s = new Rect(tx + i * (segW + segGap), r.y + 50f, segW, 20f);
            UiTheme.Bar(s, i < lv ? 1f : 0f, col);
        }

        // how long it lasts now, and after the next upgrade
        float now = PowerUps.SecondsAtLevel(kind, lv);
        string info = maxed
            ? "Lasts " + now.ToString("0") + "s  -  full power"
            : "Lasts " + now.ToString("0") + "s   >   " + PowerUps.SecondsAtLevel(kind, lv + 1).ToString("0") + "s";
        GUI.Label(new Rect(tx, r.y + 78f, rightX - tx + 10f, 30f), info, T(19, TextAnchor.MiddleLeft, maxed ? UiTheme.Gold : UiTheme.Dim));

        // the button
        var b = new Rect(r.xMax - btnW - 10f, r.y + (r.height - 62f) * 0.5f, btnW, 62f);
        if (maxed)
        {
            UiTheme.Label(b, "MAX", T(30, TextAnchor.MiddleCenter, UiTheme.Gold), 2f);
            return;
        }
        int cost = Progress.PowerCost(kind, lv + 1);
        bool canBuy = Progress.Coins >= cost;
        bool hit = canBuy ? UiTheme.Tinted(b, "      " + cost, 28, UiTheme.Tone.Green) : UiTheme.Secondary(b, "      " + cost, 28);
        DrawCoinIcon(new Rect(b.x + 12f, b.y + 17f, 28f, 28f));
        if (hit && canBuy) Progress.BuyPower(kind);
    }
}
