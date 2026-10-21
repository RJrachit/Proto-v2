using UnityEngine;

// Game rules and state: menu / playing (or paused) / game over, score, lives, papers, streaks,
// the bell and run rewards. All drawing is in GameUI.
public class GameManager : MonoBehaviour
{
    public enum RunState { Menu, Playing, GameOver }

    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    public int startingPapers = GameConfig.StartingPapers;
    [Tooltip("Consecutive deliveries needed for a hot streak (x2 score, and deliveries refill slow-mo).")]
    public int hotStreakAt = GameConfig.HotStreakAt;
    [Tooltip("The bell scares hazards this far ahead of the bike.")]
    public float bellRange = GameConfig.BellRange;
    public float bellCooldown = GameConfig.BellCooldown;

    [Header("Lives. The run ends when they reach zero.")]
    public int startingLives = GameConfig.StartingLives;
    public int maxLives = GameConfig.MaxLives;
    public float invulnerableSeconds = GameConfig.LifeInvulnerableSeconds;
    public int livesLostMissedCustomer = GameConfig.LivesLostMissedCustomer;
    public int livesLostHazard = GameConfig.LivesLostHazard;
    public int heartPointsWhenFull = GameConfig.HeartPointsWhenFull;
    [Tooltip("Slow-motion meter refilled by each delivery during a hot streak.")]
    public float hotStreakRefill = GameConfig.HotStreakRefill;

    [Header("Pause")]
    public float resumeCountdown = GameConfig.ResumeCountdown;

    public RunState State { get; private set; }
    public bool Paused { get; private set; }
    public bool Resuming { get { return resuming; } }
    public float ResumeIn { get; private set; }

    // True only while the run is actually advancing (not in a menu, not paused, not over).
    public bool Playing { get { return State == RunState.Playing && !Paused; } }
    public bool GameOver { get { return State == RunState.GameOver; } }
    public bool DailyMode { get; private set; }

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Papers { get; private set; }
    public int Streak { get; private set; }
    public int HighScore { get; private set; }
    public int RunCoins { get; private set; }
    public int RunBaskets { get; private set; }   // baskets scored this run (each ring pays once)
    public float RunTime { get; private set; }
    public string GameOverReason { get; private set; }
    public string Toast { get; private set; }
    public float ToastUntil { get; private set; }

    // 0..1, drained while aiming in slow motion (see PaperThrower)
    public float SlowMoMeter = 1f;

    public bool HotStreak { get { return Streak >= hotStreakAt; } }
    // The streak counts hits: every active area or hoop a paper scores adds 1. It only breaks when a customer's building is driven past
    // with a floor left undelivered (which costs a life). Misses, hazards, broken windows and smashed things do not touch it.
    void BreakStreak()
    {
        Streak = 0;
    }

    public bool SuperStreak { get { return Streak >= GameConfig.SuperStreakAt; } }
    public int Multiplier
    {
        get
        {
            var t = GameConfig.ComboMultipliers;
            if (Streak <= 0 || t == null || t.Length == 0) return 1;
            return t[Mathf.Min(Streak, t.Length) - 1];
        }
    }

    SegmentSpawner spawner;
    public SegmentSpawner Spawner { get { return spawner; } }
    Renderer bikeFrame;
    int papersInFlight;
    float bellTimer;
    float invulnerableUntil;
    bool resuming;

    void Awake()
    {
        Instance = this;
        GameTime.Reset();
        HighScore = PlayerPrefs.GetInt("PaperRunnerHigh", 0);
        ResetStats();
        State = RunState.Menu;
    }

    // Called once by PrototypeBootstrap.
    public void Setup(SegmentSpawner roadSpawner, Renderer bikeFrameRenderer)
    {
        spawner = roadSpawner;
        bikeFrame = bikeFrameRenderer;
        ApplySkin();
    }

    void Update()
    {
        // Android back button (Escape) toggles pause.
        if (Input.GetKeyDown(KeyCode.Escape) && State == RunState.Playing)
        {
            if (!Paused) Pause();
            else if (!resuming) Resume();
        }

        if (Paused && resuming)
        {
            ResumeIn -= Time.unscaledDeltaTime;
            if (ResumeIn <= 0f)
            {
                Paused = false;
                resuming = false;
                GameTime.Paused = false;
                GameTime.Apply();
            }
            return;
        }

        if (!Playing) return;
        RunTime += Time.deltaTime;
        if (bellTimer > 0f) bellTimer -= Time.deltaTime;
    }

