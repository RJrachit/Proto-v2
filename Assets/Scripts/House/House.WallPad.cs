using UnityEngine;

// House (partial) - The wall trampoline: a bouncy pad leaning against the house wall (its foot on the ground, 65 degrees from the ground, its top resting on the wall).
// A paper that hits it is thrown back up and out from the wall (see Paper.OnCollisionEnter and TrampolineSurface.vertical), so a throw can be banked off it. Only on some customers' houses.
// The main part of the class is in House.cs.
public partial class House
{
    bool wallPadOn;
    float wallPadZ;
    Transform wallPadT;
    TrampolineSurface wallPadSurf;

    const float WallPadHalf = 1.2f;     // the pad's radius: the same as the trampoline on the lawn (a round disc leaning against the wall)
    const float WallPadAngle = 65f;     // degrees between the pad and the ground
    const float WallPadY = 1.37f;       // height of its centre: the frame's lowest point just clears the lawn (top at 0.08), the top edge is about 2.6 m up
    const float WallPadX = 7.86f;       // distance of its centre from the street axis: the frame's top edge touches the wall (at 8.5)

    // Decides whether this house gets a wall pad and where along the wall. Called before the windows are laid out, which keep clear of it.
    void PlanWallPad(float L, float doorZ, bool wallMail, float mailZ, bool eligible)
    {
        wallPadOn = false;
        if (!eligible || Tutorial.Active || fort || isBeach || shopKind != 0 || !groundFloor || blockerKind != 0 || L < 4.6f) return;
        float wz = transform.position.z;
        float p = GameConfig.GameplayOdds(wz, GameConfig.WallPadChance, 0f);
        if (p <= 0f || Hash01(wz * 0.857f + (side > 0 ? 13.1f : 47.9f)) >= p) return;

        // a spot along the wall clear of the door, the wall mailbox and the ends
        float half = L * 0.5f;
        int found = 0;
        float[] ok = new float[16];
        for (float z = -half + WallPadHalf + 0.4f; z <= half - WallPadHalf - 0.4f && found < ok.Length; z += 0.6f)
        {
            if (Mathf.Abs(z - doorZ) < WallPadHalf + 1.3f) continue;
            if (wallMail && Mathf.Abs(z - mailZ) < WallPadHalf + 0.9f) continue;
            ok[found++] = z;
        }
        if (found == 0) return;
        wallPadZ = ok[Mathf.Min(found - 1, (int)(Hash01(wz * 2.113f + 5.5f) * found))];
        wallPadOn = true;
    }

    // Builds the pad the first time, then puts it in place (or hides it).
    void LayoutWallPad()
    {
        if (!wallPadOn)
        {
            if (wallPadT != null) wallPadT.gameObject.SetActive(false);
            return;
        }
        float s = side;
        if (wallPadT == null)
        {
            wallPadT = new GameObject("WallTrampoline").transform;
            wallPadT.SetParent(transform, false);
            Color frame = new Color(0.22f, 0.25f, 0.30f), pad = new Color(0.22f, 0.45f, 0.85f);
            // two round discs standing on their edges (rotated so their axis points at the wall): the dark frame behind, the blue skin in front
            var stand = new Vector3(0f, 0f, 90f);
            Prim.Make(PrimitiveType.Cylinder, "Rim", wallPadT, new Vector3(side * 0.04f, 0f, 0f), new Vector3(WallPadHalf * 2f + 0.36f, 0.065f, WallPadHalf * 2f + 0.36f), frame, false, stand);
            var padGo = Prim.Make(PrimitiveType.Cylinder, "Pad", wallPadT, Vector3.zero, new Vector3(WallPadHalf * 2f, 0.07f, WallPadHalf * 2f), pad, true, stand);
            Prim.RoundCollider(padGo);
            wallPadSurf = padGo.AddComponent<TrampolineSurface>();
            wallPadSurf.vertical = true;
        }
        wallPadSurf.restitution = GameConfig.TrampolineBounce;   // (a wall pad is a trampoline like the others)
        wallPadSurf.tangentKeep = GameConfig.TrampolineSlideKeep;
        wallPadSurf.pushDir = new Vector3(s, 0f, 0f);   // it sinks along its own axis (into the wall side) when hit
        wallPadT.localPosition = new Vector3(s * WallPadX, WallPadY, wallPadZ);
        // tilt: the face looks at the road and slightly up (90 - 65 degrees above level); the top leans onto the wall
        wallPadT.localRotation = Quaternion.Euler(0f, 0f, -s * (90f - WallPadAngle));
        wallPadT.gameObject.SetActive(true);
        Blob(new Vector3(s * 7.95f, 0f, wallPadZ), 2.2f, WallPadHalf * 2f + 0.8f, 0.18f);
    }
}
