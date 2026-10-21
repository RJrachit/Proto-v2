using System.Collections.Generic;
using UnityEngine;

// The way the sea leaves the road after a beach ends: instead of stopping dead at the end of the last beach lot, the coastline sweeps
// away from the road in a smooth curve across the next stretch, with the sea always visible on the outside of it, and the land between
// the road and the coast is a headland of dense palms, shrubs, grass and rocks. Decoration only: no colliders, no targets.
// A few kits are shared between all segments (see SegmentSpawner); one is laid out on the stretch that follows a beach.
public class BeachTail
{
    Transform holder;
    readonly List<Transform> parts = new List<Transform>();
    readonly List<Renderer> rends = new List<Renderer>();
    int n;
    int side;
    float segLen;
    public bool InUse;

    public static BeachTail Create(Transform parent)
    {
        var k = new BeachTail();
        var h = new GameObject("BeachTail").transform;
        h.SetParent(parent, false);
        h.gameObject.SetActive(false);
        k.holder = h;
        return k;
    }

    public void Attach(Transform segmentRoot)
    {
        InUse = true;
        holder.SetParent(segmentRoot, false);
        holder.localPosition = Vector3.zero;
        holder.localRotation = Quaternion.identity;
        holder.gameObject.SetActive(true);
    }

    public void Release(Transform spare)
    {
        InUse = false;
        holder.gameObject.SetActive(false);
        holder.SetParent(spare, false);
    }

    // ---- boxes, reused from layout to layout ----
    Transform Part(Vector3 pos, Vector3 scale, Color c, Vector3 euler = default(Vector3))
    {
        if (n >= parts.Count)
        {
            var go = Prim.Make(PrimitiveType.Cube, "TailPart", holder, Vector3.zero, Vector3.one, Color.white);
            parts.Add(go.transform);
            rends.Add(go.GetComponent<Renderer>());
        }
        var t = parts[n];
        t.gameObject.SetActive(true);
        t.localPosition = pos;
        t.localEulerAngles = euler;
        t.localScale = scale;
        rends[n].material.color = c;
        n++;
        return t;
    }