    // Pause automatically when the app goes to the background (phone call, home button, app switch).
    void OnApplicationPause(bool pause)
    {
        if (pause) Pause();
    }

    // ---- pause ----

    public void Pause()
    {
        if (State != RunState.Playing || Paused) return;
        Paused = true;
        resuming = false;
        GameTime.Paused = true;
        GameTime.Apply();
    }

    // Starts the 3-2-1 countdown; the run continues when it ends.
    public void Resume()
    {
        if (!Paused || resuming) return;
        resuming = true;
        ResumeIn = resumeCountdown;
    }

    // Ends the current run (awarding its coins) and returns to the main menu.
    public void QuitToMenu()
    {
        if (Tutorial.Active) { Tutorial.Abort(); GoToMenu(); return; }
        if (State == RunState.Playing) End("Run ended");
        GoToMenu();
    }

    // ---- run flow ----

    // The tutorial: the same road logic on a short scripted route (see Tutorial and SegmentSpawner). Nothing in it counts towards the stats.
    public void StartTutorial(bool daily = false)
    {
        DailyMode = false;
        Tutorial.Begin(daily);
        ResetStats();
        Paper.DestroyAll();
        GameTime.Reset();
        BikeForward.Instance.ResetRun();
        BikeForward.Instance.BeginTutorial(Tutorial.StartZ, Tutorial.BikeSpeed);
        spawner.ResetRun(20260101);
        BikeForward.Instance.MoveTo(spawner.TutorialRetryZ(1));   // start at throwing distance from the first house
        State = RunState.Playing;
    }

    public void StartRun(bool daily)
    {
        Tutorial.Abort();
        if (Tutorial.ShouldRun) { StartTutorial(daily); return; }   // the very first run is the tutorial
        DailyMode = daily;
        ResetStats();
        Paper.DestroyAll();
        GameTime.Reset();
        BikeForward.Instance.ResetRun();
        spawner.ResetRun(daily ? Progress.DailySeed : Random.Range(1, int.MaxValue));
        State = RunState.Playing;

        int runs = PlayerPrefs.GetInt("Runs", 0);
        if (runs < 3) ShowToast("Throw at marked houses. Pull back: the paper flies the opposite way, longer pull = farther. Tap = bell.", 5f);
        PlayerPrefs.SetInt("Runs", runs + 1);
    }

    public void GoToMenu()
    {
        DailyMode = false;
        ResetStats();
        Paper.DestroyAll();
        GameTime.Reset();
        BikeForward.Instance.ResetRun();
        spawner.ResetRun(Random.Range(1, int.MaxValue));
        State = RunState.Menu;
    }

    void ResetStats()
    {
        Score = 0;
        Streak = 0;
        Lives = startingLives;
        invulnerableUntil = 0f;
        Papers = startingPapers;
        papersInFlight = 0;
        SlowMoMeter = 1f;
        RunTime = 0f;
        RunCoins = 0;
        RunBaskets = 0;
        bellTimer = 0f;
        GameOverReason = "";
        Paused = false;
        resuming = false;
    }

