using System.Collections.Generic;
using UnityEngine;

// One lot with a house on it. The same objects are re-used every time the road recycles, so Setup() lays the lot out again:
// length, where the door is, where the mailbox stands, which windows exist.
// Most houses are Scenery (no delivery targets). Customers carry a spinning marker and a green lawn: deliver to them.
// A house's targets:
//   Garden (big, easiest)   Path (counts, garden points)   Doorstep mat (medium)   Mailbox (small, hardest)   Balconies (upper floors)
// Windows are on every house, scenery included: breaking one is free (no penalty for smashing). Points live in Scoring.cs.
// A house has 1 to 3 floors (see House.Floors.cs); a customer must be served on every floor.
// A customer you drive past without delivering costs a life. Some residents walk around, moving the garden target.
public partial class House : MonoBehaviour
{
    public enum Role { Scenery, Customer }
    public enum DoorPos { Far, Middle, Near }                       // along the road: far = the end the bike drives towards
    public enum Yard { Lawn, Parking, Stall, Garage, Plants, Hoop, Shop }     // Lawn = the lawn is a delivery target; the rest are decoration
    public enum MailPos { RoadPast, RoadBeside, RoadFront, Gate, Wall }

    [System.Serializable]
    public class WindowSlot
    {
        public Transform visual;
        public Renderer glass;
        public HouseTarget zone;
        public bool broken;
        public Material own;            // the window's own material (the night swaps in a glowing shared one)
        public int type = 5;            // the window style (see House.Windows.cs)
        public float wd = 1.2f, ht = 1.0f;   // width along the road and height of the glass: a wide window takes two grid cells
    }

    // Customers currently on the road (used by the aim assist).
    public static readonly List<House> Customers = new List<House>();

    public int side;                    // -1 = left of the road, +1 = right
    public float LotLength;             // how long the lot is along the road (set by Setup; the tutorial outlines the floors by it)

    // ---- parts (assigned by SegmentSpawner) ----
    public Transform bodyT, bandT, roofT, frontDoorT, pathVisualT, lawnT;
    public Transform mailPostT, gatePostA, gatePostB, mailBoxT, mailSnapT;
    public Transform[] tableParts;      // table top + four legs
    public HouseTarget tableZone;       // landing on the table top is a delivery
    public WindowSlot[] tableItems;     // cups and plates: smash one and you lose a life
    public Destructible[] crateUnits;   // pool (built on demand): crates for pyramids
    public Destructible[] canUnits;     // pool (built on demand): cans for pyramids
    public HouseTarget hoopZone;        // the basketball ring opening
    public Transform hoopSnapT;
    public HouseTarget hoopZone2;       // second ring (basketball courts have one at each end)
    public Transform hoopSnap2T;
    public HouseTarget[] palmZones;     // one per palm crown on a beach lot (resting on top of a palm = a basket)
    int palmN;
    bool[] palmUsed;
    public Transform[] propCubes;       // pool of boxes used for cars, scooters, stalls, garages
    public Renderer[] propCubeRenderers;
    public Transform[] propSpheres;     // pool of bushes
    public Renderer[] propSphereRenderers;
    public Transform treeTrunkT, treeCrownT, porchRoofT, porchRoof2T, porchPillarA, porchPillarB;   // optional dressing (porchRoof2T: the second panel of the trick-shot roof)
    public HouseTarget gardenB, pathZone, doorstepZone, mailZone;
    public WindowSlot[] groundWindows;
    public WindowSlot[] upperWindows;

    public Renderer bodyRenderer;
    public Renderer padRenderer;
    public Renderer mailboxRenderer;
    public Renderer beaconRenderer;
    public Renderer matRenderer;        // doorstep mat
    public Transform doorZone;          // the main garden zone (the one that moves when the resident walks around)
    public Transform mailboxFlag;
    public HouseTarget gardenTarget;    // the main garden zone; it shrinks and moves when the resident walks around
    public GameObject walker;
    public GameObject beacon;
    public GameObject[] doorObjects;    // garden + lawn
    public GameObject[] doorstepObjects;
    public GameObject[] mailboxObjects; // box, flag, zone
    public HouseTarget[] zones;         // every delivery target zone of this house (windows excluded)
    [Tooltip("How far past the house the bike can get before a missed customer costs a life. Long enough for a paper thrown at the last moment to land and settle.")]
    public float missDistance = 18f;

    public bool Subscriber { get; private set; }
    public bool Resolved { get { return resolved; } }

