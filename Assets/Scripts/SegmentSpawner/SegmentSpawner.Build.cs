using System.Collections.Generic;
using UnityEngine;

// SegmentSpawner (partial) - Build
// The main part of the class is in SegmentSpawner.cs.
public partial class SegmentSpawner
{
    Segment Build()
    {
        var s = new Segment();
        var root = new GameObject("Segment").transform;
        root.SetParent(transform, false);
        s.root = root;

        float mid = segmentLength / 2f;
        grassRenderers.Add(Prim.Make(PrimitiveType.Cube, "Grass", root, new Vector3(0f, -0.1f, mid),
                  new Vector3(70f, 0.2f, segmentLength), GameConfig.GrassColor, true).GetComponent<Renderer>());
        var roadGo = Prim.Make(PrimitiveType.Cube, "Road", root, new Vector3(0f, 0f, mid),
                  new Vector3(roadHalfWidth * 2f, 0.1f, segmentLength), new Color(0.27f, 0.27f, 0.30f), true);
        Wetness.Register(roadGo.GetComponent<Renderer>().sharedMaterial, 0.68f);   // the wet road is darker and glossy

        // Lane paint on the road, raised paved footpaths with kerbs between the road and the lots, and a zebra crossing that some segments switch on
        RoadDetail.Markings(root, mid, segmentLength, roadHalfWidth);
        RoadDetail.Footpaths(root, mid, segmentLength);
        s.crossing = RoadDetail.Crossing(root, roadHalfWidth);
        s.puddles = Puddles.Create(root, segmentLength, roadHalfWidth);

        // A few lot slots per side; Place() uses as many as fit and hides the rest.
        s.left = new House[lotsLeft];
        s.right = new House[lotsPerSide];
        for (int i = 0; i < lotsLeft; i++) s.left[i] = BuildHouse(root, -1);
        for (int i = 0; i < lotsPerSide; i++) s.right[i] = BuildHouse(root, 1);

        s.heart = HeartPickup.Create(root);
        s.backLeft = BackRow.Build(root, -1);
        s.backRight = BackRow.Build(root, 1);
        s.mistLeft = MistBank.Create(root, -1);
        s.mistRight = MistBank.Create(root, 1);

        // cow slot
        var cow = new GameObject("Cow");
        cow.transform.SetParent(root, false);
        cow.transform.localPosition = new Vector3(0f, 0f, segmentLength * 0.8f);
        Color white = new Color(0.96f, 0.94f, 0.88f);
        Prim.Make(PrimitiveType.Cube, "Body", cow.transform, new Vector3(0f, 0.9f, 0f), new Vector3(1.0f, 1.0f, 2.0f), white);
        Prim.Make(PrimitiveType.Cube, "Head", cow.transform, new Vector3(0f, 1.2f, 1.25f), new Vector3(0.6f, 0.6f, 0.7f), white);
        Prim.Make(PrimitiveType.Cube, "Patch", cow.transform, new Vector3(0f, 1.0f, 0.2f), new Vector3(1.02f, 0.5f, 0.7f), new Color(0.45f, 0.30f, 0.20f));
        s.hazard = cow.AddComponent<RoadHazard>();
        s.hazard.Init(cow.transform.localPosition);

        return s;
    }

