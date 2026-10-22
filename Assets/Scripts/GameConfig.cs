using UnityEngine;

// ONE FILE FOR EVERY TUNABLE VALUE.
// Change a number here, save, and let Unity recompile. Nothing else needs touching.
// The values below are the defaults the game ships with. All the other scripts read from this file.
// (The game builds itself at runtime, so changing values on components in the editor does nothing: use this file.)
public static class GameConfig
{
    // ============================================================ THROWING
    // true: pull back like a slingshot (the paper flies opposite to your drag). false: drag towards the target.
    public static bool SlingshotStyle = true;
    // Pulls shorter than this (fraction of the short screen edge) cancel the throw.
    public static float DeadZone = 0.08f;
    // Pull length (fraction of the short screen edge) that gives the maximum distance: a comfortable thumb pull. The pull band never goes past it.
    public static float FullPowerDrag = 0.45f;
    // The full pull never takes more than this many millimetres of finger travel (0 = no limit). A phone's full pull is about 30 mm already, so this
    // only trims big screens (tablets, an opened foldable). It needs the screen's DPI; with no believable DPI the plain screen-fraction pull is used.
    public static float MaxPullMm = 35f;
    // Near a screen edge the full pull shrinks so the finger can still reach it (never below this fraction of the short screen edge).
    public static float MinFullPowerDrag = 0.28f;
    // Landing distance (units from the bike) for the shortest valid pull.
    public static float MinThrowDistance = 3f;
    // Landing distance for a full-length pull.
    public static float MaxThrowDistance = 20f;
    // Launch angle in degrees (10-80), blended with the pull: a light pull is a low quick toss, a full pull is a high lob that drops steeply.
    // (Higher = more arc, reaches the balcony; lower = flatter and faster.)
    public static float LaunchAngleLight = 32f;
    public static float LaunchAngleFull = 70f;
    public static float TutorialPreviewShare = 0.7f;   // the same for the how-to-play lessons (0.1 - 1); a slider in the DEV menu
    public static float PreviewShare = 0.5f;   // how much of the aim arc is drawn (0.1 - 1); a slider in the DEV menu
    // Shapes the pull: 1 = linear, above 1 = fine control on short pulls, below 1 = short pulls already go far.
    public static float PowerCurve = 1.5f;
    // Steadier aim on short pulls: a short pull turns the heading a lot for a tiny finger wobble, so the heading is smoothed there.
    // AimSteady = how strongly (0 = off, follows the finger exactly; towards 1 = slow and steady); AimSteadyBelow = pulls shorter than this
    // (fraction of the short screen edge) are smoothed, longer ones follow the finger more and more exactly. Sliders in the DEV menu.
    public static float AimSteady = 0.6f;
    public static float AimSteadyBelow = 0.25f;
    // The parcel covers the first part of its arc faster, then settles to the normal speed (the path is the same, only how fast it is flown changes):
    // ThrowBoost = speed-up at the start (1 = off), ThrowBoostShare = share of the flight (by time) that is sped up, easing back to normal at its end.
    public static float ThrowBoost = 1.5f;
    public static float ThrowBoostShare = 0.55f;
    // Air resistance on a paper in flight (per second, 0 = none): the speed fades like exp(-AirDrag * t), so the arc drops quicker and falls short.
    // It stops at the first floor touch. The aim line uses the same drag, so it still shows the real arc.
    public static float AirDrag = 0f;
    // How much of the bike's forward speed the paper keeps (0-1).
    public static float ForwardInherit = 0.35f;
    // On: the paper does not keep any of the bike's speed, so it flies exactly opposite the pull (the aim arrow matches the drag at any bike speed). Off: it keeps ForwardInherit of it, a forward lean. A DEV menu toggle.
    public static bool CancelBikePush = true;
    // On: the throw's heading is worked out on the screen, so it looks exactly opposite the drag (the camera is off to the side and sees the road in perspective, which made it drift). A DEV menu toggle.
    public static bool ScreenTrueAim = true;
    // Combo juice: how much a long streak adds to the feel of every hit (bigger bursts, a harder camera kick, a bigger score popup, a rising chime). 0 = off, 1 = normal, 2 = a lot. A DEV menu slider.
    public static float ComboJuice = 1f;
    // Coins earned fly as tiny golden dust from the target up to the coin counter on the HUD (no coin is drawn). A DEV menu toggle.
    public static bool CoinDust = true;

    // In the rain papers are wet and heavy: every throw lands this share shorter than the aim guide shows (0.10 = 10%). The guide itself is unchanged.
    public static float RainThrowPenalty = 0.10f;