    bool resolved;
    bool moving;
    float phase;
    Vector3 doorBase;
    Vector3 flagBase;
    float flagBottom = 1.225f;          // top of the mailbox box
    float gardenCenterZ;
    float gardenFullLen;
    float gardenSwing;
    bool flagUp;
    float flagHeight = 0.05f;

    static readonly Color Paved = new Color(0.62f, 0.62f, 0.60f);
    static readonly Color[] CarColors =
    {
        new Color(0.85f, 0.85f, 0.88f), new Color(0.15f, 0.55f, 0.55f), new Color(0.15f, 0.30f, 0.60f),
        new Color(0.20f, 0.20f, 0.22f), new Color(0.90f, 0.75f, 0.20f),
    };
    bool paved;
    Yard yardKind;                      // the yard this lot was set up with
    Vector3 yardSpot;                   // a spot above the yard dressing where a heart can hang
    bool yardSpotValid;
    Vector3 tableSpot;                  // above the table, for a heart
    bool tableSpotValid;
    bool hoopUsed, hoopUsed2;           // the swish bonus on a lot with no customer is paid once per ring
    bool isPark;                        // this lot is a small park (no house)
    const float MarkerAboveRoof = 3.2f;     // the customer marker's height above the walls (the roof ridge is 1.5 m up): clear of the roof, even at the largest size
    float setback;                                   // how far back from the footpath this lot stands (see Setup)
    public float ShopKey = float.NaN;   // where the shop dice are rolled (set once per lot, so stretching the lot does not change them)
    public bool NoShop;                              // the spawner keeps this lot from being a shop (the easy opening)
    public bool ForceShop;                           // the spawner makes this lot a shop (a filler where plain houses were removed)
    public bool IsBare { get { return bare; } }   // a plain scenery lot with no house (the spawner closes these gaps)
    bool bare;                          // a plain scenery lot with its house left out (see GameConfig.BareScenery)
    public int GroundBlock;             // set by the spawner before Setup: 1 = walled house, 2 = building site (lawn, path and doormat are out)
    public int BeachEdge;        // set by the spawner before Setup: 1 / 2 = the first / last lot of a beach (a dune lot)
    public float BeachGreen;     // 0..1 on a beach lot: how close it is to either end of the beach (more vegetation near the ends)
    bool fort;                          // this customer's ground targets are blocked (walled house or building site)
    int beachScene;                     // on a beach: 0 = sunbather (doormat target), 1 = picnic blanket, 2 = lifeguard hut on a platform, 3 = changing cabins (scenery only)
    bool isBeach;                       // this lot is a stretch of beach (no house; a sunbather's towel may be the customer)

    static readonly Color InactivePath = new Color(0.40f, 0.38f, 0.36f);   // paths of houses that are not (or no longer) a target
    static readonly Color Gold = new Color(1.00f, 0.85f, 0.20f);
    static readonly Color Grey = new Color(0.50f, 0.50f, 0.50f);
    static readonly Color GlassColor = new Color(0.65f, 0.85f, 1.00f);
    static readonly Color BrokenColor = new Color(0.25f, 0.25f, 0.28f);
    static readonly Color BeaconIdle = Color.white;
    static readonly Color BeaconReady = new Color(1.00f, 0.92f, 0.10f);

    // Lot layout, seen from above (the right side of the road; the left side is mirrored). z runs along the road.
    //   road | footpath x 3-4.4 | lot x 4.4-8.5 | house wall at x 8.5
    //   door strip (2 wide): the path x 4.4-7.0 (hit, garden points) and the doorstep mat x 7.0-8.4
    //   garden: the rest of the lot beside the door strip (two lawns when the door is in the middle)

