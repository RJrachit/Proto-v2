using System.Collections.Generic;
using UnityEngine;

// House (partial) - Basketball hoops: the wall-mounted board on lawn houses and the hoop/ring scoring.
// The main part of the class is in House.cs.
public partial class House
{
    // A backboard and ring on the house wall, over the lawn, for lawn houses only. The paved-yard houses have the standing hoop (Yard.Hoop)
    // instead, so a house never has both. Uses the same ring zone, so it scores like the standing hoop: a delivery at a customer's, a bonus elsewhere.
    // On a house with a porch roof the ring may stand just beside the roof (the roof trick shot): the roof leans towards the ring, and a paper
    // that lands on the slope towards the ring slides off into the ring (plain physics, see SetUpRoofShot). The roof then becomes a gable that
    // stands higher, and the ring hangs lower, just below the roof's low edge.
    bool wallHoop;
    // Every backboard is the same size and stands the same way over its ring: 1.6 wide, 1.05 high, its centre 0.4 above the ring.
    const float BoardW = 1.6f, BoardH = 1.05f, BoardUp = 0.4f;
    const float RoofRingGap = 1.72f;       // ring centre from the door centre when it stands beside the porch roof

    void LayoutWallHoop(float L, float doorZ, bool wallMail, float mailZ, bool customer)
    {
        if (hoopZone == null || hoopSnapT == null || L < 4.2f) return;
        bool porchOn = porchRoofT != null && porchRoofT.gameObject.activeSelf;
        float h = Hash01(transform.position.z * 0.419f + (side > 0 ? 17.3f : 52.1f));
        float chance = customer ? (porchOn ? 0.7f : 0.5f) : (porchOn ? 0.45f : 0.3f);   // porch houses get more: they hold the trick shot
        if (h >= chance) return;

        float half = L * 0.5f;
        float hz = 0f;
        int dir = 0;   // which way the roof leans (towards the ring); 0 = an ordinary wall hoop

        // beside the porch roof, if there is room on either side
        if (porchOn)
        {
            int first = Hash01(h * 57.3f + 1.9f) < 0.5f ? 1 : -1;
            for (int k = 0; k < 2 && dir == 0; k++)
            {
                int d2 = k == 0 ? first : -first;
                float z = doorZ + d2 * RoofRingGap;
                if (z > -half + 0.9f && z < half - 0.9f && !BalconyAbove(z, 1.3f) && !(wallMail && Mathf.Abs(z - mailZ) < 1.6f))
                { dir = d2; hz = z; }
            }
        }

        // otherwise a spot along the wall, clear of the door, the balcony and a wall mailbox
        if (dir == 0)
        {
            int found = 0;
            float[] ok = new float[16];
            for (float z = -half + 1.3f; z <= half - 1.3f && found < ok.Length; z += 0.6f)
            {
                if (Mathf.Abs(z - doorZ) < 1.9f) continue;
                if (BalconyAbove(z, 1.3f)) continue;
                if (wallMail && Mathf.Abs(z - mailZ) < 1.6f) continue;
                ok[found++] = z;
            }
            if (found == 0) return;
            hz = ok[Mathf.Min(found - 1, (int)(Hash01(h * 91.7f + 3.1f) * found))];
        }
        bool combo = dir != 0;

        float s = side;
        float ringY = combo ? 2.45f : 3.1f;
        float ringX = s * 7.88f;   // 7 cm clear of the board, like the other hoops
        WallBoard(ringY, BoardUp, BoardH, hz);   // (the same board everywhere, the porch-roof combo included)
        RingParts(ringX, ringY, hz);

        SetZone(hoopZone, new Vector3(ringX, ringY, hz), new Vector3(0.9f, 0.6f, 0.9f));
        hoopZone.openingHalf = 0f;
        hoopSnapT.localPosition = new Vector3(ringX, ringY - 0.5f, hz);
        hoopZone.gameObject.SetActive(true);
        wallHoop = true;
        if (combo) LayoutPorchRoof(dir, doorZ, true);   // the roof towards the ring feeds it (the trick shot)
    }

    // The board on the house wall with its ring bracket: ringY = ring height, by = board centre above the ring, bh = board height.
    Transform WallBoard(float ringY, float by, float bh, float hz)
    {
        float s = side;
        float wallX = s * 8.47f;
        Color frame = new Color(0.25f, 0.28f, 0.34f);
        var board = Part(new Vector3(wallX - s * 0.05f, ringY + by, hz), new Vector3(0.1f, bh, 1.6f), Color.white, true);   // backboard
        SoftBoard(board);
        BoardGlow(new Vector3(wallX - s * 0.05f, ringY + by, hz), bh, true);
        Part(new Vector3(wallX - s * 0.11f, ringY + by - 0.12f, hz), new Vector3(0.04f, bh * 0.48f, 0.7f), new Color(0.55f, 0.65f, 0.80f), false);   // the square
        Part(new Vector3(wallX - s * 0.02f, ringY + by, hz), new Vector3(0.12f, bh + 0.07f, 1.68f), frame, false);                  // dark edge behind
        Part(new Vector3(s * 8.12f, ringY, hz), new Vector3(0.5f, 0.06f, 0.06f), frame, false);                                    // ring bracket
        return board;
    }

