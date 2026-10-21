using System.Collections.Generic;
using UnityEngine;

// House (partial) - Hit-zone glows, target shimmer and sparkles.
// The main part of the class is in House.cs.
public partial class House
{
    // A soft shimmer on every open target: the lawn pad, the balconies and the mailbox drift a little toward white and back.
    // (Walled houses and beach scenes have their own stronger pulse for their targets, so those are left to it.)
    void ShimmerTargets()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f + phase);
        bool intro = Tutorial.IntroGlow && NightLights.Available;   // the tutorial's first step shows the zones glowing hard, by day too
        float day = DayFactor();   // 1 in daylight, 0 at night: the zones need a stronger shimmer in the sun
        float k = intro ? 0.30f + 0.60f * pulse : (0.10f + 0.35f * pulse) * (1f + 0.5f * day);   // medium strength (more by day)
        // the tutorial's board glows orange, breathing gently
        if (boardGlowR != null && boardGlowR.gameObject.activeInHierarchy)
        {
            Color bc = Color.Lerp(new Color(1.00f, 0.86f, 0.55f), new Color(1.00f, 0.62f, 0.20f), pulse);   // (bright: a pale hot orange that breathes)
            var bm = boardGlowR.material;
            bm.color = bc;
            if (bm.HasProperty("_BaseColor")) bm.SetColor("_BaseColor", bc);
        }
        if (boardHalo != null && boardHalo.gameObject.activeSelf)
        {
            boardHaloMat.color = new Color(1.00f, 0.55f, 0.10f, 0.90f + 0.10f * pulse);
            boardHalo.localScale = new Vector3(HaloW * (1f + 0.05f * pulse), HaloH * (1f + 0.05f * pulse), 1f);
            boardHalo2.localScale = new Vector3(HaloW * 0.72f * (1f + 0.05f * pulse), HaloH * 0.72f * (1f + 0.05f * pulse), 1f);
        }
        // (what is already served stays gold and does not shimmer: the ground floor, and each balcony that was hit)
        if (HoopFocusHere && balconies != null)
            for (int i = 0; i < balconies.Length; i++)
                if (balconies[i].on && !balconies[i].hit && balconies[i].slabR != null) balconies[i].slabR.material.color = GameConfig.PadColor;   // (the basket shot: balconies stay plain)
        if (!groundDone)
        {
            GrassShimmer(k);   // the lawn's grass itself brightens and fades (nothing glows under it)
            if (mailboxRenderer != null && beachScene != 1 && beachScene != 2) mailboxRenderer.material.color = Color.Lerp(GameConfig.MailboxColor, Color.white, k);
        }
        if (balconies != null)
            for (int i = 0; i < balconies.Length; i++)
                if (BalconyOpen(i)) balconies[i].slabR.material.color = Color.Lerp(GameConfig.PadColor, Color.white, k);   // every open balcony shimmers the same

        // At night the lit colours stay dark whatever their tint, so an unlit glow sheet pulses on top of each target as well.
        bool night = NightLights.Available && NightLights.Dark > 0.1f;
        float a = intro ? 0.35f + 0.30f * pulse : 0.05f + 0.15f * pulse;   // soft at night
        bool glow = night || intro;
        TargetGlow(0, null, glow, GameConfig.PadColor, a);   // (no glow under the grass)
        TargetGlow(2, groundDone ? null : mailboxRenderer, glow, GameConfig.MailboxColor, a);
        TargetGlow(3, groundDone ? null : matRenderer, intro, GameConfig.PadColor, a);   // the doormat only glows in the intro
        if (balconies != null)
            for (int i = 0; i < balconies.Length; i++)
                TargetGlow(4 + i, BalconyOpen(i) ? balconies[i].slabR : null, glow, GameConfig.PadColor, a);
        Sparkles(intro);
        ZoneGlows();
        BalconyAuras();
    }

    // ---- the yellow glow on every hit zone: the floor rim lit, light rising off its edges, never past the zone itself ----
    sealed class EdgeFx
    {
        Transform halo;
        readonly Transform[] cur = new Transform[4];   // +x edge, -x edge, -z end, +z end
        Material mc, mh;

        void Build(Transform parent)
        {
            mc = Mat.Particle(); mc.mainTexture = BeamTex();
            mh = Mat.Particle(); mh.mainTexture = HaloTex();
            var h = BlobShadow.Create(parent); h.name = "ZoneFloorGlow";
            h.GetComponent<Renderer>().sharedMaterial = mh;
            halo = h.transform;
            for (int i = 0; i < cur.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "ZoneEdgeGlow";
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(parent, false);
                var r = q.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sharedMaterial = mc;
                cur[i] = q.transform;
            }
        }

        public void Hide()
        {
            if (halo == null) return;
            halo.gameObject.SetActive(false);
            for (int i = 0; i < cur.Length; i++) cur[i].gameObject.SetActive(false);
        }

        // skipX: -1/+1 leaves that world-x edge dark (the house wall side); 0 lights all four edges.
        public void Show(Transform parent, Renderer r, float height, float pulse, float alpha, int skipX, bool floor = true)
        {
            if (r == null || !r.gameObject.activeInHierarchy || !BlobShadow.Available) { Hide(); return; }
            if (halo == null) Build(parent);
            Bounds b = r.bounds;
            mc.color = new Color(1f, 0.86f, 0.18f, alpha);
            mh.color = new Color(1f, 0.86f, 0.18f, Mathf.Min(1f, alpha * 0.9f));
            float inset = Mathf.Min(0.05f, Mathf.Min(b.size.x, b.size.z) * 0.1f);
            float sx = Mathf.Max(0.2f, b.size.x - 2f * inset), sz = Mathf.Max(0.2f, b.size.z - 2f * inset);
            float top = b.max.y + 0.03f;
            float hh = height * (0.9f + 0.2f * pulse);
            halo.position = new Vector3(b.center.x, top, b.center.z);
            halo.localScale = new Vector3(sx, sz, 1f);
            float ex = b.size.x * 0.5f - inset, ez = b.size.z * 0.5f - inset;
            Place(0, new Vector3(b.center.x + ex, top + hh * 0.5f, b.center.z), 90f, sz, hh, skipX != 1);
            Place(1, new Vector3(b.center.x - ex, top + hh * 0.5f, b.center.z), 90f, sz, hh, skipX != -1);
            Place(2, new Vector3(b.center.x, top + hh * 0.5f, b.center.z - ez), 0f, sx, hh, true);
            Place(3, new Vector3(b.center.x, top + hh * 0.5f, b.center.z + ez), 0f, sx, hh, true);
            halo.gameObject.SetActive(floor);   // (the lawn shows only its rim: a floor wash would sit under the grass)
        }

        void Place(int i, Vector3 pos, float yaw, float w, float h, bool on)
        {
            cur[i].gameObject.SetActive(on);
            if (!on) return;
            cur[i].position = pos;
            cur[i].rotation = Quaternion.Euler(0f, yaw, 0f);
            cur[i].localScale = new Vector3(w, h, 1f);
        }
    }

    static Texture2D beamTex, haloTex;
    readonly EdgeFx fxPad = new EdgeFx(), fxMat = new EdgeFx(), fxMail = new EdgeFx(), fxPath = new EdgeFx();
    readonly EdgeFx[] fxBalc = { new EdgeFx(), new EdgeFx(), new EdgeFx(), new EdgeFx(), new EdgeFx(), new EdgeFx() };   // one per balcony in the pool

    // This balcony is in use, not served yet, and its floor is not served either.
    bool BalconyOpen(int i)
    {
        var b = balconies != null && i >= 0 && i < balconies.Length ? balconies[i] : null;
        return b != null && b.on && !b.hit && !floorDone[b.floor] && b.slabR != null && !HoopFocusHere;
    }

    static Texture2D BeamTex()
    {
        if (beamTex != null) return beamTex;
        const int w = 32, h = 64;
        beamTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        beamTex.wrapMode = TextureWrapMode.Clamp;
        beamTex.filterMode = FilterMode.Bilinear;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float v = (y + 0.5f) / h;                                  // 0 at the floor, 1 at the top
                float across = 1f - Mathf.Pow(u, 6f);                      // runs the full edge, softening only at the very ends
                float up = Mathf.Pow(Mathf.Clamp01(1f - v), 1.8f);         // brightest at the floor, gone by the top
                px[y * w + x] = new Color(1f, 1f, 1f, across * up);
            }
        beamTex.SetPixels(px);
        beamTex.Apply();
        return beamTex;
    }

    // The floor itself: bright along the rim, a faint wash in the middle.
    static Texture2D HaloTex()
    {
        if (haloTex != null) return haloTex;
        const int n = 32;
        haloTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        haloTex.wrapMode = TextureWrapMode.Clamp;
        haloTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                float e = Mathf.Max(u, v);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.10f + 0.90f * e * e * e));
            }
        haloTex.SetPixels(px);
        haloTex.Apply();
        return haloTex;
    }

    void ZoneGlows()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + phase);
        float a = 0.55f + 0.30f * pulse;
        if (!(Tutorial.IntroGlow && NightLights.Available)) a *= Mathf.Lerp(0.40f, 1f, DayFactor());   // softer at night, where it shows up on its own
        int wall = (int)(side * Mathf.Sign(transform.right.x));   // world direction of the house wall from the balcony slab
        for (int i = 0; i < fxBalc.Length; i++)
            fxBalc[i].Show(transform, BalconyOpen(i) ? balconies[i].slabR : null, 0.65f, pulse, a, wall);
        fxPad.Show(transform, (padRenderer != null && !paved && !groundDone) ? padRenderer : null, 0.9f, pulse, a, wall, false);   // the open lawn: bright yellow light along its rim only (nothing under the grass)
        // the path to the door: the same bright yellow light along its rim as the lawn (nothing on the floor itself); the edge by the doormat is left to the doormat's own glow
        Renderer pathR = pathRenderer != null ? pathRenderer : (pathVisualT != null ? pathVisualT.GetComponent<Renderer>() : null);
        fxPath.Show(transform, (pathR != null && !groundDone) ? pathR : null, 0.9f, pulse, a, wall, false);
        fxMat.Show(transform, groundDone ? null : matRenderer, 0.40f, pulse, a, 0);
        fxMail.Show(transform, (!groundDone && mailboxRenderer != null && beachScene != 1 && beachScene != 2) ? mailboxRenderer : null, 0.35f, pulse, a, 0);
        tfxOn = true;
    }

    void HideZoneGlows()
    {
        for (int i = 0; i < fxBalc.Length; i++) fxBalc[i].Hide();
        fxPad.Hide(); fxMat.Hide(); fxMail.Hide(); fxPath.Hide();
    }

    readonly Transform[] tfx = new Transform[10];        // 0 lawn, 2 mailbox, 3 doormat, 4-9 the balconies (1 is not used)
    readonly Material[] tfxMat = new Material[10];
    bool tfxOn;

    void TargetGlow(int i, Renderer r, bool night, Color tint, float alpha)
    {
        bool show = night && r != null && r.gameObject.activeInHierarchy;
        if (!show) { if (tfx[i] != null) tfx[i].gameObject.SetActive(false); return; }
        if (tfx[i] == null)
        {
            var q = BlobShadow.Create(transform);
            q.name = "TargetShimmer";
            tfxMat[i] = new Material(NightLights.Bulb);
            q.GetComponent<Renderer>().sharedMaterial = tfxMat[i];
            tfx[i] = q.transform;
        }
        Bounds b = r.bounds;
        tfx[i].position = new Vector3(b.center.x, b.max.y + 0.03f, b.center.z);
        tfx[i].localScale = new Vector3(Mathf.Max(0.3f, b.size.x), Mathf.Max(0.3f, b.size.z), 1f);
        Color g = Color.Lerp(tint, Color.white, 0.45f);
        tfxMat[i].color = new Color(g.r, g.g, g.b, alpha);
        tfx[i].gameObject.SetActive(true);
        tfxOn = true;
    }

    void HideTargetGlows()
    {
        for (int i = 0; i < tfx.Length; i++) if (tfx[i] != null) tfx[i].gameObject.SetActive(false);
        for (int i = 0; i < SparkleN; i++) HideSparkle(i);
        HideZoneGlows();
        HideBalconyAuras();
        tfxOn = false;
    }

    // ---- little glints that twinkle over the hit zones (the lawn pad, the doormat, the mailbox, the balcony) ----

    static float DayFactor() { return NightLights.Available ? 1f - Mathf.Clamp01(NightLights.Dark * 1.5f) : 1f; }

    const int NightSparkles = 9;   // by day there are more
    const int SparkleN = 14;
    readonly Transform[] spkA = new Transform[SparkleN];   // each glint is one small flat quad with a fine four-point star on it
    readonly Material[] spkMat = new Material[SparkleN];
    readonly Renderer[] spkSrc = new Renderer[4];
    static Texture2D sparkleTex;

    // A fine four-pointed star: a tiny bright core and four thin tapering spikes (not a plus sign).
    static Texture2D SparkleTex()
    {
        if (sparkleTex != null) return sparkleTex;
        const int n = 64;
        sparkleTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        sparkleTex.wrapMode = TextureWrapMode.Clamp;
        sparkleTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f);
                float v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                float star = Mathf.Clamp01(1f - (Mathf.Sqrt(u) + Mathf.Sqrt(v)));
                star = star * star;                                   // thin, tapering spikes
                float core = Mathf.Exp(-(u * u + v * v) * 70f);       // a small bright centre
                float a = Mathf.Clamp01(star * 1.4f + core);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        sparkleTex.SetPixels(px);
        sparkleTex.Apply();
        return sparkleTex;
    }

    void HideSparkle(int i)
    {
        if (spkA[i] != null) spkA[i].gameObject.SetActive(false);
    }

    void Sparkles(bool intro)
    {
        if (!BlobShadow.Available) return;
        spkSrc[0] = (padRenderer != null && !paved && !groundDone) ? padRenderer : null;   // (the lawn's glints float above the grass)
        spkSrc[1] = groundDone ? null : matRenderer;
        spkSrc[2] = groundDone ? null : mailboxRenderer;
        spkSrc[3] = null;   // (an open balcony, if there is one)
        if (balconies != null)
            for (int bi = 0; bi < balconies.Length; bi++)
                if (BalconyOpen(bi)) { spkSrc[3] = balconies[bi].slabR; break; }
        float day = DayFactor();
        for (int i = 0; i < SparkleN; i++)
        {
            Renderer r = spkSrc[i % 4];
            if (r == null || !r.gameObject.activeInHierarchy || (i >= NightSparkles && day < 0.5f)) { HideSparkle(i); continue; }

            float t = Time.time * ((intro ? 1.3f : 0.8f) + 0.3f * day) + i * 0.37f + phase;
            float life = t - Mathf.Floor(t);
            float seed = Mathf.Floor(t) + i * 7.7f + transform.position.z * 0.13f;
            float env = Mathf.Sin(life * Mathf.PI);
            env *= env;
            if (env < 0.03f) { HideSparkle(i); continue; }

            if (spkA[i] == null)
            {
                var q = BlobShadow.Create(transform);
                q.name = "Sparkle";
                spkMat[i] = new Material(Mat.Particle());
                spkMat[i].mainTexture = SparkleTex();
                q.GetComponent<Renderer>().sharedMaterial = spkMat[i];
                spkA[i] = q.transform;
            }
            Bounds b = r.bounds;
            float x = Mathf.Lerp(b.min.x, b.max.x, Hash01(seed * 1.7f));
            float z = Mathf.Lerp(b.min.z, b.max.z, Hash01(seed * 3.1f + 5f));
            float gy = b.max.y + 0.05f;
            if (i % 4 == 0) gy = b.max.y + 0.40f + 0.30f * Hash01(seed * 2.3f);   // over the grass tops (lawn glints), not buried in the blades
            spkA[i].position = new Vector3(x, gy, z);
            spkA[i].rotation = Quaternion.Euler(90f, Hash01(seed * 5.3f) * 90f, 0f);   // each glint turned a different way
            float s = (0.22f + 0.30f * env) * (intro ? 1.4f : 1f) * (1f + 0.35f * day);   // bigger in daylight, where they have to show against the sun
            spkA[i].localScale = new Vector3(s, s, 1f);
            spkMat[i].color = new Color(1f, 0.97f, 0.82f, env);
            spkA[i].gameObject.SetActive(true);
            tfxOn = true;
        }
    }
}