    // Called by SegmentSpawner every time the house is (re)placed on the road.
    public void Setup(Role role, float centerZ, float length, DoorPos doorPos, MailPos mailPos, bool flip,
                      Yard yard, bool tree, bool porch, bool table, bool tableItems, bool park, bool parking, bool court, int destruct, bool walksAround, bool allowMailbox, bool allowBalcony, bool allowWindow, bool allowBlocker, bool beach, Color bodyColor)
    {
        gameObject.SetActive(true);
        LotLength = length;
        blobN = 0;
        lampN = 0;
        doorLamp = -1;
        transform.localPosition = new Vector3(0f, 0f, centerZ);

        // A park replaces the house on a scenery lot: lawn, path, benches, trees. Never a customer's lot.
        if (park) { role = Role.Scenery; yard = Yard.Lawn; tree = false; porch = false; table = false; tableItems = false; walksAround = false; }

        // A beach lot: sand and sea instead of a house. The customer (if any) is a sunbather; the doormat is the target in front of them.
        isBeach = beach && !park;
        if (isBeach)
        {
            yard = Yard.Lawn; tree = false; porch = false; table = false; tableItems = false; destruct = 0;
            allowBlocker = false;
            if (mailPos == MailPos.Wall) mailPos = MailPos.RoadBeside;
        }

        // Some scenery houses are a shop or a restaurant instead: a paved forecourt, awning, sign and lit windows. Never a customer.
        shopKind = 0;
        if (float.IsNaN(ShopKey)) ShopKey = transform.position.z;   // (the spawner clears it for every new lot; a lot that is stretched keeps its shop dice)
        float shopP = ForceShop ? 1f : (NoShop ? 0f : GameConfig.SceneryOdds(ShopKey, GameConfig.ShopChance));
        if (role == Role.Scenery && !park && !isBeach && length >= 4.2f && shopP > 0f)
        {
            float sh = Hash01(ShopKey * 0.731f + (side > 0 ? 3.7f : 61.9f));
            if (sh < shopP)
            {
                float k = sh / shopP;
                shopKind = k < 0.5f ? 1 : 2;
                yard = Yard.Shop; tree = false; porch = false; table = false; tableItems = false; walksAround = false; destruct = 0;
                bodyColor = ShopBody[(int)(k * 977f) % ShopBody.Length];
            }
        }

        bool hasTargets = role == Role.Customer;
        // Depth: some customers' houses and shops stand a little further back from the road than the rest (the whole lot moves, so everything
        // on it stays in the same place relative to the house). Never a park, court or beach, and not on the tutorial road.
        float depth = 0f;
        setback = 0f;
        if (GameConfig.DepthVariety && !Tutorial.Active && !park && !isBeach && (hasTargets || shopKind != 0))
        {
            float dh = Hash01(ShopKey * 1.377f + (side > 0 ? 8.3f : 71.1f));
            depth = dh < 0.5f ? 0f : (dh < 0.8f ? GameConfig.DepthSmall : GameConfig.DepthBig);
        }
        setback = depth;   // (the lawn, path, doormat, fences and road-side mailboxes reach out to the footpath over this distance)
        transform.localPosition = new Vector3(side * depth, 0f, centerZ);
        // A plain scenery house (no customer, no park, beach or shop) is not built at all: the lot stays an open yard with its trees, crates, tables,
        // cars and trampolines, and the back row shows behind it. Only customers' houses (and shops) stand on the street.
        bare = GameConfig.BareScenery && role == Role.Scenery && !park && !isBeach && shopKind == 0;
        if (bare) porch = false;
        beachScene = isBeach ? (BeachEdge != 0 ? 5 : PickBeachScene(hasTargets)) : 0;
        // A walled house is an ordinary house with a wall in front of its ground floor (its targets stay, behind the wall). A building site has no
        // ground floor at all: no lawn, path, doormat or mailbox, so only its upper floors have to be served.
        fort = hasTargets && GroundBlock != 0 && !park && !isBeach && !walksAround && !allowBlocker;
        if (fort)
        {
            yard = Yard.Lawn; tree = false; porch = false; table = false; tableItems = false; destruct = 0;
            if (mailPos == MailPos.Wall) mailPos = MailPos.RoadBeside;   // a wall mailbox would be behind the wall
        }
        // The floors of this lot (1 to 3 storeys), the balconies on the upper ones and the floor hoops: see House.Floors.cs.
        // A customer's house only gets upper floors when allowBalcony is set.
        PlanFloors(hasTargets, allowBalcony, length, park, yard == Yard.Hoop);
        // Some customers' houses get a throw blocker (billboard or trees) in front. It replaces the tree and yard props.
        blockerKind = 0;
        if (hasTargets && !park && allowBlocker)   // the spawner has already rolled the odds (and the per-stretch hazard cap)
        {
            blockerKind = Hash01(transform.position.z * 37.719f + (side > 0 ? 11.3f : 91.7f)) < 0.55f ? 1 : 2;
            yard = Yard.Lawn;
            tree = false;
        }
        bool noLawn = yard != Yard.Lawn;
        paved = noLawn || park || isBeach || (fort && GroundBlock == 2);
        yardKind = yard;
        Subscriber = hasTargets;
        moving = hasTargets && walksAround && !noLawn;
        flagUp = false;
        hoopUsed = false;
        hoopUsed2 = false;
        palmN = 0;
        if (palmZones != null)
        {
            if (palmUsed == null || palmUsed.Length != palmZones.Length) palmUsed = new bool[palmZones.Length];
            for (int i = 0; i < palmZones.Length; i++) { palmUsed[i] = false; palmZones[i].gameObject.SetActive(false); }
        }
        flagHeight = 0.05f;
        phase = Random.value * 6.28f;

        var bike = BikeForward.Instance;
        // Scenery, and houses already behind the bike at spawn time, never count.
        resolved = !hasTargets || (bike != null && bike.transform.position.z > transform.position.z);

        Customers.Remove(this);
        if (Subscriber && !resolved) Customers.Add(this);

        bodyBase = bodyColor;
        if (bodyRenderer != null) bodyRenderer.material.color = hasTargets && !resolved ? bodyColor : Muted(bodyColor);   // houses that are not a customer are a little duller, so the active ones stand out

        bool gardenBOn;
        bool wallMail;
        float mailZ;
        float doorZ;
        bool pathOn;
        const float d = 0f;
        Layout(length, doorPos, mailPos, flip, noLawn, d, out gardenBOn, out pathOn, out wallMail, out mailZ, out doorZ);
        LayoutBalconies(length, doorZ, hasTargets);
        if (matRenderer != null) matRenderer.transform.localScale = new Vector3(1.3f, 0.08f, 1.8f);   // the picnic blanket borrows the doormat and resizes it
        if (fort && GroundBlock == 1)
        {
            // A walled house is an ordinary house with a wall in front of its ground floor: the mailbox by the road still counts, and so does the
            // lawn, path and doormat behind the wall (they start inside it, x 5.6, so a paper has to be lobbed over).
            lawnT.localPosition = new Vector3(side * 7.0f, 0.04f, 0f);
            lawnT.localScale = new Vector3(2.8f, 0.08f, length);
            SetZone(gardenTarget, new Vector3(side * 7.0f, 0.45f, gardenCenterZ), new Vector3(2.8f, 0.9f, gardenFullLen));
            doorBase = new Vector3(side * 7.0f, doorBase.y, doorBase.z);
            if (gardenB != null) SetZone(gardenB, new Vector3(side * 7.0f, 0.45f, gardenB.transform.localPosition.z), new Vector3(2.8f, 0.9f, gardenB.size.z));
            SetZone(pathZone, new Vector3(side * 6.3f, 0.45f, doorZ), new Vector3(1.4f, 0.9f, 2.0f));
            pathVisualT.localPosition = new Vector3(side * 6.3f, 0.1f, doorZ);
            pathVisualT.localScale = new Vector3(1.4f, 0.04f, 1.8f);
        }
        if (beachScene != 0) { pathOn = false; if (pathVisualT != null) pathVisualT.gameObject.SetActive(false); }   // picnics, huts and cabins have no path
        float blockZ, blockHalf;
        carN = 0;
        boardGlowN = 0;   // (the boards laid out below register their glow again)
        LayoutYard(yard, wallMail, flip, out blockZ, out blockHalf);
        PlanWallPad(length, doorZ, wallMail, mailZ, hasTargets && yard == Yard.Lawn);
        PlanWindows();
        LayoutWindows(length, doorZ, wallMail, mailZ, allowWindow, d, blockZ, blockHalf);
        LayoutDressing(length, doorZ, flip, noLawn ? false : tree, porch, d);
        bool tableOn = LayoutTable(!noLawn && table, tableItems, flip);

        SetAll(doorObjects, hasTargets);
        if (lawnT != null) lawnT.gameObject.SetActive(hasTargets || noLawn);   // a paved yard shows on every house
        if (noLawn && padRenderer != null) padRenderer.material.color = Paved;
        if (fort && GroundBlock == 2 && padRenderer != null) padRenderer.material.color = new Color(0.55f, 0.45f, 0.33f);   // dirt on a building site
        if (noLawn && gardenTarget != null) gardenTarget.gameObject.SetActive(false);   // no lawn: the lot is a paved forecourt
        if (gardenB != null) gardenB.gameObject.SetActive(hasTargets && gardenBOn && !noLawn);
        SetAll(doorstepObjects, hasTargets);
        if (pathZone != null) pathZone.gameObject.SetActive(hasTargets && pathOn);
        bool mailOn = hasTargets && allowMailbox && !isBeach && groundFloor;   // no mailbox on a beach: the blanket, the hut platform or the doormat is the target (nor on a building site: it has no ground floor)
        SetAll(mailboxObjects, mailOn);
        SetActiveSafe(mailPostT, mailOn && (mailPos == MailPos.RoadPast || mailPos == MailPos.RoadBeside || mailPos == MailPos.RoadFront));
        SetActiveSafe(gatePostA, mailOn && mailPos == MailPos.Gate);
        SetActiveSafe(gatePostB, mailOn && mailPos == MailPos.Gate);
        if (tableZone != null) tableZone.gameObject.SetActive(hasTargets && tableOn);   // the table itself is a target only at a customer's
        if (walker != null) walker.SetActive(moving);
        if (beacon != null)
        {
            beacon.SetActive(Subscriber && !resolved);
            // the marker floats above the roof of the house, so it shows over the top of the building (a lot without a house, a beach, keeps it low beside the road)
            beacon.transform.localPosition = isBeach || park ? new Vector3(side * 3.6f, 3.4f, 0f) : new Vector3(side * 8.5f, BodyH + MarkerAboveRoof, 0f);
        }
        ApplyGarden();
        if (fort && GroundBlock == 2) DisableGround();   // a building site: no lawn, path or doormat (behind a wall they still count)

        isPark = park;
        SetShell(!park && !isBeach && !bare);
        if (bare && pathVisualT != null) { pathOn = false; pathVisualT.gameObject.SetActive(false); }   // (no house, so no path to its door)
        LayoutRoof(length);
        lotN = 0;
        bool special = false;
        parkTreeN = 0;
        if (park) special = LayoutPark(length, flip, parking, court);
        HideParkTreesFrom(parkTreeN);
        HideCarsFrom(carN);
        if (GameConfig.StreetLamps) LayoutStreetLamp(length, flip, mailOn ? mailZ : -999f);
        if (!park && !isBeach && !bare) DoorLight(doorZ);
        if (!park && !isBeach && !bare && !noLawn && blockerKind == 0 && GameConfig.FenceChance > 0f) LayoutLowFence(length, doorZ, flip, centerZ);
        LayoutBlocker(length, doorZ);
        LayoutDoorParts(doorZ, hasTargets && !park && beachScene == 0 && !(fort && GroundBlock == 2));
        bool lawnScenery = role == Role.Scenery && !park && !isBeach && shopKind == 0 && yard == Yard.Lawn && !tree && !tableOn && destruct == 0;
        float reverseZ;
        reverseHoop = PlanReverseHoop(length, doorZ, wallMail, mailZ, lawnScenery, out reverseZ);   // (a trampoline under a ring takes the wall)
        LayoutWallBushes(length, doorZ, hasTargets, wallMail, mailZ, !park && !isBeach && !bare && !noLawn && !reverseHoop);
        LayoutHouseTrim(length, doorZ, bodyColor, park || isBeach || bare);
        LayoutTrampoline(length, doorZ, lawnScenery && !reverseHoop);
        wallHoop = false;
        if (reverseHoop) LayoutReverseHoop(reverseZ);
        else if (!park && !isBeach && !bare && !wallPadOn && shopKind == 0 && !noLawn && !fort && yard == Yard.Lawn && !Tutorial.Active && !(hasTargets && floors > 1))   // (a building with upper floors has its own hoop)
            LayoutWallHoop(length, doorZ, wallMail, mailZ, hasTargets);
        LayoutFloorHoops();
        LayoutWallPad();
        shopQuadN = 0;
        shopPropN = 0;
        ResetBeachFx();
        PlanFacade(length, doorZ, bodyColor);   // (the look of the facade: House.Facade.cs; the shop's sign needs it)
        if (shopKind != 0) LayoutShop(length, doorZ);
        if (isBeach) LayoutBeach(length, doorZ, flip);
        if (fort) LayoutFort(length, doorZ);
        LayoutFacade(length, doorZ, porch);
        LayoutGrass(length, doorZ, !park && !noLawn && !isBeach && !bare && !fort, park && !special, tableOn);
        HideShopQuads();
        HideShopProps();
        HideLotParts();
        HideBoardGlows();
        HideBlobs();
        HideLamps();
        ApplyNight();
        LayoutDestruct(hasTargets || special || park ? 0 : destruct, park, flip, tableOn, yard, length);   // (a park is only trees: no crate stacks)

        if (resolved) SetStatus(Grey); else SetStatusPad();
        UpdatePips();
    }

