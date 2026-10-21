using System.Collections.Generic;
using UnityEngine;

// House (partial) - Floors: up to three storeys (ground, first, second), the balconies on the upper ones, the hoops on a floor,
// and how a customer is served.
//
//   Ground floor   the lawn, the path, the doormat, the mailbox and a wall hoop: hitting any of them serves it. A building site has no ground floor.
//   Upper floors   each has 1 to 3 balconies (a balcony can be wide, across several windows). A floor is served when every balcony of it is hit.
//   The hoop       every building with upper floors has ONE hoop, on the first floor: a dunk through it serves all the balconies of all the upper
//                  floors at once and pays them by hand plus 25%. The ground floor still has to be served on its own.
//   The customer   is served when EVERY floor is served. Driving past with a floor still open costs a life, as a missed customer always did.
//   Windows        breaking one is free on every floor (smashing things is fun, never a penalty).
//
// The main part of the class is in House.cs.
public partial class House
{
    public const float StoreyH = 3.5f;          // height of an upper storey
    public const float GroundStoreyH = 5.0f;    // the ground storey is taller (the wall hoop's board, up to 4.0 m, has to stay under the eaves)
    public const int MaxBalconies = 3;          // per upper floor
    public const int UpperWindowSlots = 4;      // window slots per upper floor

    // One balcony: slab, railing, door and the target zone above the slab. Pooled: three per upper floor, laid out again by every Setup.
    [System.Serializable]
    public class BalconySet
    {
        public Transform slab, rail, door;
        public Renderer slabR;
        public HouseTarget zone;
        public int floor;           // 1 or 2
        public bool on;             // this house uses it right now
        public bool hit;            // served (by a paper on it, or by a dunk through the floor's hoop)
        public float z, len;        // centre along the road, and its width
    }

    public BalconySet[] balconies;              // [(floor - 1) * MaxBalconies + k]
    public HouseTarget[] floorHoopZ;            // [1]: the ring of the building's hoop (on the first floor); [2] is not used
    public Transform[] floorHoopSnap;           // [1]
    public Renderer[] pips;                     // [0..2]: one dot per floor on the customer marker, ground first

    // Set by SegmentSpawner before Setup (customers only; scenery picks its own floors).
    [System.NonSerialized] public int WantFloors = 1;
    [System.NonSerialized] public int WantBalc1 = 1, WantBalc2 = 1;
    [System.NonSerialized] public bool WantHoop;     // the building's hoop (a house with upper floors)

    int floors = 1;                              // storeys, ground included
    readonly int[] balcCount = new int[3];       // balconies planned per storey (1 and 2 are used)
    readonly bool[] floorHoopOn = new bool[3];
    readonly float[] floorHoopAt = new float[3]; // z of that floor's hoop
    readonly bool[] floorDone = new bool[3];     // [1], [2]: the upper floor is served
    bool groundDone;                             // the ground floor is served
    bool groundFloor = true;                     // false on a building site

    // Where floor f starts (0 = the ground). Shops keep their two even storeys; every other house has a taller ground floor.
    float FloorBase(int f)
    {
        if (f <= 0) return 0f;
        float g = shopKind != 0 ? StoreyH : GroundStoreyH;
        return g + (f - 1) * StoreyH;
    }

    float BodyH { get { return FloorBase(floors); } }

    // ---- planning ----

