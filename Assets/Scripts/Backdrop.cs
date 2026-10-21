using UnityEngine;

// The world beyond the road: a gradient sky that follows the day cycle, a sun, drifting clouds, a ring of far hills,
// and a huge ground plane so there is never a void beside the grass. Everything follows the camera, so it stays at the horizon.
// Sky, sun and clouds use an unlit material; hills and ground are lit like the rest of the scene.
public class Backdrop : MonoBehaviour
{
    const float SkyDist = 390f;
    static readonly float[] Edges = { -40f, 12f, 30f, 55f, 90f, 140f, 220f, 340f, 2600f };   // world heights of the sky bands (the top one is tall: a portrait screen looks far up)

    Camera cam;
    Transform bike;
    Transform rig;
    Transform ground;
    Renderer groundR;
    int groundVersion;
    bool cloudsShown = true, hillsShown = true;
    Material[] skyMats;
    Transform sun;
    Vector3 sunBase;
    Transform[] starT;
    Vector3[] starBase;
    float[] cloudBaseX, cloudK;
    Transform[] hillT;
    float[] hillBaseX;
    Material sunMat, haloMat, craterMat;
    Transform sunHalo;
    Transform[] craters;
    Material starMat;
    Transform stars;
    Material cloudMat;
    Transform[] clouds;
    float[] cloudSpeed;
    Renderer[] hillR;
    Color[] hillBase;

    public void Init(Camera camera, Transform bikeTransform)
    {
        cam = camera;
        bike = bikeTransform;
        cam.farClipPlane = 600f;

        rig = new GameObject("BackdropRig").transform;
        rig.SetParent(transform, false);

        ground = Prim.Make(PrimitiveType.Cube, "FarGround", transform, new Vector3(0f, -0.6f, 0f),
                           new Vector3(1000f, 1f, 900f), GameConfig.FarGroundColor).transform;
        groundR = ground.GetComponent<Renderer>();

        // sky bands, darker blue towards the top
        skyMats = new Material[Edges.Length - 1];
        for (int i = 0; i < skyMats.Length; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "SkyBand";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(rig, false);
            float y0 = Edges[i], y1 = Edges[i + 1];
            q.transform.localPosition = new Vector3(0f, (y0 + y1) * 0.5f, SkyDist);
            q.transform.localScale = new Vector3(1800f, y1 - y0, 1f);
            skyMats[i] = Mat.Particle();
            Setup(q.GetComponent<Renderer>(), skyMats[i]);
        }

        // sun
        sunMat = Mat.Particle();
        var sunGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sunGo.name = "Sun";
        Destroy(sunGo.GetComponent<Collider>());
        sunGo.transform.SetParent(rig, false);
        sunBase = new Vector3(-110f, 130f, 330f);   // up in the sky, left of the middle (a portrait screen shows a lot of sky above the horizon)
        sunGo.transform.localPosition = sunBase;
        sunGo.transform.localScale = Vector3.one * 34f;
        Setup(sunGo.GetComponent<Renderer>(), sunMat);
        sun = sunGo.transform;

        // a big soft halo behind the sun (the light around it), and the moon's craters in front of it (shown only at night)
        haloMat = Mat.Particle();
        haloMat.mainTexture = BlobShadow.GlowTex();
        var haloGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        haloGo.name = "SunHalo";
        Destroy(haloGo.GetComponent<Collider>());
        haloGo.transform.SetParent(sun, false);
        haloGo.transform.localPosition = new Vector3(0f, 0f, 0.6f);
        haloGo.transform.localScale = Vector3.one * GameConfig.SunHaloSize;
        Setup(haloGo.GetComponent<Renderer>(), haloMat);
        sunHalo = haloGo.transform;

        craterMat = Mat.Particle();
        craterMat.mainTexture = CraterTex();
        float[,] cr = { { -0.20f, 0.14f, 0.20f }, { 0.12f, 0.22f, 0.13f }, { 0.18f, -0.06f, 0.24f }, { -0.08f, -0.20f, 0.16f }, { -0.27f, -0.08f, 0.10f }, { 0.04f, 0.02f, 0.09f } };
        craters = new Transform[cr.GetLength(0)];
        for (int i = 0; i < craters.Length; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Quad);
            c.name = "Crater";
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(sun, false);
            float cx = cr[i, 0], cy = cr[i, 1];
            c.transform.localPosition = new Vector3(cx, cy, -(Mathf.Sqrt(Mathf.Max(0.001f, 0.25f - cx * cx - cy * cy)) + 0.01f));
            c.transform.localScale = Vector3.one * cr[i, 2];
            Setup(c.GetComponent<Renderer>(), craterMat);
            craters[i] = c.transform;
            c.SetActive(false);
        }