    // The lot is not used this time round (the segment has fewer lots on this side): hide everything.
    public void Vacate()
    {
        Customers.Remove(this);
        resolved = true;
        Subscriber = false;
        gameObject.SetActive(false);
    }

    void Layout(float L, DoorPos doorPos, MailPos mailPos, bool flip, bool noLawn, float d,
                out bool gardenBOn, out bool pathOn, out bool wallMail, out float mailZ, out float doorZ)
    {
        float s = side;
        float half = L * 0.5f;
        doorZ = doorPos == DoorPos.Far ? half - 1f : (doorPos == DoorPos.Near ? -half + 1f : 0f);

        // shell (d = how much closer to the road the house stands)
        float hx = s * (11.5f - d);
        float bodyH = BodyH;   // a taller ground floor, then 3.5 m per upper floor (see FloorBase)
        bodyT.localPosition = new Vector3(hx, bodyH * 0.5f, 0f);
        bandT.localPosition = new Vector3(hx, FloorBase(1) + 0.05f, 0f);
        roofT.localPosition = new Vector3(hx, bodyH + 0.3f, 0f);
        bodyT.localScale = new Vector3(6f, bodyH, L);
        bandT.localScale = new Vector3(6.2f, 0.2f, L + 0.2f);
        roofT.localScale = new Vector3(6.8f, 0.6f, L + 0.8f);
        if (noLawn)
        {
            float wallX = 8.5f - d;
            lawnT.localPosition = new Vector3(s * (4.4f - setback + wallX) * 0.5f, 0.04f, 0f);   // paved forecourt
            lawnT.localScale = new Vector3(wallX - 4.4f + setback, 0.08f, L);
        }
        else
        {
            lawnT.localPosition = new Vector3(s * (6.45f - setback * 0.5f), 0.04f, 0f);
            lawnT.localScale = new Vector3(4f + setback, 0.08f, L);
        }

        // door, path and doorstep
        float pathW = (8.5f - d) - 5.9f;                 // 2.6 with a lawn, 0.5 without
        pathOn = pathW > 0.3f;
        frontDoorT.localPosition = new Vector3(s * (8.47f - d), 0.9f, doorZ);
        pathVisualT.localPosition = new Vector3(s * (4.4f - setback + (pathW + setback) * 0.5f), 0.1f, doorZ);
        pathVisualT.localScale = new Vector3(Mathf.Max(0.1f, pathW + setback), 0.04f, 1.8f);   // same width as the doormat base (1.8)
        pathVisualT.gameObject.SetActive(pathOn);
        SetZone(pathZone, new Vector3(s * (4.4f - setback + (pathW + setback) * 0.5f), 0.45f, doorZ), new Vector3(Mathf.Max(0.1f, pathW + setback), 0.9f, 2.0f));
        SetZone(doorstepZone, new Vector3(s * (7.7f - d), 0.45f, doorZ), new Vector3(1.4f, 0.9f, 2.0f));

        // garden: whatever is left of the lot beside the door strip. The bigger piece is the main garden.
        float lenA = Mathf.Max(0f, doorZ - 1f + half);       // between the near end and the door strip
        float lenB = Mathf.Max(0f, half - (doorZ + 1f));     // between the door strip and the far end
        float cA = (-half + doorZ - 1f) * 0.5f;
        float cB = (doorZ + 1f + half) * 0.5f;
        float mainLen, mainC, otherLen, otherC;
        if (lenA >= lenB) { mainLen = lenA; mainC = cA; otherLen = lenB; otherC = cB; }
        else { mainLen = lenB; mainC = cB; otherLen = lenA; otherC = cA; }
        SetZone(gardenTarget, new Vector3(s * (6.45f - setback * 0.5f), 0.45f, mainC), new Vector3(3.9f + setback, 0.9f, mainLen));
        gardenBOn = otherLen > 0.3f;
        if (gardenBOn) SetZone(gardenB, new Vector3(s * (6.45f - setback * 0.5f), 0.45f, otherC), new Vector3(3.9f + setback, 0.9f, otherLen));
        doorBase = gardenTarget.transform.localPosition;
        gardenCenterZ = mainC;
        gardenFullLen = mainLen;

        // mailbox
        float mx, my;
        float sgn = flip ? 1f : -1f;
        wallMail = mailPos == MailPos.Wall;
        switch (mailPos)
        {
            case MailPos.RoadPast:      // on the road edge, just past the end of the lot: a miss is not caught by the lawn
                mx = s * (4.7f - setback); my = 1.05f; mailZ = sgn * (half + 0.25f);
                break;
            case MailPos.RoadBeside:    // on the road edge, right beside the path
                mx = s * (4.7f - setback); my = 1.05f;
                mailZ = Mathf.Clamp(doorZ + sgn * 1.45f, -half - 0.25f, half + 0.25f);
                break;
            case MailPos.RoadFront:     // on the road edge, in front of the lawn
                mx = s * (4.7f - setback); my = 1.05f; mailZ = mainC;
                break;
            case MailPos.Gate:          // on a gate post at the entrance of the path
                mx = s * (4.6f - setback); my = 1.55f; mailZ = doorZ + sgn * 1.15f;
                break;
            default:                    // on the wall beside the door, next to a window
                mx = s * (8.2f - d); my = 1.4f;
                float wsgn = doorPos == DoorPos.Far ? -1f : (doorPos == DoorPos.Near ? 1f : sgn);
                mailZ = doorZ + wsgn * 1.8f;
                break;
        }
        mailBoxT.localPosition = new Vector3(mx, my, mailZ);
        if (mailPostT != null) mailPostT.localPosition = new Vector3(mx, 0.45f, mailZ);
        if (gatePostA != null) gatePostA.localPosition = new Vector3(mx, 0.7f, doorZ + sgn * 1.15f);
        if (gatePostB != null) gatePostB.localPosition = new Vector3(mx, 0.7f, doorZ - sgn * 1.15f);
        SetZone(mailZone, new Vector3(mx, my, mailZ), Tutorial.Active ? new Vector3(2.0f, 0.9f, 2.2f) : new Vector3(1.1f, 0.7f, 1.2f));   // (the tutorial's mailbox is easier to hit)
        mailSnapT.localPosition = new Vector3(mx, my, mailZ);
        flagBase = new Vector3(mx, 0f, mailZ - 0.3f);
        flagBottom = my + 0.175f;
        mailboxFlag.localPosition = new Vector3(flagBase.x, flagBottom + 0.025f, flagBase.z);
    }

