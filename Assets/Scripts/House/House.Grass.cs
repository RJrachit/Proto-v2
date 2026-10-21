using UnityEngine;

// House (partial) - Low-poly grass: tufts of faceted blades scattered over a lawn (clear of the path, doormat and trees; low under the table) and
// through a park wood. One collider-free mesh per house with the shared facade palette material, so it costs a single draw call.
// The main part of the class is in House.cs.
public partial class House
{
    GameObject grassGO;
    Mesh grassMesh;
    static readonly FacadeBuilder gb = new FacadeBuilder();   // (its own builders: the facade's is rebuilt at other times)
    static readonly FacadeBuilder tb = new FacadeBuilder();
    GameObject turfGO;
    Mesh turfMesh;
    bool turfBuilt, grassHas;
    Material grassMat;   // this house's own copy of the palette material, so its grass can shimmer without touching the others

    // Five shades of grass: fresh, mid, bright, sun-dried (yellowish) and cool (bluish), so the lawn is never one flat green.
    static readonly Color[] GrassLights =
    {
        new Color(0.72f, 0.96f, 0.42f), new Color(0.58f, 0.92f, 0.38f), new Color(0.80f, 0.97f, 0.50f), new Color(0.90f, 0.94f, 0.46f), new Color(0.52f, 0.94f, 0.60f),
    };
    static readonly Color[] GrassDarks =
    {
        new Color(0.36f, 0.76f, 0.30f), new Color(0.42f, 0.80f, 0.32f), new Color(0.32f, 0.72f, 0.36f), new Color(0.62f, 0.78f, 0.28f), new Color(0.28f, 0.70f, 0.44f),
    };
    // The turf under the tufts: a dark, almost flat faceted layer that covers the lawn, so no lighter ground or sun-lit slopes show between blades.
    static readonly Color[] TurfShades = { new Color(0.10f, 0.30f, 0.15f), new Color(0.12f, 0.33f, 0.16f), new Color(0.09f, 0.28f, 0.17f), new Color(0.13f, 0.34f, 0.16f) };

