using UnityEngine;

// Banks of drifting mist on one side of a road segment, standing where a beach starts or ends, so the change from houses and
// tall buildings to sand and sea (or back) is wrapped in fog instead of showing a hard edge.
// Each bank is soft white-grey quads made from the glow texture. They fade in as the bike comes up to them and out as it passes.
// Decoration only: no colliders, no targets. Built the first time a segment needs one.
public class MistBank
{
    const int Clusters = 2;      // one at the start of the segment, one at its end
    const int Puffs = 7;

    int side;
    Transform holder;
    bool built;
    readonly Transform[][] puff = new Transform[Clusters][];
    readonly Vector3[][] basePos = new Vector3[Clusters][];
    readonly float[][] ph = new float[Clusters][];
    readonly Material[] mat = new Material[Clusters];
    readonly bool[] on = new bool[Clusters];
    readonly float[] zc = new float[Clusters];

    public bool Active { get { return on[0] || on[1]; } }

    public static MistBank Create(Transform parent, int side)
    {
        var b = new MistBank();
        b.side = side;
        var h = new GameObject(side < 0 ? "MistLeft" : "MistRight").transform;
        h.SetParent(parent, false);
        h.gameObject.SetActive(false);
        b.holder = h;
        return b;
    }

    void BuildIfNeeded()
    {
        if (built) return;
        built = true;
        for (int c = 0; c < Clusters; c++)
        {
            mat[c] = Mat.Particle();
            mat[c].mainTexture = BlobShadow.GlowTex();
            puff[c] = new Transform[Puffs];
            basePos[c] = new Vector3[Puffs];
            ph[c] = new float[Puffs];
            for (int i = 0; i < Puffs; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "Mist";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(holder, false);
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = mat[c];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                puff[c][i] = go.transform;
            }
        }
    }

    public void Hide()
    {
        on[0] = on[1] = false;
        if (holder != null) holder.gameObject.SetActive(false);
    }

    // atStart / atEnd: a beach starts / ends on this side at the start / end of the segment.
    public void Show(System.Random r, bool atStart, bool atEnd, float segLen)
    {
        Hide();
        if (!GameConfig.TransitionMist || !BlobShadow.Available || !(atStart || atEnd)) return;
        BuildIfNeeded();
        Setup(0, atStart, 0f, r);
        Setup(1, atEnd, segLen, r);
    }

    void Setup(int c, bool want, float z, System.Random r)
    {
        on[c] = want;
        zc[c] = z;
        for (int i = 0; i < Puffs; i++)
        {
            puff[c][i].gameObject.SetActive(want);
            if (!want) continue;
            float x = side * (7.5f + (float)r.NextDouble() * 22f);
            float w = 12f + (float)r.NextDouble() * 8f;
            float h = 6f + (float)r.NextDouble() * 5f;
            float y = h * 0.5f + (float)r.NextDouble() * 5f;
            float zz = z + ((float)r.NextDouble() - 0.5f) * 16f;
            basePos[c][i] = new Vector3(x, y, zz);
            ph[c][i] = (float)r.NextDouble() * 6.28f;
            puff[c][i].localPosition = basePos[c][i];
            puff[c][i].localScale = new Vector3(w, h, 1f);
        }
    }

    // Called every frame for the segments that have mist: fades with the distance to the bike, matches the sky's haze colour, drifts.
    public void Tick(float rootZ, float bikeZ, Color fog)
    {
        if (!Active || holder == null) return;
        float best = 0f;
        for (int c = 0; c < Clusters; c++)
        {
            if (!on[c]) continue;
            float dz = rootZ + zc[c] - bikeZ;
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(95f, 60f, dz)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-22f, -4f, dz));
            Color col = Color.Lerp(fog, Color.white, 0.4f);
            col.a = GameConfig.MistStrength * a;
            mat[c].color = col;
            best = Mathf.Max(best, a);
            if (a < 0.01f) continue;
            for (int i = 0; i < Puffs; i++)
            {
                float t = Time.time;
                var p = basePos[c][i];
                puff[c][i].localPosition = new Vector3(p.x + Mathf.Sin(t * 0.30f + ph[c][i]) * 1.4f, p.y + Mathf.Sin(t * 0.45f + ph[c][i] * 1.3f) * 0.35f, p.z);
            }
        }
        bool show = best > 0.01f;
        if (holder.gameObject.activeSelf != show) holder.gameObject.SetActive(show);
    }
}