    // Shows or hides the house itself (walls, roof, door, windows; the balconies are laid out by LayoutBalconies). A park has none of it.
    void SetShell(bool on)
    {
        SetActiveSafe(bodyT, on);
        SetActiveSafe(bandT, on && floors >= 2);
        SetActiveSafe(roofT, on);
        SetActiveSafe(frontDoorT, on && groundFloor);
        if (!on) { HideWindows(groundWindows, 0); HideWindows(upperWindows, 0); }
    }

    // A walking resident turns the lawn into a short target that sweeps along it; otherwise the zone is the whole lawn.
    void ApplyGarden()
    {
        float zoneLen = moving ? Mathf.Min(1.8f, gardenFullLen) : gardenFullLen;
        gardenSwing = (gardenFullLen - zoneLen) * 0.5f;
        if (gardenTarget != null)
        {
            var box = gardenTarget.GetComponent<BoxCollider>();
            Vector3 size = new Vector3(gardenTarget.size.x, gardenTarget.size.y, zoneLen);
            if (box != null) box.size = size;
            gardenTarget.size = size;
        }
        if (doorZone != null) doorZone.localPosition = new Vector3(doorBase.x, doorBase.y, gardenCenterZ);
    }

    void Update()
    {
        if (colorVersion != GameConfig.ColorVersion) RefreshColors();
        if (nightStep != NightLights.Step) ApplyNight();
        if (lgAnimT >= 0f) UpdateLifeguard();
        if (Subscriber && !resolved && statusIsPad) ShimmerTargets();
        else if (tfxOn) HideTargetGlows();
        if (Subscriber && !resolved && statusIsPad && (beachScene == 1 || beachScene == 2)) AnimateBeachTarget();
        else if (tGlow != null && tGlow.gameObject.activeSelf) tGlow.gameObject.SetActive(false);
        if (moving && doorZone != null)
        {
            float z = gardenCenterZ + Mathf.Sin(Time.time * 1.2f + phase) * gardenSwing;
            doorZone.localPosition = new Vector3(doorBase.x, doorBase.y, z);
        }

        // The path stones of an active customer glow in turn from the footpath to the door, and on over the mat (colour only).
        if (Subscriber && !resolved && statusIsPad && stoneCount > 0 && !groundDone) AnimateDoorStones();

        // Mailbox flag pops up after a delivery.
        if (mailboxFlag != null && mailboxFlag.gameObject.activeSelf)
        {
            flagHeight = Mathf.MoveTowards(flagHeight, flagUp ? 0.5f : 0.05f, Time.deltaTime * 3f);
            mailboxFlag.localScale = new Vector3(0.06f, flagHeight, 0.06f);
            mailboxFlag.localPosition = new Vector3(flagBase.x, flagBottom + flagHeight * 0.5f, flagBase.z);
        }

        var bike = BikeForward.Instance;
        var gm = GameManager.Instance;
        if (bike == null || gm == null) return;

        // Marker: always turns to face the camera (with a gentle sway), and grows, pulses and turns yellow when it's a good time to throw.
        if (beacon != null && beacon.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 f = cam.transform.forward; f.y = 0f;
                if (f.sqrMagnitude > 0.01f) beacon.transform.rotation = Quaternion.LookRotation(f.normalized) * Quaternion.Euler(0f, Mathf.Sin(Time.time * 2f + transform.position.z) * 16f, 0f);
            }
            float dz = transform.position.z - bike.transform.position.z;
            bool ready = dz > 1f && dz < 9f;
            float want = ready ? 1.6f * (1f + 0.06f * Mathf.Sin(Time.time * 9f)) : 1.15f;
            float size = Mathf.Lerp(beacon.transform.localScale.x, want, Time.deltaTime * 8f);
            beacon.transform.localScale = new Vector3(size, size, size);
            if (beaconRenderer != null) beaconRenderer.material.color = ready ? BeaconReady : BeaconIdle;   // the coin face
        }