    // ---- aim assist
    public static bool AimAssist = false;
    // Only throws landing within this many units (along the road) of a target are nudged.
    public static float AssistRange = 3.5f;
    // 0 = no nudge, 1 = full correction.
    public static float AssistStrength = 0.6f;

    // ---- slow motion while pulling
    public static float SlowMotionScale = 0.25f;
    public static float SlowMotionDrainPerSecond = 0.35f;
    public static float SlowMotionRefillPerSecond = 0.5f;

    // ---- tap = bell
    public static float TapMaxSeconds = 0.3f;
    public static float TapMaxMove = 0.04f;

    // ============================================================ PAPER PHYSICS
    // Share of sideways speed the paper keeps when it first lands on the ground (0 = stops dead, 1 = slides on).
    public static float PaperSkidKeep = 0.3f;
    // Share of downward speed that bounces back up on that first landing.
    public static float PaperLandBounce = 0.12f;
    // Seconds before a paper is judged wherever it is.
    public static float PaperMaxLife = 7f;
    // Below this speed (squared) the paper counts as stopped.
    public static float PaperRestSpeedSqr = 0.05f;
    // ... and below this spin (squared) ...
    public static float PaperRestSpinSqr = 1.5f;
    // ... for this many seconds.
    public static float PaperRestTime = 0.25f;
    // Size of the mailbox opening (half widths, in units). Bigger = easier to drop a paper in.
    public static float MailboxOpeningHalfX = 0.4f;
    public static float MailboxOpeningHalfZ = 0.35f;
    // Size of the basketball ring opening (half width, in units).
    public static float HoopOpeningHalf = 0.38f;
    // The little punch the camera gives on a hit (a delivery, a crash): how far it zooms (degrees of field of view), how far it pushes along the road (metres)
    // and how fast it settles (per second; lower = a longer, calmer glide back).
    public static float KickFov = 3.5f;
    public static float KickPush = 0.5f;
    public static float KickDecay = 1.6f;
    public static float WindowBounce = 0.08f;     // a paper that smashes a window: almost no bounce (the wall's is WallBounce) ...
    public static float WindowSlideKeep = 0.9f;   // ... but it keeps its sideways speed along the wall (the wall's is WallSlideKeep); gravity then drops it
    // House walls and pillars: share of the speed straight into the wall that comes back (lower = deader bounce).
    public static float WallBounce = 0.4f;
    public static float BeachBounce = 0.15f;      // how much of its speed a paper keeps off a beach object (hut, cabin, palm, steps)
    public static float BeachSlideKeep = 0.35f;   // and how much of its sliding speed
    // House walls and pillars: share of the sliding speed kept.
    public static float WallSlideKeep = 0.85f;
    public static float BoardBounce = 0.18f;      // the basketball backboard: a soft paper thuds and drops towards the ring
    public static float BoardSlideKeep = 0.55f;
    public static float RimBounce = 0.10f;        // the hoop ring is solid with a small bounce (10%): a paper that lands on it thuds and drops, it does not spring away
    public static float RimSlideKeep = 0.30f;

    // ============================================================ BIKE
    public static float BikeStartSpeed = 3f;
    public static float BikeMaxSpeed = 5f;
    // The speed follows a profile over the run, in steps: it holds the start speed for a while, rises to the plateau speed, holds there,
    // then rises to the max speed and stays there for the rest of the run. Times are seconds into the run (each rise eases in and out, so there is no jolt).
    public static float BikePlateauSpeed = 4f;
    public static float SpeedHoldStart = 60f;  // time held at the start speed
    public static float SpeedRise1 = 60f;      // start speed -> plateau speed
    public static float SpeedHold = 120f;      // time held at the plateau speed
    public static float SpeedRise2 = 60f;      // plateau speed -> max speed

    // Speed (m/s) a given number of seconds into the run.
    public static float SpeedAt(float t)
    {
        float r1 = Mathf.Max(0.01f, SpeedRise1), r2 = Mathf.Max(0.01f, SpeedRise2);
        if (t <= SpeedHoldStart) return BikeStartSpeed;
        t -= SpeedHoldStart;
        if (t <= r1) return Mathf.Lerp(BikeStartSpeed, BikePlateauSpeed, Mathf.SmoothStep(0f, 1f, t / r1));
        t -= r1;
        if (t <= SpeedHold) return BikePlateauSpeed;
        t -= SpeedHold;
        if (t <= r2) return Mathf.Lerp(BikePlateauSpeed, BikeMaxSpeed, Mathf.SmoothStep(0f, 1f, t / r2));
        return BikeMaxSpeed;
    }