    // lawn = an ordinary lawn in front of a house; wood = the lot is a park. Anything else gets no tufts.
    void LayoutGrass(float L, float doorZ, bool lawn, bool wood, bool tableOn)
    {
        if (!GameConfig.GrassTufts || !(lawn || wood))
        {
            if (grassGO != null) grassGO.SetActive(false);
            turfBuilt = false;
            if (turfGO != null) turfGO.SetActive(false);
            return;
        }
        float s = side;
        float half = L * 0.5f;
        float wz = transform.position.z;
        float x0 = wood ? 4.6f : 4.55f - setback;
        float x1 = wood ? 13.4f : 8.1f;
        float step = wood ? GameConfig.GrassStepPark : GameConfig.GrassStep;                  // a tuft every step, jittered: the blades overlap, so no bare ground shows
        int cols = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / step));
        int rows = Mathf.Max(1, Mathf.RoundToInt((L - 0.3f) / step));
        const int MaxTufts = 700;   // (a mesh holds 65k vertices: a long park thins out evenly, it never runs out part way)
        if (cols * rows > MaxTufts)
        {
            float f = Mathf.Sqrt((float)cols * rows / MaxTufts);
            cols = Mathf.Max(1, Mathf.FloorToInt(cols / f));
            rows = Mathf.Max(1, Mathf.FloorToInt(rows / f));
        }
        float cw = (x1 - x0) / cols, rh = (L - 0.3f) / rows;

        gb.Clear();
        gb.muted = false;
        tb.Clear();
        tb.muted = false;
        Vector3 table = Vector3.zero;
        bool hasTable = tableOn && tableZone != null;
        if (hasTable) table = tableZone.transform.localPosition;
        int made = 0;

        // The turf layer (shown on every lawn, see UpdateTurf).
        {
            float ts = 0.6f;
            int tc = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / ts)), tr = Mathf.Max(1, Mathf.RoundToInt(L / ts));
            float tw = (x1 - x0) / tc, th = L / tr;
            for (int r = 0; r < tr; r++)
            {
                for (int c = 0; c < tc; c++)
                {
                    int i = r * tc + c;
                    float x = x0 + (c + 0.5f) * tw + (Hash01(wz * 0.17f + i * 2.9f + 0.4f) - 0.5f) * 0.2f;
                    float z = -half + (r + 0.5f) * th + (Hash01(wz * 0.23f + i * 4.1f + 3.3f) - 0.5f) * 0.2f;
                    if (!wood && Mathf.Abs(z - doorZ) < 1.15f) continue;
                    float hh = 0.04f + 0.04f * Hash01(wz * 0.31f + i * 1.7f + 5.2f);
                    Color shade = TurfShades[(int)(Hash01(wz * 0.47f + i * 3.1f + 8.8f) * TurfShades.Length) % TurfShades.Length];
                    tb.Frustum(0.07f, 0.07f + hh, z - th * 0.62f, z + th * 0.62f, z - th * 0.55f, z + th * 0.55f, tw * 1.4f, tw * 0.9f, s * x, shade);
                    made++;
                }
            }
        }

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int i = r * cols + c;
                float x = x0 + (c + 0.15f + 0.7f * Hash01(wz * 0.71f + i * 3.77f + 1.3f)) * cw;
                float z = -half + 0.15f + (r + 0.15f + 0.7f * Hash01(wz * 0.43f + i * 5.19f + 6.7f)) * rh;
                if (!wood && Mathf.Abs(z - doorZ) < 1.15f) continue;                       // never on the path or the doormat
                if (OnTreeFoot(s * x, z)) continue;
                float k = Hash01(wz * 0.29f + i * 2.31f + 9.1f);
                int pick = (int)(k * GrassLights.Length) % GrassLights.Length;
                float h = 0.46f + 0.34f * Hash01(wz * 0.61f + i * 1.93f + 2.2f);
                if (Hash01(wz * 0.83f + i * 6.1f + 1.9f) < 0.14f) h *= 1.4f;                  // the odd tall clump
                if (hasTable && Mathf.Abs(s * x - table.x) < 0.9f && Mathf.Abs(z - table.z) < 1.2f) h = Mathf.Min(h, 0.42f);   // under the table the grass stays below its top
                float spread = 0.38f + 0.16f * Hash01(wz * 0.53f + i * 2.7f + 4.4f);
                float tint = 0.88f + 0.22f * Hash01(wz * 0.19f + i * 5.3f + 7.1f);           // each tuft a little lighter or darker
                gb.Tuft(new Vector3(s * x, 0.07f, z), h, spread, 7, GrassLights[pick] * tint, GrassDarks[pick] * tint, wz * 0.37f + i * 7.3f, 1.3f);
                made++;
            }
        }

        if (grassGO == null)
        {
            grassGO = new GameObject("Grass");
            grassGO.transform.SetParent(transform, false);
            grassGO.AddComponent<MeshFilter>();
            var mr = grassGO.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        if (grassMat == null) grassMat = new Material(FacadePalette.Mat);   // (Unity-aware null check: rebuilt after Play mode ends)
        grassMat.color = Color.white;
        grassGO.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
        grassMesh = gb.Upload(grassMesh);
        grassGO.GetComponent<MeshFilter>().sharedMesh = grassMesh;
        grassHas = made > 0;

        if (turfGO == null)
        {
            turfGO = new GameObject("Turf");
            turfGO.transform.SetParent(transform, false);
            turfGO.AddComponent<MeshFilter>();
            var tr2 = turfGO.AddComponent<MeshRenderer>();
            tr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        turfGO.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
        turfMesh = tb.Upload(turfMesh);
        turfGO.GetComponent<MeshFilter>().sharedMesh = turfMesh;
        turfBuilt = true;
        UpdateTurf();
    }

    static readonly Color GrassGlow = new Color(1.55f, 1.65f, 0.80f);   // the grass of an open lawn brightens towards this (a warm lime light)

    // An open customer lawn: its grass brightens and fades with the shimmer (k is the same 0..1 pulse the other targets use).
    void GrassShimmer(float k)
    {
        if (grassMat == null || !turfBuilt) return;
        grassMat.color = Color.Lerp(Color.white, GrassGlow, Mathf.Clamp01(k * 1.25f));
    }

    // The turf and the tufts show on every lawn, a customer's included (the lawn has no glow: the doormat, mailbox and balconies are the targets).
    void UpdateTurf()
    {
        if (grassMat != null && (resolved || groundDone || !Subscriber)) grassMat.color = Color.white;   // (no shimmer once served or on a lawn that is not a target)
        if (turfGO != null) turfGO.SetActive(turfBuilt);
        if (grassGO != null) grassGO.SetActive(turfBuilt && grassHas);
    }

    // True when (x, z) is within 0.6 m of a standing tree's trunk (lot space).
    bool OnTreeFoot(float x, float z)
    {
        if (TreeNear(lawnTree, x, z)) return true;
        for (int k = 0; k < blockerTrees.Length; k++) if (TreeNear(blockerTrees[k], x, z)) return true;
        for (int k = 0; k < parkTreeN; k++) if (TreeNear(parkTrees[k], x, z)) return true;
        return false;
    }

    static bool TreeNear(LowPolyTree t, float x, float z)
    {
        if (t == null || !t.Active) return false;
        Vector3 f = t.HeartSpot;
        float dx = f.x - x, dz = f.z - z;
        return dx * dx + dz * dz < 0.6f * 0.6f;
    }
}
