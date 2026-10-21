using System.Collections.Generic;
using UnityEngine;

// The ads on the roadside billboards, drawn in code (no image files, no fonts): a tiny pixel font plus a few shapes.
//   0  VOTE FOR + a generic cartoon face    1  HOUSE FOR RENT, with a phone number
//   2  STOP AI CAMPAIGN                     3  I SELL CONSUMERISM
// Each design is one texture on an unlit material (it dims a little at night). All the people, brands and slogans are made up.
public static class BillboardArt
{
    public const int Count = 4;
    const int W = 384, H = 300;   // the aspect matches the board face (3.2 x 2.5)

    static readonly Texture2D[] tex = new Texture2D[Count];
    static readonly Material[] mats = new Material[Count];

    public static Material Mat(int design)
    {
        design = Mathf.Abs(design) % Count;
        if (mats[design] == null)
        {
            if (tex[design] == null) tex[design] = Draw(design);
            var m = Mat_Particle();
            m.mainTexture = tex[design];
            m.color = Color.white;
            mats[design] = m;
        }
        return mats[design];
    }

    static Material Mat_Particle() { return global::Mat.Particle(); }

    // Ads dim a little as the night comes in (they are unlit).
    public static void Tick()
    {
        float k = Mathf.Lerp(1f, 0.62f, NightLights.Dark);
        var c = new Color(k, k, k, 1f);
        for (int i = 0; i < Count; i++) if (mats[i] != null) mats[i].color = c;
    }

    // ---------------------------------------------------------------- the designs

    static Texture2D Draw(int design)
    {
        var cv = new Cv(W, H);
        switch (design)
        {
            case 0: Vote(cv); break;
            case 1: Rent(cv); break;
            case 2: StopAi(cv); break;
            default: Consume(cv); break;
        }
        return cv.ToTexture();
    }

    static readonly Color White = new Color(0.97f, 0.97f, 0.95f);
    static readonly Color Ink = new Color(0.08f, 0.09f, 0.13f);

    static void Vote(Cv cv)
    {
        Color navy = new Color(0.14f, 0.24f, 0.52f);
        Color skin = new Color(0.90f, 0.70f, 0.54f);
        Color skinDark = new Color(0.78f, 0.56f, 0.42f);
        Color hair = new Color(0.24f, 0.15f, 0.10f);
        cv.Fill(navy);
        for (int i = 0; i < 14; i++)   // a scatter of stars
            cv.Ellipse(20 + (i * 97) % 350, 80 + (i * 53) % 150, 3f, 3f, new Color(0.8f, 0.86f, 1f));
        cv.TextCentered(14, "VOTE FOR", 7, White, new Color(0f, 0f, 0.1f));

        // generic cartoon head: ears, hair, face, eyes, brows, nose, grin
        cv.Ellipse(112, 178, 14, 22, skinDark);
        cv.Ellipse(272, 178, 14, 22, skinDark);
        cv.Ellipse(192, 124, 86, 52, hair);
        cv.Rect(168, 238, 216, 272, skinDark);                         // neck
        cv.Ellipse(192, 176, 80, 88, skin);
        cv.Ellipse(192, 106, 76, 30, hair);                             // hair over the forehead
        cv.Ellipse(160, 166, 17, 20, White);
        cv.Ellipse(224, 166, 17, 20, White);
        cv.Ellipse(164, 168, 8, 9, Ink);
        cv.Ellipse(228, 168, 8, 9, Ink);
        cv.Line(138, 138, 178, 132, 8f, hair);
        cv.Line(206, 132, 246, 138, 8f, hair);
        cv.Ellipse(192, 192, 9, 12, skinDark);
        cv.Ellipse(192, 214, 46, 34, new Color(0.30f, 0.09f, 0.16f), 1);   // a wide grin (lower half of an ellipse)
        cv.Rect(150, 214, 234, 226, White);                             // teeth
        cv.Ellipse(150, 200, 10, 7, new Color(0.95f, 0.55f, 0.55f));    // cheeks
        cv.Ellipse(234, 200, 10, 7, new Color(0.95f, 0.55f, 0.55f));

        cv.Rect(0, 262, W, H, White);
        cv.TextCentered(268, "TRUST ME", 4, navy, Color.clear);
    }

