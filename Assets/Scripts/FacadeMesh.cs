using System.Collections.Generic;
using UnityEngine;

// The look of a building's details (window frames, cornices, parapets, rooftop props, sign frames) is one mesh per house, drawn with ONE shared
// material, so a whole facade costs a single draw call. Every face of the mesh points at one pixel of a palette texture (32 x 32 x 32 colours,
// point filtered), so any colour can be used without its own material. The mesh has no collider: it is only for looks.

public static class FacadePalette
{
    const int Steps = 32, TexW = 256, TexH = 128;   // 32 * 32 * 32 = 256 * 128 pixels
    static Texture2D tex;
    static Material mat;

    // Unity-aware null checks (not ??): a material destroyed when Play mode ended must be rebuilt, not reused.
    public static Material Mat
    {
        get
        {
            if (mat == null)
            {
                if (tex == null) tex = Build();
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var r = tmp.GetComponent<Renderer>();
                Prim.FixMaterial(r);
                mat = new Material(r.sharedMaterial);
                Object.Destroy(tmp);
                mat.name = "FacadePalette";
                mat.mainTexture = tex;
                mat.color = Color.white;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            }
            return mat;
        }
    }

    static Texture2D Build()
    {
        var t = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false);
        t.name = "FacadePalette";
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[TexW * TexH];
        for (int i = 0; i < Steps * Steps * Steps; i++)
        {
            int b = i % Steps, g = (i / Steps) % Steps, r = i / (Steps * Steps);
            px[i] = new Color32((byte)(r * 255 / (Steps - 1)), (byte)(g * 255 / (Steps - 1)), (byte)(b * 255 / (Steps - 1)), 255);
        }
        t.SetPixels32(px);
        t.Apply(false, true);
        return t;
    }

    // The middle of the palette pixel nearest to this colour.
    public static Vector2 UV(Color c)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(c.r * (Steps - 1)), 0, Steps - 1);
        int g = Mathf.Clamp(Mathf.RoundToInt(c.g * (Steps - 1)), 0, Steps - 1);
        int b = Mathf.Clamp(Mathf.RoundToInt(c.b * (Steps - 1)), 0, Steps - 1);
        int i = (r * Steps + g) * Steps + b;
        return new Vector2(((i % TexW) + 0.5f) / TexW, ((i / TexW) + 0.5f) / TexH);
    }
}

// Collects flat-shaded boxes and prisms into lists and uploads them to a mesh. One builder is shared by all houses (they build one at a time).
public class FacadeBuilder
{
    const int MaxVerts = 60000;
    readonly List<Vector3> v = new List<Vector3>(4096);
    readonly List<Vector3> n = new List<Vector3>(4096);
    readonly List<Vector2> uv = new List<Vector2>(4096);
    readonly List<int> t = new List<int>(8192);

    public bool muted;   // dull colours, for a house that is not an active customer (same rule as the walls)

    public int Count { get { return v.Count; } }

    public void Clear() { v.Clear(); n.Clear(); uv.Clear(); t.Clear(); }

    static Color Dull(Color c)
    {
        float h, s, val;
        Color.RGBToHSV(c, out h, out s, out val);
        return Color.HSVToRGB(h, s * 0.55f, val * 0.9f);
    }

    public void Box(Vector3 center, Vector3 size, Color col) { Box(center, size, col, Quaternion.identity); }

