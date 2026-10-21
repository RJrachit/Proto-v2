using System.Collections.Generic;
using UnityEngine;

// The text signs on shops, restaurants and hotels, drawn in code with BillboardArt's pixel font (no image files, no fonts).
//   Board  a wide name board (3 : 1) with a strip of LED lettering under the name ("OPEN 9 TO 9", "AC ROOMS - FREE WIFI")
//   Blade  a small board that sticks out from the wall (HOTEL, CAFE, OPEN)
// Each is one texture on an unlit material, so it stays bright at night like an electric sign. All the names are made up. No reds or oranges
// (those colours are for the delivery markers).
public static class SignArt
{
    public static readonly string[] HotelNames = { "HOTEL SUNRISE", "GRAND HOTEL", "HOTEL PALM", "HOTEL SKY", "HOTEL ROYAL", "BLUE LODGE", "HOTEL PLAZA", "HOTEL OASIS" };
    public static readonly string[] HotelTags = { "AC ROOMS - FREE WIFI", "OPEN 24 X 7", "FAMILY ROOMS", "BOOK NOW" };
    public static readonly string[] ShopNames = { "MART", "CHEMIST", "BAKERY", "BOOKS", "TAILOR", "SALON", "GIFTS", "TOYS", "FRUITS", "SWEETS", "DAIRY", "MOBILE", "HARDWARE", "STATIONERY" };
    public static readonly string[] ShopTags = { "OPEN 9 TO 9", "BEST PRICES", "FRESH DAILY", "SALE ON NOW", "WELCOME", "LOCAL FAVOURITE" };
    public static readonly string[] FoodNames = { "CAFE", "CHAI", "PIZZA", "DINER", "BISTRO", "DHABA", "TIFFIN", "JUICE", "NOODLES", "BURGER", "SNACKS", "KEBABS" };
    public static readonly string[] FoodTags = { "OPEN LATE", "TEA COFFEE SNACKS", "FAMILY RESTAURANT", "FRESH AND HOT", "TAKEAWAY", "FREE WIFI" };
    public static readonly string[] HotelBlades = { "HOTEL", "ROOMS" };
    public static readonly string[] ShopBlades = { "OPEN", "SALE", "STORE" };
    public static readonly string[] FoodBlades = { "CAFE", "FOOD", "OPEN" };

    public const int Styles = 8;

    struct Look { public Color bg, text, border, tagBg, tagText; }

    static Look Make(Color bg, Color text, Color border, Color tagText)
    {
        return new Look { bg = bg, text = text, border = border, tagBg = new Color(0.04f, 0.05f, 0.08f), tagText = tagText };
    }

    static readonly Look[] looks =
    {
        Make(new Color(0.10f, 0.16f, 0.32f), new Color(0.98f, 0.95f, 0.80f), new Color(0.25f, 0.65f, 0.70f), new Color(0.45f, 0.95f, 1.00f)),
        Make(new Color(0.07f, 0.30f, 0.33f), new Color(1.00f, 1.00f, 0.96f), new Color(0.95f, 0.85f, 0.40f), new Color(0.55f, 1.00f, 0.60f)),
        Make(new Color(0.10f, 0.28f, 0.18f), new Color(0.98f, 0.95f, 0.82f), new Color(0.95f, 0.95f, 0.92f), new Color(1.00f, 0.90f, 0.45f)),
        Make(new Color(0.26f, 0.15f, 0.40f), new Color(1.00f, 0.90f, 0.96f), new Color(0.45f, 0.90f, 1.00f), new Color(1.00f, 0.55f, 0.85f)),
        Make(new Color(0.12f, 0.13f, 0.16f), new Color(0.45f, 0.95f, 1.00f), new Color(0.45f, 0.95f, 1.00f), new Color(0.72f, 0.58f, 1.00f)),
        Make(new Color(0.95f, 0.92f, 0.80f), new Color(0.12f, 0.20f, 0.42f), new Color(0.12f, 0.20f, 0.42f), new Color(0.45f, 0.95f, 1.00f)),
        Make(new Color(0.14f, 0.30f, 0.58f), new Color(1.00f, 1.00f, 1.00f), new Color(0.98f, 0.88f, 0.42f), new Color(1.00f, 0.95f, 0.55f)),
        Make(new Color(0.04f, 0.05f, 0.07f), new Color(0.55f, 1.00f, 0.62f), new Color(0.55f, 1.00f, 0.62f), new Color(0.45f, 0.95f, 1.00f)),
    };

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

    // The name board: 3 : 1.
    public static Material Board(string word, string tag, int style)
    {
        style = Mathf.Abs(style) % Styles;
        string key = "B|" + word + "|" + tag + "|" + style;
        return Get(key, () => DrawBoard(word, tag, looks[style], style));
    }

    // The small blade sign that sticks out from the wall: 160 : 88.
    public static Material Blade(string word, int style)
    {
        style = Mathf.Abs(style) % Styles;
        string key = "P|" + word + "|" + style;
        return Get(key, () => DrawBlade(word, looks[style]));
    }

    static Material Get(string key, System.Func<Texture2D> draw)
    {
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        Texture2D tex;
        if (!texCache.TryGetValue(key, out tex) || tex == null)
        {
            tex = draw();
            texCache[key] = tex;
        }
        m = Mat.Particle();
        m.mainTexture = tex;
        m.color = Color.white;
        cache[key] = m;
        return m;
    }

    static Color Shade(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

    static Texture2D DrawBoard(string word, string tag, Look lk, int style)
    {
        const int W = 384, H = 128;
        var cv = new BillboardArt.Cv(W, H);
        cv.Fill(lk.border);
        cv.Rect(6, 6, W - 6, H - 6, lk.bg);
        // the name
        int sc = 7;
        while (sc > 2 && (word.Length * 6 - 1) * sc > W - 44) sc--;
        int ty = 6 + (86 - 7 * sc) / 2;
        cv.Text((W - cv.TextWidth(word, sc)) / 2, ty, word, sc, lk.text, Shade(lk.bg, 0.45f));
        // a few round bulbs along the top border on some designs
        if (style % 2 == 0)
            for (int x = 20; x < W - 12; x += 24) cv.Rect(x, 2, x + 4, 6, new Color(1f, 0.97f, 0.80f));
        // the LED strip under it
        cv.Rect(6, 94, W - 6, H - 6, lk.tagBg);
        int ts = 3;
        while (ts > 2 && (tag.Length * 6 - 1) * ts > W - 30) ts--;
        cv.DotText((W - cv.TextWidth(tag, ts)) / 2, 94 + (28 - 7 * ts) / 2, tag, ts, lk.tagText);
        return cv.ToTexture();
    }

    static Texture2D DrawBlade(string word, Look lk)
    {
        const int W = 160, H = 88;
        var cv = new BillboardArt.Cv(W, H);
        cv.Fill(lk.border);
        cv.Rect(5, 5, W - 5, H - 5, lk.bg);
        int sc = 5;
        while (sc > 2 && (word.Length * 6 - 1) * sc > W - 24) sc--;
        cv.Text((W - cv.TextWidth(word, sc)) / 2, (H - 7 * sc) / 2, word, sc, lk.text, Shade(lk.bg, 0.45f));
        return cv.ToTexture();
    }
}
