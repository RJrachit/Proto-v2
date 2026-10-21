using System.Collections.Generic;
using UnityEngine;

// House (partial) - Yard dressing: paved yards, garden table, trees, porches, trampoline and wall bushes.
// The main part of the class is in House.cs.
public partial class House
{
    // Yard dressing for lots whose lawn is not a target. Everything stands in the yard beside the door strip, never in it,
    // so a straight throw from level with the door always reaches the path and the doorstep. Garage (against the wall)
    // is dropped when a wall mailbox is there; anything that does not fit becomes a row of plants.
    void LayoutYard(Yard yard, bool wallMail, bool flip, out float blockZ, out float blockHalf)
    {
        blockZ = 0f;
        blockHalf = 0f;
        yardSpotValid = false;
        if (hoopZone != null) { hoopZone.kind = HouseTarget.Kind.Hoop; hoopZone.openingHalf = 0f; hoopZone.openingHalfZ = 0f; hoopZone.reverse = false; hoopZone.gameObject.SetActive(false); }   // (the beach lifeguard hut borrows this zone)
        if (hoopZone2 != null) hoopZone2.gameObject.SetActive(false);
        if (yard == Yard.Lawn || yard == Yard.Shop) { HideFrom(propCubes, 0); HideFrom(propSpheres, 0); return; }
        int cubeN = 0, sphereN = 0;
        float s = side;
        float c = gardenCenterZ;
        float len = gardenFullLen;
        float lo = c - len * 0.5f + 0.2f;
        float hi = c + len * 0.5f - 0.2f;
        float room = hi - lo;

        if (yard == Yard.Garage && (wallMail || room < 3.0f)) yard = Yard.Parking;
        if (yard == Yard.Hoop && (wallMail || room < 2.2f)) yard = Yard.Parking;
        if (yard == Yard.Stall && room < 2.4f) yard = Yard.Plants;
        if (yard == Yard.Parking && room < 1.4f) yard = Yard.Plants;

        switch (yard)
        {
            case Yard.Parking:
            {
                float scooterZ = c;
                bool car = room >= 3.6f;
                if (car)
                {
                    bool truck = room >= 6.4f && Random.value < 0.35f;
                    bool both = room >= 7.0f && !truck;
                    int type = LowPolyCar.RandomType(false);
                    if (truck) type = LowPolyCar.Truck;
                    float cz = both ? lo + 1.8f : c;
                    float cx = s * 6.1f;
                    PlaceCar(cx, cz, type, Random.Range(0, LowPolyCar.Colors.Length), both ? 3.6f : room);
                    if (both) scooterZ = hi - 0.7f;
                    else scooterZ = float.NaN;
                    yardSpot = new Vector3(cx, LowPolyCar.Top(type) + 0.7f, cz);
                    yardSpotValid = true;
                }
                if (!float.IsNaN(scooterZ))
                {
                    if (!yardSpotValid) { yardSpot = new Vector3(s * 5.9f, 1.5f, scooterZ); yardSpotValid = true; }
                    int n = room >= 2.6f ? 2 : 1;
                    for (int i = 0; i < n; i++)
                    {
                        float sx = s * (5.9f + i * 0.9f);
                        Color col = CarColors[Random.Range(0, CarColors.Length)];
                        Cube(cubeN++, new Vector3(sx, 0.45f, scooterZ), new Vector3(0.45f, 0.55f, 1.1f), col);
                        Cube(cubeN++, new Vector3(sx, 0.85f, scooterZ - 0.15f), new Vector3(0.35f, 0.25f, 0.5f), new Color(0.15f, 0.15f, 0.15f));
                    }
                }
                break;
            }
            case Yard.Hoop:
            {
                // A basketball hoop facing the house: the board stands on the road side and the ring hangs between it and the
                // wall, so a throw that hits the wall (or comes over the board) can rebound and drop in. Nothing is scripted.
                float hz = Mathf.Clamp(c, lo + 1.1f, hi - 1.1f);
                float ringX = s * 7.5f;
                const float ringY = 2.9f;
                Cube(cubeN++, new Vector3(ringX - s * 0.8f, 1.6f, hz), new Vector3(0.15f, 3.2f, 0.15f), new Color(0.45f, 0.45f, 0.48f));
                Cube(cubeN++, new Vector3(ringX - s * 0.6f, 3.2f, hz), new Vector3(0.1f, 1.1f, 1.6f), Color.white);
                if (propCubes != null && cubeN - 1 < propCubes.Length) SoftBoard(propCubes[cubeN - 1]);
                for (int k = 0; k < 8; k++)
                {
                    float a = k * 45f * Mathf.Deg2Rad;
                    Cube(cubeN++, new Vector3(ringX + Mathf.Cos(a) * 0.45f, ringY, hz + Mathf.Sin(a) * 0.45f),
                         new Vector3(0.08f, 0.08f, 0.38f), new Color(1.00f, 0.85f, 0.20f), new Vector3(0f, -k * 45f, 0f));
                    if (propCubes != null && cubeN - 1 < propCubes.Length) SoftRim(propCubes[cubeN - 1]);   // solid but dead, like every hoop ring
                }
                for (int k = 0; k < 4; k++)
                {
                    float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                    AirCube(cubeN++, new Vector3(ringX + Mathf.Cos(a) * 0.38f, ringY - 0.27f, hz + Mathf.Sin(a) * 0.38f),
                         new Vector3(0.03f, 0.5f, 0.03f), Color.white);
                }
                if (hoopZone != null && hoopSnapT != null)
                {
                    SetZone(hoopZone, new Vector3(ringX, ringY, hz), Tutorial.Active ? new Vector3(2.2f, 0.9f, 2.2f) : new Vector3(0.9f, 0.6f, 0.9f));
                    hoopZone.openingHalf = Tutorial.Active ? 0.85f : 0f;   // (the tutorial's ring is easier to hit)
                    hoopSnapT.localPosition = new Vector3(ringX, ringY - 0.5f, hz);
                    hoopZone.gameObject.SetActive(true);
                }
                yardSpot = new Vector3(ringX, 4.4f, hz);
                yardSpotValid = true;
                break;
            }
            case Yard.Stall:
            {
                float sz = c;
                Cube(cubeN++, new Vector3(s * 6.4f, 0.5f, sz), new Vector3(1.2f, 1.0f, 1.8f), new Color(0.55f, 0.38f, 0.22f));
                Cube(cubeN++, new Vector3(s * 6.0f, 2.25f, sz), new Vector3(2.0f, 0.1f, 2.4f), new Color(0.20f, 0.60f, 0.65f));
                Cube(cubeN++, new Vector3(s * 5.15f, 1.1f, sz - 1.05f), new Vector3(0.1f, 2.2f, 0.1f), Color.white);
                Cube(cubeN++, new Vector3(s * 5.15f, 1.1f, sz + 1.05f), new Vector3(0.1f, 2.2f, 0.1f), Color.white);
                yardSpot = new Vector3(s * 6.0f, 3.0f, sz);
                yardSpotValid = true;
                break;
            }
            case Yard.Garage:
            {
                float gz = Mathf.Clamp(c, lo + 1.5f, hi - 1.5f);
                Cube(cubeN++, new Vector3(s * 7.0f, 1.3f, gz), new Vector3(3.0f, 2.6f, 3.0f), new Color(0.80f, 0.78f, 0.72f));
                Cube(cubeN++, new Vector3(s * 5.47f, 1.0f, gz), new Vector3(0.06f, 1.9f, 2.2f), new Color(0.50f, 0.55f, 0.62f));
                blockZ = gz;
                blockHalf = 1.5f;
                yardSpot = new Vector3(s * 7.0f, 3.4f, gz);
                yardSpotValid = true;
                break;
            }
            default:
            {
                int n = Mathf.Clamp(Mathf.FloorToInt(room / 1.2f), 1, propSpheres != null ? propSpheres.Length : 6);
                float step = room / n;
                yardSpot = new Vector3(s * 6.5f, 1.5f, c);
                yardSpotValid = true;
                for (int i = 0; i < n; i++)
                {
                    float size = 0.9f + 0.15f * (i % 3);
                    float x = (i % 2 == 0) ? 5.8f : (wallMail ? 6.7f : 7.2f);
                    Color col = i == 2 ? new Color(0.65f, 0.45f, 0.85f) : (i % 2 == 0 ? new Color(0.22f, 0.58f, 0.27f) : new Color(0.30f, 0.65f, 0.30f));
                    Sphere(sphereN++, new Vector3(s * x, size * 0.5f, lo + step * (i + 0.5f)), size, col);
                }
                break;
            }
        }

        HideFrom(propCubes, cubeN);
        HideFrom(propSpheres, sphereN);
    }

