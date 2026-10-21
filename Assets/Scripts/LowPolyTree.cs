using System.Collections.Generic;
using UnityEngine;

// A low-poly tree: a faceted crown (round, wide, tall or cone shaped; green, olive, pink, orange, purple, blue or autumn red) on a thin, flared,
// slightly crooked trunk with forks and roots, swaying a little in the wind. Built from meshes made here, shared by all trees.
// For gameplay the trunk is solid (a paper bounces off it) and the crown is a trigger the shape of the leaves: a paper that flies into it is held by
// the leaves and sinks through (see Paper.PushThroughFoliage). Used for the trees on lawns, the trees that block throws, and the crowns of the back rows.
public class LowPolyTree
{
    public const int ShapeCount = 6;

    // crown shapes: radius x, y, z (metres) and the height of the crown's centre; the last two (cone, slim cone) taper to a point
    public static readonly float[,] CrownShapes =
    {
        { 1.35f, 1.25f, 1.35f, 3.50f },   // 0 round
        { 1.45f, 1.15f, 1.45f, 3.40f },   // 1 round, lumpier
        { 1.75f, 1.00f, 1.75f, 3.30f },   // 2 wide
        { 1.10f, 1.70f, 1.10f, 3.70f },   // 3 tall oval
        { 1.30f, 2.00f, 1.30f, 3.80f },   // 4 cone
        { 0.95f, 2.30f, 0.95f, 4.00f },   // 5 slim cone
    };
    public static readonly float[] CrownShapeOdds = { 0.25f, 0.45f, 0.57f, 0.75f, 0.90f, 1.00f };   // cumulative

    // crown colours and how likely each is (cumulative)
    public static readonly Color[] CrownColors =
    {
        new Color(0.28f, 0.70f, 0.22f),   // green
        new Color(0.20f, 0.52f, 0.24f),   // dark green
        new Color(0.62f, 0.72f, 0.22f),   // olive
        new Color(0.96f, 0.56f, 0.72f),   // pink
        new Color(0.98f, 0.54f, 0.14f),   // orange
        new Color(0.58f, 0.34f, 0.84f),   // purple
        new Color(0.72f, 0.30f, 0.18f),   // autumn red
        new Color(0.24f, 0.50f, 0.95f),   // blue
        new Color(0.44f, 0.18f, 0.50f),   // dark purple
    };
    public static readonly float[] CrownColorOdds = { 0.22f, 0.40f, 0.50f, 0.62f, 0.74f, 0.82f, 0.88f, 0.94f, 1.00f };
    static readonly Color[] TrunkColors =
    {
        new Color(0.40f, 0.28f, 0.18f), new Color(0.52f, 0.52f, 0.54f), new Color(0.82f, 0.52f, 0.24f), new Color(0.26f, 0.20f, 0.20f),
    };

    static Mesh[] crownMeshes;
    static Mesh[] crownHulls;   // a coarser copy of each crown (80 facets) used for the leaf trigger
    static Texture2D crownTone;

    // ---- the mesh ----