    // Distance covered in the first t seconds of a rise from v0 to v1 lasting r seconds (the speed follows a smoothstep: 3u^2 - 2u^3, whose area is u^3 - u^4/2).
    static float RiseDistance(float v0, float v1, float r, float t)
    {
        float u = Mathf.Clamp01(t / r);
        return r * (v0 * u + (v1 - v0) * (u * u * u - 0.5f * u * u * u * u));
    }

    // Distance (metres) covered a given number of seconds into the run (the area under the speed profile).
    public static float DistanceAt(float t)
    {
        float r1 = Mathf.Max(0.01f, SpeedRise1), r2 = Mathf.Max(0.01f, SpeedRise2);
        float h0 = Mathf.Max(0f, SpeedHoldStart);
        float d = BikeStartSpeed * Mathf.Min(t, h0);
        if (t <= h0) return d;
        t -= h0;
        float a = Mathf.Min(t, r1);
        d += RiseDistance(BikeStartSpeed, BikePlateauSpeed, r1, a);
        if (t <= r1) return d;
        t -= r1;
        float h = Mathf.Min(t, SpeedHold);
        d += BikePlateauSpeed * h;
        if (t <= SpeedHold) return d;
        t -= SpeedHold;
        float b = Mathf.Min(t, r2);
        d += RiseDistance(BikePlateauSpeed, BikeMaxSpeed, r2, b);
        if (t <= r2) return d;
        return d + BikeMaxSpeed * (t - r2);
    }

    // ============================================================ RULES
    public static int StartingPapers = 15;
    // Consecutive deliveries needed for a hot streak (x2 score, and deliveries refill slow-mo).
    public static int HotStreakAt = 5;
    // Score multiplier by hit streak (every active area or hoop hit adds 1; only a missed customer building breaks it): entry 0 is streak 1, entry 1 is streak 2, and so on.
    // Past the end the last value stays. Streak 5 starts the hot streak, streak 10 the super hot streak.
    public static int[] ComboMultipliers = { 1, 1, 2, 3, 5, 5, 5, 8, 9, 10 };
    public static int SuperStreakAt = 10;
    // The bell scares hazards this far ahead of the bike.
    public static float BellRange = 20f;
    public static float BellCooldown = 0.6f;
    // Seconds the pause screen takes to fade away after un-pausing (no 3-2-1 any more).
    public static float ResumeCountdown = 0.5f;

    // ---- lives: the run ends when they reach zero
    public static int StartingLives = 3;
    public static int MaxLives = 5;
    // After losing a life you cannot lose another for this many seconds.
    public static float LifeInvulnerableSeconds = 1f;
    // Lives lost by each mistake (0 = that mistake is free). Smashing things (windows, dishes, crates, cans) is never a mistake: it is free.
    public static int LivesLostMissedCustomer = 1;
    public static int LivesLostHazard = 1;
    // Hearts (hit one with a paper to gain a life). At most one per stretch of road.
    public static float HeartFrom = 30f;
    public static float HeartChance = 0.30f;
    // Chance per stretch of road while you are down to your last life.
    public static float HeartChanceLowLives = 0.60f;
    // Points for a heart when you already have the maximum lives.
    public static int HeartPointsWhenFull = 50;
    // Slow-motion meter refilled by each delivery during a hot streak.
    public static float HotStreakRefill = 0.15f;

    // ============================================================ POINTS
    public static int PointsGarden = 25;
    public static int PointsPath = 40;
    public static int PointsDoorstep = 50;
    public static int PointsMailbox = 100;
    // ---- coins: earned as you play, paid out when the run ends
    public const int CoinsPerBasket = 3;     // every basket (each ring pays once)
    public const int CoinsPerBalcony = 1;    // every balcony hit, even if the house is never finished (a basket pays the ones it serves)
    // a house cleared pays one coin per storey (1, 2 or 3)

