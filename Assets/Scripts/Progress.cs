using UnityEngine;

// Everything saved between runs: coins, bike skins, and the daily-route streak (PlayerPrefs).
public static class Progress
{
    // ---- coins ----

    public static int Coins { get { return PlayerPrefs.GetInt("Coins", 0); } }

    public static void AddCoins(int amount)
    {
        PlayerPrefs.SetInt("Coins", Coins + amount);
        PlayerPrefs.Save();
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

    // ---- daily route ----

    static string Today { get { return System.DateTime.Now.ToString("yyyyMMdd"); } }
    static string Yesterday { get { return System.DateTime.Now.AddDays(-1).ToString("yyyyMMdd"); } }

    // Same number all day, so everyone gets the same route.
    public static int DailySeed { get { return int.Parse(Today); } }

    public static int DailyBest { get { return PlayerPrefs.GetInt("DailyBest_" + Today, 0); } }

    public static int DailyStreak
    {
        get
        {
            string last = PlayerPrefs.GetString("DailyLast", "");
            if (last == Today || last == Yesterday) return PlayerPrefs.GetInt("DailyStreak", 0);
            return 0;
        }
    }

    // Call when a daily run ends. Returns bonus coins (only for the first daily run of the day).
    public static int RecordDaily(int score)
    {
        int bonus = 0;
        string last = PlayerPrefs.GetString("DailyLast", "");
        if (last != Today)
        {
            int streak = last == Yesterday ? PlayerPrefs.GetInt("DailyStreak", 0) + 1 : 1;
            PlayerPrefs.SetInt("DailyStreak", streak);
            PlayerPrefs.SetString("DailyLast", Today);
            bonus = 25 * Mathf.Min(streak, 7);
        }
        if (score > DailyBest) PlayerPrefs.SetInt("DailyBest_" + Today, score);
        PlayerPrefs.Save();
        return bonus;
    }
}
