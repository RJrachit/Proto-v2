using System.Collections.Generic;
using UnityEngine;

// Everything saved between runs: coins, bike skins, the daily-route streak and daily missions (PlayerPrefs).
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

    // ---- daily missions ----

    public enum Stat { Delivered, Mailbox, Balcony, BestCombo, Bell }

    public class Mission
    {
        public int index;
        public string text;
        public Stat stat;
        public int target;
        public int reward;
    }

    static readonly Mission[] Pool =
    {
        new Mission { index = 0, text = "Deliver 15 papers",           stat = Stat.Delivered, target = 15, reward = 40 },
        new Mission { index = 1, text = "Hit 3 mailboxes",             stat = Stat.Mailbox,   target = 3,  reward = 50 },
        new Mission { index = 2, text = "Reach a 6 streak",            stat = Stat.BestCombo, target = 6,  reward = 60 },
        new Mission { index = 3, text = "Clear 3 hazards with bell",   stat = Stat.Bell,      target = 3,  reward = 40 },
        new Mission { index = 4, text = "Deliver 30 papers",           stat = Stat.Delivered, target = 30, reward = 80 },
        new Mission { index = 5, text = "Hit 6 mailboxes",             stat = Stat.Mailbox,   target = 6,  reward = 90 },
        new Mission { index = 6, text = "Hit 3 balconies",             stat = Stat.Balcony,   target = 3,  reward = 60 },
    };

    static Mission[] todays;
    static string todaysKey;

    public static Mission[] TodayMissions()
    {
        if (todays != null && todaysKey == Today) return todays;

        var rng = new System.Random(DailySeed);
        var left = new List<int>();
        for (int i = 0; i < Pool.Length; i++) left.Add(i);
        todays = new Mission[3];
        for (int k = 0; k < todays.Length; k++)
        {
            int pick = rng.Next(left.Count);
            todays[k] = Pool[left[pick]];
            left.RemoveAt(pick);
        }
        todaysKey = Today;
        return todays;
    }

    public static int Count(Mission m) { return PlayerPrefs.GetInt("MP_" + Today + "_" + m.index, 0); }

    public static bool Done(Mission m) { return Count(m) >= m.target; }

    public static void AddStat(Stat stat, int amount)
    {
        foreach (var m in TodayMissions())
        {
            if (m.stat != stat) continue;
            string key = "MP_" + Today + "_" + m.index;
            int before = PlayerPrefs.GetInt(key, 0);
            if (before >= m.target) continue;

            int after = stat == Stat.BestCombo ? Mathf.Max(before, amount) : before + amount;
            PlayerPrefs.SetInt(key, after);
            if (after >= m.target)
            {
                AddCoins(m.reward);
                if (GameManager.Instance != null)
                    GameManager.Instance.ShowToast("Mission done: " + m.text + "  +" + m.reward, 3f);
            }
        }
        PlayerPrefs.Save();
    }
}
