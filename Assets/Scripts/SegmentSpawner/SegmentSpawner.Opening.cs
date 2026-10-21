using System.Collections.Generic;
using UnityEngine;

// SegmentSpawner (partial) - The easy opening and the calmer street.
//   - the first seconds of a run (GameConfig.EasyOpeningSeconds) carry only customers' houses and shops (no yard props, crates or cans), and at most
//     GameConfig.OpeningFeatures park / car park / court (the first customer also comes sooner)
//   - never more than GameConfig.MaxNonDeliveryRun non-delivery lots (shops, parks, car parks, courts) in a row on one side of the road;
//     in the opening at most GameConfig.OpeningNonDelivery of them, never two together; and never an empty lot
//   - no trees on the lawns for the first GameConfig.NoTreesSeconds of riding, then they fade in over GameConfig.TreeFadeSeconds
// Distances come from the bike's speed profile (GameConfig.DistanceAt), so "seconds" here means the seconds of a normal run.
// None of this applies to the how-to-play road. The main part of the class is in SegmentSpawner.cs.
public partial class SegmentSpawner
{
    float placeStartZ;                       // where the segment being laid out starts
    int openingFeatures;                     // parks, car parks and courts placed inside the opening so far
    int openingND;                           // non-delivery lots placed inside the opening so far
    readonly int[] ndRun = new int[2];       // non-delivery lots in a row at the end of the road laid so far (0 = left side, 1 = right)

    bool InOpening(float absZ)
    {
        return GameConfig.EasyOpening && !Tutorial.Active && absZ < GameConfig.DistanceAt(GameConfig.EasyOpeningSeconds);
    }

    // The opening: a plain lot (shops stay: they are never deliveries), and only so many park-like ones. (What stops being a lot becomes open yard, which the gap closing handles.)
    void ApplyOpening(Lot lot, House.Role role)
    {
        lot.destruct = 0;
        lot.yard = House.Yard.Lawn;
        if (role == House.Role.Scenery && (lot.court || lot.park))
        {
            if (openingFeatures >= GameConfig.OpeningFeatures) { lot.court = false; lot.park = false; lot.parking = false; }
            else openingFeatures++;
        }
    }

    // 0 = no lawn trees yet, 1 = the normal chance; rises between the end of the tree-free stretch and the end of the fade.
    float TreeScale(float absZ)
    {
        if (Tutorial.Active || GameConfig.NoTreesSeconds <= 0f) return 1f;
        float start = GameConfig.DistanceAt(GameConfig.NoTreesSeconds);
        float fade = GameConfig.DistanceAt(GameConfig.NoTreesSeconds + Mathf.Max(0f, GameConfig.TreeFadeSeconds)) - start;
        return Mathf.Clamp01((absZ - start) / Mathf.Max(1f, fade));
    }

    // After the gaps are closed: walk each side of the new stretch in road order. A "non-delivery lot" is anything drawn that is not a customer's house
    // (a shop, park, car park or court). There are never more than GameConfig.MaxNonDeliveryRun of them in a row, and in the easy opening never more than
    // GameConfig.OpeningNonDelivery altogether and never two together. Whatever would break that becomes a customer's house instead (the lot keeps its length).
    // A lot that is still bare (only when the gap closing is off) becomes a shop, so there is never an empty space. The count carries on from the stretch before.
    void EnforceStreetRhythm()
    {
        int max = GameConfig.MaxNonDeliveryRun;
        for (int si = 0; si < 2; si++)
        {
            var side = new List<Lot>();
            foreach (var lot in lots) if ((lot.house.side < 0) == (si == 0)) side.Add(lot);
            side.Sort((a, b) => a.z.CompareTo(b.z));
            foreach (var lot in side)
            {
                if (lot.beach) { ndRun[si] = 0; continue; }
                if (lot.role == House.Role.Customer) { ndRun[si] = 0; continue; }

                float absZ = placeStartZ + lot.z;
                bool opening = absZ >= 0f && InOpening(absZ);   // (the lots behind the bike at the start do not count)
                bool mustBeCustomer = (max > 0 && ndRun[si] >= max) || (opening && (openingND >= GameConfig.OpeningNonDelivery || ndRun[si] >= 1));
                if (mustBeCustomer && lot.redo != null)
                {
                    lot.role = House.Role.Customer;
                    lot.park = false; lot.parking = false; lot.court = false; lot.destruct = 0;
                    lot.house.ForceShop = false;
                    lot.redo(lot.z, lot.length);   // (laid out again as a customer's house)
                    ndRun[si] = 0;
                    continue;
                }
                if (lot.house.IsBare && lot.redo != null)
                {
                    lot.park = false; lot.parking = false; lot.court = false; lot.destruct = 0;
                    lot.house.ForceShop = true;
                    lot.redo(lot.z, lot.length);   // (a shop: no empty lot)
                }
                ndRun[si]++;
                if (opening) openingND++;
            }
        }
    }
}
