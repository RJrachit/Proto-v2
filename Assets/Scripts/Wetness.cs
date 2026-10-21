using System.Collections.Generic;
using UnityEngine;

// What rain does to the street, kept very cheap:
//   - the road, kerbs and paving darken and get a little glossy (a handful of material colours, touched only while the rain is changing)
//   - a few puddles lie on the road, pale like the sky, with a ripple that spreads and fades in some of them
// Nothing here runs when it is dry, and the puddles of segments far ahead or behind are not drawn.
public static class Wetness
{
    public static float Amount;   // 0 dry .. 1 pouring, eased so the street dries and wets gradually

    class Entry { public Material m; public Color baseColor; public float baseSmooth; public float darken; }
    static readonly List<Entry> entries = new List<Entry>();
    static float applied = -1f;
    static readonly int SmoothId = Shader.PropertyToID("_Smoothness");

    // darken: the colour multiplier when fully wet (0.7 = 30% darker)
    public static void Register(Material m, float darken = 0.72f)
    {
        if (m == null) return;
        entries.Add(new Entry { m = m, baseColor = m.color, baseSmooth = m.HasProperty(SmoothId) ? m.GetFloat(SmoothId) : -1f, darken = darken });
        applied = -1f;
    }

    // Called every frame by the spawner; does real work only while the amount of rain is moving.
    public static void Tick(float dt)
    {
        Amount = Mathf.MoveTowards(Amount, DayCycle.RainAmount, dt * 0.25f);
        if (Mathf.Abs(Amount - applied) < 0.01f) return;
        applied = Amount;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var e = entries[i];
            if (e.m == null) { entries.RemoveAt(i); continue; }
            Color c = e.baseColor * Mathf.Lerp(1f, e.darken, Amount);
            c.a = e.baseColor.a;
            e.m.color = c;
            if (e.baseSmooth >= 0f) e.m.SetFloat(SmoothId, Mathf.Lerp(e.baseSmooth, 0.55f, Amount));
        }
    }
}

// The puddles of one road segment (built once, re-laid every time the segment is reused).
public class Puddles : MonoBehaviour
{
    const int Count = 4;
    const int Rippling = 2;       // the first ones also show a spreading ripple

    Transform content;
    readonly Transform[] pud = new Transform[Count];
    readonly Transform[] rip = new Transform[Rippling];
    readonly Material[] ripMat = new Material[Rippling];
    readonly Vector2[] size = new Vector2[Count];
    readonly float[] ripPhase = new float[Rippling];
    readonly Material[] puddleMats = new Material[3];
    readonly Material[] pudMatOf = new Material[Count];
    Material gutterMat;
    readonly Transform[] gutter = new Transform[2];
    float roadHalf, length;
    bool on;

    static Texture2D[] puddleTex;
    static Texture2D ringTex, gutterTex;

