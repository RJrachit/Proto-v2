using System.Collections.Generic;
using UnityEngine;

// House (partial) - Walled houses and building sites.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- walled house and building site: the ground targets are out, the mailbox by the road and the balcony stay ----
    void DisableGround()
    {
        if (gardenTarget != null) gardenTarget.gameObject.SetActive(false);
        if (gardenB != null) gardenB.gameObject.SetActive(false);
        if (pathZone != null) pathZone.gameObject.SetActive(false);
        if (doorstepZone != null) doorstepZone.gameObject.SetActive(false);
        if (tableZone != null) tableZone.gameObject.SetActive(false);
        if (pathVisualT != null) pathVisualT.gameObject.SetActive(false);
    }

    void LayoutFort(float L, float doorZ)
    {
        float s = side;
        float half = L * 0.5f;
        float gz = Mathf.Clamp(doorZ, -half + 2.0f, half - 2.0f);
        if (GroundBlock == 1)
        {
            // a high stone wall all round the house with a closed iron gate and a padlock in front, hedge showing over the top
            Color stone = new Color(0.86f, 0.82f, 0.74f), cap = new Color(0.72f, 0.69f, 0.62f), iron = new Color(0.20f, 0.22f, 0.27f);
            Part(new Vector3(s * 5.4f, 1.1f, 0f), new Vector3(0.3f, 2.2f, L), stone, true);
            Part(new Vector3(s * 5.4f, 2.26f, 0f), new Vector3(0.5f, 0.12f, L + 0.1f), cap, false);
            // the wall goes all the way round the house: both sides and the back
            float sideLen = 14.8f - 5.4f, sideMid = (14.8f + 5.4f) * 0.5f;
            for (int e = -1; e <= 1; e += 2)
            {
                Part(new Vector3(s * sideMid, 1.1f, e * (half - 0.15f)), new Vector3(sideLen, 2.2f, 0.3f), stone, true);
                Part(new Vector3(s * sideMid, 2.26f, e * (half - 0.15f)), new Vector3(sideLen, 0.12f, 0.5f), cap, false);
            }
            Part(new Vector3(s * 14.8f, 1.1f, 0f), new Vector3(0.3f, 2.2f, L), stone, true);
            Part(new Vector3(s * 14.8f, 2.26f, 0f), new Vector3(0.5f, 0.12f, L + 0.1f), cap, false);
            Part(new Vector3(s * 5.4f, 1.25f, -half + 0.28f), new Vector3(0.55f, 2.5f, 0.55f), cap, true);
            Part(new Vector3(s * 5.4f, 1.25f, half - 0.28f), new Vector3(0.55f, 2.5f, 0.55f), cap, true);
            Part(new Vector3(s * 5.4f, 1.2f, gz - 1.5f), new Vector3(0.42f, 2.4f, 0.42f), cap, true);
            Part(new Vector3(s * 5.4f, 1.2f, gz + 1.5f), new Vector3(0.42f, 2.4f, 0.42f), cap, true);
            Part(new Vector3(s * 5.2f, 0.95f, gz - 0.68f), new Vector3(0.08f, 1.9f, 1.3f), iron, false);
            Part(new Vector3(s * 5.2f, 0.95f, gz + 0.68f), new Vector3(0.08f, 1.9f, 1.3f), iron, false);
            Part(new Vector3(s * 5.19f, 1.98f, gz), new Vector3(0.1f, 0.12f, 2.7f), iron, false);
            Part(new Vector3(s * 5.15f, 1.0f, gz), new Vector3(0.1f, 0.16f, 0.1f), new Color(0.95f, 0.80f, 0.20f), false);   // padlock
            Color bush = new Color(0.25f, 0.55f, 0.28f);
            for (int i = -1; i <= 1; i++)
            {
                float bz = Mathf.Clamp(i * (L * 0.3f), -half + 0.9f, half - 0.9f);
                Part(new Vector3(s * 5.4f, 2.55f, bz), new Vector3(0.9f, 0.55f, 1.2f), bush, false, new Vector3(0f, 12f * i, 0f));
            }
        }
        else
        {
            // a building site: plywood hoarding with a hazard band and a sign, sand and gravel piles, a mixer, pallets, blocks, cones (no red or orange).
            // The house itself has no ground floor yet: bare concrete up to the first floor, no door, no windows.
            Part(new Vector3(s * 8.47f, FloorBase(1) * 0.5f, 0f), new Vector3(0.08f, FloorBase(1), L), new Color(0.58f, 0.58f, 0.60f), false);
            Color ply = new Color(0.80f, 0.74f, 0.58f), post = new Color(0.30f, 0.33f, 0.40f);
            Color yellow = new Color(0.98f, 0.82f, 0.12f), dark = new Color(0.15f, 0.16f, 0.20f);
            Part(new Vector3(s * 5.2f, 0.85f, 0f), new Vector3(0.14f, 1.7f, L), ply, true);
            int posts = Mathf.Max(2, Mathf.RoundToInt(L / 2.4f));
            for (int i = 0; i <= posts; i++)
                Part(new Vector3(s * 5.2f, 0.95f, -half + 0.2f + i * (L - 0.4f) / posts), new Vector3(0.2f, 1.9f, 0.2f), post, true);
            Part(new Vector3(s * 5.12f, 1.62f, 0f), new Vector3(0.05f, 0.2f, L), yellow, false);
            for (int i = 0; i < posts * 2; i++)
                Part(new Vector3(s * 5.11f, 1.62f, -half + 0.5f + i * (L - 1.0f) / (posts * 2f)), new Vector3(0.06f, 0.2f, 0.22f), dark, false);
            Part(new Vector3(s * 5.12f, 1.1f, gz), new Vector3(0.05f, 0.7f, 0.9f), yellow, false);       // warning sign
            Part(new Vector3(s * 5.1f, 1.1f, gz), new Vector3(0.06f, 0.35f, 0.5f), dark, false);

            float z0 = Mathf.Clamp(-half * 0.5f, -half + 1.2f, half - 1.2f), z2 = Mathf.Clamp(half * 0.5f, -half + 1.2f, half - 1.2f);
            Color sand = new Color(0.88f, 0.78f, 0.55f), gravel = new Color(0.62f, 0.62f, 0.64f);
            Part(new Vector3(s * 6.4f, 0.3f, z0), new Vector3(1.8f, 0.6f, 1.5f), sand, false, new Vector3(0f, 20f, 0f));
            Part(new Vector3(s * 6.4f, 0.65f, z0), new Vector3(1.0f, 0.4f, 0.9f), sand, false, new Vector3(0f, 40f, 0f));
            Part(new Vector3(s * 7.7f, 0.25f, z2), new Vector3(1.5f, 0.5f, 1.3f), gravel, false, new Vector3(0f, -15f, 0f));
            Part(new Vector3(s * 7.7f, 0.55f, z2), new Vector3(0.8f, 0.35f, 0.7f), gravel, false, new Vector3(0f, 30f, 0f));
            // grey concrete blocks stacked
            for (int i = 0; i < 3; i++) Part(new Vector3(s * 6.0f, 0.15f + 0.3f * (i / 2), z2 + (i % 2) * 0.45f - 0.2f), new Vector3(0.7f, 0.3f, 0.4f), new Color(0.66f, 0.66f, 0.68f), false);
            // cones
            for (int i = 0; i < 3; i++)
            {
                float cz = Mathf.Clamp(gz - 1.6f + i * 1.0f, -half + 0.6f, half - 0.6f);
                Part(new Vector3(s * 4.95f, 0.04f, cz), new Vector3(0.32f, 0.05f, 0.32f), dark, false);
                Part(new Vector3(s * 4.95f, 0.26f, cz), new Vector3(0.2f, 0.4f, 0.2f), yellow, false);
                Part(new Vector3(s * 4.95f, 0.32f, cz), new Vector3(0.22f, 0.07f, 0.22f), Color.white, false);
            }
            // smashable: a cement mixer and a stack of pallets (no points, no penalty)
            var mix = NewProp(new Vector3(s * 7.3f, 0f, z0 + 0.2f), new Vector3(1.0f, 1.0f, 1.0f), new Color(0.6f, 0.6f, 0.65f), false, new Vector3(0f, 0.5f, 0f));
            Kid(mix, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.3f, 0.7f), new Color(0.35f, 0.38f, 0.45f));
            Kid(mix, new Vector3(0f, 0.65f, 0f), new Vector3(0.7f, 0.65f, 0.7f), yellow, new Vector3(0f, 0f, s * 20f));
            Kid(mix, new Vector3(0f, 0.95f, 0f), new Vector3(0.4f, 0.1f, 0.4f), dark);
            var pal = NewProp(new Vector3(s * 6.3f, 0f, z2 - 0.4f), new Vector3(1.0f, 0.8f, 1.0f), new Color(0.75f, 0.62f, 0.42f), false, new Vector3(0f, 0.4f, 0f));
            for (int i = 0; i < 4; i++) Kid(pal, new Vector3(0f, 0.1f + 0.2f * i, 0f), new Vector3(1.0f, 0.12f, 0.9f), i % 2 == 0 ? new Color(0.75f, 0.62f, 0.42f) : new Color(0.68f, 0.55f, 0.36f));
        }
    }
}