    static void Rent(Cv cv)
    {
        Color teal = new Color(0.10f, 0.46f, 0.50f);
        cv.Fill(new Color(0.96f, 0.93f, 0.82f));
        cv.Rect(0, 0, W, 146, teal);
        cv.TextCentered(12, "HOUSE", 9, White, new Color(0f, 0.2f, 0.22f));
        cv.TextCentered(84, "FOR RENT", 7, new Color(1f, 0.92f, 0.45f), new Color(0f, 0.2f, 0.22f));
        // a little house
        Color roof = new Color(0.25f, 0.28f, 0.38f);
        cv.Rect(46, 206, 146, 256, White);
        cv.Rect(46, 206, 50, 256, Ink); cv.Rect(142, 206, 146, 256, Ink); cv.Rect(46, 252, 146, 256, Ink);
        cv.Tri(36, 208, 156, 208, 96, 158, roof);
        cv.Rect(86, 224, 108, 256, new Color(0.45f, 0.30f, 0.20f));
        cv.Rect(58, 218, 76, 236, new Color(0.60f, 0.80f, 0.92f));
        cv.Rect(120, 218, 138, 236, new Color(0.60f, 0.80f, 0.92f));
        cv.Text(182, 176, "CALL", 7, Ink, Color.clear);
        cv.Text(182, 228, "NOW", 5, teal, Color.clear);
        cv.Rect(0, 262, W, H, teal);
        cv.TextCentered(269, "+11 674206969", 4, White, Color.clear);
    }

    static void StopAi(Cv cv)
    {
        Color yellow = new Color(0.98f, 0.84f, 0.12f);
        cv.Fill(Ink);
        cv.Rect(10, 10, W - 10, H - 10, yellow);
        cv.TextCentered(22, "STOP AI", 8, Ink, Color.clear);
        cv.TextCentered(92, "CAMPAIGN", 6, Ink, Color.clear);
        // a robot head with a "no" sign over it
        Color grey = new Color(0.55f, 0.60f, 0.68f);
        cv.Line(192, 156, 192, 172, 5f, Ink);
        cv.Ellipse(192, 154, 7, 7, Ink);
        cv.Rect(150, 172, 234, 236, grey);
        cv.Rect(150, 172, 234, 176, Ink); cv.Rect(150, 232, 234, 236, Ink); cv.Rect(150, 172, 154, 236, Ink); cv.Rect(230, 172, 234, 236, Ink);
        cv.Rect(162, 188, 182, 206, White); cv.Rect(202, 188, 222, 206, White);
        cv.Rect(170, 194, 178, 202, Ink); cv.Rect(210, 194, 218, 202, Ink);
        cv.Rect(168, 218, 216, 224, Ink);
        cv.Ring(192, 202, 66f, 10f, new Color(0.75f, 0.10f, 0.45f));
        cv.Line(150, 156, 234, 248, 11f, new Color(0.75f, 0.10f, 0.45f));
        cv.TextCentered(268, "HUMANS FIRST", 3, Ink, Color.clear);
    }

    static void Consume(Cv cv)
    {
        Color purple = new Color(0.36f, 0.18f, 0.60f);
        Color gold = new Color(1f, 0.90f, 0.30f);
        cv.Fill(purple);
        cv.Rect(0, 0, W, 8, gold); cv.Rect(0, H - 8, W, H, gold);
        cv.TextCentered(18, "I SELL", 8, White, new Color(0.1f, 0.04f, 0.2f));
        cv.TextCentered(90, "CONSUMERISM", 5, gold, new Color(0.1f, 0.04f, 0.2f));
        // a shopping bag with a dollar sign
        Color bag = new Color(0.98f, 0.55f, 0.76f);
        cv.Ring(98, 168, 24f, 7f, White);
        cv.Rect(58, 178, 138, 262, bag);
        cv.Rect(58, 178, 138, 186, new Color(0.85f, 0.38f, 0.62f));
        cv.Text(86, 204, "$", 6, Ink, Color.clear);
        cv.Text(168, 160, "BUY NOW!", 5, White, new Color(0.1f, 0.04f, 0.2f));
        cv.Text(168, 214, "50% OFF", 5, gold, new Color(0.1f, 0.04f, 0.2f));
        cv.Text(168, 262, "SHOP. REPEAT.", 3, White, Color.clear);
    }

    // ---------------------------------------------------------------- a tiny pixel canvas

    public class Cv
    {
        readonly int w, h;
        readonly Color[] px;