    // A garden table on the lawn, with cups and plates on some. Landing on the clear part of the top is a delivery at a customer's.
    // Items only sit at the two ends of the top, so the middle is always free. Returns true when the table is placed.
    bool LayoutTable(bool wanted, bool items, bool flip)
    {
        tableSpotValid = false;
        bool fits = wanted && tableParts != null && tableParts.Length >= 5 && gardenFullLen >= 2.0f;
        if (tableParts != null) foreach (var p in tableParts) p.gameObject.SetActive(fits);
        if (tableItems != null)
            foreach (var it in tableItems) { it.visual.gameObject.SetActive(false); it.zone.gameObject.SetActive(false); }
        if (!fits) return false;

        float s = side;
        float tx = s * 7.0f;
        float tz = gardenCenterZ;
        tableParts[0].localPosition = new Vector3(tx, 0.8f, tz);
        tableParts[0].localScale = new Vector3(1.0f, 0.08f, 1.6f);
        for (int i = 0; i < 4; i++)
        {
            tableParts[1 + i].localPosition = new Vector3(tx + ((i & 1) == 0 ? -0.4f : 0.4f), 0.38f, tz + (i < 2 ? -0.7f : 0.7f));
            tableParts[1 + i].localScale = new Vector3(0.08f, 0.76f, 0.08f);
        }
        if (tableZone != null) SetZone(tableZone, new Vector3(tx, 1.0f, tz), new Vector3(1.0f, 0.4f, 1.6f));
        tableSpot = new Vector3(tx, 1.9f, tz);
        tableSpotValid = true;

        if (items && tableItems != null && tableItems.Length >= 3)
        {
            PlaceItem(tableItems[0], new Vector3(tx - 0.1f * s, 0.86f, tz - 0.55f), new Vector3(0.4f, 0.02f, 0.4f), new Vector3(0.5f, 0.4f, 0.5f));   // plate
            int second = flip ? 1 : 2;                                                                                                              // cup or teapot
            Vector3 sc = flip ? new Vector3(0.18f, 0.1f, 0.18f) : new Vector3(0.3f, 0.3f, 0.3f);
            PlaceItem(tableItems[second], new Vector3(tx + 0.1f * s, flip ? 0.94f : 0.99f, tz + 0.55f), sc, new Vector3(0.4f, 0.4f, 0.4f));
        }
        return true;
    }

