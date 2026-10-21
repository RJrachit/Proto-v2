using System.Collections.Generic;
using UnityEngine;

// Builds the endless road from primitives and recycles segments that fall behind the bike.
// Each segment = grass + road + a row of lots on each side (their own lengths, so the two sides differ)
// + a spot for a cow. Only some houses are customers; the rest are scenery. New mechanics are introduced gradually.
// The route is generated from a seed, so the Daily Route gives everyone the same road.
public partial class SegmentSpawner : MonoBehaviour
{
    public float segmentLength = 30f;
    public int segmentCount = 7;
    public int lotsPerSide = 3;          // most lots the right side of a segment can hold
    public int lotsLeft = 4;             // ... and the left side (the side view packs its one built side tighter)
    public float roadHalfWidth = 3f;

    [Header("Lots")]
    [Tooltip("Possible lot lengths along the road.")]
    public float[] lotLengths { get { return GameConfig.ChaseView ? GameConfig.LotLengths : GameConfig.SideLotLengths; } }
    // Empty grass between two lots.
    public float lotGap { get { return GameConfig.LotGap; } }

    [Header("Difficulty")]
    [Tooltip("Road distance between customers at the start and at full difficulty (randomised a little).")]
    public float customerGapStart = GameConfig.CustomerGapStart;
    public float customerGapEnd = GameConfig.CustomerGapEnd;

    [Header("House dressing (chance per lot once unlocked)")]
    [Range(0f, 1f)] public float treeChance = GameConfig.TreeChance;
    [Range(0f, 1f)] public float porchChance = GameConfig.PorchChance;
    [Range(0f, 1f)] public float yardChance = GameConfig.YardChance;     // lots whose yard is decoration (cars, stall, garage, plants) instead of a lawn target

    class Segment
    {
        public Transform root;
        public House[] left;
        public House[] right;
        public RoadHazard hazard;
        public GameObject crossing;     // zebra crossing and signals (on for some segments)
        public Puddles puddles;         // rain puddles (shown only while it rains)
        public HeartPickup heart;
        public BackRow backLeft;
        public BackRow backRight;
        public MistBank mistLeft;
        public MistBank mistRight;
        public BeachTail tailL;       // the headland and coast that follow a beach on each side (shared kits, see AcquireTail)
        public BeachTail tailR;
    }

    class Lot
    {
        public House house;
        public float z;                  // centre, along the segment
        public float length;
        public bool beach;
        public int edge;                 // on a beach: 1 = the first lot of the stretch, 2 = the last (a dune lot that eases into the houses)
        public House.DoorPos door;
        public House.MailPos mail;
        public bool flip;
        public House.Yard yard;
        public bool tree;
        public bool porch;
        public bool table;
        public bool tableItems;
        public bool park;                // a small park or car park instead of a house (scenery lots only)
        public bool parking;             // ...and it is a car park
        public bool court;               // ...or a basketball court (needs a long enough lot)
        public int destruct;             // 0 none, 1 crate stack, 2 can pyramid (scenery lots only)
        public Color color;
        public System.Action<float, float> redo;   // sets the lot up again at a new centre and length (when the gaps are closed)
    }

    readonly List<Segment> segments = new List<Segment>();
    readonly List<Lot> lots = new List<Lot>();
    readonly List<BeachTail> tailPool = new List<BeachTail>();
    readonly List<Vector3> heartSpots = new List<Vector3>();
    // tutorial: the scripted customer house of each stretch (stretch number -> house, where it stands, where its stretch starts, how to set it up again)
    readonly Dictionary<int, House> tutHouse = new Dictionary<int, House>();
    readonly Dictionary<int, float> tutZ = new Dictionary<int, float>();
    readonly Dictionary<int, float> tutStart = new Dictionary<int, float>();
    readonly Dictionary<int, System.Action> tutRearm = new Dictionary<int, System.Action>();
    Transform bike;
    float nextZ;
    int beachLeft, beachRight;          // stretches of road left in a running beach on each side
    float beachStartL, beachStartR;      // road distance where the running beach on each side started (for the vegetation near its ends)
    bool prevBeachL, prevBeachR;        // was this side a beach in the previous stretch (to find where a beach starts)
    public float FrontierZ { get { return nextZ; } }   // where the next stretch of road will be built (for the on-screen odds)
    float lastLotZ;
    float sinceCustomer;
    float nextGap;
    float[] pickedLengths;
    bool[] pickedCourt;                  // this lot was picked to be a basketball court (exactly court length, never stretched less)
    System.Random rng = new System.Random();

