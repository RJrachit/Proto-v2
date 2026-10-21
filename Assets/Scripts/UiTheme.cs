using UnityEngine;

// The look shared by every menu (main menu, shop, pause, game over, options): rounded cards and chunky buttons that are drawn once into
// small textures at run time (so no art files are needed), a soft drop shadow, a darkening gradient over the game, text with an outline,
// and a little newspaper emblem. All of it works with plain OnGUI. Styles must be built from inside OnGUI, so call Ensure() there first.
public static class UiTheme
{
    public static readonly Color Navy = new Color(0.08f, 0.11f, 0.19f);
    public static readonly Color Gold = new Color(1.00f, 0.82f, 0.25f);
    public static readonly Color Cream = new Color(0.97f, 0.95f, 0.88f);
    public static readonly Color Mint = new Color(0.45f, 0.86f, 0.62f);
    public static readonly Color Sky = new Color(0.45f, 0.75f, 0.95f);
    public static readonly Color Dim = new Color(0.70f, 0.74f, 0.84f);

    const int ButtonR = 30, CardR = 26, PillR = 20, BarR = 6;

    static Texture2D tPrimary, tPrimaryHover, tPrimaryDown, tSecondary, tSecondaryHover, tSecondaryDown, tCard, tPill, tBar, tShadow, tScrim, tThumb, tTrack;
    static GUIStyle sPrimary, sSecondary, sTab, sCard, sPill, sBar, sShadow, sSlider, sThumb;

    public static GUIStyle PrimaryStyle { get { return sPrimary; } }
    public static GUIStyle SecondaryStyle { get { return sSecondary; } }
    public static GUIStyle TabStyle { get { return sTab; } }
    public static GUIStyle SliderStyle { get { return sSlider; } }
    public static GUIStyle ThumbStyle { get { return sThumb; } }
    public static GUIStyle PillStyle { get { return sPill; } }

    // ---- textures ----