    static void PlaceItem(WindowSlot it, Vector3 pos, Vector3 scale, Vector3 zoneSize)
    {
        it.broken = false;
        it.visual.localPosition = pos;
        it.visual.localScale = scale;
        it.zone.transform.localPosition = pos;
        it.zone.size = zoneSize;
        var box = it.zone.GetComponent<BoxCollider>();
        if (box != null) box.size = zoneSize;
        if (it.glass != null) it.glass.material.color = Color.white;
        it.visual.gameObject.SetActive(true);
        it.zone.gameObject.SetActive(true);
    }

    // Optional dressing that looks like part of a normal house and changes which throws work.
    //   Tree : a tree on the lawn beside the path. Its round crown overhangs the path and doorstep and blocks arcs that pass through it.
    //   Porch: a gable porch roof on two pillars over the doorstep. The pillars bounce papers; the roof slopes shed them.
    void LayoutDressing(float L, float doorZ, bool flip, bool tree, bool porch, float d)
    {
        float s = side;
        float half = L * 0.5f;

        if (treeTrunkT != null && treeCrownT != null)
        {
            // The trunk stands on the grass beside the path, never on the path or the doormat: it keeps DoorClear (or at least 2.0) along the
            // road from the door, on the side the lot has room for. A lot too short for that has no lawn tree.
            float tz = 0f;
            bool fits = false;
            float pref = flip ? -1f : 1f;
            for (int c = 0; c < 4 && !fits; c++)
            {
                float dist = c < 2 ? GameConfig.DoorClear : 2.0f;
                tz = doorZ + (c % 2 == 0 ? pref : -pref) * dist;
                fits = tz >= -half + 0.9f && tz <= half - 0.9f;
            }
            tree = tree && fits;
            treeTrunkT.gameObject.SetActive(false);   // (the old primitive tree is never shown: the low-poly tree replaces it)
            treeCrownT.gameObject.SetActive(false);
            if (tree) Blob(new Vector3(s * 5.6f, 0f, tz), 2.8f, 2.8f, 0.26f);
            LayoutLawnTree(tree, s, tz);
        }

        if (porchRoofT != null && porchPillarA != null && porchPillarB != null)
        {
            porchRoofT.localPosition = new Vector3(s * (7.65f - d), 2.6f, doorZ);
            porchPillarA.localPosition = new Vector3(s * (6.95f - d), 1.25f, doorZ + 1.0f);
            porchPillarB.localPosition = new Vector3(s * (6.95f - d), 1.25f, doorZ - 1.0f);
            if (porchRoof2T != null) porchRoof2T.gameObject.SetActive(false);
            porchPillarA.localScale = new Vector3(0.2f, 2.5f, 0.2f);
            porchPillarB.localScale = new Vector3(0.2f, 2.5f, 0.2f);
            var chute = porchRoofT.GetComponent<ChuteSurface>();
            if (chute != null) chute.active = false;
            if (porch) LayoutPorchRoof(1, doorZ, false);   // a gable (a ring beside it may turn it into the trick-shot roof later)
            porchRoofT.gameObject.SetActive(porch);
            porchPillarA.gameObject.SetActive(porch);
            porchPillarB.gameObject.SetActive(porch);
        }
    }