        public Cv(int width, int height) { w = width; h = height; px = new Color[w * h]; }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 2;
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        public void Fill(Color c) { for (int i = 0; i < px.Length; i++) px[i] = c; }

        // y counts down from the top of the picture
        void Put(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= w || y >= h || a <= 0f || c.a <= 0f) return;
            int i = (h - 1 - y) * w + x;
            px[i] = a >= 1f ? new Color(c.r, c.g, c.b, 1f) : Color.Lerp(px[i], new Color(c.r, c.g, c.b, 1f), a);
        }

        public void Rect(int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) Put(x, y, c, 1f);
        }

        // clip: 0 = the whole ellipse, 1 = only the half below the centre, -1 = only the half above it
        public void Ellipse(float cx, float cy, float rx, float ry, Color c, int clip = 0)
        {
            int x0 = Mathf.FloorToInt(cx - rx - 1), x1 = Mathf.CeilToInt(cx + rx + 1);
            int y0 = Mathf.FloorToInt(cy - ry - 1), y1 = Mathf.CeilToInt(cy + ry + 1);
            float soft = Mathf.Min(rx, ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float py = y + 0.5f;
                    if (clip == 1 && py < cy) continue;
                    if (clip == -1 && py > cy) continue;
                    float dx = (x + 0.5f - cx) / rx, dy = (py - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Put(x, y, c, Mathf.Clamp01((1f - d) * soft + 0.5f));
                }
        }

        public void Ring(float cx, float cy, float r, float thick, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - thick), x1 = Mathf.CeilToInt(cx + r + thick);
            int y0 = Mathf.FloorToInt(cy - r - thick), y1 = Mathf.CeilToInt(cy + r + thick);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - r);
                    Put(x, y, c, Mathf.Clamp01(thick * 0.5f - d + 0.5f));
                }
        }

        public void Line(float ax, float ay, float bx, float by, float thick, Color c)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(ax, bx) - thick), x1 = Mathf.CeilToInt(Mathf.Max(ax, bx) + thick);
            int y0 = Mathf.FloorToInt(Mathf.Min(ay, by) - thick), y1 = Mathf.CeilToInt(Mathf.Max(ay, by) + thick);
            float vx = bx - ax, vy = by - ay;
            float len2 = Mathf.Max(0.0001f, vx * vx + vy * vy);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float px2 = x + 0.5f - ax, py2 = y + 0.5f - ay;
                    float t = Mathf.Clamp01((px2 * vx + py2 * vy) / len2);
                    float dx = px2 - vx * t, dy = py2 - vy * t;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Put(x, y, c, Mathf.Clamp01(thick * 0.5f - d + 0.5f));
                }
        }

        public void Tri(float ax, float ay, float bx, float by, float cx, float cy, Color c)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx))), x1 = Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx)));
            int y0 = Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy))), y1 = Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy)));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float px2 = x + 0.5f, py2 = y + 0.5f;
                    float d1 = (px2 - bx) * (ay - by) - (ax - bx) * (py2 - by);
                    float d2 = (px2 - cx) * (by - cy) - (bx - cx) * (py2 - cy);
                    float d3 = (px2 - ax) * (cy - ay) - (cx - ax) * (py2 - ay);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos)) Put(x, y, c, 1f);
                }
        }

        // ---- text: a 5x7 pixel font, drawn at a whole-number scale, optionally with a drop shadow
        public int TextWidth(string s, int sc) { return s.Length == 0 ? 0 : (s.Length * 6 - 1) * sc; }

        public void TextCentered(int y, string s, int sc, Color c, Color shadow)
        {
            Text((w - TextWidth(s, sc)) / 2, y, s, sc, c, shadow);
        }

        public void Text(int x, int y, string s, int sc, Color c, Color shadow)
        {
            if (shadow.a > 0f) DrawString(x + Mathf.Max(2, sc / 2), y + Mathf.Max(2, sc / 2), s, sc, shadow);
            DrawString(x, y, s, sc, c);
        }

        // The same letters as little square lamps with a gap between them, like an LED board (sc is the pitch of the dots, at least 2)
        public void DotText(int x, int y, string s, int sc, Color c)
        {
            int d = Mathf.Max(1, sc - 1);
            for (int i = 0; i < s.Length; i++)
            {
                string[] g = Glyph(s[i]);
                if (g == null) continue;
                for (int row = 0; row < 7; row++)
                    for (int col = 0; col < 5; col++)
                        if (g[row][col] == '#')
                        {
                            int px = x + (i * 6 + col) * sc, py = y + row * sc;
                            Rect(px, py, px + d, py + d, c);
                        }
            }
        }

        void DrawString(int x, int y, string s, int sc, Color c)
        {
            for (int i = 0; i < s.Length; i++)
            {
                string[] g = Glyph(s[i]);
                if (g != null)
                    for (int row = 0; row < 7; row++)
                        for (int col = 0; col < 5; col++)
                            if (g[row][col] == '#') Rect(x + (i * 6 + col) * sc, y + row * sc, x + (i * 6 + col + 1) * sc, y + (row + 1) * sc, c);
            }
        }
    }

    // ---------------------------------------------------------------- the font (rows are separated by '/')

    static readonly Dictionary<char, string[]> font = BuildFont();

    static string[] Glyph(char ch)
    {
        string[] g;
        return font.TryGetValue(char.ToUpperInvariant(ch), out g) ? g : null;
    }

    static Dictionary<char, string[]> BuildFont()
    {
        var f = new Dictionary<char, string[]>();
        string[] src =
        {
            "A.###./#...#/#...#/#####/#...#/#...#/#...#",
            "B####./#...#/#...#/####./#...#/#...#/####.",
            "C.####/#..../#..../#..../#..../#..../.####",
            "D####./#...#/#...#/#...#/#...#/#...#/####.",
            "E#####/#..../#..../####./#..../#..../#####",
            "F#####/#..../#..../####./#..../#..../#....",
            "G.####/#..../#..../#..##/#...#/#...#/.###.",
            "H#...#/#...#/#...#/#####/#...#/#...#/#...#",
            "I#####/..#../..#../..#../..#../..#../#####",
            "J..###/...#./...#./...#./...#./#..#./.##..",
            "K#...#/#..#./#.#../##.../#.#../#..#./#...#",
            "L#..../#..../#..../#..../#..../#..../#####",
            "M#...#/##.##/#.#.#/#.#.#/#...#/#...#/#...#",
            "N#...#/##..#/#.#.#/#..##/#...#/#...#/#...#",
            "O.###./#...#/#...#/#...#/#...#/#...#/.###.",
            "P####./#...#/#...#/####./#..../#..../#....",
            "Q.###./#...#/#...#/#...#/#.#.#/#..#./.##.#",
            "R####./#...#/#...#/####./#.#../#..#./#...#",
            "S.####/#..../#..../.###./....#/....#/####.",
            "T#####/..#../..#../..#../..#../..#../..#..",
            "U#...#/#...#/#...#/#...#/#...#/#...#/.###.",
            "V#...#/#...#/#...#/#...#/#...#/.#.#./..#..",
            "W#...#/#...#/#...#/#.#.#/#.#.#/##.##/#...#",
            "X#...#/#...#/.#.#./..#../.#.#./#...#/#...#",
            "Y#...#/#...#/.#.#./..#../..#../..#../..#..",
            "Z#####/....#/...#./..#../.#.../#..../#####",
            "0.###./#...#/#..##/#.#.#/##..#/#...#/.###.",
            "1..#../.##../..#../..#../..#../..#../.###.",
            "2.###./#...#/....#/...#./..#../.#.../#####",
            "3####./....#/....#/.###./....#/....#/####.",
            "4...#./..##./.#.#./#..#./#####/...#./...#.",
            "5#####/#..../####./....#/....#/#...#/.###.",
            "6.###./#..../#..../####./#...#/#...#/.###.",
            "7#####/....#/...#./..#../.#.../.#.../.#...",
            "8.###./#...#/#...#/.###./#...#/#...#/.###.",
            "9.###./#...#/#...#/.####/....#/....#/.###.",
            "+...../..#../..#../#####/..#../..#../.....",
            "-...../...../...../#####/...../...../.....",
            "....../...../...../...../...../.##../.##..",
            "!..#../..#../..#../..#../..#../...../..#..",
            "?.###./#...#/....#/...#./..#../...../..#..",
            "$..#../.####/#.#../.###./..#.#/####./..#..",
            "%##..#/##.#./...#./..#../.#.../.#.##/#..##",
        };
        foreach (var s in src) f[s[0]] = s.Substring(1).Split('/');
        f[' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." };
        return f;
    }
}
