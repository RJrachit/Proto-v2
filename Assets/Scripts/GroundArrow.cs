using UnityEngine;

// The aim arrow on the ground: a train of chevrons (">" shapes, like the arrows painted on a road) marching along the direction of the throw,
// with no arrowhead (they thin out towards the end). One dynamic mesh (a dark outline layer under a coloured fill layer), rebuilt every frame while aiming.
// The colour says how hard the pull is (see PowerColor). Every chevron lies flat just above the highest ground under it (several rays per
// chevron, so a kerb or a step cutting across it at an angle never sinks one corner under the pavement). Made and driven by PaperThrower.
public class GroundArrow
{
    const int MaxChev = 22;                       // chevrons at most
    const int PerLayer = MaxChev * 6 + 3;         // six vertices a chevron (the last three are spare, collapsed)
    const int Layers = 2;                         // 0 = outline, 1 = fill

    const float ChevHalf = 0.42f;                 // half width of a chevron
    const float ChevDepth = 0.55f;                // how far its point sticks out in front of its back corners
    const float ChevThick = 0.20f;                // thickness of its band
    const float Period = 1.15f;                   // distance from one chevron to the next
    const float MarchSpeed = 1.6f;                // m/s the chevrons move towards the target

    readonly GameObject go;
    readonly Mesh mesh;
    readonly MeshRenderer mr;
    readonly Vector3[] vert = new Vector3[PerLayer * Layers];
    readonly Color[] col = new Color[PerLayer * Layers];

    public GroundArrow(Transform parent)
    {
        go = new GameObject("GroundAimArrow");
        // (not a child of the bike: the vertices are world positions, so a moving parent would carry the arrow away with it)
        var mf = go.AddComponent<MeshFilter>();
        mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mat.Particle();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mesh = new Mesh { name = "GroundAimArrow" };
        mesh.MarkDynamic();
        mesh.vertices = vert;
        mesh.colors = col;
        var tri = new int[Layers * (MaxChev * 12 + 3)];
        int t = 0;
        for (int l = 0; l < Layers; l++)
        {
            int b = l * PerLayer;
            for (int c = 0; c < MaxChev; c++)
            {
                int v = b + c * 6;   // 0 A, 1 B (the point), 2 C, 3 A', 4 B', 5 C'
                tri[t++] = v; tri[t++] = v + 1; tri[t++] = v + 4;
                tri[t++] = v; tri[t++] = v + 4; tri[t++] = v + 3;
                tri[t++] = v + 1; tri[t++] = v + 2; tri[t++] = v + 5;
                tri[t++] = v + 1; tri[t++] = v + 5; tri[t++] = v + 4;
            }
            tri[t++] = b + MaxChev * 6; tri[t++] = b + MaxChev * 6 + 1; tri[t++] = b + MaxChev * 6 + 2;
        }
        mesh.triangles = tri;
        mf.sharedMesh = mesh;
        mr.enabled = false;
    }

    public void Destroy() { if (go != null) Object.Destroy(go); }

    public void SetEnabled(bool on) { if (mr != null) mr.enabled = on; }

    // Power 0..1 -> green, yellow, orange, red (below average, medium, above average, maximum).
    public static Color PowerColor(float power)
    {
        float p = Mathf.Clamp01(power);
        Color green = new Color(0.25f, 0.90f, 0.30f), yellow = new Color(1f, 0.92f, 0.20f), orange = new Color(1f, 0.55f, 0.10f), red = new Color(1f, 0.15f, 0.12f);
        if (p < 0.40f) return Color.Lerp(green, yellow, p / 0.40f);
        if (p < 0.70f) return Color.Lerp(yellow, orange, (p - 0.40f) / 0.30f);
        return Color.Lerp(orange, red, (p - 0.70f) / 0.30f);
    }

