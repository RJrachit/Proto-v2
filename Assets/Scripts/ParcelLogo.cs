using UnityEngine;

// The game's emblem: a low-poly bundle of folded newspapers (the sheet edges show on its sides, the front page on top) tied with crossed
// gold twine and a knot. Every face is split into two flat-shaded triangles, so it reads as "poly". Painted once, in code, into a small
// texture (no image file to import), then drawn tilted like the old newspaper emblem. See GameUI.DrawLogo.
public static class ParcelLogo
{
    const int N = 256;
    static Texture2D tex;
    static Color[] buf;

    // the same drawing call as UiTheme.Newspaper: the centre, the size (width and height) and the tilt in degrees
    public static void Draw(Vector2 center, float size, float angle)
    {
        UiTheme.Ensure();
        if (tex == null) Build();
        if (shadowTex == null) BuildShadow();
        // a soft blurred oval on the ground under it (not rotated, so it stays flat while the emblem tilts)
        float sw = size * 0.86f, sh = size * 0.26f;
        GUI.DrawTexture(new Rect(center.x - sw * 0.5f, center.y + size * 0.36f - sh * 0.5f, sw, sh), shadowTex, ScaleMode.StretchToFill, true);
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        var r = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        GUI.matrix = saved;
    }

    static Texture2D shadowTex;