    public void Box(Vector3 center, Vector3 size, Color col, Quaternion rot)
    {
        if (v.Count > MaxVerts) return;
        Vector2 u = FacadePalette.UV(muted ? Dull(col) : col);
        Vector3 h = size * 0.5f;
        // +x, -x
        Quad(center, rot, new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, -h.y, h.z), Vector3.right, u);
        Quad(center, rot, new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, -h.y, -h.z), Vector3.left, u);
        // +y (no underside: nobody sees it)
        Quad(center, rot, new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z), Vector3.up, u);
        // +z, -z
        Quad(center, rot, new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, -h.y, h.z), Vector3.forward, u);
        Quad(center, rot, new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, -h.y, -h.z), Vector3.back, u);
    }

    // A prism around the local y axis (an octagon for a tank, a disc for a dish), radius rb at the bottom and rt at the top.
    public void Cyl(Vector3 center, float rb, float rt, float height, int sides, Color col, Quaternion rot)
    {
        if (v.Count > MaxVerts) return;
        Vector2 u = FacadePalette.UV(muted ? Dull(col) : col);
        float hy = height * 0.5f;
        for (int i = 0; i < sides; i++)
        {
            float a0 = Mathf.PI * 2f * i / sides, a1 = Mathf.PI * 2f * (i + 1) / sides;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * rb, -hy, Mathf.Sin(a0) * rb), b1 = new Vector3(Mathf.Cos(a1) * rb, -hy, Mathf.Sin(a1) * rb);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * rt, hy, Mathf.Sin(a0) * rt), t1 = new Vector3(Mathf.Cos(a1) * rt, hy, Mathf.Sin(a1) * rt);
            float am = (a0 + a1) * 0.5f;
            Quad(center, rot, b0, t0, t1, b1, new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am)), u);
            // top cap
            Tri(center, rot, new Vector3(0f, hy, 0f), t1, t0, Vector3.up, u);
        }
    }

    // A tapered block: the bottom rectangle (y0, width w0, from zb0 at the back to zf0 at the front) and the top rectangle (y1, w1, zb1..zf1)
    // centred on x = cx. The faces slope from one to the other, so it makes a bonnet, a windscreen or a boot lid. Used for the vehicles.
    public void Frustum(float y0, float y1, float zb0, float zf0, float zb1, float zf1, float w0, float w1, float cx, Color col)
    {
        if (v.Count > MaxVerts) return;
        Vector2 u = FacadePalette.UV(muted ? Dull(col) : col);
        float a = w0 * 0.5f, b = w1 * 0.5f;
        Vector3 bl0 = new Vector3(cx - a, y0, zb0), br0 = new Vector3(cx + a, y0, zb0), fr0 = new Vector3(cx + a, y0, zf0), fl0 = new Vector3(cx - a, y0, zf0);
        Vector3 bl1 = new Vector3(cx - b, y1, zb1), br1 = new Vector3(cx + b, y1, zb1), fr1 = new Vector3(cx + b, y1, zf1), fl1 = new Vector3(cx - b, y1, zf1);
        Vector3 mid = (bl0 + br0 + fr0 + fl0 + bl1 + br1 + fr1 + fl1) / 8f;
        Quad4(br0, fr0, fr1, br1, mid, u);   // right
        Quad4(fl0, bl0, bl1, fl1, mid, u);   // left
        Quad4(fr0, fl0, fl1, fr1, mid, u);   // front
        Quad4(bl0, br0, br1, bl1, mid, u);   // back
        Quad4(bl1, br1, fr1, fl1, mid, u);   // top
    }

    void Quad4(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside, Vector2 u)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        if (Vector3.Dot(n, (a + b + c + d) * 0.25f - inside) < 0f) n = -n;
        Quad(Vector3.zero, Quaternion.identity, a, b, c, d, n, u);
    }

    static float Rnd(float x) { return Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f); }

    // A low-poly tuft of grass: 'blades' flat, faceted spikes leaning out from a point on the ground. Each blade is a thin three-sided
    // pyramid whose faces are three shades of one green (so it reads as cut facets), a few taller than the rest. seed varies every tuft.
    public void Tuft(Vector3 c, float height, float spread, int blades, Color light, Color dark, float seed, float width = 1f)
    {
        if (v.Count > MaxVerts) return;
        for (int i = 0; i < blades; i++)
        {
            float h1 = Rnd(seed + i * 1.7f + 0.3f), h2 = Rnd(seed + i * 2.9f + 3.1f), h3 = Rnd(seed + i * 4.3f + 7.7f);
            float ang = (i + h1 * 0.8f) * (Mathf.PI * 2f / blades);
            Vector3 outDir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 tan = new Vector3(-outDir.z, 0f, outDir.x);
            Vector3 b = c + outDir * (spread * 0.5f * (0.25f + 0.75f * h2));
            float hh = height * (0.5f + 0.5f * h3) * (i == 0 ? 1.3f : 1f);
            Vector3 apex = b + outDir * (spread * (0.35f + 0.7f * h1)) + Vector3.up * hh;
            float w = (0.05f + 0.035f * h2) * width, t = 0.03f * width;
            Vector3 p0 = b - tan * w, p1 = b + tan * w, p2 = b - outDir * t;
            Vector3 inside = (p0 + p1 + p2) / 3f + Vector3.up * (hh * 0.25f);
            Color baseCol = Color.Lerp(light, dark, h3);
            Face(p0, p1, apex, inside, FacadePalette.UV(baseCol));
            Face(p1, p2, apex, inside, FacadePalette.UV(baseCol * 0.9f));
            Face(p2, p0, apex, inside, FacadePalette.UV(baseCol * 0.78f));
        }
    }

    void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Vector2 u)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f) n = -n;
        Tri(Vector3.zero, Quaternion.identity, a, b, c, n, u);
    }

    // p0..p3 go round the face; the winding is fixed so that the face looks along 'normal' (Unity: clockwise seen from the front).
    void Quad(Vector3 c, Quaternion rot, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Vector2 u)
    {
        Vector3 w0 = c + rot * p0, w1 = c + rot * p1, w2 = c + rot * p2, w3 = c + rot * p3;
        Vector3 nw = rot * normal;
        bool flip = Vector3.Dot(Vector3.Cross(w1 - w0, w2 - w0), nw) < 0f;
        int i = v.Count;
        if (!flip) { v.Add(w0); v.Add(w1); v.Add(w2); v.Add(w3); }
        else { v.Add(w0); v.Add(w3); v.Add(w2); v.Add(w1); }
        for (int k = 0; k < 4; k++) { n.Add(nw); uv.Add(u); }
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        t.Add(i); t.Add(i + 2); t.Add(i + 3);
    }

    void Tri(Vector3 c, Quaternion rot, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal, Vector2 u)
    {
        Vector3 w0 = c + rot * p0, w1 = c + rot * p1, w2 = c + rot * p2;
        Vector3 nw = rot * normal;
        bool flip = Vector3.Dot(Vector3.Cross(w1 - w0, w2 - w0), nw) < 0f;
        int i = v.Count;
        if (!flip) { v.Add(w0); v.Add(w1); v.Add(w2); }
        else { v.Add(w0); v.Add(w2); v.Add(w1); }
        for (int k = 0; k < 3; k++) { n.Add(nw); uv.Add(u); }
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    // Fills the mesh with what was collected (the mesh is created the first time).
    public Mesh Upload(Mesh m)
    {
        if (m == null)
        {
            m = new Mesh();
            m.name = "Facade";
            m.MarkDynamic();
        }
        m.Clear();
        if (v.Count == 0) return m;
        m.SetVertices(v);
        m.SetNormals(n);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
}
