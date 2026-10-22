using System.Collections.Generic;
using UnityEngine;

// House (partial) - Pooled helper parts (Part, Cube, Sphere), zone setup and soft shadow blobs shared by every layout.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- extra box parts for car parks, created the first time a lot needs them ----

    readonly List<Transform> lotParts = new List<Transform>();
    readonly List<Renderer> lotPartR = new List<Renderer>();
    int lotN;

    Transform Part(Vector3 pos, Vector3 scale, Color col, bool solid, Vector3 euler = default(Vector3))
    {
        if (lotN >= lotParts.Count)
        {
            var go = Prim.Make(PrimitiveType.Cube, "LotPart", transform, Vector3.zero, Vector3.one, Color.white, true);
            go.AddComponent<BankSurface>();
            lotParts.Add(go.transform);
            lotPartR.Add(go.GetComponent<Renderer>());
        }
        var t = lotParts[lotN];
        t.localPosition = pos;
        t.localEulerAngles = euler;
        t.localScale = scale;
        if (glowRend.Remove(lotPartR[lotN])) Prim.FixMaterial(lotPartR[lotN]);   // (a part that was a glowing rim is a plain lit part again)
        lotPartR[lotN].material.color = col;
        var c = t.GetComponent<Collider>();
        if (c != null) c.enabled = solid;
        var bs = t.GetComponent<BankSurface>();
        if (bs != null)
        {
            // beach things (the hut, cabins, palms, steps) are softer than a house wall: wood, sand and leaves, not brick
            bs.restitution = isBeach ? GameConfig.BeachBounce : GameConfig.WallBounce;
            bs.tangentKeep = isBeach ? GameConfig.BeachSlideKeep : GameConfig.WallSlideKeep;
            bs.hoopPart = false;
            bs.guideRing = null;
        }
        t.gameObject.SetActive(true);
        lotN++;
        return t;
    }

    void HideLotParts()
    {
        for (int i = lotN; i < lotParts.Count; i++) lotParts[i].gameObject.SetActive(false);
    }

    void Cube(int i, Vector3 pos, Vector3 scale, Color col, Vector3 euler = default(Vector3))
    {
        if (propCubes == null || i >= propCubes.Length) return;
        propCubes[i].localPosition = pos;
        propCubes[i].localEulerAngles = euler;
        propCubes[i].localScale = scale;
        propCubes[i].gameObject.SetActive(true);
        if (propCubeRenderers[i] != null)
        {
            if (glowRend.Remove(propCubeRenderers[i])) Prim.FixMaterial(propCubeRenderers[i]);   // (a glowing rim goes back to a plain lit part)
            propCubeRenderers[i].material.color = col;
        }
        var cc = propCubes[i].GetComponent<Collider>();      // pooled boxes are reused: solid again until a rim or net claims one
        if (cc != null) cc.enabled = true;
        var bs = propCubes[i].GetComponent<BankSurface>();   // ... and back to wall settings until a board claims one
        if (bs != null) { bs.restitution = GameConfig.WallBounce; bs.tangentKeep = GameConfig.WallSlideKeep; bs.hoopPart = false; bs.guideRing = null; }
    }

    // A part papers pass through (a hoop's net), the same on every hoop.
    void AirCube(int i, Vector3 pos, Vector3 scale, Color col, Vector3 euler = default(Vector3))
    {
        Cube(i, pos, scale, col, euler);
        if (propCubes == null || i >= propCubes.Length) return;
        var cc = propCubes[i].GetComponent<Collider>();
        if (cc != null) cc.enabled = false;
    }

    // A basketball backboard is soft: the paper thuds and drops towards the ring.
    // It also glows a soft white (an unlit material); the renderer is remembered so a reused part goes back to a plain material.
    void SoftBoard(Transform t)
    {
        SetBank(t, GameConfig.BoardBounce, GameConfig.BoardSlideKeep, true);
        var r = t != null ? t.GetComponent<Renderer>() : null;
        if (r == null) return;
        Prim.GlowWhite(r);
        glowRend.Add(r);
    }

    // Every hoop ring glows orange (see Prim.GlowOrange). The renderers are remembered so a reused part goes back to a plain material.
    readonly HashSet<Renderer> glowRend = new HashSet<Renderer>();

    void GlowRim(Transform t)
    {
        var r = t != null ? t.GetComponent<Renderer>() : null;
        if (r == null) return;
        Prim.GlowOrange(r);
        glowRend.Add(r);
    }

    // A hoop ring is solid but dead (near-zero bounce), the same on every hoop: a paper that lands on it thuds and drops off.
    static void SoftRim(Transform t) { SetBank(t, GameConfig.RimBounce, GameConfig.RimSlideKeep, true); }

    static void SetBank(Transform t, float bounce, float slide, bool hoopPart = false)
    {
        if (t == null) return;
        var bs = t.GetComponent<BankSurface>();
        if (bs == null) bs = t.gameObject.AddComponent<BankSurface>();
        bs.restitution = bounce;
        bs.tangentKeep = slide;
        bs.hoopPart = hoopPart;
    }

    void Sphere(int i, Vector3 pos, float size, Color col)
    {
        if (propSpheres == null || i >= propSpheres.Length) return;
        propSpheres[i].localPosition = pos;
        propSpheres[i].localScale = new Vector3(size, size, size);
        propSpheres[i].gameObject.SetActive(true);
        if (propSphereRenderers[i] != null) propSphereRenderers[i].material.color = col;
    }

    static void HideFrom(Transform[] pool, int from)
    {
        if (pool == null) return;
        for (int i = from; i < pool.Length; i++) pool[i].gameObject.SetActive(false);
    }

    static void SetZone(HouseTarget z, Vector3 localPos, Vector3 size)
    {
        z.transform.localPosition = localPos;
        z.size = size;
        var box = z.GetComponent<BoxCollider>();
        if (box != null) box.size = size;
    }

    void Blob(Vector3 pos, float w, float l, float alpha)
    {
        if (!GameConfig.BlobShadows || !BlobShadow.Available) return;
        if (blobN >= blobs.Count) blobs.Add(BlobShadow.Create(transform).transform);
        var t = blobs[blobN++];
        t.localPosition = new Vector3(pos.x, 0.095f, pos.z);
        t.localScale = new Vector3(w, l, 1f);
        t.GetComponent<Renderer>().sharedMaterial = BlobShadow.MatFor(alpha);
        t.gameObject.SetActive(true);
    }

    void HideBlobs()
    {
        for (int i = blobN; i < blobs.Count; i++) blobs[i].gameObject.SetActive(false);
    }
}
