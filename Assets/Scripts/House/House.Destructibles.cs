using System.Collections.Generic;
using UnityEngine;

// House (partial) - Crate and can pyramids, breakable table items and the heart spots.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- destructibles: big layered pyramids of crates (kind 1) or cans (kind 2) on lots without a customer and in parks ----
    // Units are created the first time a lot needs them (pools below), so lots that never get a pyramid cost nothing.

    readonly List<Destructible> stack = new List<Destructible>();
    float stackUnitH = 0.7f;
    int clearedGroups;                  // bit mask: pyramids on this lot that have been cleared (a lot can hold two)
    static readonly Color[] CanColors = { new Color(0.95f, 0.80f, 0.20f), new Color(0.25f, 0.45f, 0.85f), new Color(0.8f, 0.8f, 0.82f) };

    Destructible Unit(Destructible[] pool, int i, PrimitiveType type, string name, Color color)
    {
        if (pool[i] == null)
        {
            var go = Prim.Make(type, name, transform, Vector3.zero, Vector3.one, color, true);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var d = go.AddComponent<Destructible>();
            d.owner = this;
            d.rend = go.GetComponent<Renderer>();
            pool[i] = d;
        }
        return pool[i];
    }

    void LayoutDestruct(int kind, bool park, bool flip, bool tableOn, Yard yard, float L)
    {
        stack.Clear();
        clearedGroups = 0;
        HideUnits(crateUnits);
        HideUnits(canUnits);
        if (kind == 0) return;

        float s = side;
        int crateN = 0, canN = 0, group = 0;
        if (park)
        {
            // Beside the path, clear of the pond: one pyramid, and a second on the other side of the path on longer lots.
            float zLo = 1.45f, zHi = L * 0.5f - 0.2f;
            float room = zHi - zLo;
            if (room < 1.0f) return;
            float zc = (zLo + zHi) * 0.5f * (flip ? -1f : 1f);
            Pyramid(kind, s * 7.8f, zc, room, group++, ref crateN, ref canN);
            if (L >= 7.5f) Pyramid(kind, s * 7.8f, -zc, room, group++, ref crateN, ref canN);
        }
        else
        {
            if (yard != Yard.Lawn || tableOn || gardenFullLen < 2.0f) return;   // only on a free lawn, never in the door strip
            Pyramid(kind, s * 6.3f, gardenCenterZ, gardenFullLen - 0.4f, group, ref crateN, ref canN);
        }
    }

    // Layers of crates (up to 5 high) or cans (up to 6 high), as wide as the room allows. Each layer has one unit fewer.
    void Pyramid(int kind, float x, float zc, float room, int group, ref int crateN, ref int canN)
    {
        if (kind == 1)
        {
            const float sp = 0.62f;
            int n = Mathf.Min(5, Mathf.FloorToInt(room / sp));
            if (n >= 2 && crateUnits != null)
            {
                stackUnitH = 0.6f;
                Vector3 sc = Vector3.one * 0.6f;
                Color wood = new Color(0.72f, 0.52f, 0.30f);
                for (int level = 0; level < n; level++)
                {
                    int count = n - level;
                    for (int k = 0; k < count; k++)
                    {
                        if (crateN >= crateUnits.Length) return;
                        float cz = zc + (k - (count - 1) * 0.5f) * sp;
                        AddUnit(Unit(crateUnits, crateN++, PrimitiveType.Cube, "Crate", wood), group, level, x, cz, sc, wood, GameConfig.CrateHits);
                    }
                }
                return;
            }
        }
        {
            const float sp = 0.34f;
            int n = Mathf.Min(6, Mathf.FloorToInt(room / sp));
            if (n < 3 || canUnits == null) return;
            stackUnitH = 0.4f;
            Vector3 sc = new Vector3(0.32f, 0.2f, 0.32f);
            int c = 0;
            for (int level = 0; level < n; level++)
            {
                int count = n - level;
                for (int k = 0; k < count; k++)
                {
                    if (canN >= canUnits.Length) return;
                    float cz = zc + (k - (count - 1) * 0.5f) * sp;
                    Color col = CanColors[c++ % CanColors.Length];
                    AddUnit(Unit(canUnits, canN++, PrimitiveType.Cylinder, "Can", col), group, level, x, cz, sc, col, 1);
                }
            }
        }
    }

    void AddUnit(Destructible u, int group, int level, float x, float z, Vector3 scale, Color color, int hits)
    {
        u.owner = this;
        u.group = group;
        u.crate = scale.y > 0.5f;
        u.Place(level, hits, new Vector3(x, level * stackUnitH + stackUnitH * 0.5f, z), scale, color);
        stack.Add(u);
    }

    static void HideUnits(Destructible[] units)
    {
        if (units == null) return;
        foreach (var u in units) if (u != null) u.gameObject.SetActive(false);
    }

    // Where (local y) a unit of this level should sit, given which rows of its own pyramid below it are still standing.
    public float StackY(int level, int group)
    {
        int eff = 0;
        for (int l = 0; l < level; l++)
        {
            bool any = false;
            foreach (var u in stack) if (u.group == group && u.level == l && u.Standing) { any = true; break; }
            if (any) eff++;
        }
        return eff * stackUnitH + stackUnitH * 0.5f;
    }

    // A unit of the stack was damaged (broken = it is gone). Pays out, may knock neighbours, and pays the refund when the stack is cleared.
    public void StackHit(Destructible unit, bool broken, bool chain)
    {
        // a shop prop: it just breaks (no points, no penalty)
        if (unit.group == -2)
        {
            if (broken)
            {
                if (unit.glass) Juice.WindowBreak(unit.transform.position);
                else Juice.Smash(unit.transform.position, unit.color, false);
                ApplyNight();
            }
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null) return;

        // a billboard: one hit breaks it, a few points, no stack logic
        if (unit.group < 0)
        {
            if (broken) gm.Smash(GameConfig.BillboardPoints, unit.transform.position, unit.color);
            return;
        }

        if (broken)
        {
            gm.Smash(unit.crate ? GameConfig.CratePoints : GameConfig.CanPoints, unit.transform.position, unit.color);

            if (!chain)
            {
                float p = unit.crate ? GameConfig.CrateChain : GameConfig.CanChain;
                Vector3 dir = (unit.transform.position - transform.position).normalized;
                var near = new List<Destructible>();
                foreach (var o in stack)
                {
                    if (o == unit || !o.Standing || Mathf.Abs(o.level - unit.level) > 1) continue;
                    if (Vector3.Distance(o.transform.localPosition, unit.transform.localPosition) > (unit.crate ? 1.2f : 0.8f)) continue;
                    near.Add(o);
                }
                foreach (var o in near)
                    if (Random.value < p) o.Hit(dir, true);
            }

            if ((clearedGroups & (1 << unit.group)) == 0)
            {
                bool any = false;
                int count = 0;
                foreach (var o in stack)
                {
                    if (o.group != unit.group) continue;
                    count++;
                    if (o.Standing) any = true;
                }
                if (!any)
                {
                    clearedGroups |= 1 << unit.group;
                    int papers = Mathf.Max(1, Mathf.RoundToInt(count * GameConfig.StackClearPapersPerUnit));
                    gm.StackCleared(papers, GameConfig.StackClearPoints + count * GameConfig.StackClearPointsPerUnit, unit.transform.position);
                }
            }
        }
    }

    // A paper smashed a cup or plate on a table.
    public void HitBreakable(HouseTarget zone, Vector3 pos)
    {
        var gm = GameManager.Instance;
        if (gm == null || tableItems == null) return;
        foreach (var it in tableItems)
        {
            if (it.zone != zone || it.broken) continue;
            it.broken = true;
            it.visual.localScale = new Vector3(it.visual.localScale.x, it.visual.localScale.y * 0.3f, it.visual.localScale.z);
            if (it.glass != null) it.glass.material.color = BrokenColor;
            zone.gameObject.SetActive(false);
            gm.BrokeDishes(pos);
            return;
        }
    }

    // Places where a heart can hang on this lot (world positions). Every house has some, customers or not:
    // above the balcony, on the porch roof, over the yard dressing (garage, car, stall, bushes), above a tree, low over a lawn.
    public void AddHeartSpots(List<Vector3> into)
    {
        float s = side;
        if (isPark || isBeach)
        {
            if (yardSpotValid) into.Add(transform.TransformPoint(yardSpot));
            return;
        }
        var hb = FirstBalcony();
        if (hb != null) into.Add(transform.TransformPoint(hb.zone.transform.localPosition));
        if (porchRoofT != null && porchRoofT.gameObject.activeSelf)
            into.Add(transform.TransformPoint(porchRoofT.localPosition + new Vector3(0f, 0.8f, 0f)));
        if (lawnTree != null && lawnTree.Active)
            into.Add(transform.TransformPoint(lawnTree.HeartSpot));
        if (tableSpotValid) into.Add(transform.TransformPoint(tableSpot));
        if (yardKind == Yard.Lawn) into.Add(transform.TransformPoint(new Vector3(s * 6.45f, 1.0f, gardenCenterZ)));
        else if (yardSpotValid) into.Add(transform.TransformPoint(yardSpot));
    }
}
