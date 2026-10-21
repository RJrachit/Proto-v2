using System.Collections.Generic;
using UnityEngine;

// House (partial) - Windows: placement and breaking.
// The main part of the class is in House.cs.
public partial class House
{
    // Window styles (drawn in House.Facade.cs). A wide one takes two cells of the grid.
    //   0 tall twin     two tall panes, pale grey glass
    //   1 wide grid     four columns, two rows of dark glass in a white frame (wide)
    //   2 wide mixed    two tall panes and a column split in two, dark brown frame (wide)
    //   3 square mixed  a tall pane and a column of a small and a tall pane
    //   4 stone classic a cream stone surround with a keystone and a corbelled sill, blue glass
    //   5 plain         the old single pane
    static readonly float[] WinW = { 1.15f, 2.5f, 2.5f, 1.2f, 1.0f, 1.2f };
    static readonly float[] WinH = { 1.55f, 1.2f, 1.2f, 1.3f, 1.6f, 1.0f };
    static readonly Color[] WinGlass =
    {
        new Color(0.74f, 0.76f, 0.80f), new Color(0.15f, 0.17f, 0.21f), new Color(0.42f, 0.52f, 0.64f),
        new Color(0.62f, 0.72f, 0.82f), new Color(0.55f, 0.78f, 0.88f), new Color(0.65f, 0.85f, 1.00f),
    };
    static bool WinWide(int t) { return t == 1 || t == 2; }

    bool winDetail;     // this lot uses the window styles (off: all the old plain windows)
    int fWinSet;        // which styles this house mixes
    int fWin;           // an extra on its windows: 0 none, 1 open shutters, 2 flower boxes, 3 tin awnings

    void PlanWindows()
    {
        winDetail = GameConfig.FacadeDetail && groundFloor;
        fWinSet = Idx(FH(1.7f), 4);
        fWin = Idx(FH(8.3f), 4);
    }

    // The style of the window at a cell, from the house's own dice.
    int WindowTypeAt(float key)
    {
        if (!winDetail) return 5;
        float r = Hash01(ShopKey * 0.53f + key * 3.7f + 11.1f + (side > 0 ? 2.3f : 37.9f));
        int t;
        switch (fWinSet)
        {
            case 0: t = r < 0.6f ? 4 : 0; break;
            case 1: t = r < 0.45f ? 1 : (r < 0.75f ? 0 : 3); break;
            case 2: t = r < 0.45f ? 2 : (r < 0.75f ? 3 : 0); break;
            default: t = r < 0.2f ? 4 : (r < 0.4f ? 1 : (r < 0.6f ? 2 : (r < 0.8f ? 0 : 3))); break;
        }
        // a shop has its name board over the awning: only the lower styles fit on the floor above it
        if (shopKind != 0 && (t == 0 || t == 4)) t = 3;
        return t;
    }

    // Windows sit on a fixed grid along the wall. The ground floor keeps clear of the door and a wall-mounted mailbox (and has none on a building site);
    // every upper floor has its own row, clear of its balcony doors and its hoop. A wide window takes two neighbouring cells (both have to be free).
    // Breaking a window is free.
    void LayoutWindows(float L, float doorZ, bool wallMail, float mailZ, bool on, float d, float blockZ, float blockHalf)
    {
        float s = side;
        float half = L * 0.5f;
        int n = Mathf.Max(1, Mathf.FloorToInt((L - 0.6f) / 2.2f));
        float step = L / n;

        float Cell(int k) { return -half + step * (k + 0.5f); }

        bool GroundOk(float zc)
        {
            return groundFloor && shopKind == 0 && Mathf.Abs(zc - doorZ) >= 1.4f && !(wallMail && Mathf.Abs(zc - mailZ) < 1.4f)
                   && !(blockHalf > 0f && Mathf.Abs(zc - blockZ) < blockHalf + 0.7f)   // garage wall
                   && !(wallPadOn && Mathf.Abs(zc - wallPadZ) < WallPadHalf + 0.7f);   // wall trampoline
        }

        // a wide window sits between two cells: it must not cover the door, the mailbox, the garage wall or the wall trampoline
        bool GroundWideOk(float mid, float wd)
        {
            return Mathf.Abs(mid - doorZ) >= wd * 0.5f + 0.9f && !(wallMail && Mathf.Abs(mid - mailZ) < wd * 0.5f + 0.8f)
                   && !(blockHalf > 0f && Mathf.Abs(mid - blockZ) < blockHalf + wd * 0.5f + 0.1f)
                   && !(wallPadOn && Mathf.Abs(mid - wallPadZ) < WallPadHalf + wd * 0.5f + 0.1f);
        }

        int gi = 0;
        for (int k = 0; k < n; k++)
        {
            if (groundWindows == null || gi >= groundWindows.Length) break;
            float zc = Cell(k);
            if (!GroundOk(zc)) continue;
            int type = WindowTypeAt(k * 1.3f);
            float z = zc;
            if (WinWide(type))
            {
                float mid = k + 1 < n ? (zc + Cell(k + 1)) * 0.5f : 0f;
                if (k + 1 < n && GroundOk(Cell(k + 1)) && GroundWideOk(mid, WinW[type])) { z = mid; k++; }
                else type = 3;
            }
            PlaceWindow(groundWindows[gi++], s, d, z, 1.9f, on, type);
        }
        HideWindows(groundWindows, gi);

        if (upperWindows == null) return;
        for (int f = 1; f <= 2; f++)
        {
            int start = (f - 1) * UpperWindowSlots;
            int used = 0;
            if (f < floors)
            {
                for (int k = 0; k < n && used < UpperWindowSlots; k++)
                {
                    float zc = Cell(k);
                    if (!UpperWindowOk(f, zc)) continue;
                    if (start + used >= upperWindows.Length) break;
                    int type = WindowTypeAt(k * 1.3f + f * 7.1f);
                    float z = zc;
                    if (WinWide(type))
                    {
                        float mid = k + 1 < n ? (zc + Cell(k + 1)) * 0.5f : 0f;
                        bool ok = k + 1 < n && UpperWindowOk(f, Cell(k + 1)) && !UnderSlab(f, mid, WinW[type] * 0.5f)
                                  && !(floorHoopOn[f] && Mathf.Abs(mid - floorHoopAt[f]) < 1.3f + WinW[type] * 0.5f - 0.6f);
                        if (ok) { z = mid; k++; }
                        else type = 3;
                    }
                    PlaceWindow(upperWindows[start + used++], s, d, z, FloorBase(f) + (shopKind != 0 ? 2.5f : 2.0f), on, type);   // (a shop's windows stand higher: its sign is below them)
                }
            }
            for (int q = used; q < UpperWindowSlots && start + q < upperWindows.Length; q++)
            {
                upperWindows[start + q].visual.gameObject.SetActive(false);
                upperWindows[start + q].zone.gameObject.SetActive(false);
            }
        }
    }

