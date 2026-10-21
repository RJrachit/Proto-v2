using System.Collections.Generic;
using UnityEngine;

// SegmentSpawner (partial) - Tutorial
// The main part of the class is in SegmentSpawner.cs.
public partial class SegmentSpawner
{
    // ---- tutorial ----

    // The lot on the given side that stands nearest the 26 m mark of its stretch (a long run-up of 17 m fits in front of it, and the whole house stays inside the stretch).
    Lot PickTutorialLot(int side)
    {
        Lot best = null;
        float bestD = 1e9f;
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