    void End(string reason)
    {
        if (State != RunState.Playing || Tutorial.Active) return;
        State = RunState.GameOver;
        Paused = false;
        resuming = false;
        GameOverReason = reason;
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt("PaperRunnerHigh", HighScore);
        }
        RunCoins = Score / 10;
        if (DailyMode) RunCoins += Progress.RecordDaily(Score);
        Progress.AddCoins(RunCoins);
        PlayerPrefs.Save();
        GameTime.Reset();
    }

    // ---- skins ----

    public void ApplySkin()
    {
        Color skin = Progress.Skins[Progress.SelectedSkin].color;
        if (Rider.HasFrame) Rider.Tint(skin);   // every frame tube shares one material
        else if (bikeFrame != null) bikeFrame.material.color = skin;
    }

    public void SelectSkin(int i)
    {
        Progress.SelectSkin(i);
        ApplySkin();
    }

    // ---- called by other scripts ----

    public bool TryUsePaper()
    {
        if (!Playing || (Papers <= 0 && !GameConfig.InfinitePapers && !Tutorial.Active)) return false;
        if (!GameConfig.InfinitePapers && !Tutorial.Active) Papers--;
        papersInFlight++;
        return true;
    }

    // Every thrown paper calls this exactly once when it is finished (hit or missed).
    public void PaperDone()
    {
        papersInFlight = Mathf.Max(0, papersInFlight - 1);
        if (Playing && Papers <= 0 && papersInFlight <= 0) End("Out of papers");
    }

    public void AddPapers(int n)
    {
        if (!Playing) return;
        Papers += n;
        ShowToast("+" + n + " papers");
    }

    // A delivery. A house with several floors pays each part as it is hit; only the part that serves the last floor completes the customer
    // (completes = true), which is what counts towards the missions; every hit adds 1 to the streak. suffix is added after the points in the toast.
    public void Delivered(int basePoints, Vector3 pos, HouseTarget.Kind kind, bool completes = true, string suffix = null)
    {
        if (!Playing) return;
        Streak++;   // every active area or hoop that scores counts, whether or not it finishes the building
        int gained = basePoints * Multiplier;
        Score += gained;
        if (HotStreak) SlowMoMeter = Mathf.Min(1f, SlowMoMeter + hotStreakRefill);

        string label = kind == HouseTarget.Kind.Mailbox ? "Perfect! +"
                     : kind == HouseTarget.Kind.Balcony ? "Balcony! +"
                     : kind == HouseTarget.Kind.Door ? "Great! +"
                     : kind == HouseTarget.Kind.Path ? "Great! +"
                     : kind == HouseTarget.Kind.Table ? "Table! +"
                     : kind == HouseTarget.Kind.Hoop ? "Dunkkkk! +"
                     : kind == HouseTarget.Kind.Lifeguard ? "Lifeguard! +"
                     : kind == HouseTarget.Kind.Palm ? "PALM SHOT! +"
                     : "Nice! +";
        ShowToast(label + gained + (suffix ?? ""));
        if (Streak == GameConfig.SuperStreakAt) ShowToast("SUPER HOT STREAK! x" + Multiplier, 2f);
        else if (Streak == hotStreakAt) ShowToast("HOT STREAK! x" + Multiplier, 2f);
        if (kind == HouseTarget.Kind.Hoop) Juice.HoopBlast(pos, 1f + Mathf.Min(Streak, 12) * 0.06f);
        else Juice.Delivery(pos, 1f + Mathf.Min(Streak, 12) * 0.06f);

        if (Tutorial.Active) { if (completes) Tutorial.OnDelivered(kind); return; }   // (practice deliveries do not count towards missions)
        if (completes) Progress.AddStat(Progress.Stat.Delivered, 1);
        if (kind == HouseTarget.Kind.Mailbox) Progress.AddStat(Progress.Stat.Mailbox, 1);
        if (kind == HouseTarget.Kind.Balcony) Progress.AddStat(Progress.Stat.Balcony, 1);
        Progress.AddStat(Progress.Stat.BestCombo, Streak);
    }

    // A hit that earns no points of its own (a hoop that was already used or cleared, a palm already scored) still adds 1 to the streak.
    public void CountHit()
    {
        if (!Playing) return;
        Streak++;
        if (!Tutorial.Active) Progress.AddStat(Progress.Stat.BestCombo, Streak);
        if (Streak == GameConfig.SuperStreakAt) ShowToast("SUPER HOT STREAK! x" + Multiplier, 2f);
        else if (Streak == hotStreakAt) ShowToast("HOT STREAK! x" + Multiplier, 2f);
    }

    public void PaperMissed(Vector3 pos)
    {
        if (!Playing) return;
        ShowToast("Missed!");
        Juice.Miss(pos);
    }

    // Smashing things is just fun: a broken window or a smashed plate costs nothing (no life, the streak stays).
    public void BrokeWindow(Vector3 pos)
    {
        if (!Playing) return;
        Juice.WindowBreak(pos);
        ShowToast("Crash!", 0.8f);
    }

    public void BrokeDishes(Vector3 pos)
    {
        if (!Playing) return;
        Juice.WindowBreak(pos);
        ShowToast("Smash!", 0.8f);
    }

    // Points for a trick that is not a delivery (a swish through a hoop at a house with no customer).
    public void Bonus(int points, string label, Vector3 pos)
    {
        if (!Playing) return;
        Streak++;               // a hoop (a swish at a house with no customer, a palm shot) counts as a hit too
        if (!Tutorial.Active) Progress.AddStat(Progress.Stat.BestCombo, Streak);
        points *= Multiplier;   // every point is multiplied by the streak
        Score += points;
        ShowToast(label + points);
        if (label.StartsWith("Dunk")) Juice.HoopBlast(pos, 1.3f);
        else Juice.Delivery(pos, 1.3f);
    }

    public void BasketScored() { if (Playing) RunBaskets++; }

    // A trick shot (a bounce off anything but a backboard or ring, then through a ring): the bonus comes with a big fiery explosion.
    public void TrickBonus(int points, string label, Vector3 pos)
    {
        if (!Playing) return;
        points *= Multiplier;
        Score += points;
        ShowToast(label + points, 2f);
        Juice.TrickShot(pos);
    }

    // A crate or can was smashed.
    public void Smash(int points, Vector3 pos, Color color)
    {
        if (!Playing) return;
        Score += points * Multiplier;
        Juice.Smash(pos, color, false);
    }

    // The last crate or can of a stack is gone: papers and points back. (A paper in flight is still counted, so the refund keeps the run alive.)
    public void StackCleared(int papers, int points, Vector3 pos)
    {
        if (!Playing) return;
        points *= Multiplier;
        Score += points;
        Papers += papers;
        ShowToast("Stack cleared!  +" + points + "  +" + papers + " papers", 2f);
        Juice.Smash(pos, Color.white, true);
    }

    public void MissedSubscriber()
    {
        if (!Playing || Tutorial.Active) return;
        BreakStreak();   // the only thing that breaks the streak: a building left with a floor undelivered, which costs a life
        LoseLife(livesLostMissedCustomer, "Missed a customer!");
    }

    public void HitHazard(string what, Vector3 pos)
    {
        if (!Playing) return;
        Juice.Crash(pos);
        LoseLife(livesLostHazard, what);
    }

    public void RingBell()
    {
        if (!Playing || bellTimer > 0f) return;
        bellTimer = bellCooldown;
        Sfx.Play("bell");
        Haptics.Pulse(20);

        int scared = RoadHazard.ScareAhead(BikeForward.Instance.transform.position, bellRange);
        if (scared > 0)
        {
            Score += 5 * scared;
            ShowToast("Road cleared! +" + 5 * scared);
            Progress.AddStat(Progress.Stat.Bell, scared);
        }
        else
        {
            ShowToast("Ring! (nothing ahead)", 0.8f);
        }
    }

    public void ShowToast(string message, float seconds = 1.5f)
    {
        Toast = message;
        ToastUntil = Time.unscaledTime + seconds;
    }

    // A mistake costs lives. A short grace period stops one collision from costing several.
    void LoseLife(int amount, string message)
    {
        if (Tutorial.Active) { ShowToast(message); return; }   // no lives are lost while learning
        if (amount <= 0) { ShowToast(message); return; }
        if (GameConfig.GodMode) { ShowToast(message + " (god mode)"); return; }
        if (Time.unscaledTime < invulnerableUntil) return;
        Lives = Mathf.Max(0, Lives - amount);
        invulnerableUntil = Time.unscaledTime + invulnerableSeconds;
        ShowToast(message + (Lives > 0 ? "  Lives: " + Lives : ""));
        if (Lives <= 0) End("Out of lives");
    }

    // A paper hit a heart.
    public void CollectHeart(Vector3 pos)
    {
        if (!Playing) return;
        if (Lives < maxLives)
        {
            Lives++;
            ShowToast("+1 life!  Lives: " + Lives, 2f);
        }
        else
        {
            int heartPoints = heartPointsWhenFull * Multiplier;
            Score += heartPoints;
            ShowToast("Heart! +" + heartPoints, 2f);
        }
        Juice.Delivery(pos, 1.6f);
    }
}