    static readonly Color[] HouseColors =
    {
        new Color(0.98f, 0.85f, 0.70f),
        new Color(0.75f, 0.90f, 0.95f),
        new Color(0.95f, 0.80f, 0.85f),
        new Color(0.85f, 0.95f, 0.80f),
        new Color(0.95f, 0.95f, 0.75f),
        new Color(0.30f, 0.62f, 0.60f),   // bolder walls, like the reference buildings (never red or orange: those are for the markers)
        new Color(0.90f, 0.74f, 0.36f),
        new Color(0.62f, 0.45f, 0.62f),
        new Color(0.40f, 0.52f, 0.72f),
    };

    public void Init(Transform bikeTransform)
    {
        bike = bikeTransform;
        pickedLengths = new float[Mathf.Max(lotsPerSide, lotsLeft)];
        pickedCourt = new bool[Mathf.Max(lotsPerSide, lotsLeft)];
        for (int i = 0; i < segmentCount; i++) segments.Add(Build());
        ResetRun(rng.Next());
    }

    // Re-lays the whole road from the start using the given seed.
    public void ResetRun(int seed)
    {
        rng = new System.Random(seed);
        tutHouse.Clear(); tutZ.Clear(); tutStart.Clear(); tutRearm.Clear();
        nextZ = -segmentLength;   // start one segment behind the bike
        beachLeft = beachRight = 0;
        prevBeachL = prevBeachR = false;
        lastLotZ = nextZ;
        sinceCustomer = 0f;
        nextGap = 50f;            // first customer shows up a little way ahead
        House.Customers.Clear();
        foreach (var s in segments) Place(s);
    }

    readonly List<Renderer> grassRenderers = new List<Renderer>();
    int grassVersion;

    void Update()
    {
        if (grassVersion != GameConfig.ColorVersion)
        {
            grassVersion = GameConfig.ColorVersion;
            foreach (var r in grassRenderers) if (r != null) r.material.color = GameConfig.GrassColor;
        }
        Wetness.Tick(Time.deltaTime);   // darken the road and paving while it rains (does nothing when the rain is steady)
        if (bike == null || segments.Count == 0) return;
        Color fogColor = RenderSettings.fogColor;
        foreach (var sg in segments)
        {
            if (sg.mistLeft.Active) sg.mistLeft.Tick(sg.root.position.z, bike.position.z, fogColor);
            if (sg.mistRight.Active) sg.mistRight.Tick(sg.root.position.z, bike.position.z, fogColor);
        }
        var first = segments[0];
        if (bike.position.z - first.root.position.z > segmentLength * 1.6f)
        {
            segments.RemoveAt(0);
            Place(first);
            segments.Add(first);
        }
    }

    // Moves a segment to the front of the road and re-rolls its contents.
    // a few shared kits for the coast that follows a beach; they are built the first time one is needed and handed back when the segment is re-laid
    BeachTail AcquireTail(Segment s)
    {
        BeachTail kit = null;
        foreach (var k in tailPool) if (!k.InUse) { kit = k; break; }
        if (kit == null && tailPool.Count < 4) { kit = BeachTail.Create(transform); tailPool.Add(kit); }
        if (kit != null) kit.Attach(s.root);
        return kit;
    }