        // stars: small flat dots high in the sky, shown only at night
        starMat = Mat.Particle();
        starMat.color = new Color(1f, 1f, 1f, 0f);
        stars = new GameObject("Stars").transform;
        stars.SetParent(rig, false);
        var sr = new System.Random(23);
        const int starCount = 220;
        starT = new Transform[starCount];
        starBase = new Vector3[starCount];
        for (int i = 0; i < starCount; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Star";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(stars, false);
            float sz = Rand(sr, 1.2f, 2.8f);
            float sy = Rand(sr, 0f, 1f);
            starBase[i] = new Vector3(Rand(sr, -700f, 700f), 40f + 1100f * sy * sy, 360f);   // from low in the sky to high overhead, more of them low
            q.transform.localPosition = starBase[i];
            q.transform.localScale = new Vector3(sz * (0.8f + 0.5f * sy), sz * (0.8f + 0.5f * sy), 1f);
            starT[i] = q.transform;
            Setup(q.GetComponent<Renderer>(), starMat);
        }
        stars.gameObject.SetActive(false);

        // clouds: three flat blobs each, one shared material
        var rng = new System.Random(5);
        cloudMat = Mat.Particle();
        clouds = new Transform[14];
        cloudBaseX = new float[clouds.Length];
        cloudK = new float[clouds.Length];
        cloudSpeed = new float[clouds.Length];
        Vector3[] blobPos = { new Vector3(0f, 0f, 0f), new Vector3(-11f, 1f, 1f), new Vector3(12f, 0.5f, -1f) };
        Vector3[] blobScale = { new Vector3(24f, 6f, 11f), new Vector3(14f, 6f, 9f), new Vector3(16f, 5f, 9f) };
        for (int i = 0; i < clouds.Length; i++)
        {
            var holder = new GameObject("Cloud").transform;
            holder.SetParent(rig, false);
            float cz = 200f + 130f * (float)rng.NextDouble();
            float cy = 36f + 320f * Mathf.Pow((float)rng.NextDouble(), 1.4f);   // low and high clouds alike
            cloudBaseX[i] = -420f + 840f * (float)rng.NextDouble();
            cloudK[i] = Mathf.Lerp(0.30f, 0.14f, (cz - 200f) / 130f);          // nearer clouds slide past faster as you ride (parallax)
            holder.localPosition = new Vector3(cloudBaseX[i], cy, cz);
            holder.localScale = Vector3.one * (0.8f + 0.8f * (float)rng.NextDouble());
            for (int b = 0; b < 3; b++)
            {
                var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blob.name = "Blob";
                Destroy(blob.GetComponent<Collider>());
                blob.transform.SetParent(holder, false);
                blob.transform.localPosition = blobPos[b];
                blob.transform.localScale = blobScale[b];
                Setup(blob.GetComponent<Renderer>(), cloudMat);
            }
            clouds[i] = holder;
            cloudSpeed[i] = 0.5f + 1.0f * (float)rng.NextDouble();   // a slow drift of its own
        }

        // far hills: a few ahead, a few to each side
        var hr = new System.Random(11);
        const int count = 14;
        hillR = new Renderer[count];
        hillT = new Transform[count];
        hillBaseX = new float[count];
        hillBase = new Color[count];
        for (int i = 0; i < count; i++)
        {
            float x, z;
            if (i < 8) { x = -330f + i * 94f + Rand(hr, -20f, 20f); z = Rand(hr, 230f, 330f); }
            else
            {
                int k = i - 8;
                float sgn = (k % 2 == 0) ? -1f : 1f;
                x = sgn * Rand(hr, 170f, 260f);
                z = 20f + (k / 2) * 90f + Rand(hr, -20f, 20f);
            }
            float w = Rand(hr, 110f, 180f), h = Rand(hr, 36f, 70f), d = Rand(hr, 80f, 120f);
            float far = Mathf.Clamp01((z - 20f) / 300f);
            Color c = Color.Lerp(new Color(0.34f, 0.58f, 0.38f), new Color(0.42f, 0.60f, 0.56f), far);
            var hill = Prim.Make(PrimitiveType.Sphere, "Hill", rig, new Vector3(x, 0f, z), new Vector3(w, h, d), c);
            var r = hill.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            hillR[i] = r;
            hillBase[i] = c;
            hillT[i] = hill.transform;
            hillBaseX[i] = x;
        }