    // Creates every part of a lot once. House.Setup() moves and resizes them each time the lot is re-used
    // (lot length, door position, mailbox spot, windows), so the positions below are only starting values.
    House BuildHouse(Transform root, int side)
    {
        var houseGo = new GameObject("House");
        houseGo.transform.SetParent(root, false);
        var house = houseGo.AddComponent<House>();
        house.side = side;
        Transform t = houseGo.transform;
        Color brown = new Color(0.45f, 0.30f, 0.20f);

        float hx = side * 11.5f;
        var body = Prim.Make(PrimitiveType.Cube, "Body", t, new Vector3(hx, 3.5f, 0f),
                             new Vector3(6f, 7f, 7.5f), Color.white, true);
        body.AddComponent<BankSurface>();   // the house wall bounces papers
        var band = Prim.Make(PrimitiveType.Cube, "FloorBand", t, new Vector3(hx, 3.55f, 0f),
                             new Vector3(6.2f, 0.2f, 7.7f), new Color(0.55f, 0.50f, 0.50f));
        var roof = Prim.Make(PrimitiveType.Cube, "Roof", t, new Vector3(hx, 7.3f, 0f),
                             new Vector3(6.8f, 0.6f, 8.3f), new Color(0.32f, 0.36f, 0.46f), true);   // solid: a paper that lands on a flat roof stays up there
        roof.AddComponent<BankSurface>();

        // Balconies of the upper floors: a pool of three per upper floor (two upper floors). House.LayoutBalconies() places and sizes them each
        // time the lot is re-used; the unused ones are switched off.
        house.balconies = new House.BalconySet[House.MaxBalconies * 2];
        for (int bi = 0; bi < house.balconies.Length; bi++)
        {
            house.balconies[bi] = MakeBalcony(t, side, house, brown);
            house.balconies[bi].zone.index = bi;
            house.balconies[bi].floor = bi / House.MaxBalconies + 1;
        }

        // Customer marker, floating beside the road where it is easy to see.
        // A big round sign that always faces the camera: a dark outline, a red ring, a white face (turns yellow when it is time to throw) and a bold red "!".
        var beacon = new GameObject("Marker");
        beacon.transform.SetParent(t, false);
        beacon.transform.localPosition = new Vector3(side * 3.6f, 3.4f, 0f);
        GameObject beaconFace;
        var flat = new Vector3(90f, 0f, 0f);   // cylinder axis along z: a coin standing on its edge
        Color outline = new Color(0.30f, 0.02f, 0.04f);
        Color red = new Color(1.00f, 0.10f, 0.08f);      // a vivid, unlit red: bright in any light
        Color signWhite = new Color(1.00f, 1.00f, 1.00f);
        System.Action<GameObject, Color> glow = (glowObj, glowCol) => Prim.GlowColor(glowObj.GetComponent<Renderer>(), glowCol);
        var pOutline = Prim.Make(PrimitiveType.Cylinder, "Outline", beacon.transform, new Vector3(0f, 0f, 0f), new Vector3(1.78f, 0.05f, 1.78f), outline, false, flat); glow(pOutline, outline);
        var pRing = Prim.Make(PrimitiveType.Cylinder, "Ring", beacon.transform, new Vector3(0f, 0f, 0f), new Vector3(1.56f, 0.07f, 1.56f), red, false, flat); glow(pRing, red);
        beaconFace = Prim.Make(PrimitiveType.Cylinder, "Face", beacon.transform, new Vector3(0f, 0f, 0f), new Vector3(1.30f, 0.09f, 1.30f), signWhite, false, flat); glow(beaconFace, signWhite);
        // the "!": a bar that is wider at the top, and a round dot, with the same dark edge as the sign
        var pBarEdge = Prim.Make(PrimitiveType.Cube, "BarEdge", beacon.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.40f, 0.80f, 0.19f), outline); glow(pBarEdge, outline);
        var pBar = Prim.Make(PrimitiveType.Cube, "Bar", beacon.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.30f, 0.72f, 0.22f), red); glow(pBar, red);
        var pBarTop = Prim.Make(PrimitiveType.Cube, "BarTop", beacon.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.38f, 0.28f, 0.22f), red); glow(pBarTop, red);
        var pDotEdge = Prim.Make(PrimitiveType.Sphere, "DotEdge", beacon.transform, new Vector3(0f, -0.40f, 0f), new Vector3(0.40f, 0.40f, 0.30f), outline); glow(pDotEdge, outline);
        var pDot = Prim.Make(PrimitiveType.Sphere, "Dot", beacon.transform, new Vector3(0f, -0.40f, 0f), new Vector3(0.32f, 0.32f, 0.36f), red); glow(pDot, red);
        // one dot per floor of a house with several floors, stacked above the coin (ground at the bottom): green while open, gold when served
        house.pips = new Renderer[3];
        for (int pi = 0; pi < house.pips.Length; pi++)
        {
            var pip = Prim.Make(PrimitiveType.Sphere, "FloorPip", beacon.transform, new Vector3(0f, 1.0f + 0.26f * pi, 0f), new Vector3(0.2f, 0.2f, 0.2f), Color.white);
            house.pips[pi] = pip.GetComponent<Renderer>();
            pip.SetActive(false);
        }

        // Front door and the path to it (visual)
        var frontDoor = Prim.Make(PrimitiveType.Cube, "FrontDoor", t, new Vector3(side * 8.47f, 0.9f, 0f),
                                  new Vector3(0.08f, 1.8f, 1.0f), brown);
        var pathVisual = Prim.Make(PrimitiveType.Cube, "Path", t, new Vector3(side * 5.7f, 0.1f, 0f),
                                   new Vector3(2.6f, 0.04f, 1.6f), new Color(0.78f, 0.72f, 0.62f), true);

        // Lawn patch (covers the whole lot) and the garden zones: low triggers, so a paper counts when it comes down
        // on the lawn. The main zone has a kinematic rigidbody so it can move when the resident walks around.
        var lawn = Prim.Make(PrimitiveType.Cube, "Lawn", t, new Vector3(side * 6.45f, 0.04f, 0f),
                             new Vector3(4.0f, 0.08f, 7.5f), Color.white, true);
        var garden = MakeZone("GardenZone", t, new Vector3(side * 6.45f, 0.45f, 0f),
                              new Vector3(3.9f, 0.9f, 5.5f), HouseTarget.Kind.Garden, house);
        var rb = garden.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var walker = Prim.Make(PrimitiveType.Cylinder, "Walker", garden.transform, new Vector3(0f, 0.35f, 0f),
                               new Vector3(0.5f, 0.8f, 0.5f), new Color(0.55f, 0.35f, 0.85f));
        // second lawn, used when the door is in the middle of the lot
        var gardenB = MakeZone("GardenZoneB", t, new Vector3(side * 6.45f, 0.45f, 0f),
                               new Vector3(3.9f, 0.9f, 2.0f), HouseTarget.Kind.Garden, house);

        // Path: counts as a delivery, for the garden's points. No snap-in.
        var pathZone = MakeZone("PathZone", t, new Vector3(side * 5.7f, 0.45f, 0f),
                                new Vector3(2.6f, 0.9f, 2.0f), HouseTarget.Kind.Path, house);

        // Doorstep: a mat touching the wall at the end of the path. Medium size. The paper has to come to rest on it.
        var doorstep = MakeZone("DoorstepZone", t, new Vector3(side * 7.7f, 0.45f, 0f),
                                new Vector3(1.4f, 0.9f, 2.0f), HouseTarget.Kind.Door, house);
        var mat = Prim.Make(PrimitiveType.Cube, "DoorMat", doorstep.transform, new Vector3(0f, -0.33f, 0f),
                            new Vector3(1.3f, 0.08f, 1.8f), Color.white, true);

        // Mailbox: small and hard. It is a container: a paper that comes down into the opening stays there (it is tucked
        // inside and the flag pops up). Where it stands changes from lot to lot (see House.MailPos).
        var post = Prim.Make(PrimitiveType.Cube, "MailboxPost", t, new Vector3(side * 4.7f, 0.45f, 0f),
                             new Vector3(0.15f, 0.9f, 0.15f), new Color(0.35f, 0.25f, 0.20f));
        var gatePostA = Prim.Make(PrimitiveType.Cube, "GatePostA", t, new Vector3(side * 4.6f, 0.7f, 0f),
                                  new Vector3(0.22f, 1.4f, 0.22f), new Color(0.42f, 0.32f, 0.24f));
        var gatePostB = Prim.Make(PrimitiveType.Cube, "GatePostB", t, new Vector3(side * 4.6f, 0.7f, 0f),
                                  new Vector3(0.22f, 1.4f, 0.22f), new Color(0.42f, 0.32f, 0.24f));
        var mailbox = Prim.Make(PrimitiveType.Cube, "MailboxBox", t, new Vector3(side * 4.7f, 1.05f, 0f),
                                new Vector3(0.6f, 0.35f, 0.45f), Color.white);
        var flag = Prim.Make(PrimitiveType.Cube, "MailboxFlag", t, new Vector3(side * 4.7f, 1.25f, -0.3f),
                             new Vector3(0.06f, 0.05f, 0.06f), new Color(0.90f, 0.15f, 0.15f));
        var mailboxZone = MakeZone("MailboxZone", t, new Vector3(side * 4.7f, 1.05f, 0f),
                                   new Vector3(1.1f, 0.7f, 1.2f), HouseTarget.Kind.Mailbox, house);
        mailboxZone.snapPoint = MakeSnap(t);
        mailboxZone.tuckIn = true;

        // Dressing: a tree beside the path and a porch roof on two pillars over the doorstep.
        var trunk = Prim.Make(PrimitiveType.Cylinder, "TreeTrunk", t, new Vector3(side * 5.6f, 1.15f, 0f),
                              new Vector3(0.35f, 1.15f, 0.35f), new Color(0.40f, 0.28f, 0.18f), true);
        var crown = Prim.Make(PrimitiveType.Sphere, "TreeCrown", t, new Vector3(side * 5.6f, 3.4f, 0f),
                              new Vector3(2.5f, 2.5f, 2.5f), new Color(0.22f, 0.58f, 0.27f), true);
        var porchRoof = Prim.Make(PrimitiveType.Cube, "PorchRoof", t, new Vector3(side * 7.65f, 2.6f, 0f),
                                  new Vector3(1.7f, 0.15f, 2.2f), new Color(0.32f, 0.36f, 0.46f), true);
        var pillarA = Prim.Make(PrimitiveType.Cube, "PorchPillarA", t, new Vector3(side * 6.95f, 1.25f, 0f),
                                new Vector3(0.2f, 2.5f, 0.2f), Color.white, true);
        var pillarB = Prim.Make(PrimitiveType.Cube, "PorchPillarB", t, new Vector3(side * 6.95f, 1.25f, 0f),
                                new Vector3(0.2f, 2.5f, 0.2f), Color.white, true);
        porchRoof.AddComponent<ChuteSurface>();   // switched on by House.LayoutWallHoop when a ring stands beside the roof
        // the second panel of that roof (the roof becomes a gable with two slopes); off unless the house has the roof trick shot
        var porchRoof2 = Prim.Make(PrimitiveType.Cube, "PorchRoofB", t, new Vector3(side * 7.65f, 2.6f, 0f),
                                   new Vector3(1.7f, 0.15f, 1.15f), new Color(0.32f, 0.36f, 0.46f), true);
        porchRoof2.AddComponent<ChuteSurface>();
        porchRoof2.SetActive(false);
        pillarA.AddComponent<BankSurface>();
        pillarB.AddComponent<BankSurface>();

        // Destructibles: a pool of 3 crates and 6 cans, set up as a stack by House.LayoutDestruct(). Kinematic so they can move.
        house.crateUnits = new Destructible[30];   // built on demand by House.Unit
        house.canUnits = new Destructible[42];

        // Yard props: a pool of boxes (cars, scooters, stalls, garages) and bushes, re-arranged by House.LayoutYard().
        var cubes = new Transform[16];   // (14 + the two marks on the standing hoop's board)
        var cubeRenderers = new Renderer[16];
        for (int i = 0; i < cubes.Length; i++)
        {
            var c = Prim.Make(PrimitiveType.Cube, "Prop", t, Vector3.zero, Vector3.one, Color.white, true);
            c.AddComponent<BankSurface>();
            cubes[i] = c.transform;
            cubeRenderers[i] = c.GetComponent<Renderer>();
        }
        var spheres = new Transform[6];
        var sphereRenderers = new Renderer[6];
        for (int i = 0; i < spheres.Length; i++)
        {
            var sp = Prim.Make(PrimitiveType.Sphere, "Bush", t, Vector3.zero, Vector3.one, Color.white, true);
            spheres[i] = sp.transform;
            sphereRenderers[i] = sp.GetComponent<Renderer>();
        }
        // Lawn table (top and four legs), the dishes on it, and the basketball hoop's catch zone.
        var tableParts = new Transform[5];
        for (int i = 0; i < tableParts.Length; i++)
        {
            var part = Prim.Make(PrimitiveType.Cube, i == 0 ? "TableTop" : "TableLeg", t, Vector3.zero, Vector3.one,
                                 new Color(0.55f, 0.38f, 0.22f), true);
            if (i == 0) part.AddComponent<StickySurface>();   // a paper that lands on the top stays there
            tableParts[i] = part.transform;
            part.SetActive(false);
        }
        var tableZone = MakeZone("TableZone", t, Vector3.zero, new Vector3(1.0f, 0.4f, 1.6f), HouseTarget.Kind.Table, house);
        var items = new House.WindowSlot[3];
        PrimitiveType[] itemTypes = { PrimitiveType.Cylinder, PrimitiveType.Cylinder, PrimitiveType.Sphere };
        Color[] itemColors = { new Color(0.95f, 0.95f, 0.98f), new Color(0.30f, 0.75f, 0.70f), new Color(0.35f, 0.55f, 0.85f) };
        for (int i = 0; i < items.Length; i++)
        {
            var vis = Prim.Make(itemTypes[i], i == 0 ? "Plate" : (i == 1 ? "Cup" : "Teapot"), t, Vector3.zero,
                                new Vector3(0.2f, 0.1f, 0.2f), itemColors[i]);
            var zone = MakeZone("DishZone", t, Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f), HouseTarget.Kind.Breakable, house);
            var slot = new House.WindowSlot();
            slot.visual = vis.transform;
            slot.glass = vis.GetComponent<Renderer>();
            slot.zone = zone;
            items[i] = slot;
            vis.SetActive(false);
            zone.gameObject.SetActive(false);
        }
        var hoopZone = MakeZone("HoopZone", t, Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), HouseTarget.Kind.Hoop, house);
        hoopZone.snapPoint = MakeSnap(t);
        hoopZone.tuckIn = true;
        hoopZone.gameObject.SetActive(false);
        var hoopZone2 = MakeZone("HoopZone2", t, Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), HouseTarget.Kind.Hoop, house);   // second ring, for basketball courts
        hoopZone2.snapPoint = MakeSnap(t);
        hoopZone2.tuckIn = true;
        hoopZone2.gameObject.SetActive(false);
        house.hoopZone2 = hoopZone2;
        // the rings of the upper floors (at most one per floor), placed by House.LayoutFloorHoops()
        house.floorHoopZ = new HouseTarget[3];
        house.floorHoopSnap = new Transform[3];
        for (int fi = 1; fi <= 2; fi++)
        {
            var fz = MakeZone("FloorHoopZone", t, Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), HouseTarget.Kind.Hoop, house);
            fz.snapPoint = MakeSnap(t);
            fz.tuckIn = true;
            fz.gameObject.SetActive(false);
            house.floorHoopZ[fi] = fz;
            house.floorHoopSnap[fi] = fz.snapPoint;
        }
        // palm crowns (beach lots): a paper that comes to rest on top of a palm counts like a basket
        house.palmZones = new HouseTarget[4];
        for (int pi = 0; pi < house.palmZones.Length; pi++)
        {
            var pz = MakeZone("PalmZone", t, Vector3.zero, new Vector3(2.2f, 0.9f, 2.2f), HouseTarget.Kind.Palm, house);
            pz.gameObject.SetActive(false);
            house.palmZones[pi] = pz;
        }
        house.hoopSnap2T = hoopZone2.snapPoint;
        house.tableParts = tableParts;
        house.tableZone = tableZone;
        house.tableItems = items;
        house.hoopZone = hoopZone;
        house.hoopSnapT = hoopZone.snapPoint;
        house.propCubes = cubes;
        house.propCubeRenderers = cubeRenderers;
        house.propSpheres = spheres;
        house.propSphereRenderers = sphereRenderers;

        // Windows: ground floor and second floor. Hitting one breaks it and costs mood.
        house.groundWindows = MakeWindows(t, house, 4);
        house.upperWindows = MakeWindows(t, house, House.UpperWindowSlots * 2);   // a row of four on each of the two upper floors

        house.treeTrunkT = trunk.transform;
        house.treeCrownT = crown.transform;
        house.porchRoofT = porchRoof.transform;
        house.porchRoof2T = porchRoof2.transform;
        house.porchPillarA = pillarA.transform;
        house.porchPillarB = pillarB.transform;
        house.bodyT = body.transform;
        house.bandT = band.transform;
        house.roofT = roof.transform;
        house.frontDoorT = frontDoor.transform;
        house.pathVisualT = pathVisual.transform;
        house.lawnT = lawn.transform;
        house.mailPostT = post.transform;
        house.gatePostA = gatePostA.transform;
        house.gatePostB = gatePostB.transform;
        house.mailBoxT = mailbox.transform;
        house.mailSnapT = mailboxZone.snapPoint;
        house.gardenB = gardenB;
        house.pathZone = pathZone;
        house.doorstepZone = doorstep;
        house.mailZone = mailboxZone;

        house.bodyRenderer = body.GetComponent<Renderer>();
        house.padRenderer = lawn.GetComponent<Renderer>();
        house.matRenderer = mat.GetComponent<Renderer>();
        house.mailboxFlag = flag.transform;
        house.mailboxRenderer = mailbox.GetComponent<Renderer>();
        house.beaconRenderer = beaconFace.GetComponent<Renderer>();
        house.beacon = beacon;
        house.gardenTarget = garden;
        house.doorZone = garden.transform;
        house.walker = walker;
        house.doorObjects = new GameObject[] { garden.gameObject, lawn };
        house.doorstepObjects = new GameObject[] { doorstep.gameObject, pathZone.gameObject };
        house.mailboxObjects = new GameObject[] { mailbox, flag, mailboxZone.gameObject };
        var zoneList = new List<HouseTarget> { garden, gardenB, pathZone, doorstep, mailboxZone, tableZone, hoopZone, hoopZone2, house.floorHoopZ[1], house.floorHoopZ[2] };
        foreach (var bset in house.balconies) zoneList.Add(bset.zone);
        house.zones = zoneList.ToArray();
        return house;
    }

    // One balcony of the pool: slab, railing, door and the target zone above the slab (starting values; House.PlaceBalcony sets the real ones).
    static House.BalconySet MakeBalcony(Transform t, int side, House house, Color doorColor)
    {
        var slab = Prim.Make(PrimitiveType.Cube, "BalconySlab", t, new Vector3(side * 7.7f, 3.95f, 0f),
                             new Vector3(1.6f, 0.15f, 2.6f), Color.white, true);   // solid: a paper can rest on it
        // Proper boundary: a parapet panel with a handrail and a middle rail on the front and both sides, with posts at the
        // corners and the middle. All of it is solid (papers bounce off it like a wall), so a throw has to clear the rail to
        // land on the slab. Parent sits at the front rail (x 6.95, 0.5 above the slab's top); its length follows the slab's (z scale).
        var rail = new GameObject("BalconyRail");
        rail.transform.SetParent(t, false);
        rail.transform.localPosition = new Vector3(side * 6.95f, 4.45f, 0f);
        {
            Transform rt = rail.transform;
            Color panel = new Color(0.93f, 0.93f, 0.90f), steel = new Color(0.30f, 0.33f, 0.40f);
            float sx = side * 0.775f;     // middle of the side runs (front rail to the wall)
            Solid(Prim.Make(PrimitiveType.Cube, "FrontPanel", rt, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.45f, 2.6f), panel, true));
            Solid(Prim.Make(PrimitiveType.Cube, "FrontHand", rt, new Vector3(0f, 0.48f, 0f), new Vector3(0.14f, 0.1f, 2.7f), steel, true));
            Solid(Prim.Make(PrimitiveType.Cube, "FrontMid", rt, new Vector3(0f, 0.15f, 0f), new Vector3(0.08f, 0.08f, 2.6f), steel, true));
            for (int k = -1; k <= 1; k++)
                Solid(Prim.Make(PrimitiveType.Cube, "Post", rt, new Vector3(0f, 0.02f, k * 1.27f), new Vector3(0.14f, 1.0f, 0.14f), steel, true));
            for (int e = -1; e <= 1; e += 2)
            {
                Solid(Prim.Make(PrimitiveType.Cube, "SidePanel", rt, new Vector3(sx, -0.2f, e * 1.27f), new Vector3(1.5f, 0.45f, 0.12f), panel, true));
                Solid(Prim.Make(PrimitiveType.Cube, "SideHand", rt, new Vector3(sx, 0.48f, e * 1.27f), new Vector3(1.55f, 0.1f, 0.14f), steel, true));
                Solid(Prim.Make(PrimitiveType.Cube, "SideMid", rt, new Vector3(sx, 0.15f, e * 1.27f), new Vector3(1.5f, 0.08f, 0.08f), steel, true));
            }
        }
        var door = Prim.Make(PrimitiveType.Cube, "BalconyDoor", t, new Vector3(side * 8.47f, 4.85f, 0f),
                             new Vector3(0.08f, 1.4f, 1.0f), doorColor);

        // The target: the paper has to come to rest on the balcony floor (the zone covers the floor and the air just above it).
        var zone = MakeZone("BalconyZone", t, new Vector3(side * 7.7f, 4.7f, 0f),
                            new Vector3(1.5f, 1.4f, 2.6f), HouseTarget.Kind.Balcony, house);

        var set = new House.BalconySet();
        set.slab = slab.transform;
        set.rail = rail.transform;
        set.door = door.transform;
        set.slabR = slab.GetComponent<Renderer>();
        set.zone = zone;
        return set;
    }

    // Marks a solid part as a wall-like surface the paper bounces off.
    static void Solid(GameObject go)
    {
        if (go != null && go.GetComponent<BankSurface>() == null) go.AddComponent<BankSurface>();
    }

    static House.WindowSlot[] MakeWindows(Transform houseRoot, House house, int count)
    {
        var slots = new House.WindowSlot[count];
        for (int i = 0; i < count; i++)
        {
            var visual = Prim.Make(PrimitiveType.Cube, "Window", houseRoot, Vector3.zero,
                                   new Vector3(0.08f, 1.0f, 1.2f), Color.white);
            var zone = MakeZone("WindowZone", houseRoot, Vector3.zero, new Vector3(0.6f, 1.0f, 1.2f),
                                HouseTarget.Kind.Window, house);
            var slot = new House.WindowSlot();
            slot.visual = visual.transform;
            slot.glass = visual.GetComponent<Renderer>();
            slot.zone = zone;
            slots[i] = slot;
        }
        return slots;
    }

    // An empty point where a paper comes to rest. Created under the house root so it stays put even when a zone moves.
    static Transform MakeSnap(Transform houseRoot)
    {
        var go = new GameObject("SnapPoint");
        go.transform.SetParent(houseRoot, false);
        return go.transform;
    }

    static HouseTarget MakeZone(string name, Transform parent, Vector3 localPos, Vector3 size,
                                HouseTarget.Kind kind, House owner)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        var target = go.AddComponent<HouseTarget>();
        target.kind = kind;
        target.owner = owner;
        target.size = size;
        return target;
    }
}
