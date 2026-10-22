using UnityEngine;

// Everything saved between runs: coins and bike skins (PlayerPrefs).
public static class Progress
{
    // ---- coins ----

    public static int Coins { get { return PlayerPrefs.GetInt("Coins", 0); } }

    public static void AddCoins(int amount)
    {
        PlayerPrefs.SetInt("Coins", Coins + amount);
        PlayerPrefs.Save();
    }

    // ---- power-up upgrades (bought in the shop with coins) ----
    // Each power-up has 5 levels. Level 0 is the starting length; every level bought makes its timer longer (GameConfig.PowerLevelSec).

    public const int PowerMaxLevel = 5;

    // upgrades a power-up has: 5 each
    public static int MaxLevel(PowerKind k) { return PowerMaxLevel; }

    public static int PowerLevel(PowerKind k) { return Mathf.Clamp(PlayerPrefs.GetInt("PowerLv_" + (int)k, 0), 0, MaxLevel(k)); }

    public static void SetPowerLevel(PowerKind k, int level)
    {
        PlayerPrefs.SetInt("PowerLv_" + (int)k, Mathf.Clamp(level, 0, MaxLevel(k)));
        PlayerPrefs.Save();
    }

    // coins to go from level (next - 1) to level next (next = 1..5) for this power-up
    public static int PowerCost(PowerKind k, int next)
    {
        var c = GameConfig.PowerUpgradeCost[(int)k];
        return c[Mathf.Clamp(next - 1, 0, c.Length - 1)];
    }

    public static bool BuyPower(PowerKind k)
    {
        int lv = PowerLevel(k);
        if (lv >= MaxLevel(k)) return false;
        int cost = PowerCost(k, lv + 1);
        if (Coins < cost) return false;
        PlayerPrefs.SetInt("Coins", Coins - cost);
        PlayerPrefs.SetInt("PowerLv_" + (int)k, lv + 1);
        PlayerPrefs.Save();
        return true;
    }

    // ---- bike skins ----

    public struct Skin
    {
        public string name;
        public Color color;
        public int cost;
        public Skin(string name, Color color, int cost) { this.name = name; this.color = color; this.cost = cost; }
    }

    public static readonly Skin[] Skins =
    {
        new Skin("Classic Blue", new Color(0.15f, 0.40f, 0.90f), 0),
        new Skin("Chilli Red",   new Color(0.90f, 0.20f, 0.20f), 150),
        new Skin("Mint",         new Color(0.20f, 0.80f, 0.60f), 300),
        new Skin("Gold Rush",    new Color(1.00f, 0.80f, 0.20f), 600),
    };

    public static int SelectedSkin { get { return Mathf.Clamp(PlayerPrefs.GetInt("SkinSel", 0), 0, Skins.Length - 1); } }

    public static bool OwnsSkin(int i) { return i == 0 || PlayerPrefs.GetInt("Skin_" + i, 0) == 1; }

    public static bool BuySkin(int i)
    {
        if (OwnsSkin(i) || Coins < Skins[i].cost) return false;
        PlayerPrefs.SetInt("Coins", Coins - Skins[i].cost);
        PlayerPrefs.SetInt("Skin_" + i, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void SelectSkin(int i)
    {
        if (!OwnsSkin(i)) return;
        PlayerPrefs.SetInt("SkinSel", i);
        PlayerPrefs.Save();
    }
}
