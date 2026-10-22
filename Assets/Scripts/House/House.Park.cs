using System.Collections.Generic;
using UnityEngine;

// House (partial) - Parks: basketball court, car park and park layout.
// The main part of the class is in House.cs.
public partial class House
{
    // A basketball court laid out the long way (along the road): a hoop at each end, so it faces the camera whichever way you pass.
    // Painted lines, keys, a railing along the footpath and a hedge at the back. Each ring is a basket: a swish scores the hoop bonus
    // (once per ring). The boards, poles and rims bounce papers.
    void LayoutCourt(float lotLength, bool flip)
    {
        float s = side;
        float L = Mathf.Min(lotLength, GameConfig.CourtLength);   // the court is always the same length; spare lot is plain grass
        float half = L * 0.5f;
        yardSpotValid = false;
        tableSpotValid = false;

        const float xBack = 13.9f;
        const float xl = 4.9f, xr = 13.4f;
        float cx = s * (xl + xr) * 0.5f;
        lawnT.localPosition = new Vector3(s * (4.4f + xBack) * 0.5f, 0.04f, 0f);
        lawnT.localScale = new Vector3(xBack - 4.4f, 0.08f, L);
        lawnT.gameObject.SetActive(true);
        if (padRenderer != null) padRenderer.material.color = new Color(0.26f, 0.45f, 0.70f);
        pathVisualT.gameObject.SetActive(false);

        Color paint = new Color(0.95f, 0.95f, 0.92f);
        Color red = new Color(0.55f, 0.35f, 0.70f);   // court key paint (purple, never a red)
        Color metal = new Color(0.25f, 0.30f, 0.38f);

        // court lines: sidelines, end lines, centre line, and a painted key at each end
        Part(new Vector3(s * xl, 0.1f, 0f), new Vector3(0.1f, 0.02f, L - 0.8f), paint, false);
        Part(new Vector3(s * xr, 0.1f, 0f), new Vector3(0.1f, 0.02f, L - 0.8f), paint, false);
        Part(new Vector3(cx, 0.1f, half - 0.4f), new Vector3(xr - xl, 0.02f, 0.1f), paint, false);
        Part(new Vector3(cx, 0.1f, -half + 0.4f), new Vector3(xr - xl, 0.02f, 0.1f), paint, false);
        Part(new Vector3(cx, 0.1f, 0f), new Vector3(xr - xl, 0.02f, 0.1f), paint, false);
        for (int e = -1; e <= 1; e += 2)
        {
            float kz = e * (half - 1.9f);
            Part(new Vector3(cx, 0.1f, kz), new Vector3(3.2f, 0.02f, 3.0f), paint, false);
            Part(new Vector3(cx, 0.115f, kz), new Vector3(2.9f, 0.03f, 2.7f), red, false);
        }

        CourtHoop(s, 1f, half, cx, hoopZone, hoopSnapT);
        CourtHoop(s, -1f, half, cx, hoopZone2, hoopSnap2T);

        // railing along the footpath, hedge along the back
        RoadFence(s, -half + 0.15f, half - 0.15f, metal);
        Part(new Vector3(s * 13.65f, 0.45f, 0f), new Vector3(0.8f, 0.9f, lotLength), new Color(0.20f, 0.50f, 0.25f), false);

        yardSpot = new Vector3(cx, 4.4f, (flip ? -1f : 1f) * (half - 1.5f));
        yardSpotValid = true;

        HideFrom(propCubes, 0);
        HideFrom(propSpheres, 0);
    }

    // One basket: pole and backboard at the end (e = +1 far end, -1 near end), ring in front of the board, net below, and the catch zone.
    void CourtHoop(float s, float e, float half, float cx, HouseTarget zone, Transform snap)
    {
        const float ringY = 2.9f;
        float zr = e * (half - 1.5f);
        Part(new Vector3(cx, 1.6f, e * (half - 0.75f)), new Vector3(0.15f, 3.2f, 0.15f), new Color(0.45f, 0.45f, 0.48f), true);
        SoftBoard(Part(new Vector3(cx, ringY + BoardUp, e * (half - 0.9f)), new Vector3(BoardW, BoardH, 0.1f), Color.white, true));
        BoardMarks(new Vector3(cx, ringY + BoardUp, e * (half - 0.9f)), BoardH, false);   // (the same edge and square as every other board)
        RingParts(cx, ringY, zr);
        if (zone != null && snap != null)
        {
            SetZone(zone, new Vector3(cx, ringY, zr), new Vector3(0.9f, 0.6f, 0.9f));
            snap.localPosition = new Vector3(cx, ringY - 0.5f, zr);
            zone.gameObject.SetActive(true);
        }
    }