    public static int PointsBalcony = 75;
    // Bonus for ending on a precise spot (off: the base points above are the whole score; the paper still snaps to the spot).
    public static int SnapBonusDoorstep = 0;
    public static int SnapBonusMailbox = 0;
    public static int SnapBonusBalcony = 0;
    // Landing a paper on the lawn table.
    public static int PointsTable = 40;
    // A paper swished through the basketball hoop on a customer's lot, and the bonus for a swish on any other lot.
    public static int PointsHoop = 300;
    public static int SnapBonusHoop = 0;
    public static int PointsHoopBonus = 200;
    // The porch roof beside a wall hoop leans towards the ring and is slippery, so a paper that lands on it slides off into the ring
    // (plain physics, nothing scripted). That is one kind of trick shot; any bounce off a surface other than a backboard or ring counts.
    public static int TrickShotMultiplier = 2;   // a trick shot basket pays this many times a normal basket (the extra comes with the fiery burst)
    public static float ChuteTilt = 15f;           // degrees the roof leans towards the ring
    public static float RoofFriction = 0.08f;      // friction of that roof (low = slippery)
    public static float RoofMaxSpeed = 4f;         // a paper on a porch roof never slides faster than this (m/s)
    public static float RoofSlideAccel = 3f;       // extra downhill push (m/s^2) on a paper lying on a porch roof, so it slides off quickly (0 = plain gravity only)

    // ============================================================ ROAD AND HOUSES
    // Possible lot lengths along the road, and the empty grass between two lots.
    public static float[] LotLengths = { 6f, 7.5f, 10f, 12.5f };
    // The side view has houses on one side only, so they are packed tighter (shorter lots, smaller gaps): a house, lawn or hoop is on screen all the time.
    public static readonly float[] SideLotLengths = { 5.5f, 6.5f, 8f, 10f };
    public static float LotGap { get { return ChaseView ? 2.5f : 1.2f; } }
    // Side view: dressing that makes a lot a target or a feature is more likely (trees and porches are not: they would hide the houses).
    public const float SideDressingBoost = 1.5f;
    // Side view: feature lots (hoop yards, parks, courts, destructibles) per segment on the one built side.
    public static int FeaturesOnLeftSide { get { return ChaseView ? FeaturesPerSide : (FeaturesPerSide > 0 ? FeaturesPerSide * 2 : 0); } }
    // Road distance between customers at the start and at full difficulty.
    public static float CustomerGapStart = 14f;
    public static float CustomerGapEnd = 18f;

    // ---- Spawn odds: nothing unlocks at a fixed distance. Every extra is a chance that rises as the run goes on.
    // Two paths: SCENERY (trees, yards, parks, shops, crates, trampolines...) ramps up fast so the street is never dull, and
    // GAMEPLAY (cows, walkers, blockers, balcony targets) ramps up slowly so players can learn. The ...Chance values in this file are the
    // odds at the end of the ramp; at the start they are that value times a "start share". The odds climb quickly at first and then
    // flatten. Ramp times are seconds into the run (turned into distance with the speed profile above).
    public static float SceneryRampSeconds = 30f;
    public static float GameplayRampSeconds = 150f;
    public static float SceneryStart = 0.5f;       // scenery: start odds / full odds
    public static float TargetStart = 0.5f;        // balcony targets at a customer's house
    public static float HazardStart = 0.1f;        // walkers, cows, throw blockers
    public static float PlainStart = 45f;          // metres: the first houses carry no gameplay extras or hazards (scenery is not held back)
    public static int HazardBudget = 2;            // at most this many hazards (cow, walker, blocker) per stretch of road
    public static float TrampolineChance = 0.25f;  // share of empty lawns (no customer) with a trampoline at full ramp
    public static float TrampolineBounce = 1.8f;   // the speed into the pad comes back out at this multiple (1.8 = 180%), upwards on a lawn pad, off the wall on the wall pad (every trampoline, reverse baskets included)
    public static float TrampolineSlideKeep = 0.2f;  // ... and how much of the sideways speed a paper keeps
    public static float TrampolineMinBounce = 7f;   // a hit on a trampoline always throws the paper back at least this fast (m/s, away from the pad), so even a soft touch launches it
    public static float TrampolineMaxSpeed = 20f;    // a paper never leaves a trampoline faster than this (so repeated hops cannot add up to a rocket)
    public static float WallPadChance = 0.35f;     // share of customers' houses with a trampoline standing upright against the wall (a paper thrown at it bounces off)
    public static float ReverseHoopChance = 0.20f;  // share of empty lawns (no customer) with a reverse basket at full ramp: a trampoline with a ring above it
    public static float ReverseRingY = 2.7f;        // ring height of a reverse basket (the pad top is at 0.68; the pad throws a paper up fast, well past the ring): meant to be hard
    public static float ReverseOpeningHalf = 0.34f; // how far from the ring centre a paper may pass through it and still count
    // Beaches: a stretch of sand and sea in place of the houses on one side of the road (rarely both), only after BeachFrom metres.
    public static float BeachFrom = 500f;
    public static bool BeachTest = false;          // test switch: beaches from the very start, much more often
    public static float BeachFromNow { get { return BeachTest ? 0f : BeachFrom; } }
    public static float BeachChanceNow { get { return BeachTest ? 0.6f : BeachChance; } }
    public static float BeachChance = 0.06f;       // per stretch of road, when no beach is running
    public static float BeachBothShare = 0.12f;    // share of beaches that run along both sides
    public static int BeachSegmentsMin = 3;        // length of a beach, in stretches of road (30 m each)
    public static int BeachSegmentsMax = 5;
    public static Color SandColor = new Color(0.93f, 0.84f, 0.60f);
    public static float WalkerChance = 0f;         // share of customers who walk around the garden
    public static float CowChance = 0f;            // chance per stretch of road
    // FLOORS: a customer's house has 1 to 3 floors (ground, first, second). Every upper floor has 1 to 3 balconies and all of the house's floors
    // must be served. All of these ramp up with the distance (GameplayOdds), starting small.
    public static float BalconyTargetChance = 0.75f;   // share of customers with a first floor (and so a balcony to hit) at full ramp
    public static float SecondFloorChance = 0.50f;     // share of those with a second floor on top, at full ramp (starts at 0)
    public static float ExtraBalconyChance = 0.45f;    // chance of a 2nd balcony on an upper floor, and half that for a 3rd, at full ramp (starts at 0)
    public static float FloorHoopBonus = 0.25f;        // the building's hoop (on the first floor) serves every upper balcony at once and pays them by hand plus this share

