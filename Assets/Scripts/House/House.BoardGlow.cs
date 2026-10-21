using System.Collections.Generic;
using UnityEngine;

// House (partial) - A soft haze of light round every basketball backboard, so the boards glow a little. Each board registers itself
// while a house is laid out (see BoardGlow calls); the haze is a soft quad standing in the board's plane, so the board hides its middle
// and only the glow past its edges shows. The strength is a DEV menu slider (Look tab, 0 = off), applied to all of them at once.
// The main part of the class is in House.cs.
public partial class House
{
    static Material boardGlowMat;
    readonly List<Transform> boardGlows = new List<Transform>();
    int boardGlowN;

    static readonly Color BoardGlowColor = new Color(1.00f, 0.86f, 0.50f);

    // Called every frame from Look: applies the slider.
    public static void TickBoardGlow()
    {
        if (boardGlowMat == null) return;
        float a = Mathf.Clamp01(GameConfig.BoardGlowStrength);
        boardGlowMat.color = new Color(BoardGlowColor.r, BoardGlowColor.g, BoardGlowColor.b, a);
    }

    // c = board centre (house-local), bh = board height (width 1.6), thinX = the board is thin along x (else along z).
    bool skipBoardGlow;

    void BoardGlow(Vector3 c, float bh, bool thinX)
    {
        if (skipBoardGlow) return;
        if (boardGlowMat == null)
        {
            boardGlowMat = new Material(Mat.Particle());
            boardGlowMat.mainTexture = AuraTex();   // (the soft rounded square the balcony glows use)
            TickBoardGlow();
        }
        if (boardGlowN >= boardGlows.Count)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "BoardGlow";
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            var r = q.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sharedMaterial = boardGlowMat;
            boardGlows.Add(q.transform);
        }
        var t = boardGlows[boardGlowN++];
        t.localPosition = c;
        t.localRotation = thinX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        t.localScale = new Vector3(1.6f + 1.5f, bh + 1.3f, 1f);
        t.gameObject.SetActive(true);
    }

    void HideBoardGlows()
    {
        for (int i = boardGlowN; i < boardGlows.Count; i++) boardGlows[i].gameObject.SetActive(false);
    }
}