    // Every porch roof is a gable: two slopes that meet in a ridge over the door, both slippery, on two pillars. A paper that lands on one
    // slides off its low edge (plain physics, nothing scripted). dir = which slope is the "main" panel (the one on the +dir side of the door).
    // When a ring stands beside the roof (the roof trick shot, trick = true), dir points at it: the slope towards the ring feeds it, so a paper that
    // lands there in line with the ring drops through; the other slope slides away from it.
    static PhysicsMaterial roofMaterial;
    const float RoofRidgeTop = 3.10f;      // the ridge's top surface: high enough that the low edge still clears a ring hanging beside it

    void LayoutPorchRoof(int dir, float doorZ, bool trick)
    {
        if (porchRoof2T == null) return;
        var a = porchRoofT.GetComponent<ChuteSurface>();
        var b = porchRoof2T.GetComponent<ChuteSurface>();
        if (a == null || b == null) return;
        float tilt = GameConfig.ChuteTilt;
        float rad = tilt * Mathf.Deg2Rad;
        float tan = Mathf.Tan(rad), cos = Mathf.Cos(rad);
        const float panelLen = 1.15f;                 // along the slope; it spans about 1.1 m of the road
        float half = panelLen * cos * 0.5f;           // horizontal reach of a panel from the ridge, halved = where its centre is
        float roofX = porchRoofT.localPosition.x;

        // the ridge's top surface; a panel's centre sits half its reach down the slope, and half its thickness below the surface
        float centreY = RoofRidgeTop - half * tan - 0.075f / cos;
        PlacePanel(porchRoofT, new Vector3(roofX, centreY, doorZ + dir * half), dir * tilt, panelLen);     // a positive pitch lowers the +z end
        PlacePanel(porchRoof2T, new Vector3(roofX, centreY, doorZ - dir * half), -dir * tilt, panelLen);
        porchRoof2T.gameObject.SetActive(true);

        // both pillars reach the underside of the roof where they stand (1 m either side of the ridge)
        float under = RoofRidgeTop - 0.15f / cos - 1.0f * tan;
        porchPillarA.localScale = new Vector3(0.2f, under, 0.2f);
        porchPillarA.localPosition = new Vector3(porchPillarA.localPosition.x, under * 0.5f, porchPillarA.localPosition.z);
        porchPillarB.localScale = new Vector3(0.2f, under, 0.2f);
        porchPillarB.localPosition = new Vector3(porchPillarB.localPosition.x, under * 0.5f, porchPillarB.localPosition.z);

        // slippery: the lowest friction of the two colliders wins
        if (roofMaterial == null)
        {
            roofMaterial = new PhysicsMaterial("SlipperyRoof");
            roofMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            roofMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            roofMaterial.bounciness = 0f;
        }
        roofMaterial.dynamicFriction = GameConfig.RoofFriction;
        roofMaterial.staticFriction = GameConfig.RoofFriction;
        var colA = porchRoofT.GetComponent<Collider>();
        var colB = porchRoof2T.GetComponent<Collider>();
        if (colA != null) colA.sharedMaterial = roofMaterial;
        if (colB != null) colB.sharedMaterial = roofMaterial;
        a.active = trick;   // only a roof with a ring beside it counts for the trick-shot bonus
        b.active = trick;
    }