    // The highest ground under a point (the lowest it may be is the road).
    static float Hi(System.Func<float, float, float> groundY, Vector2 p) { return Mathf.Max(0.04f, groundY(p.x, p.y)); }

    // a = start, b = tip (ground x, z); groundY(x, z) = the height of the surface there; valid = a real throw (else a faint white arrow).
    public void Draw(Vector2 a, Vector2 b, System.Func<float, float, float> groundY, bool valid, float power)
    {
        float total = (b - a).magnitude;
        if (total < 0.3f) { mr.enabled = false; return; }
        mr.enabled = true;

        Vector2 d = (b - a) / total;
        if (total > 2.5f) a += d * 0.9f;   // (it starts a little ahead of the bike, not under it)
        float len = (b - a).magnitude;
        Vector2 perp = new Vector2(-d.y, d.x);
        float shaftLen = len;   // (no arrowhead: the running chevrons point the way, and thin out towards the end)

        Color hue = valid ? PowerColor(power) : Color.white;
        float baseA = valid ? 1f : 0.28f;

        // collapse everything first, then fill in what is used
        Vector3 nowhere = new Vector3(a.x, -5f, a.y);
        for (int i = 0; i < vert.Length; i++) { vert[i] = nowhere; col[i] = Color.clear; }

        // chevrons march towards the head: the k-th sits at (k * Period + shift) along the shaft
        float shift = (Time.unscaledTime * MarchSpeed) % Period;
        for (int c = 0; c < MaxChev; c++)
        {
            float s = c * Period + shift - Period * 0.5f;          // where its back corners are, along the shaft
            if (s < 0f) continue;                                  // (still behind the start)
            if (s + ChevDepth > shaftLen) break;                   // (it would pass the end)
            Vector2 back = a + d * s;
            Vector2 tip = back + d * ChevDepth;
            Vector2 left = back + perp * ChevHalf, right = back - perp * ChevHalf;
            float h = Mathf.Max(Mathf.Max(Hi(groundY, left), Hi(groundY, right)), Mathf.Max(Hi(groundY, tip), Hi(groundY, back)));
            float k = s / Mathf.Max(0.1f, shaftLen);
            float fade = Mathf.Clamp01(s / 1.2f);                  // appears gradually in front of the bike
            float tailOff = Mathf.Clamp01((shaftLen - s - ChevDepth) / 1.4f);   // thins out over the last metre and a half
            float alpha = baseA * Mathf.Lerp(0.35f, 1f, k) * fade * Mathf.Lerp(0.25f, 1f, tailOff);

            for (int l = 0; l < Layers; l++)
            {
                bool fill = l == 1;
                float g = fill ? 0f : 0.07f;                       // the outline layer is a little bigger all round
                float lift = fill ? 0.10f : 0.08f;
                int v = l * PerLayer + c * 6;
                float w = ChevHalf + g, th = ChevThick + g * 2f;
                Vector2 pb = back - d * g;
                vert[v]     = P3(pb + perp * w, h + lift);
                vert[v + 1] = P3(back + d * (ChevDepth + g * 1.6f), h + lift);
                vert[v + 2] = P3(pb - perp * w, h + lift);
                vert[v + 3] = P3(pb + perp * w - d * (th - g * 2f), h + lift);
                vert[v + 4] = P3(back + d * (ChevDepth - ChevThick - g * 0.4f), h + lift);
                vert[v + 5] = P3(pb - perp * w - d * (th - g * 2f), h + lift);
                Color cc = fill ? new Color(hue.r, hue.g, hue.b, alpha * 0.92f) : new Color(0.02f, 0.10f, 0.20f, alpha * (valid ? 0.45f : 0.15f));
                for (int q = 0; q < 6; q++) col[v + q] = cc;
            }
        }

        mesh.vertices = vert;
        mesh.colors = col;
        mesh.RecalculateBounds();
    }

    static Vector3 P3(Vector2 p, float y) { return new Vector3(p.x, y, p.y); }
}