    // A rounded box ready to be a 9-slice background: r = corner radius in pixels, a vertical colour gradient (top to bottom),
    // an optional edge line, and an optional darker band along the bottom edge (the "lip" that makes a button look chunky).
    static Texture2D Round(int r, Color top, Color bottom, Color edge, float edgeW, int lip, Color lipColor)
    {
        int size = r * 2 + 6;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - r);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - r);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float sd = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;   // negative inside
                float a = Mathf.Clamp01(0.5f - sd);
                Color c = Color.Lerp(bottom, top, y / (size - 1f));
                if (lip > 0 && y < lip) c = lipColor;
                else if (edgeW > 0f && sd > -edgeW) c = Color.Lerp(c, edge, Mathf.Clamp01(1f - (-sd) / edgeW));
                c.a *= a;
                px[y * size + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D Scrim()
    {
        const int n = 64;
        var tex = new Texture2D(1, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < n; y++)
        {
            float t = y / (n - 1f);                                   // 0 = bottom of the screen, 1 = top
            float a = Mathf.Lerp(0.78f, 0.22f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.6f)));
            if (t > 0.6f) a = Mathf.Lerp(0.22f, 0.70f, Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f));
            tex.SetPixel(0, y, new Color(0.04f, 0.06f, 0.12f, a));
        }
        tex.Apply();
        return tex;
    }

    static GUIStyle Box(Texture2D tex, int r)
    {
        var s = new GUIStyle();
        s.normal.background = tex;
        s.hover.background = tex;      // (a control shows nothing in a state that has no background, so every state gets it)
        s.active.background = tex;
        s.focused.background = tex;
        s.border = new RectOffset(r, r, r, r);
        return s;
    }

    static GUIStyle Button(Texture2D n, Texture2D hov, Texture2D down, Color text)
    {
        var s = new GUIStyle();
        s.normal.background = n;
        s.hover.background = hov;
        s.active.background = down;
        s.focused.background = n;
        s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = text;
        s.border = new RectOffset(ButtonR, ButtonR, ButtonR, ButtonR);
        s.alignment = TextAnchor.MiddleCenter;
        s.fontStyle = FontStyle.Bold;
        s.fontSize = 32;
        s.padding = new RectOffset(10, 10, 0, 7);
        s.clipping = TextClipping.Overflow;
        return s;
    }

    public static void Ensure()
    {
        if (sPrimary != null && tPrimary != null) return;

        Color clear = new Color(0f, 0f, 0f, 0f);
        var goldTop = new Color(1.00f, 0.90f, 0.45f); var goldBot = new Color(1.00f, 0.76f, 0.20f); var goldLip = new Color(0.78f, 0.52f, 0.10f);
        tPrimary = Round(ButtonR, goldTop, goldBot, clear, 0f, 8, goldLip);
        tPrimaryHover = Round(ButtonR, new Color(1.00f, 0.94f, 0.60f), new Color(1.00f, 0.82f, 0.30f), clear, 0f, 8, goldLip);
        tPrimaryDown = Round(ButtonR, goldBot, new Color(0.92f, 0.68f, 0.16f), clear, 0f, 3, goldLip);

        var slTop = new Color(0.34f, 0.41f, 0.57f); var slBot = new Color(0.24f, 0.30f, 0.44f); var slLip = new Color(0.13f, 0.17f, 0.28f);
        var slEdge = new Color(0.52f, 0.60f, 0.78f);
        tSecondary = Round(ButtonR, slTop, slBot, slEdge, 2f, 8, slLip);
        tSecondaryHover = Round(ButtonR, new Color(0.42f, 0.50f, 0.68f), new Color(0.30f, 0.37f, 0.53f), slEdge, 2f, 8, slLip);
        tSecondaryDown = Round(ButtonR, slBot, new Color(0.20f, 0.25f, 0.38f), slEdge, 2f, 3, slLip);

        tCard = Round(CardR, new Color(0.14f, 0.19f, 0.31f, 0.94f), new Color(0.07f, 0.10f, 0.19f, 0.94f), new Color(0.40f, 0.48f, 0.68f, 0.95f), 2f, 0, clear);
        tPill = Round(PillR, Color.white, Color.white, clear, 0f, 0, clear);
        tBar = Round(BarR, Color.white, Color.white, clear, 0f, 0, clear);
        tShadow = Round(ButtonR, new Color(0f, 0f, 0f, 0.38f), new Color(0f, 0f, 0f, 0.38f), clear, 0f, 0, clear);
        tScrim = Scrim();
        tThumb = Round(17, new Color(1.00f, 0.90f, 0.45f), new Color(1.00f, 0.76f, 0.20f), clear, 0f, 4, goldLip);
        tTrack = Round(BarR, new Color(0.04f, 0.06f, 0.12f, 0.9f), new Color(0.12f, 0.16f, 0.26f, 0.9f), clear, 0f, 0, clear);

        sPrimary = Button(tPrimary, tPrimaryHover, tPrimaryDown, new Color(0.14f, 0.12f, 0.08f));
        sSecondary = Button(tSecondary, tSecondaryHover, tSecondaryDown, Cream);

        // tabs: the chosen one is gold
        sTab = Button(tSecondary, tSecondaryHover, tSecondaryDown, Cream);
        sTab.onNormal.background = tPrimary;
        sTab.onHover.background = tPrimaryHover;
        sTab.onActive.background = tPrimaryDown;
        sTab.onFocused.background = tPrimary;
        sTab.onNormal.textColor = sTab.onHover.textColor = sTab.onActive.textColor = sTab.onFocused.textColor = new Color(0.14f, 0.12f, 0.08f);
        sTab.fontSize = 20;

        sCard = Box(tCard, CardR);
        sPill = Box(tPill, PillR);
        sBar = Box(tBar, BarR);
        sShadow = Box(tShadow, ButtonR);

        sSlider = Box(tTrack, BarR);
        sSlider.fixedHeight = 14f;
        sSlider.overflow = new RectOffset(0, 0, 0, 0);
        sThumb = new GUIStyle();
        sThumb.normal.background = tThumb;
        sThumb.hover.background = tThumb;
        sThumb.active.background = tThumb;
        sThumb.border = new RectOffset(17, 17, 17, 17);
        sThumb.fixedWidth = 34f;
        sThumb.fixedHeight = 34f;
    }

    // ---- drawing helpers ----

    // A soft shadow under a rounded shape, a little below it.
    public static void Shadow(Rect r, float lift = 7f)
    {
        Ensure();
        GUI.Box(new Rect(r.x - 2f, r.y + lift, r.width + 4f, r.height), GUIContent.none, sShadow);
    }

    public static bool Primary(Rect r, string text, int fontSize = 34)
    {
        Ensure();
        Shadow(r);
        int old = sPrimary.fontSize;
        sPrimary.fontSize = fontSize;
        bool hit = GUI.Button(r, text, sPrimary);
        sPrimary.fontSize = old;
        return hit;
    }

    public static bool Secondary(Rect r, string text, int fontSize = 28)
    {
        Ensure();
        Shadow(r);
        int old = sSecondary.fontSize;
        sSecondary.fontSize = fontSize;
        bool hit = GUI.Button(r, text, sSecondary);
        sSecondary.fontSize = old;
        return hit;
    }

    // A rounded panel with a shadow.
    public static void Card(Rect r)
    {
        Ensure();
        Shadow(r, 9f);
        GUI.Box(r, GUIContent.none, sCard);
    }

    // A small rounded chip in any colour (tint it dark for a label backdrop, or bright for a badge).
    public static void Pill(Rect r, Color tint)
    {
        Ensure();
        Color old = GUI.color;
        GUI.color = tint;
        GUI.Box(r, GUIContent.none, sPill);
        GUI.color = old;
    }

    // A thin rounded progress bar.
    public static void Bar(Rect r, float fill, Color color)
    {
        Ensure();
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.Box(r, GUIContent.none, sBar);
        fill = Mathf.Clamp01(fill);
        if (fill > 0.001f)
        {
            GUI.color = color;
            GUI.Box(new Rect(r.x, r.y, Mathf.Max(r.height, r.width * fill), r.height), GUIContent.none, sBar);
        }
        GUI.color = old;
    }

    // The darkening gradient over the game behind a menu (darker at the top and the bottom so the text reads, clear in the middle).
    public static void Scrim(float w, float h, float strength = 1f)
    {
        Ensure();
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, strength);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), tScrim, ScaleMode.StretchToFill);
        GUI.color = old;
    }

    // Text with a drop shadow (and optionally a dark outline), so it reads over the 3D scene.
    public static void Label(Rect r, string text, GUIStyle style, float outline = 0f)
    {
        Color keep = style.normal.textColor;
        Color dark = new Color(0.04f, 0.06f, 0.12f, 0.85f);
        style.normal.textColor = dark;
        if (outline > 0f)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * outline, r.y + Mathf.Sin(a) * outline, r.width, r.height), text, style);
            }
        }
        GUI.Label(new Rect(r.x, r.y + Mathf.Max(2f, outline + 1.5f), r.width, r.height), text, style);
        style.normal.textColor = keep;
        GUI.Label(r, text, style);
    }

    // The emblem: a folded newspaper, tilted, with a masthead, two columns of text and a picture.
    public static void Newspaper(Vector2 center, float size, float angle)
    {
        Ensure();
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        var sheet = new Rect(center.x - size * 0.5f, center.y - size * 0.6f, size, size * 1.2f);
        Shadow(sheet, 6f);
        Pill(sheet, Cream);
        float pad = size * 0.11f;
        Pill(new Rect(sheet.x + pad, sheet.y + pad, sheet.width - pad * 2f, size * 0.16f), Navy);
        float lineH = Mathf.Max(5f, size * 0.045f);
        float y = sheet.y + pad + size * 0.16f + size * 0.10f;
        float colW = (sheet.width - pad * 2f - size * 0.08f) * 0.5f;
        // left column: a picture, then text lines
        var pic = new Rect(sheet.x + pad, y, colW, size * 0.34f);
        Color old = GUI.color;
        GUI.color = Sky;
        GUI.Box(pic, GUIContent.none, sBar);
        GUI.color = Gold;
        GUI.Box(new Rect(pic.x + pic.width * 0.58f, pic.y + pic.height * 0.2f, pic.width * 0.22f, pic.width * 0.22f), GUIContent.none, sBar);
        GUI.color = old;
        for (int i = 0; i < 3; i++)
            Line(new Rect(sheet.x + pad, pic.yMax + size * 0.07f + i * (lineH + size * 0.05f), colW * (i == 2 ? 0.6f : 1f), lineH));
        // right column: text lines
        for (int i = 0; i < 6; i++)
            Line(new Rect(sheet.x + pad + colW + size * 0.08f, y + i * (lineH + size * 0.065f), colW * (i == 5 ? 0.55f : 1f), lineH));
        GUI.matrix = saved;
    }

    static void Line(Rect r)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.55f, 0.58f, 0.66f);
        GUI.Box(r, GUIContent.none, sBar);
        GUI.color = old;
    }
}