        // haze: far things melt into the horizon colour (where the render pipeline supports fog)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 110f;
        RenderSettings.fogEndDistance = 380f;

        BuildNightMist();
    }

    // ---- night mist: soft low banks drifting beside the road, so the dark has some fog in it you can actually see ----
    const int MistN = 12;
    const float MistBack = 12f, MistSpan = 80f;
    Transform mistHolder;
    Transform[] mistQ;
    Vector3[] mistBase;   // x, y offsets and z phase
    float[] mistPhase;
    Vector2[] mistSize;
    Material mistMat;

    void BuildNightMist()
    {
        if (!BlobShadow.Available) return;
        mistHolder = new GameObject("NightMist").transform;
        mistQ = new Transform[MistN];
        mistBase = new Vector3[MistN];
        mistPhase = new float[MistN];
        mistSize = new Vector2[MistN];
        mistMat = Mat.Particle();
        mistMat.mainTexture = BlobShadow.GlowTex();
        var rnd = new System.Random(4242);
        for (int i = 0; i < MistN; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "NightMistPuff";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(mistHolder, false);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mistMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            float sideSign = (i % 2 == 0) ? -1f : 1f;
            float x = sideSign * (5f + (float)rnd.NextDouble() * 20f);
            float w = 14f + (float)rnd.NextDouble() * 12f;
            float h = 2.6f + (float)rnd.NextDouble() * 2.2f;
            mistBase[i] = new Vector3(x, 0.4f + h * 0.5f, (float)i / MistN * MistSpan);
            mistPhase[i] = (float)rnd.NextDouble() * 6.28f;
            mistSize[i] = new Vector2(w, h);
            go.transform.localScale = new Vector3(w, h, 1f);
            mistQ[i] = go.transform;
        }
        mistHolder.gameObject.SetActive(false);
    }

    void TickNightMist(Color horizon)
    {
        if (mistHolder == null) return;
        float night = Mathf.Clamp01(NightLights.Dark * 1.5f) * GameConfig.NightDarkness;
        bool show = night > 0.03f;
        if (mistHolder.gameObject.activeSelf != show) mistHolder.gameObject.SetActive(show);
        if (!show) return;
        float bz = bike.position.z;
        Color col = Color.Lerp(horizon, Color.white, 0.40f);
        for (int i = 0; i < MistN; i++)
        {
            // each puff is pinned to the world and recycled from behind the bike to far ahead, fading at both ends
            float rel = Mathf.Repeat(mistBase[i].z - bz + MistBack, MistSpan) - MistBack;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-MistBack, -MistBack + 14f, rel)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(MistSpan - MistBack, MistSpan - MistBack - 18f, rel));
            float t = Time.time;
            mistQ[i].position = new Vector3(mistBase[i].x + Mathf.Sin(t * 0.25f + mistPhase[i]) * 1.8f, mistBase[i].y + Mathf.Sin(t * 0.4f + mistPhase[i] * 1.3f) * 0.3f, bz + rel);
            // the material is shared, so the fade at the ends is done by size: a puff swells in and shrinks away
            float k = edge * (0.8f + 0.2f * Mathf.Sin(t * 0.3f + mistPhase[i]));
            mistQ[i].localScale = new Vector3(mistSize[i].x * k, mistSize[i].y * k, 1f);
            mistQ[i].gameObject.SetActive(k > 0.02f);
        }
        col.a = 0.30f * night;
        mistMat.color = col;
    }

    static float Rand(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    static Texture2D craterTex;
    // A soft round smudge with a faint lighter rim: one moon crater.
    static Texture2D CraterTex()
    {
        if (craterTex != null) return craterTex;
        const int n = 32;
        craterTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        craterTex.wrapMode = TextureWrapMode.Clamp;
        craterTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                px[y * n + x] = new Color(1f, 1f, 1f, d >= 1f ? 0f : Mathf.Clamp01((1f - d) * 3f));
            }
        craterTex.SetPixels(px);
        craterTex.Apply();
        return craterTex;
    }

    static void Setup(Renderer r, Material m)
    {
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void LateUpdate()
    {
        if (groundR != null && groundVersion != GameConfig.ColorVersion)
        {
            groundVersion = GameConfig.ColorVersion;
            groundR.material.color = GameConfig.FarGroundColor;
        }
        if (clouds != null && cloudsShown != GameConfig.ShowClouds)
        {
            cloudsShown = GameConfig.ShowClouds;
            foreach (var c in clouds) if (c != null) c.gameObject.SetActive(cloudsShown);
        }
        if (hillR != null && hillsShown != GameConfig.ShowHills)
        {
            hillsShown = GameConfig.ShowHills;
            foreach (var hr in hillR) if (hr != null) hr.gameObject.SetActive(hillsShown);
        }
        if (cam == null || bike == null || rig == null) return;

        rig.position = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
        rig.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);   // the sky faces wherever the camera looks (the side view looks across the road)
        ground.position = new Vector3(0f, -0.6f, bike.position.z + 200f);

        Color horizon = DayCycle.SkyHorizon;
        Color top = DayCycle.SkyTop;
        for (int i = 0; i < skyMats.Length; i++)
        {
            float u = (float)i / (skyMats.Length - 1);
            skyMats[i].color = Color.Lerp(horizon, top, Mathf.Pow(u, 0.8f));
        }
        RenderSettings.fogColor = horizon;
        // at night the haze closes in, so you only see what is near
        float nd = NightLights.Dark * GameConfig.NightDarkness;
        RenderSettings.fogStartDistance = Mathf.Lerp(110f, 18f, nd);
        RenderSettings.fogEndDistance = Mathf.Lerp(380f, 105f, nd);
        TickNightMist(horizon);

        // Parallax: the farther a thing is, the slower it slides past as the bike rides. "Along the screen" is the camera's sideways axis, so this
        // does nothing in the chase view (the bike rides straight into the distance) and moves the sky in the side view.
        float par = Vector3.Dot(bike.position, rig.right);
        sun.localPosition = new Vector3(Mathf.Repeat(sunBase.x - par * 0.03f + 450f, 900f) - 450f, sunBase.y, sunBase.z);

        // A big, cinematic sun (a soft halo round it); it sinks lower at dawn and dusk, and at night it is a big pale moon with craters.
        float moonK = Mathf.Clamp01((NightLights.Amount - 0.45f) / 0.5f);
        Color tint = DayCycle.SunTint;
        float lowK = Mathf.Clamp01((0.8f - tint.b) / 0.5f) * (1f - moonK);
        float vis = Mathf.Lerp(DayCycle.SunVisible, 0.9f, moonK);
        sun.localPosition = new Vector3(sun.localPosition.x, sunBase.y - GameConfig.SunDuskDrop * lowK, sunBase.z);
        sunMat.color = tint;
        sun.localScale = Vector3.one * (GameConfig.SunSize * vis);
        haloMat.color = new Color(tint.r, tint.g, tint.b, 0.42f * Mathf.Lerp(1f, 0.6f, moonK));
        craterMat.color = new Color(0.55f, 0.62f, 0.82f, 0.55f * moonK);
        bool showCraters = moonK > 0.02f;
        for (int i = 0; i < craters.Length; i++) if (craters[i].gameObject.activeSelf != showCraters) craters[i].gameObject.SetActive(showCraters);

        float starA = Mathf.Clamp01((NightLights.Amount - 0.45f) / 0.5f);
        if (stars != null)
        {
            bool showStars = starA > 0.02f;
            if (stars.gameObject.activeSelf != showStars) stars.gameObject.SetActive(showStars);
            starMat.color = new Color(1f, 1f, 0.95f, starA);
            if (showStars)
                for (int i = 0; i < starT.Length; i++)
                    starT[i].localPosition = new Vector3(Mathf.Repeat(starBase[i].x - par * 0.015f + 700f, 1400f) - 700f, starBase[i].y, starBase[i].z);
        }

        cloudMat.color = DayCycle.CloudTint;
        for (int i = 0; i < clouds.Length; i++)
        {
            var p = clouds[i].localPosition;
            cloudBaseX[i] += cloudSpeed[i] * Time.deltaTime;     // its own slow drift
            p.x = Mathf.Repeat(cloudBaseX[i] - par * cloudK[i] + 450f, 900f) - 450f;
            clouds[i].localPosition = p;
        }

        for (int i = 0; i < hillR.Length; i++)
        {
            hillR[i].material.color = Color.Lerp(hillBase[i], horizon, 0.25f);
            if (i < 8)   // the ring of far hills ahead slides past very slowly
            {
                var hp = hillT[i].localPosition;
                hp.x = Mathf.Repeat(hillBaseX[i] - par * 0.08f + 450f, 900f) - 450f;
                hillT[i].localPosition = hp;
            }
        }
    }
}