        if (resolved || !Subscriber || !gm.Playing) return;
        if (bike.transform.position.z > transform.position.z + missDistance)
        {
            Finish(Grey);
            gm.MissedSubscriber();
        }
    }

    public void HandleHit(HouseTarget.Kind kind, Vector3 pos, bool snapped)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.NoteScorePos(pos);   // (where the coin dust starts)

        // A hoop at a house with no customer is just a trick shot: a small bonus, once.
        if (kind == HouseTarget.Kind.Hoop && !Subscriber)
        {
            if (hoopUsed) { gm.CountHit(); return; }
            hoopUsed = true;
            gm.Bonus(GameConfig.PointsHoopBonus, "Swish! +", pos);
            return;
        }

        if (resolved) return;
        if (!Subscriber) return;

        // on a beach, the path beside the sunbather's towel scores like the doormat (and the picnic blanket already does)
        if (isBeach && kind == HouseTarget.Kind.Path) kind = HouseTarget.Kind.Door;

        // Anything on the ground (lawn, path, doormat, mailbox, hoop...) serves the ground floor, once. A building site has none.
        if (!groundFloor) return;
        if (groundDone) return;
        groundDone = true;
        Tutorial.OnGroundCleared();

        if (kind == HouseTarget.Kind.Mailbox) flagUp = true;
        if (kind == HouseTarget.Kind.Lifeguard) lgAnimT = 0f;
        bool all = AllServed();
        if (all) { Finish(Gold); gm.AddRunCoins(floors); } else MarkGroundServed();   // a house cleared: one coin per storey
        UpdatePips();
        gm.Delivered(Scoring.Points(kind, snapped), pos, kind, all);
    }

    // Marks the house as done: hides the marker and removes it from the aim-assist list.
    void Finish(Color status)
    {
        resolved = true;
        Customers.Remove(this);
        if (beacon != null) beacon.SetActive(false);
        SetStatus(status);
        if (bodyRenderer != null) bodyRenderer.material.color = Muted(bodyBase);   // a house that is done (or missed) goes dull like the rest
        RefreshFacadeTint();   // ... and so do its window frames and trim
        ApplyNight();   // ... and its door and window lights go out
    }

    Color bodyBase;                     // the house's own wall colour
    // The wall colour of an inactive house: paler and a little darker.
    static Color Muted(Color c)
    {
        float h, s, v;
        Color.RGBToHSV(c, out h, out s, out v);
        return Color.HSVToRGB(h, s * 0.55f, v * 0.9f);
    }

    bool statusIsPad;                   // the status colour is the player's delivery-pad colour (so it follows option changes)
    int colorVersion = -1;

    Renderer pathRenderer;
    static void SetAll(GameObject[] objects, bool on)
    {
        if (objects == null) return;
        foreach (var g in objects)
            if (g != null) g.SetActive(on);
    }

    static void SetActiveSafe(Transform t, bool on)
    {
        if (t != null) t.gameObject.SetActive(on);
    }
}