    // Decides the storeys of this lot, the balconies on each and which floors have a hoop.
    void PlanFloors(bool hasTargets, bool allowUpper, float length, bool park, bool hoopYard)
    {
        for (int f = 0; f < 3; f++) { balcCount[f] = 0; floorHoopOn[f] = false; floorDone[f] = false; floorHoopAt[f] = 0f; }
        groundDone = false;
        groundFloor = !(fort && GroundBlock == 2);
        floors = 1;
        if (park || isBeach || bare) return;
        if (shopKind != 0) { floors = 2; return; }   // shops keep their two storeys and have no balconies

        // how many balconies fit on one floor: 0.5 m clear at each end, 2 m wide at least, 0.3 m between two
        int cap = Mathf.Clamp(Mathf.FloorToInt((length - 1.0f + 0.3f) / 2.3f), 1, MaxBalconies);
        float wz = transform.position.z;

        if (hasTargets)
        {
            if (allowUpper && !hoopYard) floors = Mathf.Clamp(WantFloors, 1, 3);   // (a yard with its own hoop keeps the house to one floor: one hoop per building)
            if (!groundFloor && floors < 2) floors = 2;   // a building site has no ground floor to serve: it needs an upper one
            balcCount[1] = floors >= 2 ? Mathf.Clamp(WantBalc1, 1, cap) : 0;
            balcCount[2] = floors >= 3 ? Mathf.Clamp(WantBalc2, 1, cap) : 0;
            floorHoopOn[1] = floors >= 2 && WantHoop;
            return;
        }

        // scenery: the floors and balconies are only for the look. Low houses are more common early on.
        float p1 = GameConfig.SceneryOdds(wz, 0.85f), p2 = GameConfig.SceneryOdds(wz, 0.35f);
        float h1 = Hash01(wz * 0.613f + (side > 0 ? 4.1f : 27.9f));
        float h2 = Hash01(wz * 1.377f + (side > 0 ? 9.3f : 51.7f));
        if (h1 < p1) { floors = 2; if (h2 < p2) floors = 3; }
        for (int f = 1; f < floors; f++)
        {
            float hb = Hash01(wz * 2.719f + f * 13.7f + (side > 0 ? 1.7f : 33.1f));
            if (hb >= GameConfig.BalconyChance) continue;
            float he = Hash01(wz * 5.31f + f * 3.3f + 0.7f);
            int n = 1;
            if (he < GameConfig.ExtraBalconyChance && cap >= 2) n = 2;
            if (n == 2 && Hash01(he * 91.7f + 2.2f) < 0.4f && cap >= 3) n = 3;
            balcCount[f] = n;
        }
    }

    // ---- balconies ----

    // Places the balconies of every upper floor (and switches the unused ones off). Called by Setup right after Layout.
    void LayoutBalconies(float L, float doorZ, bool targets)
    {
        if (balconies == null) return;
        float half = L * 0.5f;
        for (int i = 0; i < balconies.Length; i++) SetBalconyShown(balconies[i], false);
        for (int f = 1; f <= 2; f++)
        {
            if (floorHoopZ != null && floorHoopZ[f] != null) floorHoopZ[f].gameObject.SetActive(false);
        }

        for (int f = 1; f < floors; f++)
        {
            int c = Mathf.Min(balcCount[f], MaxBalconies);
            float usable = L - 1.0f, gap = 0.3f;
            for (int k = 0; k < c; k++)
            {
                var b = balconies[(f - 1) * MaxBalconies + k];
                float w, z;
                if (c == 1)
                {
                    // one balcony: usually the size of a door, sometimes wide, in front of several windows
                    bool wide = usable >= 4.4f && Hash01(transform.position.z * 0.877f + f * 5.3f + (side > 0 ? 3.3f : 40.7f)) < 0.45f;
                    float wideLen = 4.6f + (Hash01(transform.position.z * 1.931f + f * 2.1f) < 0.4f ? 2.2f : 0f);
                    w = wide ? Mathf.Min(usable, wideLen) : Mathf.Min(2.6f, usable);
                    z = Mathf.Clamp(doorZ, -half + 0.1f + w * 0.5f, half - 0.1f - w * 0.5f);
                }
                else
                {
                    float sw = (usable - gap * (c - 1)) / c;
                    w = Mathf.Min(sw, 3.4f);
                    z = -half + 0.5f + sw * (k + 0.5f) + gap * k;
                }
                PlaceBalcony(b, f, w, z, targets);
            }

            // a floor with no balcony at all cannot be served by hand: count it as served
            if (c == 0 || FloorBalconiesAllHit(f)) floorDone[f] = true;

        }

        // the building's hoop hangs over a balcony of the first floor; with a second floor above, it keeps out from under that floor's balconies
        if (floorHoopOn[1])
        {
            var b0 = balconies[0];
            if (!b0.on) floorHoopOn[1] = false;
            else
            {
                float hz = b0.z;
                float reach = b0.len * 0.5f - 0.5f;
                if (floors >= 3)
                {
                    for (int t = 0; t < 9; t++)
                    {
                        float off = t == 0 ? 0f : ((t % 2 == 1) ? 1f : -1f) * ((t + 1) / 2) * 0.5f;
                        float z = b0.z + Mathf.Clamp(off, -reach, reach);
                        if (!UnderSlab(2, z, 0.8f)) { hz = z; break; }
                    }
                }
                else if (b0.len > 3.5f)
                    hz = b0.z + (Hash01(transform.position.z * 3.7f + 1.1f) < 0.5f ? -1f : 1f) * b0.len * 0.25f;
                floorHoopAt[1] = hz;
            }
        }
    }