    // 0 at the start, 1 once `seconds` of riding have passed: climbs quickly, then flattens.
    public static float Ramp(float distance, float seconds)
    {
        float r = Mathf.Clamp01(distance / Mathf.Max(1f, DistanceAt(seconds)));
        return 1f - (1f - r) * (1f - r);
    }
    // Odds of a scenery item at this distance.
    public static float SceneryOdds(float distance, float full)
    {
        if (full <= 0f) return 0f;
        return full * Mathf.Lerp(SceneryStart, 1f, Ramp(distance, SceneryRampSeconds));
    }
    // Odds of a gameplay item at this distance (0 while the road is still plain).
    public static float GameplayOdds(float distance, float full, float startShare)
    {
        if (distance < PlainStart || full <= 0f) return 0f;
        return full * Mathf.Lerp(startShare, 1f, Ramp(distance, GameplayRampSeconds));
    }

    // Share of the upper floors of scenery houses (no customer) that have balconies. A customer's upper floors always have them (see the FLOORS settings above).
    public static float BalconyChance = 0.5f;

    // ---- house dressing: chance per lot once unlocked (0-1)
    public static float TreeChance = 0.65f;
    public static float PorchChance = 0.40f;
    // Lots whose yard is cars / stall / garage / plants instead of a lawn target.
    public static float YardChance = 0.45f;
    // Lawn tables, and the chance that a table carries cups and plates (smashing the dishes costs nothing).
    public static float TableChance = 0.30f;
    public static float TableItemChance = 0.50f;
    // Chance that a lot without a customer is a small park (lawn, path, benches, trees, lamp) instead of a house.
    public static float ParkChance = 0.30f;
    // Dev menu cheats (not saved).
    public static bool GodMode = false;
    public static bool InfinitePapers = true;

