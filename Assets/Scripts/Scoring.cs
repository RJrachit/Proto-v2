using UnityEngine;

// All delivery points in one place, so they are easy to change later.
// Difficulty order: Garden (easiest) < Doorstep < Mailbox (hardest). Balcony stays as the high-throw bonus.
// A paper that ends on a precise spot (mailbox, doorstep mat, balcony floor) earns the snap bonus on top.
// How it got there makes no difference: a bank shot scores the same as a straight throw.
public static class Scoring
{
    public static int Base(HouseTarget.Kind kind)
    {
        switch (kind)
        {
            case HouseTarget.Kind.Garden:  return GameConfig.PointsGarden;
            case HouseTarget.Kind.Path:    return GameConfig.PointsPath;   // on the customer's lot, but not on the mat
            case HouseTarget.Kind.Door:    return GameConfig.PointsDoorstep;   // doorstep
            case HouseTarget.Kind.Mailbox: return GameConfig.PointsMailbox;
            case HouseTarget.Kind.Balcony: return GameConfig.PointsBalcony;
            case HouseTarget.Kind.Table:   return GameConfig.PointsTable;
            case HouseTarget.Kind.Hoop:    return GameConfig.PointsHoop;
            case HouseTarget.Kind.Lifeguard: return GameConfig.PointsMailbox;   // a dunk into the lifeguard hut is worth the same as a mailbox
            case HouseTarget.Kind.Palm:    return GameConfig.PointsHoop;   // a paper that stays on top of a palm is worth a basket
            default: return 0;
        }
    }

    // Which spots the paper jumps to (the mat, the mailbox, the balcony floor, a hoop). Independent of the snap bonus, which is now 0.
    public static bool Snaps(HouseTarget.Kind kind)
    {
        return kind == HouseTarget.Kind.Door || kind == HouseTarget.Kind.Mailbox
            || kind == HouseTarget.Kind.Balcony || kind == HouseTarget.Kind.Hoop;
    }

    public static int SnapBonus(HouseTarget.Kind kind)
    {
        switch (kind)
        {
            case HouseTarget.Kind.Door:    return GameConfig.SnapBonusDoorstep;
            case HouseTarget.Kind.Mailbox: return GameConfig.SnapBonusMailbox;
            case HouseTarget.Kind.Balcony: return GameConfig.SnapBonusBalcony;
            case HouseTarget.Kind.Hoop:    return GameConfig.SnapBonusHoop;
            default: return 0;   // the garden has no snap: the paper just lands on the lawn
        }
    }

    public static int Points(HouseTarget.Kind kind, bool snapped)
    {
        return Base(kind) + (snapped ? SnapBonus(kind) : 0);
    }

    // A dunk through the building's hoop serves every upper balcony at once: what they pay by hand plus FloorHoopBonus (25%).
    // Every balcony pays the same, whichever floor it is on (flat, not rising with height).
    public static int FloorHoop(int handTotal)
    {
        return Mathf.RoundToInt(handTotal * (1f + GameConfig.FloorHoopBonus));
    }
}