    static void PlacePanel(Transform t, Vector3 localPos, float pitch, float length)
    {
        t.localPosition = localPos;
        t.localEulerAngles = new Vector3(pitch, 0f, 0f);
        t.localScale = new Vector3(1.7f, 0.15f, length);
    }

    // ---- a trampoline on an empty lawn: a paper that lands on the pad bounces (see TrampolineSurface and Paper) ----
    Transform trampoline;
    TrampolineSurface trampPad, trampRim;
    Transform trampMark;       // the spot to land on when the trampoline sits under a reverse basket

    void LayoutTrampoline(float L, float doorZ, bool eligible)
    {
        float p = GameConfig.SceneryOdds(transform.position.z, GameConfig.TrampolineChance);
        bool on = eligible && p > 0f && Hash01(transform.position.z * 2.377f + (side > 0 ? 17.1f : 53.3f)) < p;
        if (!on)
        {
            if (trampoline != null) trampoline.gameObject.SetActive(false);
            return;
        }
        float z = TrampolineZ(L, doorZ);
        PlaceTrampoline(6.2f, z, float.NaN);
    }

    // The trampoline sits on the side of the lawn away from the door.
    static float TrampolineZ(float L, float doorZ)
    {
        float half = L * 0.5f;
        return Mathf.Clamp(doorZ >= 0f ? -half * 0.5f : half * 0.5f, -half + 1.6f, half - 1.6f);
    }