    // Soft-edged oval puddles (three slightly different glints); the feathered edge blends into the road.
    static Texture2D[] PuddleTexes()
    {
        if (puddleTex != null && puddleTex[0] != null) return puddleTex;
        puddleTex = new Texture2D[3];
        for (int k = 0; k < 3; k++)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float p1 = H(k * 1.7f + 0.3f) * 6.28f, p2 = H(k * 2.9f + 1.1f) * 6.28f, p3 = H(k * 4.1f + 2.3f) * 6.28f, p4 = H(k * 5.3f + 3.7f) * 6.28f;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v), th = Mathf.Atan2(v, u);
                    // a plain oval whose edge is feathered over most of its radius, so it melts into the asphalt with no visible rim
                    float f = 1f - Mathf.SmoothStep(0.10f, 0.98f, d);
                    float a = f * f;
                    float glint = Mathf.Clamp01(1f - Mathf.Abs(u * 0.8f + v * 0.5f + 0.1f) * 3f) * 0.35f * Mathf.Clamp01(1f - d);   // a pale streak of sky across the water
                    float c = Mathf.Clamp01(0.84f + glint + 0.08f * k);
                    px[y * n + x] = new Color(c, c, c, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            puddleTex[k] = tex;
        }
        return puddleTex;
    }

    // A strip of water along the kerb: soft on both long edges.
    static Texture2D GutterTex()
    {
        if (gutterTex != null) return gutterTex;
        const int w = 32, h = 4;
        gutterTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        gutterTex.wrapMode = TextureWrapMode.Clamp;
        gutterTex.filterMode = FilterMode.Bilinear;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                px[y * w + x] = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.45f, 1f, u));
            }
        gutterTex.SetPixels(px);
        gutterTex.Apply();
        return gutterTex;
    }

    public static Texture2D RingTex()
    {
        if (ringTex != null) return ringTex;
        const int n = 64;
        ringTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        ringTex.wrapMode = TextureWrapMode.Clamp;
        ringTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                float k = (d - 0.78f) / 0.09f;
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Exp(-k * k) * (d < 1f ? 1f : 0f));
            }
        ringTex.SetPixels(px);
        ringTex.Apply();
        return ringTex;
    }

    public static Puddles Create(Transform root, float length, float roadHalf)
    {
        if (!BlobShadow.Available) return null;
        var go = new GameObject("Puddles");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<Puddles>();
        p.Build(length, roadHalf);
        return p;
    }

    void Build(float len, float half)
    {
        length = len; roadHalf = half;
        content = new GameObject("PuddleContent").transform;
        content.SetParent(transform, false);
        var texes = PuddleTexes();
        for (int k = 0; k < 3; k++) { puddleMats[k] = Mat.Particle(); puddleMats[k].mainTexture = texes[k]; }
        for (int i = 0; i < Count; i++)
        {
            var q = BlobShadow.Create(content);
            q.name = "Puddle";
            pudMatOf[i] = puddleMats[i % 3];
            q.GetComponent<Renderer>().sharedMaterial = pudMatOf[i];
            pud[i] = q.transform;
        }
        // water running along the kerb on both sides
        gutterMat = Mat.Particle();
        gutterMat.mainTexture = GutterTex();
        for (int i = 0; i < 2; i++)
        {
            var q = BlobShadow.Create(content);
            q.name = "Gutter";
            q.GetComponent<Renderer>().sharedMaterial = gutterMat;
            float side = i == 0 ? -1f : 1f;
            q.transform.localPosition = new Vector3(side * (half - 0.2f), 0.064f, len * 0.5f);
            q.transform.localScale = new Vector3(0.46f, len, 1f);
            gutter[i] = q.transform;
        }
        for (int i = 0; i < Rippling; i++)
        {
            var q = BlobShadow.Create(content);
            q.name = "Ripple";
            ripMat[i] = Mat.Particle();
            ripMat[i].mainTexture = RingTex();
            q.GetComponent<Renderer>().sharedMaterial = ripMat[i];
            rip[i] = q.transform;
        }
        content.gameObject.SetActive(false);
    }

    static float H(float x) { return Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f); }

    // Where the puddles lie for the stretch of road starting at startZ (always the same for the same stretch).
    public void Layout(float startZ, bool crossing)
    {
        if (content == null) return;
        int placed = 0;
        for (int i = 0; i < Count * 3 && placed < Count; i++)
        {
            float z = 1.5f + H(startZ * 0.31f + i * 7.13f) * (length - 3f);
            if (crossing && z > 3.5f && z < 10.5f) continue;                  // keep the zebra crossing clear
            float x = (H(startZ * 0.57f + i * 3.71f) * 2f - 1f) * (roadHalf - 0.9f);
            float w = 1.4f + H(startZ * 0.83f + i * 5.37f) * 1.5f;
            float l = w * (0.50f + H(startZ * 0.41f + i * 2.29f) * 0.25f);   // an oval, longer across the road than along it
            pud[placed].localPosition = new Vector3(x, 0.066f, z);
            pud[placed].localRotation = Quaternion.Euler(90f, (H(startZ + i) - 0.5f) * 50f, 0f);   // a little turned
            pud[placed].localScale = new Vector3(w, l, 1f);
            size[placed] = new Vector2(w, l);
            if (placed < Rippling)
            {
                rip[placed].localPosition = new Vector3(x, 0.068f, z);
                rip[placed].localRotation = pud[placed].localRotation;
                ripPhase[placed] = H(startZ * 1.3f + i * 9.1f);
            }
            placed++;
        }
    }

    void Update()
    {
        float wet = Wetness.Amount;
        var bike = BikeForward.Instance;
        bool want = wet > 0.05f && bike != null;
        if (want)
        {
            float dz = transform.parent.position.z - bike.transform.position.z;
            want = dz > -45f && dz < 85f;   // only the stretches near the camera
        }
        if (want != on) { on = want; content.gameObject.SetActive(on); }
        if (!on) return;

        Color sky = Color.Lerp(DayCycle.SkyHorizon, Color.white, 0.30f);
        sky.a = 0.28f * Mathf.Clamp01(wet * 1.3f);   // subtle: a faint sheen, not a patch
        for (int k = 0; k < 3; k++) puddleMats[k].color = sky;
        Color gc = sky; gc.a *= 0.45f;
        gutterMat.color = gc;
        for (int i = 0; i < Rippling; i++)
        {
            float t = Mathf.Repeat(Time.time * 0.8f + ripPhase[i], 1f);
            float s = 0.15f + 0.85f * t;
            rip[i].localScale = new Vector3(size[i].x * s, size[i].y * s, 1f);
            Color c = Color.white;
            c.a = (1f - t) * 0.30f * Mathf.Clamp01(wet * 1.3f);
            ripMat[i].color = c;
        }
    }
}

// Rain falling on things: little rings flash where drops hit the ground (grass, lawns, pavement). One small particle system that follows
// the bike, and nothing at all when it is dry.
public class RainSurface : MonoBehaviour
{
    ParticleSystem splash;
    float lastWet = -1f;

    static ParticleSystem Make(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // it auto-plays; stop before configuring
        return ps;
    }

    void Start()
    {
        // splashes on the ground: flat rings that spread and fade
        splash = Make("Splashes", transform);
        var m = splash.main;
        m.loop = true; m.playOnAwake = false;
        m.startLifetime = 0.45f; m.startSpeed = 0f; m.startSize = 0.55f;
        m.startColor = new Color(0.85f, 0.92f, 1f, 0.7f);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 140;
        var sh = splash.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(26f, 0.01f, 56f);
        var em = splash.emission; em.rateOverTime = 0f;
        var sz = splash.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.2f, 1f, 1f));
        var col = splash.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = splash.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        var mat = Mat.Particle();
        mat.mainTexture = Puddles.RingTex();
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        splash.Play();
    }

    void LateUpdate()
    {
        var bike = BikeForward.Instance;
        if (bike == null || splash == null) return;
        float wet = Wetness.Amount;
        // the systems ride along ahead of the bike
        transform.position = new Vector3(0f, 0.09f, bike.transform.position.z + 22f);
        if (Mathf.Abs(wet - lastWet) < 0.01f) return;
        lastWet = wet;
        float k = Mathf.Clamp01((wet - 0.05f) / 0.95f);
        var e = splash.emission; e.rateOverTime = 95f * k;
    }
}
