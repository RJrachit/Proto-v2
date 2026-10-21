using System.Collections.Generic;
using UnityEngine;

// SegmentSpawner (partial) - Lots
// The main part of the class is in SegmentSpawner.cs.
public partial class SegmentSpawner
{
    // At most one heart per stretch of road, hanging anywhere on any lot: balconies, porch roofs, garages, cars, stalls, trees.
    // The dice are always rolled, so the route is the same for everyone whatever their lives.
    void PlaceHeart(Segment s, float startZ)
    {
        s.heart.Hide();
        double roll = rng.NextDouble();
        int pick = rng.Next(1000000);
        if (startZ < GameConfig.HeartFrom) return;

        var gm = GameManager.Instance;
        bool low = gm != null && gm.Lives <= 1;
        float chance = low ? GameConfig.HeartChanceLowLives : GameConfig.HeartChance;
        if (roll >= chance) return;

        heartSpots.Clear();
        foreach (var lot in lots) lot.house.AddHeartSpots(heartSpots);
        if (heartSpots.Count == 0) return;
        s.heart.Show(heartSpots[pick % heartSpots.Count]);
    }

    // Picks lot lengths until the next one no longer fits; the leftover road stays green. The lots are shifted by a
    // random share of the leftover, so the green gaps move around.
    void FillSide(House[] pool, float treeP, float porchP, float yardP, float tableP, float parkP, float destP, bool beach, int edge, bool empty = false)
    {
        int firstLot = lots.Count;
        int count = 0;
        float used = 0f;
        while (!empty && count < pool.Length)
        {
            int fits = 0;
            for (int i = 0; i < lotLengths.Length; i++)
                if (used + lotLengths[i] + lotGap <= segmentLength + 0.001f) fits++;
            if (fits == 0) break;

            // Sometimes the next lot is a basketball court: a lot as long as the court, if it fits in what is left of the segment.
            float courtLen = GameConfig.CourtLength;
            if (parkP > 0f && rng.NextDouble() < parkP * GameConfig.CourtShare && used + courtLen + lotGap <= segmentLength + 0.001f)
            {
                pickedCourt[count] = true;
                pickedLengths[count++] = courtLen;
                used += courtLen + lotGap;
                continue;
            }
            pickedCourt[count] = false;

            int pick = rng.Next(fits);
            float len = lotLengths[0];
            for (int i = 0; i < lotLengths.Length; i++)
            {
                if (used + lotLengths[i] + lotGap > segmentLength + 0.001f) continue;
                if (pick == 0) { len = lotLengths[i]; break; }
                pick--;
            }
            pickedLengths[count++] = len;
            used += len + lotGap;
        }

        // The leftover road is shared out among the lots (they grow a little) so no big bare gaps are left between them.
        float extra = count > 0 ? (segmentLength - used) / count : 0f;
        float cursor = 0f;
        int features = 0;   // feature lots so far on this side (see GameConfig.FeaturesPerSide)
        int featureCap = pool == null || pool.Length == 0 || pool[0].side < 0 ? GameConfig.FeaturesOnLeftSide : GameConfig.FeaturesPerSide;
        for (int i = 0; i < pool.Length; i++)
        {
            if (i >= count) { pool[i].Vacate(); continue; }

            float len = pickedLengths[i] + extra;
            double r = rng.NextDouble();
            var lot = new Lot();
            lot.house = pool[i];
            lot.z = cursor + lotGap * 0.5f + len * 0.5f;
            lot.length = len;
            lot.door = (House.DoorPos)rng.Next(3);
            lot.mail = r < 0.25 ? House.MailPos.RoadPast
                     : r < 0.50 ? House.MailPos.RoadBeside
                     : r < 0.70 ? House.MailPos.RoadFront
                     : r < 0.85 ? House.MailPos.Gate
                     : House.MailPos.Wall;
            lot.flip = rng.NextDouble() < 0.5;
            bool decorated = rng.NextDouble() < yardP;
            int yardKind = rng.Next(5);
            lot.yard = decorated ? (House.Yard)(1 + yardKind) : House.Yard.Lawn;
            lot.tree = rng.NextDouble() < treeP;
            lot.porch = rng.NextDouble() < porchP;
            lot.table = rng.NextDouble() < tableP;
            lot.tableItems = rng.NextDouble() < GameConfig.TableItemChance;
            lot.color = HouseColors[rng.Next(HouseColors.Length)];
            lot.court = pickedCourt[i];
            lot.park = rng.NextDouble() < parkP || lot.court;
            lot.parking = rng.NextDouble() < GameConfig.ParkingShare && !lot.court;
            rng.NextDouble();   // (keeps the dice sequence the same as before courts got their own lots)
            int destKind = 1 + rng.Next(2);
            lot.destruct = rng.NextDouble() < destP ? destKind : 0;

            // Keep it calm: only so many feature lots per side of a segment; the rest become plain houses. (The dice above are
            // always rolled, so the route does not change when the limit does.) A court always stays what it is.
            bool feature = lot.park || lot.destruct > 0 || lot.yard != House.Yard.Lawn;
            if (feature && !lot.court && featureCap > 0 && features >= featureCap)
            {
                lot.park = false; lot.parking = false; lot.destruct = 0; lot.yard = House.Yard.Lawn;
            }
            else if (feature) features++;
            lot.beach = beach;
            if (beach) { lot.yard = House.Yard.Lawn; lot.tree = false; lot.porch = false; lot.table = false; lot.park = false; lot.parking = false; lot.court = false; lot.destruct = 0; }
            lots.Add(lot);
            cursor += len + lotGap;
        }
        // a beach eases in and out: the lot next to the houses (the first one at the start of the stretch, the last one at its end) is a dune lot
        if (beach && edge != 0 && lots.Count > firstLot)
            lots[edge == 1 ? firstLot : lots.Count - 1].edge = edge;
    }

