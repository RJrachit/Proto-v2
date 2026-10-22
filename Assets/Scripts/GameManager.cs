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
    public enum ToastIcon { None, Fire, FireDouble, Alert }   // drawn after the toast text (GameUI.Hud)
    public ToastIcon ToastIconKind { get; private set; }
    public float ToastAt { get; private set; }     // when the toast appeared (the HUD pops it in from then)
    public Vector3 ScorePos { get; private set; }  // where the latest scoring hit happened (the coin dust starts there)
    public void NoteScorePos(Vector3 p) { ScorePos = p; }

    // How hot the streak is, 0..1 (more with the Dev menu's combo slider): a long streak makes every hit feel bigger.
    public float Combo { get { return Mathf.Clamp(Streak / 15f, 0f, 1f) * GameConfig.ComboJuice; } }
    // The hit chime climbs a semitone for every hit in the streak, up to an octave.
    float ComboPitch() { return Mathf.Pow(1.0595f, Mathf.Min(Streak, 12)); }

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

    // Starts the short fade back into the game; the run continues when it ends.
    public void Resume()
    {
        if (!Paused || resuming) return;
        resuming = true;
        ResumeIn = GameConfig.ResumeCountdown;   // (not the inspector copy: an old scene may still hold 3)
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
    public void StartTutorial()
    {
        Tutorial.Begin();
        ResetStats();
        Paper.DestroyAll();
        GameTime.Reset();
        Tutorial.RestoreLessonTime();   // (the tutorial opens straight into its slowed near shot)
        BikeForward.Instance.ResetRun();
        BikeForward.Instance.BeginTutorial(Tutorial.StartZ, Tutorial.BikeSpeed);
        spawner.ResetRun(20260101);
        BikeForward.Instance.MoveTo(spawner.TutorialRetryZ(1));   // start at throwing distance from the first house
        State = RunState.Playing;
    }

    public void StartRun()
    {
        Tutorial.Abort();
        if (Tutorial.ShouldRun) { StartTutorial(); return; }   // the very first run is the tutorial
        ResetStats();
        Paper.DestroyAll();
        GameTime.Reset();
        BikeForward.Instance.ResetRun();
        spawner.ResetRun(Random.Range(1, int.MaxValue));
        State = RunState.Playing;

        int runs = PlayerPrefs.GetInt("Runs", 0);
        PlayerPrefs.SetInt("Runs", runs + 1);
    }

    public void GoToMenu()
    {
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
        // (the run's coins were counted as they were earned)
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
    }

    // A delivery. A house with several floors pays each part as it is hit; only the part that serves the last floor completes the customer
    // (completes = true); every hit adds 1 to the streak. suffix is added after the points in the toast.
    public void Delivered(int basePoints, Vector3 pos, HouseTarget.Kind kind, bool completes = true, string suffix = null)
    {
        if (!Playing) return;
        Streak++;   // every active area or hoop that scores counts, whether or not it finishes the building
        int gained = basePoints * Multiplier;
        Score += gained;
        if (HotStreak) SlowMoMeter = Mathf.Min(1f, SlowMoMeter + hotStreakRefill);

        string label = kind == HouseTarget.Kind.Mailbox ? "Perfect!! +"
                     : kind == HouseTarget.Kind.Balcony ? "Balcony! +"
                     : kind == HouseTarget.Kind.Door ? "Great! +"
                     : kind == HouseTarget.Kind.Path ? "Great! +"
                     : kind == HouseTarget.Kind.Table ? null   // (a table shows no text)
                     : kind == HouseTarget.Kind.Hoop ? "Swish! +"
                     : "Nice! +";
        if (label != null) ShowToast(label + gained + (suffix ?? ""));
        StreakToast();
        if (kind == HouseTarget.Kind.Hoop) Juice.HoopBlast(pos, ComboPitch(), Combo);
        else Juice.Delivery(pos, ComboPitch(), Combo);
        if (Streak > 0 && Streak % 5 == 0 && GameConfig.ComboJuice > 0f) Juice.ComboMilestone(pos, Streak);

        if (Tutorial.Active && completes) Tutorial.OnDelivered(kind);
    }

    // A hit that earns no points of its own (a hoop that was already used or cleared, a palm already scored) still adds 1 to the streak.
    public void CountHit()
    {
        if (!Playing) return;
        Streak++;
        StreakToast();
    }

    // The streak just reached the hot (or the super hot) level: the words, with one flame or two after them.
    void StreakToast()
    {
        if (Streak == GameConfig.SuperStreakAt) ShowToast("SUPER HOT", 2f, ToastIcon.FireDouble);
        else if (Streak == hotStreakAt) ShowToast("HOT STREAK", 2f, ToastIcon.Fire);
    }

    public void PaperMissed(Vector3 pos)
    {
        if (!Playing) return;
        Juice.Miss(pos);
    }

    // Smashing things is just fun: a broken window or a smashed plate costs nothing (no life, the streak stays).
    public void BrokeWindow(Vector3 pos)
    {
        if (!Playing) return;
        Juice.WindowBreak(pos);
    }

    public void BrokeDishes(Vector3 pos)
    {
        if (!Playing) return;
        Juice.WindowBreak(pos);
    }

    // Points for a trick that is not a delivery (a swish through a hoop at a house with no customer).
    public void Bonus(int points, string label, Vector3 pos)
    {
        if (!Playing) return;
        Streak++;               // a hoop (a swish at a house with no customer, a palm shot) counts as a hit too
        points *= Multiplier;   // every point is multiplied by the streak
        Score += points;
        ShowToast(label + points);
        if (label.StartsWith("Swish")) Juice.HoopBlast(pos, ComboPitch(), Combo);
        else Juice.Delivery(pos, ComboPitch(), Combo);
        if (Streak > 0 && Streak % 5 == 0 && GameConfig.ComboJuice > 0f) Juice.ComboMilestone(pos, Streak);
    }

    // A basket was scored (each ring pays once): it is worth 3 coins.
    public void BasketScored()
    {
        if (!Playing) return;
        RunBaskets++;
        AddRunCoins(GameConfig.CoinsPerBasket);
    }

    // Coins earned in this run (shown on the HUD, paid out when the run ends). The tutorial earns none.
    public void AddRunCoins(int n)
    {
        if (!Playing || Tutorial.Active || n <= 0) return;
        RunCoins += n;
        if (GameConfig.CoinDust) GameUI.AddCoinDust(ScorePos, n);   // tiny golden dust flies from where it was earned up to the coin counter
    }

    // A trick shot (a bounce off anything but a backboard or ring, then through a ring): the bonus comes with a big fiery explosion.
    public void TrickBonus(int points, string label, Vector3 pos)
    {
        if (!Playing) return;
        points *= Multiplier;
        Score += points;
        ShowToast(label, 2f);   // (the words only, no points)
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
        ShowToast("Stack Cleared!", 2f);
        Juice.Smash(pos, Color.white, true);
    }

    public void MissedSubscriber()
    {
        if (!Playing || Tutorial.Active) return;
        BreakStreak();   // the only thing that breaks the streak: a building left with a floor undelivered, which costs a life
        LoseLife(livesLostMissedCustomer, "Customer complain", ToastIcon.Alert);
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
        }
    }

    public void ShowToast(string message, float seconds = 1.5f, ToastIcon icon = ToastIcon.None)
    {
        if (string.IsNullOrEmpty(message)) return;   // (no text: nothing is shown)
        Toast = message;
        ToastIconKind = icon;
        ToastUntil = Time.unscaledTime + seconds;
        ToastAt = Time.unscaledTime;
    }

    // A mistake costs lives. A short grace period stops one collision from costing several.
    void LoseLife(int amount, string message, ToastIcon icon = ToastIcon.None)
    {
        if (Tutorial.Active) { ShowToast(message, 1.5f, icon); return; }   // no lives are lost while learning
        if (amount <= 0) { ShowToast(message, 1.5f, icon); return; }
        if (GameConfig.GodMode) { ShowToast(string.IsNullOrEmpty(message) ? null : message + " (god mode)", 1.5f, icon); return; }
        if (Time.unscaledTime < invulnerableUntil) return;
        Lives = Mathf.Max(0, Lives - amount);
        invulnerableUntil = Time.unscaledTime + invulnerableSeconds;
        ShowToast(message, 1.5f, icon);
        if (Lives <= 0) End("Too many customer complaints.");
    }

    // A paper hit a heart.
    public void CollectHeart(Vector3 pos)
    {
        if (!Playing) return;
        if (Lives < maxLives)
        {
            Lives++;
        }
        else
        {
            int heartPoints = heartPointsWhenFull * Multiplier;
            Score += heartPoints;
        }
        Juice.Delivery(pos, 1.6f);
    }
}