    void Place(Segment s)
    {
        if (s.tailL != null) { s.tailL.Release(transform); s.tailL = null; }
        if (s.tailR != null) { s.tailR.Release(transform); s.tailR = null; }
        float startZ = nextZ;
        s.root.position = new Vector3(0f, 0f, startZ);
        nextZ += segmentLength;
        if (s.crossing != null) s.crossing.SetActive(!Tutorial.Active && RoadDetail.WantsCrossing(startZ));
        if (s.puddles != null) s.puddles.Layout(startZ, s.crossing != null && s.crossing.activeSelf);

        // Everything is a chance that rises with the distance (see GameConfig.SceneryOdds and GameplayOdds); gameplay extras wait PlainStart metres.
        float d = startZ;
        float diff = Mathf.Clamp01(d / GameConfig.DistanceAt(GameConfig.GameplayRampSeconds));
        float hz = GameConfig.HazardStart, tg = GameConfig.TargetStart;
        float walkerChance = GameConfig.GameplayOdds(d, GameConfig.WalkerChance, hz);
        float cowChance = GameConfig.GameplayOdds(d, GameConfig.CowChance, hz);
        float blockerChance = GameConfig.GameplayOdds(d, GameConfig.BlockerChance, hz);
        // floors of a customer's house: the chance of a first floor, of a second one on top, of extra balconies and of a floor hoop all start small and rise
        float firstFloorChance = GameConfig.GameplayOdds(d, GameConfig.BalconyTargetChance, tg);
        float secondFloorChance = GameConfig.GameplayOdds(d, GameConfig.SecondFloorChance, 0f);
        float extraBalconyChance = GameConfig.GameplayOdds(d, GameConfig.ExtraBalconyChance, 0f);

        float treeP = GameConfig.SceneryOdds(d, GameConfig.TreeChance);   // (not the serialized copy, so the setting in GameConfig always applies)
        float tableP = GameConfig.SceneryOdds(d, GameConfig.TableChance);
        float porchP = GameConfig.SceneryOdds(d, porchChance);
        float yardP = GameConfig.YardProps ? GameConfig.SceneryOdds(d, yardChance) : 0f;
        float parkP = GameConfig.SceneryOdds(d, GameConfig.ParkChance);
        float destP = GameConfig.SceneryOdds(d, GameConfig.DestructibleChance);
        if (!GameConfig.ChaseView)   // one built side: more yards, tables, parks and destructibles (trees and porches stay as they are)
        {
            float boost = GameConfig.SideDressingBoost;
            yardP = Mathf.Min(1f, yardP * boost); tableP = Mathf.Min(1f, tableP * boost);
            parkP = Mathf.Min(1f, parkP * boost); destP = Mathf.Min(1f, destP * boost);
        }

        // a stretch of road holds only so many hazards (a cow, a walking resident or a blocker); never two on one house
        int budget = GameConfig.HazardBudget;
        bool cow = rng.NextDouble() < cowChance && budget > 0;
        if (cow) budget--;
        if (Tutorial.Active) { cow = false; budget = 0; }   // the tutorial road has no hazards

        // a beach starts now and then (one side, rarely both) once the bike is far enough, and runs for a few stretches
        // (never straight after a beach has ended: the stretch after a beach is always city on both sides, which is what the dune ends and the coast headland assume.
        // The check comes after the dice so the route stays the same as before for everything else.)
        if (startZ >= GameConfig.BeachFromNow && beachLeft <= 0 && beachRight <= 0 && rng.NextDouble() < GameConfig.BeachChanceNow && !prevBeachL && !prevBeachR)
        {
            int span = Mathf.Max(1, GameConfig.BeachSegmentsMax - GameConfig.BeachSegmentsMin + 1);
            int len = GameConfig.BeachSegmentsMin + rng.Next(span);
            double r = rng.NextDouble();
            if (r < GameConfig.BeachBothShare) { beachLeft = len; beachRight = len; }
            else if (rng.NextDouble() < 0.5) beachLeft = len;
            else beachRight = len;
        }
        if (!GameConfig.ChaseView) beachRight = 0;   // the side view builds nothing on the right (it stands behind the camera)
        bool beachL = beachLeft > 0, beachR = beachRight > 0;
        if (beachLeft > 0) beachLeft--;
        if (beachRight > 0) beachRight--;
        // the first stretch of a beach and the last one each get a dune lot at the edge next to the houses
        int edgeL = beachL ? (!prevBeachL ? 1 : (beachLeft == 0 ? 2 : 0)) : 0;
        int edgeR = beachR ? (!prevBeachR ? 1 : (beachRight == 0 ? 2 : 0)) : 0;
        // mist stands where a beach starts (the segment's start) and where it ends (the segment's end)
        bool mistStartL = beachL && !prevBeachL, mistEndL = beachL && beachLeft == 0;
        bool mistStartR = beachR && !prevBeachR, mistEndR = beachR && beachRight == 0;
        if (mistStartL) beachStartL = startZ;
        if (mistStartR) beachStartR = startZ;
        bool taperL = prevBeachL && !beachL, taperR = prevBeachR && !beachR;   // the stretch right after a beach: the back row steps down
        prevBeachL = beachL;
        prevBeachR = beachR;

        // each side fills its own stretch of road with whichever lots fit
        lots.Clear();
        FillSide(s.left, treeP, porchP, yardP, tableP, beachL ? 0f : parkP, destP, beachL, edgeL);
        FillSide(s.right, treeP, porchP, yardP, tableP, beachR ? 0f : parkP, destP, beachR, edgeR, !GameConfig.ChaseView);
        lots.Sort((a, b) => a.z.CompareTo(b.z));

        // the tutorial road: stretches 1..4 each get one scripted customer (lawn, doormat, mailbox, hoop), the rest is plain scenery
        int tutK = Mathf.RoundToInt(startZ / segmentLength), tutStep = -1;
        Lot tutPick = null;
        if (Tutorial.Active && tutK >= 1 && tutK <= 3) { tutStep = tutK - 1; tutPick = PickTutorialLot(Tutorial.SideOfStep(tutStep)); }

        for (int li = 0; li < lots.Count; li++)
        {
            var lot = lots[li];
            float absZ = startZ + lot.z;
            sinceCustomer += Mathf.Max(0f, absZ - lastLotZ);
            lastLotZ = absZ;

            // Side view: never let a stretch with nothing to aim at run long. If waiting for the next lot would overshoot the gap by much,
            // this lot becomes the customer now (when it is at least 70% of the way there).
            bool due = sinceCustomer >= nextGap;
            if (!due && !GameConfig.ChaseView && sinceCustomer >= nextGap * 0.7f)
            {
                float nextAbsZ = li + 1 < lots.Count ? startZ + lots[li + 1].z : startZ + segmentLength + 6f;
                if (sinceCustomer + Mathf.Max(0f, nextAbsZ - absZ) > nextGap * 1.25f) due = true;
            }

            House.Role role = House.Role.Scenery;
            if (due && !lot.court && lot.edge == 0 && !Tutorial.Active)   // a court (or a dune lot) is never a customer: the next lot is
            {
                role = House.Role.Customer;
                sinceCustomer = 0f;
                nextGap = Mathf.Lerp(customerGapStart, customerGapEnd, diff) * (0.8f + 0.4f * (float)rng.NextDouble());
            }

            bool tutorialLot = tutPick != null && lot == tutPick;
            if (tutorialLot) { role = House.Role.Customer; ApplyTutorialLot(lot, tutStep); }
            else if (Tutorial.Active && tutK >= 0 && tutK <= 3) ClearTutorialNeighbour(lot);   // no tree, porch or yard prop beside a house the player must hit

            bool walks = false, blocks = false, balcony = true;   // mailboxes and windows are always there (balcony = upper floors allowed)
            int groundBlock = 0;                                    // 1 = walled house (a wall in front of the ground floor), 2 = building site (no ground floor)
            int wantFloors = 1, wantB1 = 1, wantB2 = 1;
            bool wantH = false;
            if (role == House.Role.Customer && !tutorialLot)
            {
                double walkRoll = rng.NextDouble(), blockRoll = rng.NextDouble();
                if (budget > 0 && walkRoll < walkerChance) { walks = true; budget--; }
                else if (budget > 0 && blockRoll < blockerChance) { blocks = true; budget--; }
                // floors: first a first floor, then maybe a second on top; each upper floor has one balcony and maybe a second and a third
                double floorRoll = rng.NextDouble(), secondRoll = rng.NextDouble();
                if (floorRoll < firstFloorChance) wantFloors = secondRoll < secondFloorChance ? 3 : 2;
                double e1 = rng.NextDouble(), e1b = rng.NextDouble(), e2 = rng.NextDouble(), e2b = rng.NextDouble();
                wantB1 = 1 + (e1 < extraBalconyChance ? (e1b < extraBalconyChance * 0.5f ? 2 : 1) : 0);
                wantB2 = 1 + (e2 < extraBalconyChance ? (e2b < extraBalconyChance * 0.5f ? 2 : 1) : 0);
                wantH = true;   // every building with upper floors has a hoop
                double fortRoll = rng.NextDouble();
                float walledP = GameConfig.GameplayOdds(d, GameConfig.WalledChance, hz), siteP = GameConfig.GameplayOdds(d, GameConfig.ConstructionChance, hz);
                if (!walks && !blocks && !lot.beach)
                    groundBlock = fortRoll < walledP ? 1 : (fortRoll < walledP + siteP ? 2 : 0);
                if (groundBlock == 2 && wantFloors < 2) wantFloors = 2;   // a building site has no ground floor: it needs an upper one
            }
            if (tutorialLot)
            {
                // stretch 1: a plain one-floor house; 2: two floors, one balcony; 3: two floors, two balconies and the building's hoop
                balcony = tutStep >= 1;
                wantFloors = tutStep >= 1 ? 2 : 1;
                wantB1 = tutStep >= 2 ? 2 : 1;
                wantH = tutStep >= 2;
            }
            lot.house.BeachEdge = lot.beach ? lot.edge : 0;
            // how close to either end of its beach this lot is (0 = deep in the middle, 1 = at an end): the vegetation grows thicker towards the ends
            bool leftSide = lot.house.side < 0;
            float dFromStart = absZ - (leftSide ? beachStartL : beachStartR);
            float dToEnd = startZ + segmentLength * (1 + (leftSide ? beachLeft : beachRight)) - absZ;
            lot.house.BeachGreen = lot.beach ? 1f - Mathf.Clamp01(Mathf.Min(dFromStart, dToEnd) / 60f) : 0f;
            lot.house.GroundBlock = groundBlock;
            lot.house.WantFloors = wantFloors;
            lot.house.WantBalc1 = wantB1; lot.house.WantBalc2 = wantB2;
            lot.house.WantHoop = wantH;
            lot.house.ShopKey = float.NaN;
            lot.house.ForceShop = false;
            lot.house.Setup(role, lot.z, lot.length, lot.door, lot.mail, lot.flip, lot.yard, lot.tree, lot.porch, lot.table, lot.tableItems, lot.park && role == House.Role.Scenery, lot.parking, lot.court, role == House.Role.Scenery ? lot.destruct : 0,
                            walks, true, balcony, true, blocks, lot.beach, lot.color);

            {
                var lt2 = lot; var r2 = role; bool wk2 = walks, bl2 = blocks, bc2 = balcony;
                lot.redo = (zz, ll) => lt2.house.Setup(r2, zz, ll, lt2.door, lt2.mail, lt2.flip, lt2.yard, lt2.tree, lt2.porch, lt2.table, lt2.tableItems,
                                                       lt2.park && r2 == House.Role.Scenery, lt2.parking, lt2.court, r2 == House.Role.Scenery ? lt2.destruct : 0,
                                                       wk2, true, bc2, true, bl2, lt2.beach, lt2.color);
            }

            if (tutorialLot)
            {
                var hs = lot.house; var lt = lot; bool wk = walks, bl = blocks, bc = balcony;
                tutHouse[tutK] = hs; tutZ[tutK] = absZ; tutStart[tutK] = startZ;
                tutRearm[tutK] = () => hs.Setup(House.Role.Customer, lt.z, lt.length, lt.door, lt.mail, lt.flip, lt.yard, lt.tree, lt.porch, lt.table, lt.tableItems,
                                                false, false, false, 0, wk, true, bc, true, bl, false, lt.color);
            }
        }

        if (GameConfig.CloseGaps && !Tutorial.Active) CloseGaps();

        s.hazard.Setup(cow);
        PlaceHeart(s, startZ);
        if (Tutorial.Active) s.heart.Hide();   // nothing to pick up while learning

        // buildings and trees behind the houses (their own dice, so the route is not affected)
        var br = new System.Random(unchecked((int)(startZ * 13.7f)) + 4242);
        s.backLeft.SetVisible(GameConfig.BackRows && !beachL);     // the sea has nothing behind it
        s.backRight.SetVisible(GameConfig.BackRows && !beachR && GameConfig.ChaseView);
        if (GameConfig.BackRows)
        {
            if (!beachL) s.backLeft.Layout(br, segmentLength, taperL ? 1 : 0);
            if (!beachR && GameConfig.ChaseView) s.backRight.Layout(br, segmentLength, taperR ? 1 : 0);
        }

        // fog banks at the two ends of a beach (their own dice, so the route is not affected)
        var mr = new System.Random(unchecked((int)(startZ * 7.1f)) + 777);
        s.mistLeft.Show(mr, mistStartL, mistEndL, segmentLength);
        s.mistRight.Show(mr, mistStartR, mistEndR, segmentLength);

        // right after a beach the sea sweeps away from the road and the land beside it is a headland of palms, shrubs and rocks
        if (taperL) { s.tailL = AcquireTail(s); if (s.tailL != null) s.tailL.Layout(-1, segmentLength, startZ); }
        if (taperR) { s.tailR = AcquireTail(s); if (s.tailR != null) s.tailR.Layout(1, segmentLength, startZ); }
    }
}