    public static float TreeMinTop = 6.2f;      // every tree rises at least this high (the second floor spans 5.0 - 8.5 m)
    public static float TreeGirth = 1.7f;      // how much thicker than the first design the trunk, foot and roots are
    public static float TreeWidth = 1.35f;      // how much wider the leaves spread than the first design (the height is unchanged)
    public static float TreeMaxRadius = 2.6f;   // ...but never wider than this from the trunk, so a crown keeps off the wall
    public static float TreeMaxTop = 11.5f;    // the tallest reach the third floor (it spans 8.5 - 12.0 m)
    public static float GrassStep = 0.325f;      // grass: one tuft per this many metres on a house lawn (smaller = denser)
    public static float GrassStepPark = 0.425f;  // ...and in a park
    public static float SunSize = 92f;          // the sun / moon disc (it used to be 34)
    public static float SunHaloSize = 4.2f;     // the soft glow round it, as a multiple of the disc
    public static float SunDuskDrop = 60f;      // how far the sun sinks at dawn and dusk
    public static bool GrassTufts = true;       // low-poly grass tufts on lawns and in parks (visual only)
    public static float TreeSink = 0.40f;       // trees stand this far below the grass, so the foot grows out of the ground
    public static float DoorClear = 2.4f;       // a lawn tree's trunk stands at least this far along the road from the door (off the path and doormat)
    public static float ParkTreeGap = 1.6f;     // park trees: the least distance between two trunks (the crowns may overlap, the trunks never)
    public static int ParkTreeMax = 16;         // park trees: the most trees in one park      // ...and none higher than this
    public static float FoliageDrag = 5f;       // how strongly leaves hold a paper that flies into a tree (per second; the paper sinks at about 10 / this m/s)
    public static bool DepthVariety = true;     // some customers' houses and shops stand further back from the road
    public static float DepthSmall = 0.6f;     // how far back (m): 30% of them
    public static float DepthBig = 1.2f;       // ...and 20% of them (the rest stay at the footpath)
    public static bool CloseGaps = true;       // lots left without a house are removed and the houses stack up against each other
    public static float StackGap = 0f;         // space between stacked houses (m): they touch
    public static float MaxHouseLength = 15f;  // a stretched house or shop never gets longer than this
    public static float FillerMin = 6f;        // the road freed by removed lots gets a shop or small park if at least this much is free...
    public static float FillerTypical = 9f;    // ...usually this long...
    public static float FillerMax = 15f;       // ...and never longer than this
    public static bool BareScenery = true;   // plain scenery houses are left out: only customers' houses, shops, parks, courts and beaches stand on the street
    // Street lamps on the footpath (solid: papers bounce off) and low picket fences on some house lots (see-through).
    public static bool StreetLamps = true;
    public static float LampSpacing = 20f;   // distance between lamps on one side; the other side is offset by half
    public static float FenceChance = 0.15f;
    // ---- clutter controls (DEV menu, Clutter tab) ----
    // How many "feature" lots (park, car park, court, destructible pyramid, decorated yard) one side of a segment may have. 0 = no limit.
    public static int FeaturesPerSide = 1;
    // ---- the easy opening and the calmer street (SegmentSpawner.Opening.cs; not used on the how-to-play road) ----
    public static bool EasyOpening = true;        // the first seconds carry only customers' houses, and at most OpeningFeatures park / car park / court
    public static float EasyOpeningSeconds = 15f; // how long it lasts (seconds of a normal run; the bike is slow at first, so about 45 m)
    public static int OpeningFeatures = 1;        // parks, car parks and courts allowed inside the opening
    public static float EasyFirstCustomer = 12f;  // the first customer comes about this many metres ahead of the bike's start (about 20 without the easy opening)
    public static int MaxNonDeliveryRun = 2;      // never more non-delivery lots (shops, parks, car parks, courts) than this in a row on one side of the road (0 = no limit)
    public static int OpeningNonDelivery = 2;     // in the easy opening: at most this many non-delivery lots altogether, and never two together
    public static float NoTreesSeconds = 120f;    // no trees on the lawns for this long (0 = trees from the start)...
    public static float TreeFadeSeconds = 30f;    // ...then they come back over this long

    // ---- look ----
    public static bool PitchedRoofs = true;       // gable roofs instead of flat slabs (applies to lots spawned after a change)
    public static float PitchedRoofShare = 0.4f; // of the houses that may have a gable roof, how many do (the rest have a flat roof with a parapet and rooftop props)
    public static bool FacadeDetail = true;       // window frames, cornices, parapets, rooftop props, door surrounds and signs (House.Facade.cs); off = the plain walls
    public static float HotelChance = 0.3f;       // share of buildings with upper floors that are hotels (roof board and blade sign)
    public static float ChimneyChance = 0.35f;    // share of pitched roofs with a chimney
    public static int MsaaSamples = 4;            // edge smoothing (anti-aliasing): 1 = off, 2, 4 or 8 samples. 4 is cheap on phones and smooths the low-poly edges
    public static int TargetFps = 60;             // the frame rate cap: 30 (saves battery), 60, 90 or 120 (a Dev Menu slider; phones default to 30 without a cap)
    // The aim arrow floats flat at this height (metres above the road), clear of the kerb (17 cm), so it is never under the pavement and needs no
    // ground checks. 0 = follow the ground instead (a ray cast under every chevron). A DEV menu slider.
    public static float ArrowHeight = 0.30f;
    // A faint vertical line from the end of the aim arc straight down to the end of the aim arrow (joins the two, shows where the arc end is over the ground).
    public static bool AimDropLine = true;
    public static float RenderScale = 1f;         // 1 = draw at the screen's full resolution (below 1 looks soft and pixelated)
    public static bool BlobShadows = true;        // soft contact shadows under houses, trees, bushes and the bike
    public static bool AmbientGradient = true;    // cool sky light, darker warm ground
    public static float VignetteStrength = 0.35f; // screen edges darken a little (0 = off)
    public static float ParcelGlow = 0.4f;        // how strongly a thrown parcel glows white (0 = no halo; the parcel itself stays bright white)
    public static bool ParcelSeeThrough = true;   // the parcel's light shows through walls, roofs and trees
    public static float BoardGlowStrength = 0.28f; // the soft haze round every basketball backboard (0 = off)
    public static bool HoopXRay = true;            // the hoop rings show through walls and other things, dimmer
    public static float HoopXRayAlpha = 0.25f;      // how bright the ring looks through things (0 - 1)
    public static float LampLight = 1f;           // brightness of the real lamp and door lights at night
    public static float NightDarkness = 0.9f;     // how dark and foggy the night is (0 = bright, 1 = very dark)
    public static bool SunShadows = true;         // real soft sun shadows in the daytime phase only (fade out at dawn, dusk and rain)

