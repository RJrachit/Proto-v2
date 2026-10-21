using System.Collections.Generic;
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
    static GUIStyle sPrimary, sSecondary, sTab, sCard, sPill, sBar, sShadow, sSlider, sThumb, sPrimaryText, sSecondaryText, sSoftShadow;
    static Texture2D tSoftShadow;

    // The button colours: yellow (the main action), orange, green (the three colours of the style sheet) and the old dark blue.
    public enum Tone { Yellow, Orange, Green, Slate }
    static readonly GUIStyle[] toneBody = new GUIStyle[4];
    static readonly GUIStyle[] toneText = new GUIStyle[4];
    static readonly Color[] toneOutline = new Color[4];   // the dark colour around a button's text

    // The font of the button text: Lilita One (Assets/Resources/Fonts/LilitaOne-Regular.ttf). Null if the file is missing (the default font is used then).
    static Font buttonFont; static bool fontTried;
    public static Font ButtonFont
    {
        get
        {
            if (!fontTried) { fontTried = true; try { buttonFont = Resources.Load<Font>("Fonts/LilitaOne-Regular"); } catch (System.Exception) { } }
            return buttonFont;
        }
    }
    const int SoftB = 12;   // how far a button's soft shadow spreads

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
        s.padding = new RectOffset(10, 10, 0, 1);
        s.clipping = TextClipping.Overflow;
        return s;
    }

    // A button style in one colour: a light top fading to a deeper bottom; lighter when hovered, deeper when pressed.
    static GUIStyle ToneButton(Color top, Color bottom, Color outline, Color text)
    {
        Texture2D n, hov, down;
        return ToneButton(top, bottom, outline, text, out n, out hov, out down);
    }

    // A cartoon button: a flat colour (a little lighter at the top) inside a thick dark outline; lighter when hovered, deeper when pressed.
    static GUIStyle ToneButton(Color top, Color bottom, Color outline, Color text, out Texture2D n, out Texture2D hov, out Texture2D down)
    {
        Color lighten = new Color(0.07f, 0.07f, 0.07f, 0f);
        n = Cartoon(top, bottom, outline);
        hov = Cartoon(top + lighten, bottom + lighten, outline);
        down = Cartoon(Darken(top, 0.9f), Darken(bottom, 0.9f), Darken(outline, 0.9f));
        return Button(n, hov, down, text);
    }

    static Color Darken(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

    // The 9-slice picture of a cartoon button: a vertical colour fade inside a crisp outline.
    static Texture2D Cartoon(Color top, Color bottom, Color outline)
    {
        const float ow = 3.5f;   // outline thickness in texture pixels
        int size = ButtonR * 2 + 6;
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
                float qx = Mathf.Abs(x + 0.5f - half) - (half - ButtonR);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - ButtonR);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float sd = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - ButtonR;
                float a = Mathf.Clamp01(0.5f - sd);
                float inset = -sd;
                Color body = Color.Lerp(bottom, top, y / (size - 1f));
                Color c = Color.Lerp(body, outline, Mathf.Clamp01(ow - inset + 0.5f));
                c.a = a;
                px[y * size + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static GUIStyle TextOnly(GUIStyle from)
    {
        var s = new GUIStyle(from);
        s.normal.background = s.hover.background = s.active.background = s.focused.background = null;
        s.onNormal.background = s.onHover.background = s.onActive.background = s.onFocused.background = null;
        return s;
    }

    // A rounded shape with a blurred edge, for the soft shadow under a button (9-slice: the border is the corner radius plus the blur).
    static Texture2D SoftShadowTex()
    {
        int rr = ButtonR + SoftB;
        int size = rr * 2 + 6;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color[size * size];
        float half = size * 0.5f;
        float innerHalf = half - SoftB;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (innerHalf - ButtonR);
                float qy = Mathf.Abs(y + 0.5f - half) - (innerHalf - ButtonR);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float sd = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - ButtonR;
                float a = Mathf.Clamp01(0.5f - sd / (2f * SoftB));
                px[y * size + x] = new Color(0f, 0f, 0f, 0.55f * a * a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ---- the shine: a glossy highlight over the top half of a button, a soft shade towards its bottom and a thin bright rim along its top edge ----

    static readonly Dictionary<long, Texture2D> shineCache = new Dictionary<long, Texture2D>();

    // One overlay per button size (drawn once, then stretched to the exact rectangle, so a pulsing button does not make new textures every frame).
    static Texture2D ShineTex(int w, int h, bool primary)
    {
        long key = ((long)w * 4096L + h) * 2L + (primary ? 1L : 0L);
        Texture2D tex;
        if (shineCache.TryGetValue(key, out tex) && tex != null) return tex;

        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color[w * h];
        float r = Mathf.Min(ButtonR, h * 0.5f);
        float glossK = primary ? 1f : 0.8f;
        for (int y = 0; y < h; y++)
        {
            float v = 1f - (y + 0.5f) / h;       // 0 at the top edge, 1 at the bottom edge
            for (int x = 0; x < w; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - r);
                float qy = Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - r);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float sd = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;   // negative inside
                float mask = Mathf.Clamp01(0.5f - sd);
                if (mask <= 0f) { px[y * w + x] = new Color(0f, 0f, 0f, 0f); continue; }
                float inset = -sd;               // pixels in from the edge
                float inside = Mathf.Clamp01((inset - 3.2f) / 1.2f);   // (the dark outline itself is left alone)

                // the inner highlight: a light line just inside the outline, along the top and the upper sides
                float ring = Mathf.Clamp01((inset - 3.2f) / 1.0f) * Mathf.Clamp01(1f - (inset - 5.2f) / 1.2f);
                float ra = ring * Mathf.Lerp(0.65f, 0f, Mathf.Clamp01(v / 0.75f)) * glossK;
                // a soft light over the top of the face
                float ga = 0.20f * glossK * Mathf.Pow(Mathf.Clamp01(1f - v / 0.55f), 1.2f) * inside;
                float wa = 1f - (1f - ra) * (1f - ga);
                // a smooth shade towards the bottom of the face (no hard line)
                float sa = 0.16f * Mathf.Pow(Mathf.Clamp01((v - 0.55f) / 0.45f), 1.5f) * inside;

                px[y * w + x] = wa >= sa ? new Color(1f, 1f, 1f, wa * mask) : new Color(0f, 0f, 0f, sa * mask);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        shineCache[key] = tex;
        return tex;
    }

    // The glossy shine over a button that is already drawn in r. primary: the gold buttons (a stronger shine than the blue ones).
    public static void Shine(Rect r, bool primary, float strength = 1f)
    {
        Ensure();
        int w = Mathf.Clamp(Mathf.RoundToInt(r.width / 16f) * 16, 16, 1024);
        int h = Mathf.Clamp(Mathf.RoundToInt(r.height / 4f) * 4, 8, 256);
        Color old = GUI.color;
        GUI.color = new Color(old.r, old.g, old.b, old.a * strength);
        GUI.DrawTexture(r, ShineTex(w, h, primary), ScaleMode.StretchToFill, true);
        GUI.color = old;
    }

    // The same for a button laid out with GUILayout (call right after the button).
    public static void ShineLast(bool primary)
    {
        if (Event.current.type != EventType.Repaint) return;
        Shine(GUILayoutUtility.GetLastRect(), primary);
    }

    // A soft shadow under a button: it spreads a little and sits below, so the button seems to lift off the screen.
    public static void ButtonShadow(Rect r)
    {
        Ensure();
        GUI.Box(new Rect(r.x - SoftB, r.y - SoftB + 9f, r.width + 2f * SoftB, r.height + 2f * SoftB), GUIContent.none, sSoftShadow);
    }

    // ---- the sticky note: a torn piece of squared paper with a strip of yellow tape (the tutorial's step cards) ----

    static Font noteTitleFont, noteBodyFont; static bool noteFontsTried;
    static void LoadNoteFonts()
    {
        if (noteFontsTried) return;
        noteFontsTried = true;
        try
        {
            noteTitleFont = Resources.Load<Font>("Fonts/PermanentMarker-Regular");   // Assets/Resources/Fonts
            noteBodyFont = Resources.Load<Font>("Fonts/PatrickHand-Regular");
        }
        catch (System.Exception) { }
    }
    public static Font NoteTitleFont { get { LoadNoteFonts(); return noteTitleFont; } }   // (null if the file is missing: the default font is used then)
    public static Font NoteBodyFont { get { LoadNoteFonts(); return noteBodyFont; } }

    const int NoteM = 22, NoteTop = 26;   // room around the paper for the shadow, and above it for the tape
    static readonly Dictionary<long, Texture2D> noteCache = new Dictionary<long, Texture2D>();

    static float NoiseAt(int n)
    {
        unchecked
        {
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;   // -1..1
        }
    }

    // Smooth value noise along the torn edge.
    static float Smooth1D(float x, int seed)
    {
        int i = Mathf.FloorToInt(x);
        float f = x - i;
        f = f * f * (3f - 2f * f);
        return Mathf.Lerp(NoiseAt(i * 31 + seed), NoiseAt((i + 1) * 31 + seed), f);
    }

    // The height (in pixels, up or down) of the torn bottom edge at column x.
    static float TearAt(int x)
    {
        return 0.55f * Smooth1D(x / 16f, 7) + 0.30f * Smooth1D(x / 5f, 19) + 0.15f * NoiseAt(x * 7 + 3);
    }

    static Texture2D NoteTex(int qw, int qh)
    {
        long key = (long)qw * 4096L + qh;
        Texture2D tex;
        if (noteCache.TryGetValue(key, out tex) && tex != null) return tex;

        int tw = qw + NoteM * 2, th = qh + NoteM + NoteTop;
        tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color[tw * th];
        float baseBottom = NoteTop + qh - 9f;
        float cx = NoteM + qw * 0.5f;
        var paperA = new Color(0.985f, 0.955f, 0.895f);
        var paperB = new Color(0.945f, 0.905f, 0.835f);
        for (int ty = 0; ty < th; ty++)
        {
            int y = th - 1 - ty;                  // from the top of the picture
            for (int x = 0; x < tw; x++)
            {
                float yb = baseBottom + TearAt(x) * 7f;
                // the paper
                float cov = Mathf.Clamp01(x + 0.5f - NoteM) * Mathf.Clamp01(NoteM + qw - x - 0.5f) * Mathf.Clamp01(y + 0.5f - NoteTop) * Mathf.Clamp01(yb - y);
                // the shadow, a soft shape a little right of and below the paper
                float yb2 = baseBottom + TearAt(x - 4) * 7f + 7f;
                float dx = Mathf.Max(Mathf.Max(NoteM + 4 - x, x - (NoteM + qw + 4)), 0f);
                float dy = Mathf.Max(Mathf.Max(NoteTop + 7 - y, y - yb2), 0f);
                float dsh = Mathf.Sqrt(dx * dx + dy * dy);
                float sha = 0.40f * Mathf.Pow(Mathf.Clamp01(1f - dsh / 15f), 2f);
                Color c = new Color(0f, 0f, 0f, sha);
                if (cov > 0f)
                {
                    float t = Mathf.Clamp01((y - NoteTop) / (float)qh);
                    Color p = Color.Lerp(paperA, paperB, t * 0.8f);
                    int gx = x - NoteM, gy = y - NoteTop;
                    if (gx % 14 == 0 || gy % 14 == 0) p = Color.Lerp(p, new Color(0.62f, 0.68f, 0.78f), 0.20f);   // the squares
                    float g = NoiseAt(x * 131 + y * 17) * 0.012f;
                    p = new Color(p.r + g, p.g + g, p.b + g);
                    float nearTear = yb - y;
                    if (nearTear < 3.2f) p = Color.Lerp(p, new Color(1f, 0.995f, 0.985f), 0.8f * (1f - nearTear / 3.2f));   // the pale fibres of a torn edge
                    c = Color.Lerp(new Color(0f, 0f, 0f, sha), new Color(p.r, p.g, p.b, 1f), cov);
                    c.a = cov + (1f - cov) * sha;
                }
                // the tape: a yellow strip across the top, slightly slanted, with ragged ends
                float xs = x - (y - NoteTop) * 0.10f;
                float half = 36f + NoiseAt(y * 5 + 1) * 1.5f;
                float tcov = Mathf.Clamp01(half - Mathf.Abs(xs - cx)) * Mathf.Clamp01(y + 0.5f - (NoteTop - 18)) * Mathf.Clamp01(NoteTop + 11 - y - 0.5f);
                if (tcov > 0f)
                {
                    float tt = (y - (NoteTop - 18)) / 29f;
                    Color tape = Color.Lerp(new Color(1f, 0.88f, 0.28f), new Color(0.97f, 0.76f, 0.05f), tt);
                    float ta = 0.92f * tcov;
                    Color over = new Color(c.r, c.g, c.b);
                    Color mix = Color.Lerp(over, tape, ta);
                    c = new Color(mix.r, mix.g, mix.b, Mathf.Clamp01(c.a + (1f - c.a) * ta));
                }
                px[ty * tw + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        noteCache[key] = tex;
        return tex;
    }

    // Draws the note so that its paper fills r (the tape sticks up above it and the shadow spreads around it). Not clickable: it only draws.
    public static void Note(Rect r)
    {
        int qw = Mathf.Clamp(Mathf.RoundToInt(r.width / 8f) * 8, 64, 1024);
        int qh = Mathf.Clamp(Mathf.RoundToInt(r.height / 8f) * 8, 64, 512);
        float sx = r.width / qw, sy = r.height / qh;
        GUI.DrawTexture(new Rect(r.x - NoteM * sx, r.y - NoteTop * sy, (qw + NoteM * 2) * sx, (qh + NoteM + NoteTop) * sy), NoteTex(qw, qh), ScaleMode.StretchToFill, true);
    }

    public static void Ensure()
    {
        if (sPrimary != null && tPrimary != null) return;

        Color clear = new Color(0f, 0f, 0f, 0f);
        var goldLip = new Color(0.78f, 0.52f, 0.10f);
        var ink = new Color(0.14f, 0.12f, 0.08f);

        // the button colours: yellow #FFD400, orange #FE7E0F, green #87C830 (each with a darker outline of its own), and the old dark blue
        var yellowOut = new Color(0.62f, 0.40f, 0.02f); var orangeOut = new Color(0.62f, 0.22f, 0.00f);
        var greenOut = new Color(0.22f, 0.45f, 0.05f); var slateOut = new Color(0.10f, 0.13f, 0.24f);
        toneOutline[(int)Tone.Yellow] = new Color(0.46f, 0.29f, 0.00f);
        toneOutline[(int)Tone.Orange] = new Color(0.50f, 0.17f, 0.00f);
        toneOutline[(int)Tone.Green] = new Color(0.15f, 0.34f, 0.03f);
        toneOutline[(int)Tone.Slate] = new Color(0.06f, 0.09f, 0.18f);

        sPrimary = ToneButton(new Color(1.00f, 0.89f, 0.22f), new Color(1.00f, 0.78f, 0.00f), yellowOut, ink, out tPrimary, out tPrimaryHover, out tPrimaryDown);
        sSecondary = ToneButton(new Color(0.40f, 0.48f, 0.66f), new Color(0.30f, 0.37f, 0.54f), slateOut, Cream, out tSecondary, out tSecondaryHover, out tSecondaryDown);

        tCard = Round(CardR, new Color(0.14f, 0.19f, 0.31f, 0.94f), new Color(0.07f, 0.10f, 0.19f, 0.94f), new Color(0.40f, 0.48f, 0.68f, 0.95f), 2f, 0, clear);
        tPill = Round(PillR, Color.white, Color.white, clear, 0f, 0, clear);
        tBar = Round(BarR, Color.white, Color.white, clear, 0f, 0, clear);
        tShadow = Round(ButtonR, new Color(0f, 0f, 0f, 0.38f), new Color(0f, 0f, 0f, 0.38f), clear, 0f, 0, clear);
        tScrim = Scrim();
        tThumb = Round(17, new Color(1.00f, 0.90f, 0.45f), new Color(1.00f, 0.76f, 0.20f), clear, 0f, 4, goldLip);
        tTrack = Round(BarR, new Color(0.04f, 0.06f, 0.12f, 0.9f), new Color(0.12f, 0.16f, 0.26f, 0.9f), clear, 0f, 0, clear);

        // tabs: the chosen one is gold
        sTab = Button(tSecondary, tSecondaryHover, tSecondaryDown, Cream);
        sTab.onNormal.background = tPrimary;
        sTab.onHover.background = tPrimaryHover;
        sTab.onActive.background = tPrimaryDown;
        sTab.onFocused.background = tPrimary;
        sTab.onNormal.textColor = sTab.onHover.textColor = sTab.onActive.textColor = sTab.onFocused.textColor = new Color(0.14f, 0.12f, 0.08f);
        sTab.fontSize = 20;

        // the text of a button is drawn on top of its shine, with these (the same look, no background)
        sPrimaryText = TextOnly(sPrimary);
        sSecondaryText = TextOnly(sSecondary);

        toneBody[(int)Tone.Yellow] = sPrimary;
        toneBody[(int)Tone.Slate] = sSecondary;
        toneBody[(int)Tone.Orange] = ToneButton(new Color(1.00f, 0.60f, 0.20f), new Color(0.99f, 0.46f, 0.03f), orangeOut, ink);
        toneBody[(int)Tone.Green] = ToneButton(new Color(0.60f, 0.85f, 0.27f), new Color(0.49f, 0.75f, 0.14f), greenOut, ink);
        for (int i = 0; i < toneBody.Length; i++)
        {
            toneText[i] = TextOnly(toneBody[i]);
            if (ButtonFont != null) toneText[i].font = ButtonFont;   // the chunky button font (Assets/Resources/Fonts)
        }

        tSoftShadow = SoftShadowTex();
        sSoftShadow = Box(tSoftShadow, ButtonR + SoftB);

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

    // A button: its soft shadow, the body (which reacts to hover and press), the shine on top, and the text above the shine.
    static bool Face(Rect r, string text, int fontSize, Tone tone)
    {
        Ensure();
        GUIStyle body = toneBody[(int)tone], textStyle = toneText[(int)tone];
        bool primary = tone != Tone.Slate;
        ButtonShadow(r);
        bool hit = GUI.Button(r, GUIContent.none, body);
        bool down = Event.current != null && Input.GetMouseButton(0) && r.Contains(Event.current.mousePosition);
        Shine(r, primary, down ? 0.55f : 1f);
        if (!string.IsNullOrEmpty(text))
        {
            int old = textStyle.fontSize;
            textStyle.fontSize = fontSize;
            Rect tr = r;
            if (down) tr.y += 2f;                               // (the label sinks a little while pressed)
            float o = Mathf.Max(2f, fontSize * 0.075f);         // outline thickness
            Color dark = toneOutline[(int)tone];
            Color keep = textStyle.normal.textColor;
            textStyle.normal.textColor = new Color(dark.r, dark.g, dark.b, 0.75f);
            GUI.Label(new Rect(tr.x, tr.y + o + 2.5f, tr.width, tr.height), text, textStyle);   // the drop shadow
            textStyle.normal.textColor = dark;
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI * 0.25f;
                GUI.Label(new Rect(tr.x + Mathf.Cos(ang) * o, tr.y + Mathf.Sin(ang) * o, tr.width, tr.height), text, textStyle);   // the outline
            }
            textStyle.normal.textColor = Color.white;
            GUI.Label(tr, text, textStyle);
            textStyle.normal.textColor = keep;
            textStyle.fontSize = old;
        }
        return hit;
    }

    public static bool Primary(Rect r, string text, int fontSize = 34)
    {
        return Face(r, text, fontSize, Tone.Yellow);
    }

    // A button in one of the style-sheet colours (yellow, orange, green).
    public static bool Tinted(Rect r, string text, int fontSize, Tone tone)
    {
        return Face(r, text, fontSize, tone);
    }

    public static bool Secondary(Rect r, string text, int fontSize = 28)
    {
        return Face(r, text, fontSize, Tone.Slate);
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