    static Texture2D CrownTone()
    {
        if (crownTone != null) return crownTone;
        const int n = 32;
        crownTone = new Texture2D(n, 1, TextureFormat.RGBA32, false);
        crownTone.filterMode = FilterMode.Point;
        crownTone.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < n; i++)
        {
            float v = 0.60f + 0.40f * (i / (float)(n - 1));   // darker and lighter facets
            crownTone.SetPixel(i, 0, new Color(v, v, v, 1f));
        }
        crownTone.Apply();
        return crownTone;
    }

    public static Mesh CrownMesh(int variant, bool hull = false)
    {
        if (crownMeshes == null) { crownMeshes = new Mesh[CrownShapes.GetLength(0)]; crownHulls = new Mesh[CrownShapes.GetLength(0)]; }
        var cache = hull ? crownHulls : crownMeshes;
        if (cache[variant] != null) return cache[variant];
        int rounds = hull ? 1 : 2;

        var rnd = new System.Random(4100 + variant * 131);
        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        var verts = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        int[] ico = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };

        // two rounds of subdivision: 20 faces become 80, then 320 (many small facets: leafy)
        var tris = new List<int>(ico);
        for (int round = 0; round < rounds; round++)
        {
            var next = new List<int>();
            var mid = new Dictionary<long, int>();
            System.Func<int, int, int> middle = (a, b) =>
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                int m;
                if (mid.TryGetValue(key, out m)) return m;
                verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                m = verts.Count - 1;
                mid[key] = m;
                return m;
            };
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = middle(a, b), bc = middle(b, c), ca = middle(c, a);
                next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris = next;
        }

        // shape and roughen the vertices (shared, so the surface stays closed)
        bool cone = variant >= 4;
        var p = new Vector3[verts.Count];
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];
            // a lumpy outline (big soft bumps) plus a little roughness on every vertex
            float lump = Mathf.Sin(v.x * 3.1f + variant) * Mathf.Sin(v.y * 2.7f + 0.4f * variant) * Mathf.Sin(v.z * 3.4f + 2.1f);
            float rough = (1f + 0.20f * lump) * (0.90f + 0.20f * (float)rnd.NextDouble());
            float h = (v.y + 1f) * 0.5f;
            if (cone)
            {
                float taper = Mathf.Lerp(1.15f, 0.08f, Mathf.Pow(h, variant == 5 ? 0.8f : 1.0f));
                v = new Vector3(v.x * taper, v.y, v.z * taper);
            }
            p[i] = v * rough;
            if (cone) p[i].y = v.y * (0.9f + 0.2f * (float)rnd.NextDouble());
        }

        // flat facets: every triangle gets its own three vertices, one normal and one tone
        var mv = new List<Vector3>(tris.Count);
        var mn = new List<Vector3>(tris.Count);
        var muv = new List<Vector2>(tris.Count);
        var mt = new List<int>(tris.Count);
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = p[tris[i]], b = p[tris[i + 1]], c = p[tris[i + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(n, (a + b + c)) < 0f) { Vector3 tmp = b; b = c; c = tmp; n = -n; }   // wound outwards
            float tone = Mathf.Clamp01(0.52f + 0.30f * n.y + (float)(rnd.NextDouble() - 0.5) * 0.55f);   // lighter on top, darker underneath, each facet a little different
            var uv = new Vector2((Mathf.Floor(tone * 31f) + 0.5f) / 32f, 0.5f);
            int baseI = mv.Count;
            mv.Add(a); mv.Add(b); mv.Add(c);
            mn.Add(n); mn.Add(n); mn.Add(n);
            muv.Add(uv); muv.Add(uv); muv.Add(uv);
            mt.Add(baseI); mt.Add(baseI + 1); mt.Add(baseI + 2);
        }
        var mesh = new Mesh { name = "TreeCrown" + variant };
        mesh.SetVertices(mv);
        mesh.SetNormals(mn);
        mesh.SetUVs(0, muv);
        mesh.SetTriangles(mt, 0);
        mesh.RecalculateBounds();
        cache[variant] = mesh;
        return mesh;
    }

    // ---- trunk and limbs (low-poly, flat facets, built once and shared) ----

    static Mesh trunkMesh, limbMesh;

    // A faceted tapered tube along +y (length 1): flared at the foot, narrowing, a little crooked, every side a different tone.
    // flare: how much wider the foot is; sides: facets round it; rings: rings along it; thick: radius at the foot.
    static Mesh LimbMesh(string name, int sides, int rings, float radius, float topShare, float flare, float wobble, int seed)
    {
        var rnd = new System.Random(seed);
        var ring = new Vector3[rings + 1, sides];
        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;
            float rad = radius * Mathf.Lerp(1f, topShare, t) * (1f + flare * Mathf.Exp(-9f * t));
            float ox = wobble * Mathf.Sin(t * 5.1f + seed), oz = wobble * Mathf.Sin(t * 4.3f + 2f + seed);
            float twist = t * 0.45f;
            for (int k = 0; k < sides; k++)
            {
                float a = twist + k * Mathf.PI * 2f / sides;
                float jit = 0.88f + 0.24f * (float)rnd.NextDouble();
                ring[r, k] = new Vector3(ox + Mathf.Cos(a) * rad * jit, t, oz + Mathf.Sin(a) * rad * jit);
            }
        }
        var mv = new List<Vector3>(); var mn = new List<Vector3>(); var muv = new List<Vector2>(); var mt = new List<int>();
        System.Action<Vector3, Vector3, Vector3, float, bool> face = (a, b, c, tone, cap) =>
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            bool flip = cap ? n.y < 0f : Vector3.Dot(n, (a + b + c) / 3f - new Vector3(0f, (a.y + b.y + c.y) / 3f, 0f)) < 0f;   // wound outwards (a lid: upwards)
            if (flip) { var tmp = b; b = c; c = tmp; n = -n; }
            var uv = new Vector2((Mathf.Floor(Mathf.Clamp01(tone) * 31f) + 0.5f) / 32f, 0.5f);
            int bi = mv.Count;
            mv.Add(a); mv.Add(b); mv.Add(c);
            mn.Add(n); mn.Add(n); mn.Add(n);
            muv.Add(uv); muv.Add(uv); muv.Add(uv);
            mt.Add(bi); mt.Add(bi + 1); mt.Add(bi + 2);
        };
        for (int r = 0; r < rings; r++)
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                float band = (r & 1) == 0 ? 0.0f : 0.12f;   // darker and lighter bands of bark
                float tone = 0.30f + band + 0.45f * (float)rnd.NextDouble();
                face(ring[r, k], ring[r + 1, k], ring[r + 1, k2], tone, false);
                face(ring[r, k], ring[r + 1, k2], ring[r, k2], tone + 0.1f * ((float)rnd.NextDouble() - 0.5f), false);
            }
        // the top cap (a flat lid, mostly hidden in the leaves)
        Vector3 topC = new Vector3(wobble * Mathf.Sin(5.1f + seed), 1f, wobble * Mathf.Sin(4.3f + 2f + seed));
        for (int k = 0; k < sides; k++) face(ring[rings, k], topC, ring[rings, (k + 1) % sides], 0.55f, true);
        var mesh = new Mesh { name = name };
        mesh.SetVertices(mv); mesh.SetNormals(mn); mesh.SetUVs(0, muv); mesh.SetTriangles(mt, 0);
        mesh.RecalculateBounds();
        return mesh;
    }


    static float Hash01(float x) { return Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f); }

    public static float Pick(float h, float[] cumulative)
    {
        for (int i = 0; i < cumulative.Length; i++) if (h <= cumulative[i]) return i;
        return cumulative.Length - 1;
    }

    // ---- the cheap copy for the back rows (BackRow): coarse crown (80 facets), a 5-sided trunk, shared meshes and materials, no collider, no sway ----

    // back-row crown colours, picked at random from this list (no orange: that colour belongs to the delivery markers)
    public static readonly int[] BackColourPool = { 0, 0, 0, 1, 1, 1, 2, 2, 3, 5, 6, 7, 8 };
    static Mesh backTrunkMesh;
    static Material[] backCrownMats, backTrunkMats;

    // the background is a little duller than the trees on the lawns: less colour, a touch darker (so the lawns and the delivery markers stand out)
    public const float BackDullness = 0.45f, BackDim = 0.86f;
    static Color Dull(Color c)
    {
        float g = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        var grey = new Color(g, g, g);
        Color d = Color.Lerp(c, grey, BackDullness);
        return new Color(d.r * BackDim, d.g * BackDim, d.b * BackDim, 1f);
    }

    public static Mesh BackCrownMesh(int shape) { return CrownMesh(Mathf.Clamp(shape, 0, ShapeCount - 1), true); }

    public static Mesh BackTrunkMesh()
    {
        if (backTrunkMesh == null) backTrunkMesh = LimbMesh("BackTrunk", 5, 3, 0.17f, 0.62f, 0.9f, 0.04f, 11);
        return backTrunkMesh;
    }

    public static Material BackCrownMat(int colour)
    {
        if (backCrownMats == null) backCrownMats = new Material[CrownColors.Length];
        colour = Mathf.Clamp(colour, 0, CrownColors.Length - 1);
        if (backCrownMats[colour] == null) { backCrownMats[colour] = NewMaterial(); backCrownMats[colour].color = Dull(CrownColors[colour]); }
        return backCrownMats[colour];
    }

    public static Material BackTrunkMat(int i)
    {
        if (backTrunkMats == null) backTrunkMats = new Material[2];
        i = Mathf.Clamp(i, 0, 1);
        if (backTrunkMats[i] == null) { backTrunkMats[i] = NewMaterial(); backTrunkMats[i].color = Dull(TrunkColors[i == 0 ? 0 : 3]); }
        return backTrunkMats[i];
    }

    // ---- materials ----

    static Material baseMat;

    // A new lit material with the facet tones (tint it with .color).
    public static Material NewMaterial()
    {
        if (baseMat == null)
        {
            var tmp = Prim.Make(PrimitiveType.Cube, "TreeMatSource", null, Vector3.zero, Vector3.one, Color.white);
            baseMat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
            Object.Destroy(tmp);
        }
        var m = new Material(baseMat);
        m.mainTexture = CrownTone();
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    // ---- one tree ----

    Transform root, crownVis, trunkVis;
    Transform[] limbs;                      // 3 forks, then 4 roots
    Material crownMat, trunkMat;
    CapsuleCollider trunkCol;
    readonly Transform[] foliageT = new Transform[ShapeCount];       // per crown shape, built when first used
    readonly TreeFoliage[] foliage = new TreeFoliage[ShapeCount];
    TreeSway sway;
    Vector3 heartSpot;

    public bool Active { get { return root != null && root.gameObject.activeSelf; } }
    public Vector3 HeartSpot { get { return heartSpot; } }   // just above the leaves, in the parent's space

    public static LowPolyTree Create(Transform parent, string name)
    {
        var t = new LowPolyTree();
        t.root = new GameObject(name).transform;
        t.root.SetParent(parent, false);
        t.sway = t.root.gameObject.AddComponent<TreeSway>();

        var crownGo = new GameObject("Crown");
        t.crownVis = crownGo.transform;
        t.crownVis.SetParent(t.root, false);
        crownGo.AddComponent<MeshFilter>();
        var mr = crownGo.AddComponent<MeshRenderer>();
        t.crownMat = NewMaterial();
        mr.sharedMaterial = t.crownMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        t.trunkMat = NewMaterial();
        if (trunkMesh == null) trunkMesh = LimbMesh("TreeTrunk", 7, 9, 0.17f, 0.62f, 1.1f, 0.05f, 11);
        if (limbMesh == null) limbMesh = LimbMesh("TreeLimb", 5, 3, 0.5f, 0.35f, 0.15f, 0.04f, 23);
        t.trunkVis = t.MakeBark("Trunk", trunkMesh);
        t.limbs = new Transform[7];
        for (int i = 0; i < t.limbs.Length; i++) t.limbs[i] = t.MakeBark(i < 3 ? "Fork" : "Root", limbMesh);

        var colGo = new GameObject("TrunkCollider");
        colGo.transform.SetParent(t.root, false);
        t.trunkCol = colGo.AddComponent<CapsuleCollider>();
        t.trunkCol.direction = 1;
        return t;
    }

    Transform MakeBark(string name, Mesh mesh)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = trunkMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    public void Hide() { if (root != null) root.gameObject.SetActive(false); }

    // Puts the tree on the ground at pos (in the parent's space). key makes every tree different (shape, colour, size, height, lean).
    public void Place(Vector3 pos, float key)
    {
        int shape = (int)Pick(Hash01(key * 0.613f + 1.7f), CrownShapeOdds);
        int colour = (int)Pick(Hash01(key * 1.171f + 5.9f), CrownColorOdds);
        float size = 0.90f + 0.25f * Hash01(key * 1.91f + 4.1f);
        float yaw = Hash01(key * 3.17f + 0.9f) * 360f;
        Color trunkCol0 = TrunkColors[(int)(Hash01(key * 2.71f + 6.1f) * TrunkColors.Length) % TrunkColors.Length];

        // height: every tree reaches the second floor (GameConfig.TreeMinTop), heights spread evenly up to TreeMaxTop (the third floor)
        float ry = CrownShapes[shape, 1], cy = CrownShapes[shape, 3];
        float top = Mathf.Lerp(GameConfig.TreeMinTop, GameConfig.TreeMaxTop, Hash01(key * 0.771f + 3.1f)) + GameConfig.TreeSink;   // (measured from the buried foot, so the part above ground keeps its height)
        float grow = 1f, cs = size;
        for (int i = 0; i < 4; i++)
        {
            cs = size * (0.8f + 0.2f * grow);                 // the crown widens a little as the tree grows
            grow = Mathf.Max(0.6f, (top - ry * cs) / cy);
        }
        cs = size * (0.8f + 0.2f * grow);
        float crownY = cy * grow;
        float crownBottom = crownY - ry * cs;
        float trunkTop = crownBottom + ry * cs * 0.9f;        // the trunk reaches well into the leaves
        float thick = (0.75f + 0.35f * grow) * GameConfig.TreeGirth;   // a sturdier trunk and foot, in scale with the buildings

        root.localPosition = pos + new Vector3(0f, -GameConfig.TreeSink, 0f);   // the foot sits a little below the grass, so it grows out of the ground
        root.gameObject.SetActive(true);
        sway.phase = Hash01(key * 2.37f + 3.3f) * 6.28f;

        crownVis.GetComponent<MeshFilter>().sharedMesh = CrownMesh(shape);
        crownVis.localPosition = new Vector3(0f, crownY, 0f);
        crownVis.localRotation = Quaternion.Euler(0f, yaw, 0f);
        // the leaves spread wider (the height stays: it is set by TreeMinTop/TreeMaxTop), but never past TreeMaxRadius so they keep off the wall
        float fw = Mathf.Min(GameConfig.TreeWidth, GameConfig.TreeMaxRadius / (Mathf.Max(CrownShapes[shape, 0], CrownShapes[shape, 2]) * cs));
        fw = Mathf.Max(1f, fw);
        crownVis.localScale = new Vector3(CrownShapes[shape, 0] * fw, ry, CrownShapes[shape, 2] * fw) * cs;
        crownMat.color = CrownColors[colour];
        heartSpot = root.localPosition + new Vector3(0f, crownY + ry * cs + 0.4f, 0f);

        trunkMat.color = trunkCol0;
        trunkVis.localPosition = Vector3.zero;
        trunkVis.localRotation = Quaternion.Euler(0f, yaw, 0f);
        trunkVis.localScale = new Vector3(thick, trunkTop, thick);
        trunkCol.radius = 0.19f * thick;
        trunkCol.height = Mathf.Max(trunkTop, 2f * trunkCol.radius + 0.01f);
        trunkCol.center = new Vector3(0f, trunkTop * 0.5f, 0f);

        // forks: thin branches leaning out of the trunk into the crown (round and oval trees)
        bool forks = shape <= 3;
        for (int i = 0; i < 3; i++)
        {
            var f = limbs[i];
            f.gameObject.SetActive(forks);
            if (!forks) continue;
            float a = yaw * Mathf.Deg2Rad + i * 2.1f, dx = Mathf.Cos(a), dz = Mathf.Sin(a);
            float lean = 30f + 14f * Hash01(key * 1.3f + i * 4.7f);
            float len = (0.8f + 0.5f * Hash01(key * 0.9f + i * 2.3f)) * Mathf.Sqrt(grow) * Mathf.Sqrt(GameConfig.TreeGirth);
            f.localPosition = new Vector3(dx * 0.1f, trunkTop - ry * cs * (0.8f - 0.2f * i), dz * 0.1f);
            f.localRotation = Quaternion.Euler(dz * lean, 0f, -dx * lean);
            f.localScale = new Vector3(0.20f * thick, len, 0.20f * thick);
        }
        // no roots: the splayed root limbs popped out of the grass. The trunk goes straight into the ground.
        for (int i = 3; i < 7; i++) limbs[i].gameObject.SetActive(false);

        // the trigger that holds a paper in the leaves follows the crown's shape
        for (int i = 0; i < ShapeCount; i++)
            if (foliageT[i] != null) foliageT[i].gameObject.SetActive(i == shape);
        if (foliageT[shape] == null)
        {
            var fg = new GameObject("Foliage");
            fg.transform.SetParent(crownVis, false);
            var mc = fg.AddComponent<MeshCollider>();
            mc.sharedMesh = CrownMesh(shape, true);
            mc.convex = true;
            mc.isTrigger = true;
            foliage[shape] = fg.AddComponent<TreeFoliage>();
            foliageT[shape] = fg.transform;
        }
        foliage[shape].leaf = CrownColors[colour];
        foliage[shape].sway = sway;
    }
}

// A gentle sway for a tree: the whole tree leans a degree or so back and forth around its foot. A paper in the leaves shakes it harder for a moment.
public class TreeSway : MonoBehaviour
{
    public float phase;
    float kick;

    public void Kick(float strength) { kick = Mathf.Max(kick, strength); }

    void Update()
    {
        float t = Time.time;
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 0.9f);
        float shake = kick * Mathf.Sin(t * 16f) * 4f;   // a quick tremble that dies away
        transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f + phase) * 1.0f + shake * 0.6f, 0f, Mathf.Sin(t * 0.7f + phase * 1.3f) * 1.3f + shake);
    }
}

// The leaves of a tree (a trigger the shape of the crown): a paper that flies in is held by them (see Paper.PushThroughFoliage).
public class TreeFoliage : MonoBehaviour
{
    public Color leaf = Color.green;
    public TreeSway sway;
    float lastRustle = -1f;

    public void Rustle(Vector3 pos, float speed)
    {
        if (Time.time - lastRustle < 0.2f) return;
        lastRustle = Time.time;
        if (sway != null) sway.Kick(Mathf.Clamp(speed / 12f, 0.3f, 1.2f));
        Juice.Rustle(pos, leaf, speed);
    }
}