    // Lots that ended up without a house (plain scenery, see House.IsBare) are taken out and the houses stack up against each other
    // (GameConfig.StackGap between them). The road that frees up is filled with a shop or a small park in place of a removed lot, if it is long
    // enough for one; whatever is left is shared out by stretching the buildings a little (never beyond GameConfig.MaxHouseLength, or 1.35 times their
    // own length) and, last of all, by widening the gaps. Courts and beaches keep their length.
    void CloseGaps()
    {
        for (int sideSign = -1; sideSign <= 1; sideSign += 2)
        {
            var side = new List<Lot>();
            foreach (var lot in lots) if (lot.house.side * sideSign > 0) side.Add(lot);
            bool anyBeach = false;
            foreach (var lot in side) if (lot.beach) anyBeach = true;
            if (side.Count == 0 || anyBeach) continue;   // (a beach keeps its own layout)

            foreach (var lot in side) if (lot.court) lot.length = Mathf.Min(lot.length, GameConfig.CourtLength);   // (no spare grass beside a court)
            float gap = GameConfig.StackGap;
            float room = segmentLength;
            foreach (var lot in side) if (!lot.house.IsBare) room -= lot.length + gap;

            // each removed lot, in order, becomes a filler while there is room for one; otherwise it goes
            var ordered = new List<Lot>();
            var gone = new List<Lot>();
            var orig = new List<float>();
            foreach (var lot in side)
            {
                if (!lot.house.IsBare) { ordered.Add(lot); orig.Add(lot.length); continue; }
                if (room >= GameConfig.FillerMin + gap)
                {
                    float f = room - gap <= GameConfig.FillerMax ? room - gap : GameConfig.FillerTypical;
                    room -= f + gap;
                    lot.length = f;
                    lot.court = false; lot.destruct = 0; lot.tree = false; lot.tableItems = false; lot.table = false;
                    if (rng.NextDouble() < 0.5) { lot.park = true; lot.parking = rng.NextDouble() < 0.3; lot.house.ForceShop = false; }
                    else { lot.park = false; lot.parking = false; lot.house.ForceShop = true; }
                    ordered.Add(lot); orig.Add(f);
                }
                else gone.Add(lot);
            }

            int n = ordered.Count;
            var len = new float[n];
            for (int i2 = 0; i2 < n; i2++) len[i2] = orig[i2];
            float extra = room;
            for (int pass = 0; pass < 4 && extra > 0.01f; pass++)
            {
                int open = 0;
                for (int i2 = 0; i2 < n; i2++) if (CanStretch(ordered[i2], len[i2], orig[i2])) open++;
                if (open == 0) break;
                float share = extra / open;
                for (int i2 = 0; i2 < n; i2++)
                {
                    if (!CanStretch(ordered[i2], len[i2], orig[i2])) continue;
                    float add = Mathf.Min(share, Mathf.Min(GameConfig.MaxHouseLength, orig[i2] * 1.35f) - len[i2]);
                    len[i2] += add;
                    extra -= add;
                }
            }
            float g = gap + Mathf.Max(0f, extra) / Mathf.Max(1, n);
            float cursor = 0f;
            for (int i2 = 0; i2 < n; i2++)
            {
                var lot = ordered[i2];
                lot.length = len[i2];
                lot.z = cursor + g * 0.5f + len[i2] * 0.5f;
                cursor += len[i2] + g;
                if (lot.redo != null) lot.redo(lot.z, lot.length);
            }
            foreach (var lot in gone) { lot.house.Vacate(); lots.Remove(lot); }
        }
    }

    static bool CanStretch(Lot lot, float len, float orig)
    {
        return !lot.court && !lot.beach && len < Mathf.Min(GameConfig.MaxHouseLength, orig * 1.35f) - 0.01f;
    }
}