    // The marks every backboard carries: a dark edge all round and the light-blue shooting square. They show on both faces, so a board looks
    // the same from whichever side you pass it. c = board centre, bh = board height (width 1.6), thinX = the board is thin along x (else along z).
    // Used by the court boards and the standing hoops (the wall board draws its own marks, WallBoard).
    static readonly Color BoardFrameCol = new Color(0.25f, 0.28f, 0.34f);
    static readonly Color BoardSquareCol = new Color(0.55f, 0.65f, 0.80f);

    void BoardMarks(Vector3 c, float bh, bool thinX)
    {
        BoardGlow(c, bh, thinX);
        Part(c, thinX ? new Vector3(0.08f, bh + 0.07f, 1.68f) : new Vector3(1.68f, bh + 0.07f, 0.08f), BoardFrameCol, false);   // (thinner than the board, so only a rim shows round it, never over its face)
        Part(c + new Vector3(0f, -0.12f, 0f), thinX ? new Vector3(0.14f, bh * 0.48f, 0.7f) : new Vector3(0.7f, bh * 0.48f, 0.14f), BoardSquareCol, false);
    }

    // A ring (solid, dead rim) and its stringy net (not solid) centred on (x, y, z).
    void RingParts(float x, float y, float z)
    {
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            var rim = Part(new Vector3(x + Mathf.Cos(a) * 0.45f, y, z + Mathf.Sin(a) * 0.45f),
                 new Vector3(0.10f, 0.10f, 0.38f), Prim.RimOrange, true, new Vector3(0f, -k * 45f, 0f));
            SoftRim(rim);
            GlowRim(rim);   // (orange and bright: the ring must stand out)
        }
        NetParts(x, y, z);
    }

    // The net under a ring (not solid: papers pass through): a hanging mesh of crossing strings that narrows towards the bottom, and a small
    // ring of cord round the bottom. Every hoop uses this one, so the nets all look the same.
    void NetParts(float x, float y, float z)
    {
        const float topR = 0.40f, botR = 0.22f, drop = 0.52f;
        Color cord = new Color(0.97f, 0.97f, 0.95f);
        const int n = 8;
        for (int set = 0; set < 2; set++)                       // two sets of strings, slanting opposite ways, so they cross in diamonds
            for (int k = 0; k < n; k++)
            {
                float a0 = (k * 360f / n) * Mathf.Deg2Rad;
                float a1 = (k * 360f / n + (set == 0 ? 1f : -1f) * 360f / n) * Mathf.Deg2Rad;
                Vector3 top = new Vector3(x + Mathf.Cos(a0) * topR, y - 0.04f, z + Mathf.Sin(a0) * topR);
                Vector3 bot = new Vector3(x + Mathf.Cos(a1) * botR, y - drop, z + Mathf.Sin(a1) * botR);
                Vector3 d = bot - top;
                float len = d.magnitude;
                Part((top + bot) * 0.5f, new Vector3(0.028f, len, 0.028f), cord, false, Quaternion.FromToRotation(Vector3.up, d / len).eulerAngles);
            }
        for (int k = 0; k < n; k++)                              // the cord round the bottom
        {
            float a = (k * 360f / n) * Mathf.Deg2Rad;
            float chord = 2f * botR * Mathf.Sin(Mathf.PI / n);
            float rc = botR * Mathf.Cos(Mathf.PI / n);
            float am = a + Mathf.PI / n;
            Part(new Vector3(x + Mathf.Cos(am) * rc, y - drop, z + Mathf.Sin(am) * rc), new Vector3(0.04f, 0.04f, chord + 0.02f), cord, false, new Vector3(0f, -am * Mathf.Rad2Deg, 0f));
        }
    }

    // A paper dropped through one of the rings. At a customer's house the hoop is a delivery; elsewhere a small bonus, once per ring.
    // A paper that bounced off anything but a backboard or ring before going in (trick) pays the basket again on top (twice a normal basket in all), once, with a big fiery burst.
    public void HandleHoop(HouseTarget zone, Vector3 pos, bool snapped, bool trick = false)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.NoteScorePos(pos);   // (where the coin dust starts)
        if (Subscriber)
        {
            int before = gm.Streak;
            int ff = FloorHoopIndex(zone);
            if (ff > 0)
            {
                HandleFloorHoop(ff, pos, trick);   // the ring of an upper floor serves that floor
                if (gm.Streak == before) gm.CountHit();   // (a ring that was already cleared still counts as a hit)
                return;
            }
            bool open = !resolved && !groundDone && groundFloor;
            HandleHit(HouseTarget.Kind.Hoop, pos, snapped);
            if (gm.Streak == before) gm.CountHit();      // (the same for the ground-floor ring)
            if (open) gm.BasketScored();
            if (trick && open) gm.TrickBonus(Scoring.Points(HouseTarget.Kind.Hoop, snapped) * (GameConfig.TrickShotMultiplier - 1), "Trick Shot!", pos);
            return;
        }

        bool second = zone == hoopZone2;
        if (second ? hoopUsed2 : hoopUsed) { gm.CountHit(); return; }   // (a ring already used pays nothing, but the hit counts)
        if (second) hoopUsed2 = true; else hoopUsed = true;
        gm.BasketScored();
        gm.Bonus(GameConfig.PointsHoopBonus, "Swish! +", pos);
        if (trick) gm.TrickBonus(GameConfig.PointsHoopBonus * (GameConfig.TrickShotMultiplier - 1), "Trick Shot!", pos);
    }
}
