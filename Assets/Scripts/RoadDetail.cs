using UnityEngine;

// The details that dress the street: lane paint on the road, raised paved footpaths with kerbs, and now and then a zebra crossing with a
// blinking traffic signal on each side. Built once per road segment by SegmentSpawner (the segment's root moves, so the paint moves with it).
// Everything is a primitive and carries no collider except the footpath, which the papers land on. The road's top is at y = 0.05.
public static class RoadDetail
{
    public const float RoadTop = 0.05f;
    public const float FootpathTop = 0.17f;       // a real kerb: the footpath stands 12 cm over the road
    public const float CrossingZ = 7f;            // where a crossing sits along its segment

    static Material Lit(Color c)
    {
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var r = tmp.GetComponent<Renderer>();
        Prim.FixMaterial(r);
        var m = new Material(r.sharedMaterial);
        m.color = c;
        Object.Destroy(tmp);
        return m;
    }

    static Transform Block(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = false)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        if (!collider) Object.Destroy(g.GetComponent<Collider>());
        var t = g.transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return t;
    }

    static Texture2D pavingTex;

    // Paving slabs: two by two to a tile, pale grey with a darker grout line and a little variation from slab to slab.
    static Texture2D PavingTex()
    {
        if (pavingTex != null) return pavingTex;
        const int n = 128;
        pavingTex = new Texture2D(n, n, TextureFormat.RGBA32, true);
        pavingTex.wrapMode = TextureWrapMode.Repeat;
        pavingTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        Color baseC = new Color(0.82f, 0.81f, 0.77f), grout = new Color(0.55f, 0.54f, 0.51f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int sx = x / (n / 2), sy = y / (n / 2);                      // which slab
                int lx = x % (n / 2), ly = y % (n / 2);                      // where in the slab
                float shade = 0.94f + 0.09f * (((sx * 7 + sy * 13) % 5) / 4f);
                bool edge = lx < 2 || ly < 2;
                float speck = ((x * 73856093) ^ (y * 19349663)) % 17 == 0 ? 0.96f : 1f;
                Color c = edge ? grout : baseC * shade * speck;
                c.a = 1f;
                px[y * n + x] = c;
            }
        pavingTex.SetPixels(px);
        pavingTex.Apply();
        return pavingTex;
    }

    // ---- footpaths ----

    public static void Footpaths(Transform root, float mid, float len)
    {
        Material body = Lit(new Color(0.66f, 0.65f, 0.62f));
        Material kerb = Lit(new Color(0.84f, 0.83f, 0.80f));
        Material paving = Lit(Color.white);
        paving.mainTexture = PavingTex();
        paving.mainTextureScale = new Vector2(1f, len / 1.4f);   // a tile is 1.4 m square: two slabs across the footpath
        Wetness.Register(body, 0.70f); Wetness.Register(kerb, 0.75f); Wetness.Register(paving, 0.72f);   // darker and glossy in the rain

        for (int side = -1; side <= 1; side += 2)
        {
            float h = FootpathTop + 0.03f;   // sunk a little into the ground
            Block(PrimitiveType.Cube, "Footpath", root, new Vector3(side * 3.7f, FootpathTop - h * 0.5f, mid), new Vector3(1.4f, h, len), body, true);
            Block(PrimitiveType.Cube, "Kerb", root, new Vector3(side * 3.07f, FootpathTop - h * 0.5f + 0.004f, mid), new Vector3(0.14f, h, len), kerb);
            var top = Block(PrimitiveType.Quad, "Paving", root, new Vector3(side * 3.77f, FootpathTop + 0.003f, mid), new Vector3(1.26f, len, 1f), paving);
            top.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    // ---- lane paint ----

    public static void Markings(Transform root, float mid, float len, float roadHalf)
    {
        Material yellow = Lit(new Color(0.95f, 0.78f, 0.12f));
        Material white = Lit(new Color(0.92f, 0.92f, 0.90f));
        float y = RoadTop + 0.005f;
        const float th = 0.01f;
        // double yellow line down the middle
        Block(PrimitiveType.Cube, "CentreLine", root, new Vector3(-0.1f, y, mid), new Vector3(0.09f, th, len), yellow);
        Block(PrimitiveType.Cube, "CentreLine", root, new Vector3(0.1f, y, mid), new Vector3(0.09f, th, len), yellow);
        // solid white at both edges
        for (int side = -1; side <= 1; side += 2)
            Block(PrimitiveType.Cube, "EdgeLine", root, new Vector3(side * (roadHalf - 0.28f), y, mid), new Vector3(0.12f, th, len), white);
        // dashed white lane lines either side of the centre
        int dashes = Mathf.FloorToInt(len / 5f);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < dashes; i++)
                Block(PrimitiveType.Cube, "Dash", root, new Vector3(side * 1.55f, y, (i + 0.5f) * 5f), new Vector3(0.1f, th, 2.2f), white);
    }

    // ---- zebra crossing and its signals ----

    // Returns a holder the spawner switches on for the segments that get a crossing.
    public static GameObject Crossing(Transform root, float roadHalf)
    {
        var holder = new GameObject("Crossing").transform;
        holder.SetParent(root, false);
        Material white = Lit(new Color(0.94f, 0.94f, 0.92f));
        Material metal = Lit(new Color(0.22f, 0.23f, 0.26f));
        Material housing = Lit(new Color(0.10f, 0.10f, 0.12f));

        float y = RoadTop + 0.011f;
        int bars = Mathf.RoundToInt(roadHalf * 2f / 0.9f);
        float w = roadHalf * 2f / bars;
        for (int i = 0; i < bars; i++)
            Block(PrimitiveType.Cube, "Zebra", holder, new Vector3(-roadHalf + w * (i + 0.5f), y, CrossingZ), new Vector3(w * 0.52f, 0.012f, 2.8f), white);
        // stop lines in front of the crossing, in each lane
        Block(PrimitiveType.Cube, "StopLine", holder, new Vector3(-roadHalf * 0.5f, y, CrossingZ - 2.1f), new Vector3(roadHalf - 0.5f, 0.012f, 0.28f), white);
        Block(PrimitiveType.Cube, "StopLine", holder, new Vector3(roadHalf * 0.5f, y, CrossingZ + 2.1f), new Vector3(roadHalf - 0.5f, 0.012f, 0.28f), white);

        // a signal on each side, on the far side of the crossing, its lamps facing the oncoming bike
        for (int side = -1; side <= 1; side += 2)
        {
            float px = side * (roadHalf + 0.25f), pz = CrossingZ + 1.9f;
            Block(PrimitiveType.Cylinder, "SignalPole", holder, new Vector3(px, FootpathTop + 1.5f, pz), new Vector3(0.09f, 1.5f, 0.09f), metal);
            Block(PrimitiveType.Cube, "SignalHead", holder, new Vector3(px, FootpathTop + 2.75f, pz - 0.15f), new Vector3(0.32f, 0.78f, 0.24f), housing);
            Block(PrimitiveType.Cube, "SignalHood", holder, new Vector3(px, FootpathTop + 3.17f, pz - 0.2f), new Vector3(0.36f, 0.04f, 0.34f), housing);

            var head = holder.gameObject.AddComponent<SignalHead>();
            head.amber = Lamp(holder, new Vector3(px, FootpathTop + 2.93f, pz - 0.29f));
            head.green = Lamp(holder, new Vector3(px, FootpathTop + 2.57f, pz - 0.29f));
        }
        holder.gameObject.SetActive(false);
        return holder.gameObject;
    }

    static Material LampMat()
    {
        var m = Mat.Particle();   // unlit, so the lamps stay bright after dark
        return m;
    }

    static Renderer Lamp(Transform parent, Vector3 pos)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = "SignalLamp";
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = new Vector3(0.2f, 0.2f, 0.12f);
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = LampMat();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    // Deterministic: the same stretch of road always gets the same crossings (nothing is drawn from the route's random numbers).
    public static bool WantsCrossing(float startZ)
    {
        if (startZ < 150f) return false;
        float h = Mathf.Repeat(Mathf.Sin(startZ * 0.1731f + 3.7f) * 43758.5453f, 1f);
        return h < 0.30f;
    }
}

// Blinks one signal's two lamps: steady green for a while, then a flashing amber, over and over. The same clock for every signal.
public class SignalHead : MonoBehaviour
{
    public Renderer amber, green;
    static readonly Color AmberOn = new Color(1f, 0.74f, 0.10f), AmberOff = new Color(0.30f, 0.22f, 0.05f);
    static readonly Color GreenOn = new Color(0.25f, 1f, 0.45f), GreenOff = new Color(0.05f, 0.22f, 0.10f);

    void Update()
    {
        float t = Mathf.Repeat(Time.time, 8f);
        bool greenPhase = t < 4f;
        bool amberOn = !greenPhase && Mathf.Repeat(Time.time * 2f, 1f) < 0.5f;
        if (green != null) green.sharedMaterial.color = greenPhase ? GreenOn : GreenOff;
        if (amber != null) amber.sharedMaterial.color = amberOn ? AmberOn : AmberOff;
    }
}