    static void PlaceWindow(WindowSlot w, float s, float d, float z, float y, bool on, int type)
    {
        w.broken = false;
        w.type = type;
        w.wd = WinW[type];
        w.ht = WinH[type];
        w.visual.localPosition = new Vector3(s * (8.47f - d), y, z);
        w.visual.localScale = new Vector3(0.08f, w.ht, w.wd);
        w.zone.transform.localPosition = new Vector3(s * (8.2f - d), y, z);
        w.zone.size = new Vector3(0.6f, w.ht, w.wd);
        var box = w.zone.GetComponent<BoxCollider>();
        if (box != null) box.size = w.zone.size;
        if (w.glass != null)
        {
            if (w.own == null) w.own = w.glass.material;
            w.glass.sharedMaterial = w.own;
            w.own.color = WinGlass[type];
        }
        w.visual.gameObject.SetActive(on);
        w.zone.gameObject.SetActive(on);
    }

    static void HideWindows(WindowSlot[] arr, int from)
    {
        if (arr == null) return;
        for (int i = from; i < arr.Length; i++)
        {
            arr[i].visual.gameObject.SetActive(false);
            arr[i].zone.gameObject.SetActive(false);
        }
    }

    // A paper smashed into one of this house's windows.
    public void HitWindow(HouseTarget zone, Vector3 pos)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var slot = FindWindow(groundWindows, zone) ?? FindWindow(upperWindows, zone);
        if (slot == null) return;
        if (slot.broken) { gm.ShowToast("Already broken"); return; }
        slot.broken = true;
        if (slot.glass != null)
        {
            if (slot.own == null) slot.own = slot.glass.material;
            slot.glass.sharedMaterial = slot.own;   // a broken window goes dark, even at night
            slot.own.color = BrokenColor;
        }
        ApplyNight();   // its glow and spill of light go out
        gm.BrokeWindow(pos);
    }

    // Called by a paper the moment it hits this house's wall. Breaks the window under the impact point, if there is one.
    public bool TryBreakWindowAt(Vector3 worldPoint)
    {
        Vector3 p = transform.InverseTransformPoint(worldPoint);
        return TryBreakIn(groundWindows, p, worldPoint) || TryBreakIn(upperWindows, p, worldPoint);
    }

    bool TryBreakIn(WindowSlot[] arr, Vector3 local, Vector3 worldPoint)
    {
        if (arr == null) return false;
        foreach (var w in arr)
        {
            if (!w.visual.gameObject.activeSelf) continue;
            Vector3 c = w.visual.localPosition;
            if (Mathf.Abs(local.y - c.y) <= w.ht * 0.5f + 0.05f && Mathf.Abs(local.z - c.z) <= w.wd * 0.5f + 0.05f)
            {
                HitWindow(w.zone, worldPoint);
                return true;
            }
        }
        return false;
    }

    static WindowSlot FindWindow(WindowSlot[] arr, HouseTarget zone)
    {
        if (arr == null) return null;
        foreach (var w in arr)
            if (w.zone == zone) return w;
        return null;
    }
}
