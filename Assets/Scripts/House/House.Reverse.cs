using UnityEngine;

// House (partial) - The reverse basket: a trampoline with a ring above it. A paper that lands on the pad is thrown almost straight up and passes
// through the ring from below (Paper scores it on the way up; one that drops in from above scores as well). The ring is either on the house wall
// or on a stand beside the wall. Nothing is scripted: the pad's bounce and the ring's position do all the work.
// Only on empty scenery lawns, and it takes the place of the plain trampoline and of the wall hoop, so a house never has two hoops.
// The main part of the class is in House.cs.
public partial class House
{
    bool reverseHoop;                       // this lot has a reverse basket
    bool reverseOnWall;                     // ... with its ring on the wall (otherwise on a stand)

    const float ReverseDrift = 0.45f;       // how far a bounced paper drifts towards the wall on its way up to the ring (the pad keeps half its sideways speed and throws it up fast); sets where the landing mark is

    // Decides whether this lot gets a reverse basket, and where along the wall the trampoline stands (z).
    bool PlanReverseHoop(float L, float doorZ, bool wallMail, float mailZ, bool eligible, out float z)
    {
        z = 0f;
        if (!eligible || Tutorial.Active || hoopZone == null || hoopSnapT == null || L < 4.6f) return false;
        float p = GameConfig.SceneryOdds(transform.position.z, GameConfig.ReverseHoopChance);
        if (p <= 0f || Hash01(transform.position.z * 1.913f + (side > 0 ? 7.7f : 41.3f)) >= p) return false;

        z = TrampolineZ(L, doorZ);
        if (Mathf.Abs(z - doorZ) < 2.7f) return false;                        // clear of the door strip and the porch
        if (BalconyAbove(z, 1.3f)) return false;                              // and of a balcony overhead
        if (wallMail && Mathf.Abs(z - mailZ) < 1.9f) return false;           // and of a wall mailbox
        reverseOnWall = !bare && Hash01(transform.position.z * 3.171f + (side > 0 ? 29.9f : 11.3f)) < 0.5f;   // (no house, no wall: the ring stands on a pole)
        return true;
    }

    void LayoutReverseHoop(float z)
    {
        float s = side;
        float ringY = GameConfig.ReverseRingY;
        float ringAbs = reverseOnWall ? 7.88f : 6.9f;       // ring centre from the road side (wall version hangs on the wall; the stand is 0.7 m out from it)
        float padAbs = reverseOnWall ? 6.9f : 5.9f;         // the pad stands so the landing spot is under the ring (less the drift)
        float ringX = s * ringAbs;

        PlaceTrampoline(padAbs, z, ringAbs - ReverseDrift - padAbs);

        if (reverseOnWall)
        {
            WallBoard(ringY, 0.4f, 1.05f, z);
        }
        else
        {
            Color steel = new Color(0.45f, 0.45f, 0.48f);
            Part(new Vector3(s * (ringAbs + 0.8f), (ringY + 1.0f) * 0.5f, z), new Vector3(0.15f, ringY + 1.0f, 0.15f), steel, true);      // pole, behind the board
            SoftBoard(Part(new Vector3(s * (ringAbs + 0.6f), ringY + 0.4f, z), new Vector3(0.1f, 1.05f, 1.6f), Color.white, true));       // backboard, facing the road
            BoardMarks(new Vector3(s * (ringAbs + 0.6f), ringY + 0.4f, z), 1.05f, true);
        }
        RingParts(ringX, ringY, z);

        SetZone(hoopZone, new Vector3(ringX, ringY, z), new Vector3(0.9f, 0.6f, 0.9f));
        hoopZone.openingHalf = GameConfig.ReverseOpeningHalf;
        hoopZone.openingHalfZ = 0f;
        hoopZone.reverse = true;
        hoopSnapT.localPosition = new Vector3(ringX, ringY - 0.5f, z);
        hoopZone.gameObject.SetActive(true);
        wallHoop = true;   // (the one hoop of this house)
    }
}