    // True when a balcony of that floor stands over this z (plus a margin).
    bool UnderSlab(int f, float z, float margin)
    {
        for (int k = 0; k < MaxBalconies; k++)
        {
            var b = balconies[(f - 1) * MaxBalconies + k];
            if (b.on && Mathf.Abs(z - b.z) < b.len * 0.5f + margin) return true;
        }
        return false;
    }

    void PlaceBalcony(BalconySet b, int f, float w, float z, bool targets)
    {
        float s = side;
        float baseY = FloorBase(f);
        b.floor = f; b.z = z; b.len = w; b.hit = false;
        b.slab.localPosition = new Vector3(s * 7.7f, baseY + 0.45f, z);
        b.slab.localScale = new Vector3(1.6f, 0.15f, w);
        b.rail.localPosition = new Vector3(s * 6.95f, baseY + 0.95f, z);
        b.rail.localScale = new Vector3(1f, 1f, w / 2.6f);
        b.door.localPosition = new Vector3(s * 8.47f, baseY + 1.35f, z);
        SetZone(b.zone, new Vector3(s * 7.7f, baseY + 1.2f, z), new Vector3(1.5f, 1.4f, w));
        SetBalconyShown(b, true);
        b.zone.gameObject.SetActive(targets);
    }

    static void SetBalconyShown(BalconySet b, bool on)
    {
        b.on = on;
        if (b.slab != null) b.slab.gameObject.SetActive(on);
        if (b.rail != null) b.rail.gameObject.SetActive(on);
        if (b.door != null) b.door.gameObject.SetActive(on);
        if (!on && b.zone != null) b.zone.gameObject.SetActive(false);
    }

    // True when a balcony of the first floor hangs over this z (plus a margin): the ground floor keeps tall things (rings, trampolines) out from under it.
    bool BalconyAbove(float z, float margin)
    {
        if (balconies == null) return false;
        for (int k = 0; k < MaxBalconies; k++)
        {
            var b = balconies[k];
            if (b.on && Mathf.Abs(z - b.z) < b.len * 0.5f + margin) return true;
        }
        return false;
    }

    BalconySet FirstBalcony()
    {
        if (balconies == null) return null;
        for (int i = 0; i < balconies.Length; i++) if (balconies[i].on) return balconies[i];
        return null;
    }

    // ---- windows above the ground floor ----

    // A window cell on an upper floor is free unless it is under the door of a balcony (or beside a narrow one) or the floor's hoop.
    bool UpperWindowOk(int f, float zc)
    {
        if (balconies != null)
        {
            for (int k = 0; k < MaxBalconies; k++)
            {
                var b = balconies[(f - 1) * MaxBalconies + k];
                if (!b.on) continue;
                float clear = b.len <= 3.5f ? b.len * 0.5f + 0.6f : 1.0f;   // a wide balcony passes in front of its windows
                if (Mathf.Abs(zc - b.z) < clear) return false;
            }
        }
        if (floorHoopOn[f] && Mathf.Abs(zc - floorHoopAt[f]) < 1.3f) return false;
        return true;
    }

    // ---- floor hoops ----

