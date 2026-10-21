using System.Collections.Generic;
using UnityEngine;

// House (partial) - The trees of a lot: the tree on the lawn and the two trees that block throws. Both are LowPolyTree (see LowPolyTree.cs).
// The main part of the class is in House.cs.
public partial class House
{
    LowPolyTree lawnTree;
    readonly LowPolyTree[] blockerTrees = new LowPolyTree[2];

    // The lawn tree at (s * 5.6, tz), or none.
    void LayoutLawnTree(bool on, float s, float tz)
    {
        if (!on) { if (lawnTree != null) lawnTree.Hide(); return; }
        if (lawnTree == null) lawnTree = LowPolyTree.Create(transform, "LawnTree");
        lawnTree.Place(new Vector3(s * 5.6f, 0f, tz), transform.position.z);
    }

    void HideBlockerTrees()
    {
        for (int k = 0; k < blockerTrees.Length; k++) if (blockerTrees[k] != null) blockerTrees[k].Hide();
    }

    void PlaceBlockerTree(int k, Vector3 pos)
    {
        if (blockerTrees[k] == null) blockerTrees[k] = LowPolyTree.Create(transform, "BlockerTree" + k);
        blockerTrees[k].Place(pos, transform.position.z + 17.3f * (k + 1));
    }

    // ---- the trees of a park ----
    // The whole lot is a wood: trees on a loose staggered grid covering the grass. The crowns overlap into a canopy, but two trunks never
    // stand closer than GameConfig.ParkTreeGap.
    readonly List<LowPolyTree> parkTrees = new List<LowPolyTree>();
    int parkTreeN;

    void LayoutParkTrees(float L)
    {
        float s = side;
        float half = L * 0.5f;
        float wz = transform.position.z;
        float[] cols = { 6.0f, 7.7f, 9.4f, 11.1f, 12.8f };
        float rowStep = 1.7f;
        int rows = Mathf.Max(1, Mathf.FloorToInt((L - 1.6f) / rowStep) + 1);
        float zStep = rows > 1 ? (L - 1.6f) / (rows - 1) : 0f;
        var xs = new List<float>();
        var zs = new List<float>();
        for (int r = 0; r < rows && xs.Count < GameConfig.ParkTreeMax; r++)
        {
            float zr = rows > 1 ? -half + 0.8f + r * zStep : 0f;
            for (int c = 0; c < cols.Length && xs.Count < GameConfig.ParkTreeMax; c++)
            {
                float h1 = Hash01(wz * 0.37f + r * 7.13f + c * 3.31f + 1.1f);
                float h2 = Hash01(wz * 0.53f + r * 5.77f + c * 2.19f + 4.6f);
                float h3 = Hash01(wz * 0.29f + r * 3.91f + c * 6.43f + 8.2f);
                if (h3 > 0.92f) continue;                                   // the odd clearing
                float x = cols[c] + (h1 - 0.5f) * 0.7f + ((r & 1) == 1 ? 0.8f : 0f);
                float z = zr + (h2 - 0.5f) * 0.7f;
                x = Mathf.Min(x, 13.0f);
                z = Mathf.Clamp(z, -half + 0.7f, half - 0.7f);
                bool clear = true;
                for (int i = 0; i < xs.Count && clear; i++)
                {
                    float dx = xs[i] - x, dz = zs[i] - z;
                    if (dx * dx + dz * dz < GameConfig.ParkTreeGap * GameConfig.ParkTreeGap) clear = false;
                }
                if (!clear) continue;
                xs.Add(x);
                zs.Add(z);
            }
        }
        for (int i = 0; i < xs.Count; i++)
        {
            if (parkTreeN >= parkTrees.Count) parkTrees.Add(LowPolyTree.Create(transform, "ParkTree" + parkTrees.Count));
            Vector3 p = new Vector3(s * xs[i], 0f, zs[i]);
            parkTrees[parkTreeN++].Place(p, wz + 11.7f * (i + 1) + 300f);
            Blob(p, 2.8f, 2.8f, 0.24f);
        }
    }

    // Hides the park trees from index 'from' on (0 = all of them: this lot is not a park).
    void HideParkTreesFrom(int from)
    {
        for (int i = from; i < parkTrees.Count; i++) parkTrees[i].Hide();
    }
}