    static float R(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    // where the coast is (distance from the road) z metres into the stretch: it starts where the beach's sea started and sweeps away
    static float CoastX(float z, float len)
    {
        float u = Mathf.Clamp01(z / len);
        return 17.5f + 57.5f * Mathf.Pow(u, 2.2f);
    }

    // one band of the coast: o1..o2 is measured from the coast line towards the sea (negative = land side), in a box turned along the coast
    void Band(Vector2 mid, Vector2 nrm, float yaw, float o1, float o2, float y, float h, Color c, float length)
    {
        Vector2 p = mid + nrm * ((o1 + o2) * 0.5f);
        Part(new Vector3(side * p.x, y, p.y), new Vector3(o2 - o1, h, length), c, new Vector3(0f, yaw, 0f));
    }

    public void Layout(int sideSign, float segmentLength, float seed)
    {
        side = sideSign;
        segLen = segmentLength;
        n = 0;
        Color sand = GameConfig.SandColor;
        Color wet = Color.Lerp(sand, new Color(0.45f, 0.38f, 0.28f), 0.35f);
        Color deep = new Color(0.22f, 0.58f, 0.82f), shallow = new Color(0.42f, 0.80f, 0.88f), foam = new Color(0.92f, 0.96f, 0.98f);

        // the coast: a chain of boxes turned along it (sea, shallows, foam, wet sand, dry sand)
        const int pieces = 12;
        for (int i = 0; i < pieces; i++)
        {
            float z0 = segLen * i / pieces, z1 = segLen * (i + 1) / pieces;
            Vector2 a = new Vector2(CoastX(z0, segLen), z0), b = new Vector2(CoastX(z1, segLen), z1);
            Vector2 d = b - a;
            float len = d.magnitude;
            d /= len;
            Vector2 nrm = new Vector2(d.y, -d.x);           // towards the sea
            Vector2 mid = (a + b) * 0.5f;
            float yaw = Mathf.Atan2(side * d.x, d.y) * Mathf.Rad2Deg;
            float L = len + 0.4f;
            Band(mid, nrm, yaw, 0f, 300f, 0.03f, 0.06f, deep, L);
            Band(mid, nrm, yaw, 0f, 2.8f, 0.036f, 0.06f, shallow, L);
            Band(mid, nrm, yaw, -0.1f, 0.35f, 0.07f, 0.05f, foam, L);
            Band(mid, nrm, yaw, -0.9f, 0f, 0.05f, 0.065f, wet, L);
            Band(mid, nrm, yaw, -4.2f, -0.8f, 0.045f, 0.07f, sand, L);
        }

        // the land: dense vegetation and rocks between the houses and the coast
        var r = new System.Random(unchecked((int)(seed * 13.1f)) + side * 7 + 55);
        float x, z;
        for (int i = 0, placed = 0; i < 40 && placed < 9; i++)
            if (Land(r, 5.0f, out x, out z)) { Palm(x, z, r); placed++; }
        for (int i = 0, placed = 0; i < 40 && placed < 12; i++)
            if (Land(r, 4.6f, out x, out z)) { Shrub(x, z, R(r, 0.9f, 1.6f), r); placed++; }
        for (int i = 0, placed = 0; i < 40 && placed < 14; i++)
            if (Land(r, 4.4f, out x, out z)) { Clump(x, z, r); placed++; }
        for (int i = 0; i < 10; i++)
        {
            // rocks: along the sand edge, a few out in the shallows, a few inland
            float rz = R(r, 0.8f, segLen - 1.0f);
            float rx;
            if (i < 5) rx = CoastX(rz, segLen) - R(r, 0.6f, 3.4f);
            else if (i < 7) rx = CoastX(rz, segLen) + R(r, 0.6f, 3.0f);
            else rx = R(r, 15.5f, Mathf.Max(16.5f, CoastX(rz, segLen) - 5f));
            Rock(rx, rz, r);
        }
        Finish();
    }

    // a random spot on the land, at least `margin` from the coast and clear of the house lots
    bool Land(System.Random r, float margin, out float x, out float z)
    {
        for (int k = 0; k < 12; k++)
        {
            z = R(r, 1.5f, segLen - 1.5f);
            float xmax = CoastX(z, segLen) - margin;
            if (xmax - 15.2f < 1.0f) continue;
            x = R(r, 15.2f, xmax);
            return true;
        }
        x = z = 0f;
        return false;
    }

    void Rock(float x, float z, System.Random r)
    {
        Color c = r.NextDouble() < 0.5 ? new Color(0.55f, 0.55f, 0.58f) : new Color(0.46f, 0.46f, 0.50f);
        float w = R(r, 0.6f, 1.6f);
        Part(new Vector3(side * x, w * 0.3f, z), new Vector3(w, w * 0.6f, w * R(r, 0.7f, 1.0f)), c, new Vector3(R(r, -6f, 6f), R(r, 0f, 90f), R(r, -8f, 8f)));
    }

    void Shrub(float x, float z, float size, System.Random r)
    {
        Color g1 = new Color(0.30f, 0.56f, 0.30f), g2 = new Color(0.40f, 0.66f, 0.34f), g3 = new Color(0.48f, 0.70f, 0.38f);
        float yaw = R(r, 0f, 80f);
        Part(new Vector3(side * x, size * 0.3f, z), new Vector3(size, size * 0.6f, size * 0.9f), g1, new Vector3(0f, yaw, 0f));
        Part(new Vector3(side * (x + size * 0.15f), size * 0.58f, z - size * 0.1f), new Vector3(size * 0.7f, size * 0.5f, size * 0.7f), g2, new Vector3(0f, yaw * 0.5f + 15f, 0f));
        Part(new Vector3(side * (x - size * 0.2f), size * 0.42f, z + size * 0.2f), new Vector3(size * 0.5f, size * 0.4f, size * 0.55f), g3, new Vector3(0f, yaw * 0.8f, 0f));
    }

    void Clump(float x, float z, System.Random r)
    {
        Color g = Color.Lerp(new Color(0.40f, 0.62f, 0.30f), new Color(0.58f, 0.72f, 0.34f), (float)r.NextDouble());
        for (int bl = 0; bl < 3; bl++)
            Part(new Vector3(side * (x + (bl - 1) * 0.1f), 0.24f, z + ((bl & 1) == 0 ? -0.06f : 0.06f)), new Vector3(0.07f, 0.48f, 0.07f), g, new Vector3((bl - 1) * 13f, bl * 50f, (bl - 1) * 11f));
    }

    void Palm(float x, float z, System.Random r)
    {
        float h = R(r, 4.2f, 6.6f);
        Color frond = new Color(0.28f, 0.56f, 0.28f);
        Part(new Vector3(side * x, h * 0.5f, z), new Vector3(0.38f, h, 0.38f), new Color(0.52f, 0.38f, 0.26f));
        float off = R(r, 0f, 50f);
        for (int f = 0; f < 7; f++)
        {
            Vector3 e = new Vector3(24f, off + f * (360f / 7f), 0f);
            Vector3 p = new Vector3(side * x, h, z) + Quaternion.Euler(e) * new Vector3(0f, 0f, 1.25f);
            Part(p, new Vector3(0.5f, 0.07f, 2.8f), frond, e);
        }
    }

    // switch off the boxes this layout does not use
    public void Finish()
    {
        for (int i = n; i < parts.Count; i++) parts[i].gameObject.SetActive(false);
    }
}