    // The hoops of the upper floors: a board and ring on the wall above the floor's first balcony. Called after the lot's parts were reset.
    void LayoutFloorHoops()
    {
        if (floorHoopZ == null || floorHoopSnap == null) return;
        float s = side;
        for (int f = 1; f < floors; f++)
        {
            if (!floorHoopOn[f] || floorHoopZ[f] == null) continue;
            float hz = floorHoopAt[f];
            float ringY = FloorBase(f) + 2.3f;
            float ringX = s * 7.88f;
            WallBoard(ringY, 0.4f, 1.05f, hz);
            RingParts(ringX, ringY, hz);
            SetZone(floorHoopZ[f], new Vector3(ringX, ringY, hz), new Vector3(0.9f, 0.6f, 0.9f));
            floorHoopZ[f].openingHalf = 0f;
            floorHoopSnap[f].localPosition = new Vector3(ringX, ringY - 0.5f, hz);
            floorHoopZ[f].gameObject.SetActive(Subscriber);
        }
    }

    // ---- serving ----

    bool AllServed()
    {
        if (groundFloor && !groundDone) return false;
        for (int f = 1; f < floors; f++) if (!floorDone[f]) return false;
        return true;
    }

    int FloorsLeft()
    {
        int n = groundFloor && !groundDone ? 1 : 0;
        for (int f = 1; f < floors; f++) if (!floorDone[f]) n++;
        return n;
    }

    bool FloorBalconiesAllHit(int f)
    {
        if (balconies == null) return true;
        for (int k = 0; k < MaxBalconies; k++)
        {
            var b = balconies[(f - 1) * MaxBalconies + k];
            if (b.on && !b.hit) return false;
        }
        return true;
    }

    // Toast text after a part of a house with several floors was served.
    string LeftSuffix(bool all)
    {
        if (floors <= 1) return null;
        if (all) return "  House served!";
        int left = FloorsLeft();
        return "  " + left + (left == 1 ? " floor to go" : " floors to go");
    }

    // A paper touched a floor slab from above (called by Paper on contact). If it is the slab of a balcony that is still open, the balcony counts right now.
    // Returns true when it did. A balcony that is already done, or any slab of a house that is not a customer's, is left to the normal landing.
    public bool TouchBalconyFloor(Collider c, Vector3 pos)
    {
        if (balconies == null || !Subscriber || resolved) return false;
        for (int i = 0; i < balconies.Length; i++)
        {
            var b = balconies[i];
            if (!b.on || b.slab == null || b.slab != c.transform) continue;
            if (b.hit || floorDone[b.floor]) return false;
            HandleBalcony(b.zone, pos, true);
            return true;
        }
        return false;
    }

    // A paper came to rest on a balcony (a paper that touched the floor has already counted: see TouchBalconyFloor).
    public void HandleBalcony(HouseTarget zone, Vector3 pos, bool snapped)
    {
        var gm = GameManager.Instance;
        if (gm == null || !Subscriber) return;
        if (resolved) { gm.ShowToast("Already handled"); return; }
        BalconySet b = balconies != null && zone != null && zone.index >= 0 && zone.index < balconies.Length ? balconies[zone.index] : null;
        if (b == null || !b.on) return;
        if (b.hit || floorDone[b.floor]) { gm.ShowToast("Balcony already done"); return; }

        b.hit = true;
        if (b.slabR != null) b.slabR.material.color = Gold;
        if (FloorBalconiesAllHit(b.floor)) floorDone[b.floor] = true;
        bool all = AllServed();
        if (all) Finish(Gold);
        UpdatePips();
        gm.Delivered(Scoring.Points(HouseTarget.Kind.Balcony, snapped), pos, HouseTarget.Kind.Balcony, all, LeftSuffix(all));
    }

