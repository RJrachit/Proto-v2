using System.Collections.Generic;
using UnityEngine;

// House (partial) - Throw blockers (billboards, trees), street lamps and fences.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- throw blockers ----
    int blockerKind;                    // 0 none, 1 billboard, 2 two trees
    static float LotGapWidth { get { return GameConfig.LotGap; } }   // the gap between two lots along the road
    Destructible billboard;             // built the first time a lot needs it
    static readonly Color BoardColor = new Color(0.30f, 0.45f, 0.62f);

    // True while something stands between the road and the door that a straight throw would hit (the aim assist backs off then).
    public bool Blocked { get { return blockerKind == 2 || (blockerKind == 1 && billboard != null && billboard.Standing); } }

    // A tall billboard on two posts (breaks after one paper) or two solid trees, in the gap before the lot.
    // Only a part of the frontage is covered, so a throw from further back or a lob over the top still gets through.
    void LayoutBlocker(float L, float doorZ)
    {
        if (billboard != null) billboard.gameObject.SetActive(false);
        HideBlockerTrees();
        if (blockerKind == 0) return;

        float s = side;
        float half = L * 0.5f;
        // The blocker stands in the gap between this lot and the one before it (the bike arrives from that side), running from the
        // footpath edge back into the plot, slanted so it cuts off the direct diagonal throw at the door.
        float bz = -half - 0.5f * LotGapWidth;
        // Side view: the blocker stands on the lawn a little in front of the house (before the door along the road), the ad facing the camera.
        bool sideView = !GameConfig.ChaseView;
        if (sideView) bz = Mathf.Max(doorZ - 3.0f, -half + 1.7f);
        Color metal = new Color(0.22f, 0.24f, 0.28f);

        if (blockerKind == 1)
        {
            // A roadside ad: its face looks along the road towards the oncoming bike. The board runs from the footpath edge back
            // into the plot, 22 degrees off straight across; the end by the footpath is the one further along the road.
            float x = s * (sideView ? 5.4f : 6.1f);
            float yaw = sideView ? 0f : -s * 68f;   // side view: the board runs along the road, its face towards the camera
            float yr = yaw * Mathf.Deg2Rad;
            Vector3 along = new Vector3(Mathf.Sin(yr), 0f, Mathf.Cos(yr));   // the board's width direction
            Part(new Vector3(x, 1.2f, bz) - along * 1.3f, new Vector3(0.22f, 2.4f, 0.22f), metal, true);
            Part(new Vector3(x, 1.2f, bz) + along * 1.3f, new Vector3(0.22f, 2.4f, 0.22f), metal, true);
            if (billboard == null)
            {
                var go = Prim.Make(PrimitiveType.Cube, "Billboard", transform, Vector3.zero, Vector3.one, BoardColor, true);
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                var d = go.AddComponent<Destructible>();
                d.rend = go.GetComponent<Renderer>();
                d.group = -1;
                d.fixedPos = true;
                billboard = d;
            }
            billboard.owner = this;
            billboard.crate = true;
            billboard.Place(0, 1, new Vector3(x, 2.6f, bz), new Vector3(0.2f, 2.8f, 3.4f), BoardColor);
            billboard.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // the ad on the face towards the oncoming bike (a child, so it flies off with the board)
            var ad = billboard.transform.Find("Ad");
            if (ad == null)
                ad = Prim.Make(PrimitiveType.Quad, "Ad", billboard.transform, Vector3.zero, Vector3.one, Color.white).transform;
            ad.localPosition = new Vector3(-s * 0.62f, 0f, 0f);
            ad.localRotation = Quaternion.Euler(0f, s * 90f, 0f);
            ad.localScale = new Vector3(3.2f / 3.4f, 2.5f / 2.8f, 1f);
            var adR = ad.GetComponent<Renderer>();
            if (adR != null) adR.sharedMaterial = BillboardArt.Mat(Mathf.FloorToInt(Hash01(transform.position.z * 0.377f + 5.1f) * BillboardArt.Count));
        }
        else
        {
            for (int k = 0; k < 2; k++)
            {
                // two trees one behind the other across the gap, from the footpath back into the plot
                float z = bz + (k == 0 ? 0.25f : -0.25f);
                float x = s * (k == 0 ? 5.0f : 7.1f);
                if (sideView)   // side view: two trees side by side along the road, a little in front of the house
                {
                    float off = k == 0 ? 2.2f : 4.3f;
                    z = Mathf.Max(doorZ - off, -half + 0.8f);
                    if (Mathf.Abs(z - doorZ) < 2.0f) z = Mathf.Min(doorZ + off, half - 0.8f);   // (a short lot: stand on the other side, never on the path or the doormat)
                    x = s * 5.3f;
                }
                PlaceBlockerTree(k, new Vector3(x, 0f, z));
                Blob(new Vector3(x, 0f, z), 2.6f, 2.6f, 0.28f);
            }
        }
    }

    // A street lamp on the footpath at one end of the lot: solid base, pole and lamp head (papers bounce off it).
    void LayoutStreetLamp(float L, bool flip, float avoidZ = -999f)
    {
        // Lamps sit on a fixed grid along the road (world z), every LampSpacing, the right side offset by half a step so the two sides stagger. Each lot places the grid points inside its stretch (the gap after it included), so spacing stays even.
        float s = side;
        float sp = GameConfig.LampSpacing;
        float wz = transform.position.z;
        float half = L * 0.5f + 1.25f;
        float off = s > 0f ? sp * 0.5f : 0f;   // right side offset by half a step so the two sides stagger
        float lx = s * (3.7f - setback);   // (on the footpath whatever the lot's setback)
        Color metal = new Color(0.22f, 0.24f, 0.28f);
        int k0 = Mathf.CeilToInt((wz - half - off) / sp);
        for (int k = k0; off + k * sp < wz + half; k++)
        {
            float lz = off + k * sp - wz;
            if (Mathf.Abs(lz - avoidZ) < 1.8f) continue;   // never a lamp pole in front of a customer's mailbox: it would block the throw
            Part(new Vector3(lx, 0.2f, lz), new Vector3(0.4f, 0.4f, 0.4f), metal, true);
            Part(new Vector3(lx, 1.9f, lz), new Vector3(0.14f, 3.4f, 0.14f), metal, true);
            Part(new Vector3(lx - s * 0.25f, 3.55f, lz), new Vector3(0.6f, 0.08f, 0.08f), metal, false);
            Part(new Vector3(lx - s * 0.5f, 3.4f, lz), new Vector3(0.45f, 0.22f, 0.45f), new Color(1.0f, 0.93f, 0.55f), true);
            LampGlow(new Vector3(lx - s * 0.5f, 3.4f, lz));   // the bulb covers the whole lamp head, so no dark corners show
        }
    }

    // A low picket fence around some house lots only: along the footpath edge (gap at the path) and down both sides. See-through, no collider.
    void LayoutLowFence(float L, float doorZ, bool flip, float centerZ)
    {
        float h = Mathf.Repeat(Mathf.Sin(centerZ * 12.9898f + (side > 0f ? 0f : 78.233f)) * 43758.5453f, 1f);
        if (h > GameConfig.FenceChance) return;
        float half = L * 0.5f;
        Color wood = new Color(0.96f, 0.94f, 0.88f);
        LowFenceRun(side, -half + 0.3f, Mathf.Min(doorZ - 1.3f, half - 0.3f), wood);
        LowFenceRun(side, Mathf.Max(doorZ + 1.3f, -half + 0.3f), half - 0.3f, wood);
        // both sides of the lot, from the footpath edge back to the house wall, so the whole plot is fenced
        LowFenceSide(side, -half + 0.15f, wood);
        LowFenceSide(side, half - 0.15f, wood);
    }

    void LowFenceSide(float s, float z, Color col)
    {
        float x0 = 4.5f - setback;
        const float x1 = 8.4f;
        float len = x1 - x0;
        Part(new Vector3(s * (x0 + x1) * 0.5f, 0.38f, z), new Vector3(len, 0.07f, 0.05f), col, false);
        int posts = Mathf.RoundToInt(len / 1.1f) + 1;
        for (int i = 0; i < posts; i++)
            Part(new Vector3(s * (x0 + len * i / (posts - 1)), 0.22f, z), new Vector3(0.09f, 0.44f, 0.09f), col, false);
    }

    void LowFenceRun(float s, float z0, float z1, Color col)
    {
        float len = z1 - z0;
        if (len < 0.8f) return;
        float c = (z0 + z1) * 0.5f;
        Part(new Vector3(s * (4.5f - setback), 0.38f, c), new Vector3(0.05f, 0.07f, len), col, false);
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.1f) + 1);
        for (int i = 0; i < posts; i++)
            Part(new Vector3(s * (4.5f - setback), 0.22f, z0 + len * i / (posts - 1)), new Vector3(0.09f, 0.44f, 0.09f), col, false);
    }

    void RoadFence(float s, float z0, float z1, Color metal)
    {
        float len = z1 - z0;
        if (len < 0.6f) return;
        float c = (z0 + z1) * 0.5f;
        Part(new Vector3(s * 4.55f, 0.85f, c), new Vector3(0.06f, 0.06f, len), metal, false);
        Part(new Vector3(s * 4.55f, 0.45f, c), new Vector3(0.06f, 0.06f, len), metal, false);
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 2.2f) + 1);
        for (int i = 0; i < posts; i++)
            Part(new Vector3(s * 4.55f, 0.45f, z0 + len * i / (posts - 1)), new Vector3(0.1f, 0.9f, 0.1f), metal, false);
    }
}