    // Builds the trampoline the first time and puts it at (|x| = padX, z). Every trampoline bounces the same (GameConfig.TrampolineBounce / SlideKeep).
    // markX = where to show the landing mark (distance from the pad centre towards the wall), or NaN for none.
    void PlaceTrampoline(float padX, float z, float markX)
    {
        if (trampoline == null)
        {
            trampoline = new GameObject("Trampoline").transform;
            trampoline.SetParent(transform, false);
            Color frame = new Color(0.22f, 0.25f, 0.30f), pad = new Color(0.22f, 0.45f, 0.85f);
            // a round trampoline: a dark frame ring with the blue skin inside it (the skin is what vibrates), on four legs
            var rimGo = Prim.Make(PrimitiveType.Cylinder, "Rim", trampoline, new Vector3(0f, 0.60f, 0f), new Vector3(2.76f, 0.065f, 2.76f), frame, true);
            Prim.RoundCollider(rimGo);
            trampRim = rimGo.AddComponent<TrampolineSurface>();   // (the frame throws a paper back too: otherwise a paper touching frame and skin together could land dead)
            trampRim.still = true;
            var padGo = Prim.Make(PrimitiveType.Cylinder, "Pad", trampoline, new Vector3(0f, 0.62f, 0f), new Vector3(2.4f, 0.06f, 2.4f), pad, true);
            Prim.RoundCollider(padGo);
            trampPad = padGo.AddComponent<TrampolineSurface>();
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cube, "Leg", trampoline, new Vector3((i & 1) == 0 ? -0.85f : 0.85f, 0.3f, (i & 2) == 0 ? -0.85f : 0.85f), new Vector3(0.1f, 0.6f, 0.1f), frame);
            var mark = Prim.Make(PrimitiveType.Cylinder, "LandingMark", trampoline, new Vector3(0f, 0.684f, 0f), new Vector3(0.8f, 0.004f, 0.8f), new Color(1.00f, 0.85f, 0.20f));
            trampMark = mark.transform;
        }
        trampPad.restitution = GameConfig.TrampolineBounce;
        trampPad.tangentKeep = GameConfig.TrampolineSlideKeep;
        trampRim.restitution = GameConfig.TrampolineBounce;
        trampRim.tangentKeep = GameConfig.TrampolineSlideKeep;
        trampoline.localPosition = new Vector3(side * padX, 0f, z);
        trampoline.gameObject.SetActive(true);
        bool marked = !float.IsNaN(markX);
        trampMark.gameObject.SetActive(marked);
        if (marked) trampMark.localPosition = new Vector3(side * markX, 0.684f, 0f);
        Blob(new Vector3(side * padX, 0f, z), 3.6f, 3.6f, 0.3f);
    }

    // ---- wall bushes: solid round bushes against the house wall (papers bounce off them) ----
    // Quiet houses get a short row, customers' houses only a bush at each end of the wall, and never near the door, the wall mailbox,
    // or the lawn pad, so the delivery areas stay clear. Built from a lazy pool.

    readonly List<Transform> bushes = new List<Transform>();
    readonly List<Renderer> bushR = new List<Renderer>();
    readonly List<Transform> bloomT = new List<Transform>();
    readonly List<Renderer> bloomR = new List<Renderer>();
    int bushN;
    static readonly Color[] BushColors = { new Color(0.16f, 0.42f, 0.30f), new Color(0.27f, 0.38f, 0.18f), new Color(0.20f, 0.35f, 0.38f), new Color(0.12f, 0.33f, 0.22f) };
    static readonly Color[] BloomColors = { new Color(0.95f, 0.95f, 0.90f), new Color(0.72f, 0.62f, 0.88f), new Color(0.96f, 0.88f, 0.40f) };

    void LayoutWallBushes(float L, float doorZ, bool hasTargets, bool wallMail, float mailZ, bool allowed)
    {
        bushN = 0;
        float wz = transform.position.z;
        float h = Mathf.Repeat(Mathf.Sin(wz * 55.173f + (side > 0 ? 3.7f : 61.9f)) * 43758.5453f, 1f);
        if (allowed && GameConfig.WallBushChance > 0f && h < GameConfig.WallBushChance)
        {
            float half = L * 0.5f;
            int count;
            float z0, z1;
            if (hasTargets) { count = 2; z0 = -half + 0.7f; z1 = half - 0.7f; }
            else
            {
                count = Mathf.Clamp(Mathf.FloorToInt((L - 1.2f) / 1.9f), 1, 5);
                z0 = -half + 0.9f; z1 = half - 0.9f;
            }
            for (int k = 0; k < count; k++)
            {
                float z = count == 1 ? 0f : Mathf.Lerp(z0, z1, k / (float)(count - 1));
                if (Mathf.Abs(z - doorZ) < 1.6f) continue;                       // the door strip
                if (wallMail && Mathf.Abs(z - mailZ) < 1.3f) continue;           // the mailbox on the wall
                float r1 = Mathf.Repeat(Mathf.Sin((wz + k * 7.3f) * 91.7f + (side > 0 ? 1.3f : 40.1f)) * 43758.5453f, 1f);
                float r2 = Mathf.Repeat(Mathf.Sin((wz + k * 3.1f) * 17.9f + 8.2f) * 43758.5453f, 1f);
                if (r1 > 0.8f) continue;                                         // a few gaps
                float size = hasTargets ? 0.75f : Mathf.Lerp(0.8f, 1.05f, r2);
                float bx = side * (8.5f - 0.1f - size * 0.5f);
                Blob(new Vector3(bx, 0f, z), size * 1.5f, size * 1.5f, 0.28f);
                Bush(new Vector3(bx, 0.3f, z), size, BushColors[(int)(r1 * 97f) % BushColors.Length],
                     r2 > 0.55f ? BloomColors[(int)(r2 * 53f) % BloomColors.Length] : (Color?)null);
            }
        }
        for (int i = bushN; i < bushes.Count; i++) bushes[i].gameObject.SetActive(false);
    }

    void Bush(Vector3 pos, float size, Color col, Color? bloom)
    {
        if (bushN >= bushes.Count)
        {
            var go = Prim.Make(PrimitiveType.Sphere, "WallBush", transform, Vector3.zero, Vector3.one, Color.white, true);
            go.AddComponent<BankSurface>();
            var b = Prim.Make(PrimitiveType.Sphere, "Bloom", go.transform, new Vector3(0.22f, 0.36f, 0.12f), Vector3.one * 0.3f, Color.white);
            bushes.Add(go.transform);
            bushR.Add(go.GetComponent<Renderer>());
            bloomT.Add(b.transform);
            bloomR.Add(b.GetComponent<Renderer>());
        }
        var t = bushes[bushN];
        t.localPosition = pos;
        t.localScale = new Vector3(size, size, size);
        bushR[bushN].material.color = col;
        bool flower = bloom.HasValue;
        bloomT[bushN].gameObject.SetActive(flower);
        if (flower) bloomR[bushN].material.color = bloom.Value;
        t.gameObject.SetActive(true);
        bushN++;
    }
}