    // Solid bushes along the house walls (papers bounce off them). Share of houses that have them; 0 = none.
    public static float WallBushChance = 0.5f;
    // Shops and restaurants: scenery buildings on some house lots (never a customer, never a park)
    public static float ShopChance = 0.2f;

    // Throw blockers: a billboard (breaks after one paper) or a clump of trees in front of some customers' houses.
    public static float WalledChance = 0.10f;        // share of customer houses behind a high wall at full ramp (the lawn, path and doormat are out: mailbox or balcony only)
    public static float ConstructionChance = 0.10f;  // share of customer houses with a building site in front (same rule)
    public static float BlockerChance = 0.14f;   // share of customer houses that get one at full ramp
    public static int BillboardPoints = 25;
    public static bool BackRows = true;       // buildings and trees behind the houses
    public static bool TransitionMist = false; // fog banks where a beach starts and ends (off for now; the DEV menu can switch it on)
    public static float MistStrength = 0.45f; // how thick the mist is at its core (0..1)
    public static bool YardProps = true;      // cars, garages, stalls, plants in yards
    public static bool ShowClouds = true;
    public static bool ShowHills = true;
    // Share of those lots that are car parks (paved, rows of parked cars, a P sign) instead of parks.
    public static float ParkingShare = 0.5f;
    // Chance that a park lot is a basketball court (it gets a lot of its own, as long as CourtLength): a hoop at each end, a bonus for each swish.
    public static float CourtShare = 0.35f;
    // Every basketball court is exactly this long (along the road). Only lots at least this long (plus a little) can hold one.
    public static float CourtLength = 18f;

    // ---- destructibles: a stack of crates or a pyramid of cans on lots without a customer (and in parks).
    // Hit them with papers. Clearing a whole stack pays papers back, so spamming them is worth it.
    public static float DestructibleChance = 0.5f;   // per eligible lot
    public static int CrateHits = 2;                  // hits to smash a crate
    public static int CratePoints = 12;
    public static int CanPoints = 8;
    public static float CrateChain = 0.15f;           // chance a broken crate also hits a neighbour
    public static float CanChain = 0.35f;
    public static float StackClearPapersPerUnit = 0.4f;   // papers refunded per crate/can when a whole pyramid is cleared
    public static int StackClearPointsPerUnit = 7;
    public static int StackClearPoints = 50;

    // ============================================================ CAMERA
    // Third-person camera. The built-in view is the SIDE view of a side runner: the camera stands on the right footpath, a little behind and above the
    // bike, turned about 70 degrees to look across the road at the houses on the left, so the bike rides from left to right. (Offset is from the bike; turn 0 would
    // look straight down the road.) The throw follows the camera's turn, so pulling left throws to the right on screen.
    public static Vector3 CameraOffset = new Vector3(4.83f, 6.46f, -2.37f);
    public static float CameraPitch = 8f;
    public static float CameraYaw = -69.9f;          // degrees the camera is turned sideways (0 = looking straight down the road)
    public static float CameraFov = 50f;
    public const float CameraMinHorizontalFov = 100f;   // the view is widened until at least this many degrees show across the screen
    // Side view only: while a paper is flying (and for a moment after it lands) the camera slides ahead along the road to centre it, then eases back.
    public static float CameraFlightFollow = 0.05f;     // how strongly the camera centres on a flying paper (0 = off)
    public static float CameraFlightMaxAhead = 3f;    // furthest the camera slides ahead of its normal spot (metres); also limited so the bike stays on screen
    // false = the side view above (the road's right-hand houses are left out: they would stand behind the camera); true = the original chase view
    // behind and above the bike, with houses on both sides. The DEV menu's Camera tab switches between them (kept on the device).
    public static bool ChaseView = false;
    // The chase view (behind and above the bike).
    public static readonly Vector3 ChaseOffset = new Vector3(0f, 12f, -14f);
    public const float ChasePitch = 36f, ChaseYaw = 0f, ChaseFov = 60f, ChaseMinHorizontalFov = 60f;
    public static float CameraHotStreakFovBonus = 10f;
    // A little zoom while you aim (slow-mo), into the point where the arc will land (the camera does not move): this share at a light pull,
    // and this share at a full pull (keep it modest: the far landing must stay well on screen).
    public static float AimZoomLight = 0.12f;
    public static float AimZoomFull = 0.22f;