    // A car park: a paved lot stretching back to a brick wall, three lanes of parked cars with marked bays, a low railing along the
    // footpath with an entrance (stone pillars, an attendant booth with a P sign), and a couple of lamp posts.
    // Pure scenery. Cars and the back wall bounce papers; the railing is see-through and does not. Parts are made on first use.
    void LayoutCarPark(float L, bool flip)
    {
        float s = side;
        float half = L * 0.5f;
        yardSpotValid = false;
        tableSpotValid = false;

        const float xBack = 13.9f;
        lawnT.localPosition = new Vector3(s * (4.4f + xBack) * 0.5f, 0.04f, 0f);
        lawnT.localScale = new Vector3(xBack - 4.4f, 0.08f, L);
        lawnT.gameObject.SetActive(true);
        if (padRenderer != null) padRenderer.material.color = new Color(0.40f, 0.40f, 0.43f);
        pathVisualT.gameObject.SetActive(false);

        Color metal = new Color(0.25f, 0.30f, 0.38f);
        Color stone = new Color(0.80f, 0.77f, 0.70f);
        Color brick = new Color(0.55f, 0.57f, 0.62f);
        Color paint = new Color(0.92f, 0.92f, 0.90f);

        // marked bays
        float[] lineX = { 5.7f, 8.1f, 10.5f, 12.9f };
        foreach (float lx in lineX)
            Part(new Vector3(s * lx, 0.1f, 0f), new Vector3(0.08f, 0.02f, L - 0.6f), paint, false);

        // parked cars (low-poly: see LowPolyCar.cs): three lanes, about three in four bays filled, at least one
        int rows = Mathf.Max(1, Mathf.FloorToInt((L - 0.4f) / 4.0f));
        float[] laneX = { 6.9f, 9.3f, 11.7f };
        int bays = rows * 3;
        int placed = 0;
        for (int n = 0; n < bays; n++)
        {
            bool lastChance = n == bays - 1 && placed == 0;
            if (!lastChance && Random.value > 0.75f) continue;
            int row = n / 3;
            float cz = -half + (row + 0.5f) * L / rows;
            float cx = s * laneX[n % 3];
            float roomLen = L / rows - 0.2f;
            PlaceCar(cx, cz, LowPolyCar.RandomType(false), Random.Range(0, LowPolyCar.Colors.Length), roomLen);
            if (placed == 0) { yardSpot = new Vector3(cx, 2.0f, cz); yardSpotValid = true; }
            placed++;
        }

        // back wall with pillars
        Part(new Vector3(s * (xBack + 0.2f), 1.1f, 0f), new Vector3(0.4f, 2.2f, L), brick, true);
        int pillars = L >= 10f ? 3 : 2;
        for (int k = 0; k < pillars; k++)
        {
            float pz = -half + 0.3f + k * (L - 0.6f) / (pillars - 1);
            Part(new Vector3(s * (xBack + 0.2f), 1.25f, pz), new Vector3(0.7f, 2.5f, 0.7f), stone, true);
        }

        // railing along the footpath, with an entrance near one end
        float ze = flip ? half - 1.6f : -half + 1.6f;
        RoadFence(s, -half + 0.15f, Mathf.Min(ze - 1.5f, half - 0.15f), metal);
        RoadFence(s, Mathf.Max(ze + 1.5f, -half + 0.15f), half - 0.15f, metal);
        Part(new Vector3(s * 4.55f, 0.7f, ze - 1.6f), new Vector3(0.4f, 1.4f, 0.4f), stone, true);
        Part(new Vector3(s * 4.55f, 0.7f, ze + 1.6f), new Vector3(0.4f, 1.4f, 0.4f), stone, true);

        // attendant booth with a P sign
        float bz = ze + (ze > 0f ? -2.3f : 2.3f);
        Part(new Vector3(s * 5.4f, 1.1f, bz), new Vector3(1.4f, 2.2f, 1.4f), new Color(0.92f, 0.92f, 0.86f), true);
        Part(new Vector3(s * 5.4f, 2.3f, bz), new Vector3(1.9f, 0.18f, 1.9f), new Color(0.20f, 0.30f, 0.50f), false);
        Part(new Vector3(s * 5.4f, 2.75f, bz), new Vector3(0.1f, 0.7f, 0.1f), metal, false);
        Part(new Vector3(s * 5.4f, 3.3f, bz), new Vector3(0.1f, 0.9f, 0.9f), new Color(0.15f, 0.35f, 0.85f), false);

        // lamp posts
        for (int k = 0; k < 2; k++)
        {
            float lz = k == 0 ? -half + 1.0f : half - 1.0f;
            Part(new Vector3(s * 13.3f, 1.6f, lz), new Vector3(0.1f, 3.2f, 0.1f), metal, false);
            Sphere(k, new Vector3(s * 13.3f, 3.3f, lz), 0.45f, new Color(1.0f, 0.95f, 0.60f));
        }

        HideFrom(propCubes, 0);
        HideFrom(propSpheres, 2);
    }

    // A park filling the lot: lighter grass covered with a dense stand of trees and nothing else.
    // Pure scenery: no targets, and no windows to break.
    // Returns true when the lot became a car park or a court (those carry no crate stacks).
    bool LayoutPark(float L, bool flip, bool parking, bool court)
    {
        if (parking) { LayoutCarPark(L, flip); return true; }
        if (court && L >= GameConfig.CourtLength - 0.01f) { LayoutCourt(L, flip); return true; }
        float s = side;
        yardSpotValid = false;
        tableSpotValid = false;

        const float xBack = 13.9f;
        lawnT.localPosition = new Vector3(s * (4.4f + xBack) * 0.5f, 0.04f, 0f);
        lawnT.localScale = new Vector3(xBack - 4.4f, 0.08f, L);
        lawnT.gameObject.SetActive(true);
        if (padRenderer != null) padRenderer.material.color = GameConfig.ParkColor;

        pathVisualT.gameObject.SetActive(false);   // no path: the whole park is trees on grass

        // Nothing else stands here: no railing, pond, benches, flower beds, hedge or lamps. Just a wood (see LayoutParkTrees).
        LayoutParkTrees(L);

        HideFrom(propCubes, 0);
        HideFrom(propSpheres, 0);
        return false;
    }
}