    // A paper dropped through the building's ring: every balcony of every upper floor is served at once. It pays all of them by hand plus 25%
    // (less what the balconies already paid, so the upper floors never pay more than that in all). The ground floor is not part of it.
    void HandleFloorHoop(int f, Vector3 pos, bool trick)
    {
        var gm = GameManager.Instance;
        if (gm == null || !Subscriber) return;
        if (resolved || floors < 2 || UpperServed()) { gm.ShowToast("Balconies already done"); return; }

        int each = Scoring.Points(HouseTarget.Kind.Balcony, true);
        int count = 0, paid = 0;
        for (int i = 0; i < balconies.Length; i++)
        {
            var b = balconies[i];
            if (!b.on) continue;
            count++;
            if (b.hit) paid += each;
            b.hit = true;
            if (b.slabR != null) b.slabR.material.color = Gold;
        }
        for (int u = 1; u < floors; u++) floorDone[u] = true;
        int pay = Mathf.Max(each, Scoring.FloorHoop(count * each) - paid);
        bool all = AllServed();
        if (all) Finish(Gold);
        UpdatePips();
        Tutorial.OnBuildingHoop();
        gm.BasketScored();
        gm.Delivered(pay, pos, HouseTarget.Kind.Hoop, all, LeftSuffix(all));
        if (trick) gm.TrickBonus(pay * (GameConfig.TrickShotMultiplier - 1), "Drunk Dunkkk! +", pos);
    }

    // Every upper floor is served.
    bool UpperServed()
    {
        for (int f = 1; f < floors; f++) if (!floorDone[f]) return false;
        return true;
    }

    // Which upper floor's hoop this zone is (0 = none).
    int FloorHoopIndex(HouseTarget zone)
    {
        if (floorHoopZ == null || zone == null) return 0;
        for (int f = 1; f < floorHoopZ.Length; f++) if (floorHoopZ[f] == zone) return f;
        return 0;
    }

    // Is this zone still worth anything (the aim assist leaves served balconies, floors and the served ground floor alone)?
    public bool ZoneOpen(HouseTarget z)
    {
        if (z == null) return false;
        if (z.kind == HouseTarget.Kind.Balcony)
        {
            var b = balconies != null && z.index >= 0 && z.index < balconies.Length ? balconies[z.index] : null;
            return b != null && b.on && !b.hit && !floorDone[b.floor];
        }
        int hf = FloorHoopIndex(z);
        if (hf > 0) return floors >= 2 && !UpperServed();
        return groundFloor && !groundDone;
    }

    // ---- looks ----

    void SetBalconyColors(Color c)
    {
        if (balconies == null) return;
        for (int i = 0; i < balconies.Length; i++)
            if (balconies[i].on && balconies[i].slabR != null) balconies[i].slabR.material.color = c;
    }

    // The ground floor is served but the house is not done: its targets turn gold and stop shimmering.
    void MarkGroundServed()
    {
        if (pathRenderer == null && pathVisualT != null) pathRenderer = pathVisualT.GetComponent<Renderer>();
        if (padRenderer != null && !paved) padRenderer.material.color = GameConfig.TurfColor;   // the served lawn turns into plain grass
        if (matRenderer != null) matRenderer.material.color = Gold;
        if (mailboxRenderer != null) mailboxRenderer.material.color = Gold;
        if (pathRenderer != null) pathRenderer.material.color = InactivePath;
        ShowDoorParts(false);
        UpdateTurf();
    }

    // After the status colours were set to the delivery-pad colour: what is already served stays gold.
    void ReapplyServed()
    {
        if (groundDone) MarkGroundServed();
        if (balconies == null) return;
        for (int i = 0; i < balconies.Length; i++)
            if (balconies[i].on && balconies[i].hit && balconies[i].slabR != null) balconies[i].slabR.material.color = Gold;
    }

    // One dot per floor on the customer marker, ground at the bottom: yellow while the floor is open, green when served (the same colours as the balcony glows).
    void UpdatePips()
    {
        if (pips == null) return;
        bool show = Subscriber && !resolved && floors > 1;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            bool exists = show && i < floors && (i > 0 || groundFloor);
            pips[i].gameObject.SetActive(exists);
            if (!exists) continue;
            bool done = i == 0 ? groundDone : floorDone[i];
            pips[i].material.color = done ? AuraDone : AuraOpen;
        }
    }
}
