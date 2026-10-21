using System.Collections.Generic;
using UnityEngine;

// SegmentSpawner (partial) - Tutorial
// The main part of the class is in SegmentSpawner.cs.
public partial class SegmentSpawner
{
    // ---- tutorial ----

    // The scripted house: the longest lot on the given side that stands wholly inside its stretch, near the middle of it. Its lawn is the biggest
    // (the door is put at the far end, see ApplyTutorialLot), so the lesson throws have plenty of room for a mistake.
    Lot PickTutorialLot(int side)
    {
        Lot best = null;
        float bestScore = -1e9f;
        foreach (var l in lots)
        {
            if (l.house.side != side || l.court || l.beach) continue;
            if (l.z - l.length * 0.5f < 2f || l.z + l.length * 0.5f > segmentLength - 1f) continue;
            float score = l.length * 10f - Mathf.Abs(l.z - 24f);
            if (score > bestScore) { bestScore = score; best = l; }
        }
        if (best != null) return best;
        float bestD = 1e9f;       // (no lot fits: the one nearest the 26 m mark)
        foreach (var l in lots)
        {
            if (l.house.side != side || l.court || l.beach) continue;
            float d = Mathf.Abs(l.z - 26f);
            if (d < bestD) { bestD = d; best = l; }
        }
        return best;
    }

    // The lots around the scripted houses are plain: a tree crown, a porch or a car in front of the next lot would cut off the throw.
    static void ClearTutorialNeighbour(Lot lot)
    {
        lot.park = false; lot.parking = false; lot.court = false; lot.destruct = 0;
        lot.tree = false; lot.porch = false; lot.table = false; lot.tableItems = false;
        lot.yard = House.Yard.Lawn;
    }

    // A plain lawn house with a mailbox by the road (the floors are set by SegmentSpawner.Place).
    static void ApplyTutorialLot(Lot lot, int step)
    {
        lot.park = false; lot.parking = false; lot.court = false; lot.destruct = 0;
        lot.tree = false; lot.porch = false; lot.table = false; lot.tableItems = false; lot.beach = false;
        lot.yard = House.Yard.Lawn;
        lot.mail = House.MailPos.RoadBeside;
        lot.door = House.DoorPos.Far;   // the door at the far end: the whole rest of the lot is lawn, one big target
    }

    // ---- the tower (step 3) ----

    // Where the tower's balconies stand along the house (0 = the middle of it, negative = the end the bike comes from), as (centre, width) pairs.
    // They all have the usual balcony width (House.StdBalconyW). The middle floor has two balconies one metre apart at the far end, with the
    // building's basketball hoop over the near one. The top floor's balcony (the long shot's target, first in the list) is at the bike's end,
    // clear of both, with a second one beside it, so nothing stands over the balconies below it.
    const float TowerBalcW = House.StdBalconyW, TowerEdge = 7.0f;   // (the longest house is 15 m: its balconies keep 0.5 m clear of each end)
    public const float TowerTopZ = -TowerEdge + TowerBalcW * 0.5f + 1.0f;
    public static readonly Vector2[] TowerMiddle =
    {
        new Vector2(TowerEdge - TowerBalcW * 1.5f - 1.0f, TowerBalcW),
        new Vector2(TowerEdge - TowerBalcW * 0.5f, TowerBalcW)
    };
    public static readonly Vector2[] TowerTop =
    {
        new Vector2(TowerTopZ, TowerBalcW),
        new Vector2(TowerTopZ + TowerBalcW + 0.3f, TowerBalcW)
    };

    // Grows the tower's lot to the longest a house can be and packs the lots beside it on the same side of the road around it, so nothing overlaps.
    // A neighbour that no longer fits is shortened (or taken out when it would be too short to be a house).
    void FitTutorialTower(Lot pick)
    {
        float gap = lotGap, T = GameConfig.MaxHouseLength;
        float slot = T + gap;
        pick.length = T;
        pick.z = Mathf.Clamp(pick.z, 2f + slot * 0.5f, segmentLength - 1f - slot * 0.5f);
        float start = pick.z - slot * 0.5f, end = pick.z + slot * 0.5f;

        var before = new List<Lot>();   // nearest to the tower first
        var after = new List<Lot>();
        foreach (var l in lots)
        {
            if (l == pick || l.house.side != pick.house.side) continue;
            if (l.z < pick.z) before.Insert(0, l); else after.Add(l);
        }
        PackBeside(before, start, 0f, -1, gap);
        PackBeside(after, end, segmentLength, 1, gap);
        lots.Sort((a, b) => a.z.CompareTo(b.z));
    }

    // Lays the lots out one after the other from the tower outwards (dir -1 = towards the start of the segment, +1 = towards its end).
    void PackBeside(List<Lot> group, float from, float limit, int dir, float gap)
    {
        float room = Mathf.Abs(limit - from), used = 0f;
        var keep = new List<Lot>();
        foreach (var l in group)
        {
            float len = Mathf.Min(l.length, room - used - gap);
            if (len < 3.5f) { l.house.Vacate(); lots.Remove(l); continue; }
            l.length = len;
            used += len + gap;
            keep.Add(l);
        }
        float extra = keep.Count > 0 ? (room - used) / keep.Count : 0f;
        float cursor = from;
        foreach (var l in keep)
        {
            l.length += Mathf.Clamp(extra, 0f, Mathf.Max(0f, GameConfig.MaxHouseLength - l.length));
            float sl = l.length + gap;
            l.z = cursor + dir * sl * 0.5f;
            cursor += dir * sl;
        }
    }

    public House TutorialHouse(int k) { House h; return tutHouse.TryGetValue(k, out h) ? h : null; }
    public float TutorialZ(int k) { float z; return tutZ.TryGetValue(k, out z) ? z : 0f; }

    // Where the bike should stand to try the scripted house again.
    public float TutorialRetryZ(int k)
    {
        float st, hz;
        if (!tutStart.TryGetValue(k, out st) || !tutZ.TryGetValue(k, out hz)) return bike != null ? bike.position.z : 0f;
        return Mathf.Max(hz - 17f, st + 8f);
    }

    // Sets the scripted house up again so it is a customer once more. The bike must already be back in front of it
    // (a house behind the bike counts as done when it is set up).
    public void TutorialRearm(int k)
    {
        System.Action a;
        if (tutRearm.TryGetValue(k, out a) && a != null) a();
    }
}