    // a soft round blob: black, fading smoothly from the middle to nothing at the edge (stretched into an oval when drawn)
    static void BuildShadow()
    {
        const int S = 64;
        shadowTex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        shadowTex.wrapMode = TextureWrapMode.Clamp;
        shadowTex.filterMode = FilterMode.Bilinear;
        shadowTex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt(Mathf.Pow((x + 0.5f - S * 0.5f) / (S * 0.5f), 2f) + Mathf.Pow((y + 0.5f - S * 0.5f) / (S * 0.5f), 2f));
                float a = Mathf.Clamp01(1f - d);
                px[y * S + x] = new Color(0f, 0f, 0f, a * a * (3f - 2f * a) * 0.45f);   // smoothstep falloff
            }
        shadowTex.SetPixels(px);
        shadowTex.Apply();
    }

    // ---- painting ----

    static readonly Color Navy = new Color(0.08f, 0.11f, 0.19f);
    static readonly Color Gold = new Color(1.00f, 0.80f, 0.22f);
    static readonly Color GoldDark = new Color(0.82f, 0.56f, 0.08f);
    static readonly Color Sky = new Color(0.45f, 0.75f, 0.95f);
    static readonly Color Ink = new Color(0.56f, 0.59f, 0.67f);

    static Color Shade(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, c.a); }

    static void Build()
    {
        buf = new Color[N * N];
        for (int i = 0; i < buf.Length; i++) buf[i] = new Color(0f, 0f, 0f, 0f);

        // a bundle of folded newspapers: a thick slab seen from a corner (screen coordinates, y down), the sheet edges showing on its sides
        Vector2 T = new Vector2(141f, 44f), R = new Vector2(227.6f, 94f), L = new Vector2(28.4f, 109f), B = new Vector2(115f, 159f);
        Vector2 down = new Vector2(0f, 50f);
        Vector2 Rb = R + down, Bb = B + down, Lb = L + down;

        Color top = new Color(0.98f, 0.96f, 0.89f), left = new Color(0.86f, 0.82f, 0.72f), right = new Color(0.72f, 0.68f, 0.58f);

        // a dark outline round the whole shape
        Vector2 mid = new Vector2(128f, 126f);
        Vector2[] sil = { T, R, Rb, Bb, Lb, L };
        for (int i = 0; i < sil.Length; i++) sil[i] = mid + (sil[i] - mid) * 1.04f;
        Fill(sil, Navy);

        // faces, each in two flat triangles of slightly different tone
        Fill(new[] { T, R, B }, top);
        Fill(new[] { T, B, L }, Shade(top, 0.955f));
        Fill(new[] { L, B, Lb }, left);
        Fill(new[] { B, Bb, Lb }, Shade(left, 0.94f));
        Fill(new[] { B, R, Rb }, right);
        Fill(new[] { B, Rb, Bb }, Shade(right, 0.93f));

        // the sides: the edges of many folded sheets, thin darker lines at uneven spacing
        Vector2 lu = B - L, lv = Lb - L;
        Vector2 ru = R - B, rv = Bb - B;
        float[] layer = { 0.11f, 0.22f, 0.30f, 0.43f, 0.52f, 0.64f, 0.72f, 0.85f };
        for (int k = 0; k < layer.Length; k++)
        {
            Quad(L, lu, lv, 0f, 1f, layer[k], layer[k] + 0.035f, Shade(left, 0.80f));
            Quad(B, ru, rv, 0f, 1f, layer[k] + 0.02f, layer[k] + 0.055f, Shade(right, 0.80f));
        }

        // the front page on top: a dark masthead, a picture with a sun, columns of text (origin T, u runs T->R, v runs T->L)
        Vector2 tu = R - T, tv = L - T;
        Quad(T, tu, tv, 0.07f, 0.93f, 0.05f, 0.17f, Navy);
        Quad(T, tu, tv, 0.07f, 0.47f, 0.24f, 0.58f, Sky);
        Quad(T, tu, tv, 0.31f, 0.40f, 0.29f, 0.37f, Gold);
        for (int i = 0; i < 5; i++) Quad(T, tu, tv, 0.54f, i == 4 ? 0.74f : 0.93f, 0.24f + i * 0.085f, 0.24f + i * 0.085f + 0.04f, Ink);
        for (int i = 0; i < 3; i++) Quad(T, tu, tv, 0.07f, i == 2 ? 0.30f : 0.47f, 0.64f + i * 0.085f, 0.64f + i * 0.085f + 0.04f, Ink);
        for (int i = 0; i < 2; i++) Quad(T, tu, tv, 0.54f, i == 1 ? 0.80f : 0.93f, 0.68f + i * 0.085f, 0.68f + i * 0.085f + 0.04f, Ink);

        // twine: one strap round the bundle each way, crossing on top and carrying on down the two front sides
        Quad(T, tu, tv, 0.45f, 0.55f, 0.00f, 1.00f, Gold);                 // along the top, down the left side
        Quad(T, tu, tv, 0.00f, 1.00f, 0.46f, 0.56f, Gold);                 // across the top, down the right side
        Quad(L, lu, lv, 0.45f, 0.55f, 0.00f, 1.00f, Shade(Gold, 0.90f));
        Quad(B, ru, rv, 0.44f, 0.54f, 0.00f, 1.00f, Shade(Gold, 0.80f));

        // a plain knot where the straps cross, with two short tails (no bow: it should look tied, not wrapped as a present)
        Vector2 C = T + tu * 0.50f + tv * 0.51f;
        Vector2[] knot = { C + new Vector2(0f, -10f), C + new Vector2(13f, 0f), C + new Vector2(0f, 10f), C + new Vector2(-13f, 0f) };
        Fill(Grow(knot, 1.22f), Navy);
        Fill(knot, Gold);
        Fill(new[] { knot[0], knot[1], knot[3] }, new Color(1f, 0.92f, 0.55f));
        Fill(new[] { C + new Vector2(2f, 6f), C + new Vector2(20f, 20f), C + new Vector2(10f, 24f) }, GoldDark);
        Fill(new[] { C + new Vector2(-2f, 6f), C + new Vector2(-18f, 22f), C + new Vector2(-8f, 25f) }, GoldDark);

        // a flat highlight on the top face's front edge
        Quad(T, tu, tv, 0.00f, 1.00f, 0.97f, 1.00f, new Color(1f, 1f, 1f, 0.55f));

        tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixels(buf);
        tex.Apply();
        buf = null;
    }

    static Vector2[] Grow(Vector2[] p, float k)
    {
        Vector2 c = Vector2.zero;
        for (int i = 0; i < p.Length; i++) c += p[i];
        c /= p.Length;
        var q = new Vector2[p.Length];
        for (int i = 0; i < p.Length; i++) q[i] = c + (p[i] - c) * k;
        return q;
    }

    // a parallelogram on a face: o + u * U + v * V for the corners of [u0, u1] x [v0, v1]
    static void Quad(Vector2 o, Vector2 U, Vector2 V, float u0, float u1, float v0, float v1, Color c)
    {
        Fill(new[] { o + U * u0 + V * v0, o + U * u1 + V * v0, o + U * u1 + V * v1, o + U * u0 + V * v1 }, c);
    }

    // fills a convex polygon (as a fan of triangles), 3 x 3 samples a pixel for smooth edges, blended over what is there
    static void Fill(Vector2[] p, Color c)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < p.Length; i++)
        {
            minX = Mathf.Min(minX, p[i].x); maxX = Mathf.Max(maxX, p[i].x);
            minY = Mathf.Min(minY, p[i].y); maxY = Mathf.Max(maxY, p[i].y);
        }
        int x0 = Mathf.Max(0, Mathf.FloorToInt(minX)), x1 = Mathf.Min(N - 1, Mathf.CeilToInt(maxX));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(N - 1, Mathf.CeilToInt(maxY));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                    {
                        Vector2 q = new Vector2(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f);
                        for (int t = 1; t < p.Length - 1; t++)
                            if (InTri(q, p[0], p[t], p[t + 1])) { hit++; break; }
                    }
                if (hit == 0) continue;
                float a = hit / 9f * c.a;
                int idx = (N - 1 - y) * N + x;   // (the texture's y runs up)
                Color d = buf[idx];
                float oa = a + d.a * (1f - a);
                buf[idx] = new Color((c.r * a + d.r * d.a * (1f - a)) / oa, (c.g * a + d.g * d.a * (1f - a)) / oa, (c.b * a + d.b * d.a * (1f - a)) / oa, oa);
            }
    }

    static bool InTri(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(q, a, b), d2 = Cross(q, b, c), d3 = Cross(q, c, a);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }

    static float Cross(Vector2 p, Vector2 a, Vector2 b) { return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y); }
}