    // ============================================================ WORLD
    // Distance ridden per day/night phase.
    public static float DistancePerPhase = 350f;
    // Rain in the rain phase (off by default: on a phone the specks read as white grain).
    public static bool EnableRain = true;

    // ============================================================ COLOURS (set from the DEV menu, saved on the phone)
    // 0 = delivery pads (lawn pad, mat, mailbox, balcony of a customer), 1 = grass beside the road, 2 = park lawns, 3 = far ground.
    public static Color PadColor = new Color(0.15f, 0.52f, 0.24f);
    public static Color GrassColor = new Color(0.17f, 0.44f, 0.21f);
    public static Color ParkColor = new Color(0.12f, 0.34f, 0.17f);
    public static Color TurfColor = new Color(0.12f, 0.34f, 0.17f);   // a served lawn: the dark turf colour of the grass (not a setting)
    public static Color FarGroundColor = new Color(0.08f, 0.26f, 0.12f);   // dark green behind the houses
    public static Color MailboxColor = new Color(1.00f, 0.35f, 0.15f);   // 4: an active customer's mailbox
    public static Color PathColor = new Color(0.93f, 0.85f, 0.62f);      // 5: an active customer's path and doorstep mat
    public static int ColorVersion;   // bumped on every change; objects re-colour themselves when they see a new number

    static readonly string[] ColorKeys = { "col_pad", "col_grass", "col_park", "col_far2", "col_mail", "col_path" };

    public static Color GetColor(int i)
    {
        switch (i) { case 0: return PadColor; case 1: return GrassColor; case 2: return ParkColor; case 4: return MailboxColor; case 5: return PathColor; default: return FarGroundColor; }
    }

    public static void SetColor(int i, Color c, bool save = true)
    {
        switch (i) { case 0: PadColor = c; break; case 1: GrassColor = c; break; case 2: ParkColor = c; break; case 4: MailboxColor = c; break; case 5: PathColor = c; break; default: FarGroundColor = c; break; }
        ColorVersion++;
        if (!save) return;
        PlayerPrefs.SetFloat(ColorKeys[i] + "_r", c.r);
        PlayerPrefs.SetFloat(ColorKeys[i] + "_g", c.g);
        PlayerPrefs.SetFloat(ColorKeys[i] + "_b", c.b);
    }

    public static void LoadColors()
    {
        for (int i = 0; i < 6; i++)
        {
            if (!PlayerPrefs.HasKey(ColorKeys[i] + "_r")) continue;
            SetColor(i, new Color(PlayerPrefs.GetFloat(ColorKeys[i] + "_r"), PlayerPrefs.GetFloat(ColorKeys[i] + "_g"),
                                  PlayerPrefs.GetFloat(ColorKeys[i] + "_b")), false);
        }
    }

    // 0 original, 1 bright pads, 2 dull grass, 3 blue pads
    public static void ApplyColorPreset(int p)
    {
        Color pad = new Color(0.25f, 0.80f, 0.35f), grass = new Color(0.38f, 0.70f, 0.32f);
        Color park = new Color(0.34f, 0.78f, 0.36f), far = new Color(0.35f, 0.66f, 0.30f);
        if (p == 1) pad = new Color(0.30f, 1.00f, 0.42f);
        if (p == 2) { grass = new Color(0.45f, 0.60f, 0.37f); park = new Color(0.43f, 0.62f, 0.38f); far = new Color(0.43f, 0.58f, 0.36f); }
        if (p == 3) pad = new Color(0.20f, 0.60f, 1.00f);
        SetColor(0, pad); SetColor(1, grass); SetColor(2, park); SetColor(3, far);
        SetColor(4, new Color(1.00f, 0.35f, 0.15f)); SetColor(5, new Color(0.93f, 0.85f, 0.62f));
    }
}
