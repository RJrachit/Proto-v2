using UnityEngine;

// The first-run tutorial. It plays inside the real game, on a short scripted road (SegmentSpawner builds one customer house per stretch
// while Tutorial.Active is on), at a slow fixed bike speed, with no lives lost and no papers used up.
//   0  (not used: the old intro slide)
//   1  clear the ground floor: anything on the ground counts (lawn, path, doormat, mailbox)
//   2  clear a whole building: a two-floor house, every floor has to be cleared
//   3  several balconies: dunk the building's hoop to clear all the balconies at once (then the ground floor)
//   4  the streak (caption)    5  the lives (caption)   6  ready: start the real run
// A step finishes when the player does the thing. If the bike passes the house without it, the bike is put back in front of the house
// (the house is re-armed) and the same step is tried again. The tutorial shows on the first Play and can be replayed from Options.
public partial class Tutorial : MonoBehaviour
{
    public static bool Active { get; private set; }
    public static bool Done { get { return PlayerPrefs.GetInt("TutorialDone", 0) == 1; } }
    public static bool ShouldRun { get { return !Done; } }

    public const float BikeSpeed = 2.2f;       // m/s: slow, so there is time to read and aim
    public const float StartZ = 32f;           // the bike starts a little way along the road, the first house is ahead

    const int StepCount = 7;
    const int FirstHouseStep = 1;
    const int LastHouseStep = 3;               // steps 1..3 each have a house of their own (stretch = step)
    static bool IsHouseStep(int s) { return s >= FirstHouseStep && s <= LastHouseStep; }

    static Tutorial inst;
    static bool firstTime, pendingDaily;

    // ---- the script ----

    // which side each scripted house is on: +1 right, -1 left (index 0 = the lawn house; used by SegmentSpawner and by the ghost hand)
    public static int SideOfStep(int houseIndex) { return !GameConfig.ChaseView ? -1 : ((houseIndex & 1) == 0 ? 1 : -1); }   // (the side view has houses only on the left)

    static readonly string[] Titles =
    {
        "Look for the glow",
        "Clear the ground floor",
        "Clear the whole building",
        "Dunk the hoop!",
        "Keep the streak going",
        "These are your lives",
        "You're ready!",
    };
    static readonly string[] Subs =
    {
        "Glowing spots score. Land your paper on one.",
        "Drag back, then let go: the paper flies the opposite way. Land it on anything glowing on the ground: the lawn, the path, the doormat or the mailbox.",
        "This house has two floors. Clear the ground floor, then land a paper on the balcony above it. Every floor has to be cleared.",
        "Some floors have several balconies. Drop a paper through the hoop to clear all the balconies at once, then clear the ground floor too.",
        "Every glowing spot or hoop you hit adds to your streak, and a longer streak multiplies your points. Only driving past a house you have not finished breaks it.",
        "Drive past a house with a floor left and you lose a life. Lose them all and the run ends. Hit a heart with a paper to win one back.",
        "Deliver as many papers as you can.",
    };
    static readonly string[] Hints =
    {
        "Pull farther for a longer throw. The yellow line shows where it will go.",
        "Throw high and strong to reach the balcony: pull farther back.",
        "Aim high so the paper falls down through the ring.",
    };
    static readonly string[] Praise = { "Nice!", "Great!", "Dunkkkk!" };

    // ---- state ----

    int step;
    int throws;
    float stepTime;
    bool ghost;
    bool wrongKind;
    bool hoopDunked;           // step 3: the paper went through the building's hoop
    string praise;
    float praiseUntil;
    float fadeStart = -10f;
    bool transitioning;        // a step is done: the next one loads behind a fade
    float jumpAt;
    int moveKind;              // 0 none, 1 load the next step, 2 try the house again (done when the screen is black)
    int retryK;
    int introPhase;            // step 0: 0 = the side-by-side card (scores / no score), 1 = the real zones lit up on the road
    GUIStyle tileS, tileDimS;
    GUIStyle titleS, subS, hintS, praiseS, skipS;
    static Rect cardRectScreen, extraRectScreen;       // screen pixels, origin top-left: presses here never start a throw

    void Awake() { inst = this; }

    // ---- entry points (called by GameManager, PaperThrower, GameUI) ----

    public static void Begin(bool daily)
    {
        firstTime = !Done;
        pendingDaily = daily;
        Active = true;
        if (inst != null) inst.ResetState();
    }

    // Leaves the tutorial without marking it done (Home from the pause menu, or a restart).
    public static void Abort()
    {
        Active = false;
        cardRectScreen = new Rect(0f, 0f, 0f, 0f);
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);
    }

    static void Leave()
    {
        var gm = GameManager.Instance;
        bool first = firstTime;
        Active = false;
        cardRectScreen = new Rect(0f, 0f, 0f, 0f);
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.SetInt("Runs", Mathf.Max(3, PlayerPrefs.GetInt("Runs", 0)));   // the tutorial replaces the first-runs hint
        PlayerPrefs.Save();
        if (gm == null) return;
        if (first) gm.StartRun(pendingDaily); else gm.GoToMenu();
    }

    public static bool BlocksInput(Vector2 pTopLeft)
    {
        return Active && (cardRectScreen.Contains(pTopLeft) || extraRectScreen.Contains(pTopLeft));
    }

    public static void OnThrow()
    {
        if (Active && inst != null) inst.Thrown();
    }

    // The building's hoop was scored (House calls this just before it reports the delivery).
    public static void OnBuildingHoop()
    {
        if (Active && inst != null) inst.hoopDunked = true;
    }

    public static void OnDelivered(HouseTarget.Kind kind)
    {
        if (Active && inst != null) inst.Delivered(kind);
    }

    void ResetState()
    {
        step = FirstHouseStep;   // the intro slide (step 0) is skipped: the glowing zones explain themselves
        throws = 0;
        stepTime = 0f;
        ghost = true;
        wrongKind = false;
        hoopDunked = false;
        praise = null;
        fadeStart = -10f;
        transitioning = false;
        moveKind = 0;
        introPhase = 0;
    }

    // ---- progress ----

    void Thrown()
    {
        if (transitioning || moveKind != 0 || !IsHouseStep(step)) return;
        throws++;
        ghost = false;
        // after several tries the step passes anyway, so nobody is stuck
        if (throws >= 8) Advance("Let's move on");
    }

    void Delivered(HouseTarget.Kind kind)
    {
        if (step > LastHouseStep || transitioning || moveKind != 0) return;
        if (step == 0) NextStep();   // a paper landed during the intro: count it as the first throw
        bool ok = false;
        switch (step)
        {
            case 1: ok = true; break;                 // anything on the ground
            case 2: ok = true; break;                 // every floor cleared (a delivery is only reported when the whole house is done)
            case 3: ok = hoopDunked; break;           // the balconies went in one go through the hoop
        }
        if (ok) { Advance(Praise[step - 1]); return; }

        // the house is done, but not the way this step is about: it is tried again once the bike is past it
        wrongKind = true;
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast("Close! This time dunk the hoop to clear the balconies", 2.2f);
    }

    // A finished house step shows its praise, then the screen fades and the next step loads (the bike is put at throwing distance from the next target).
    // The caption steps simply move on.
    void Advance(string message)
    {
        praise = message;
        praiseUntil = Time.unscaledTime + 1.4f;
        if (IsHouseStep(step))
        {
            transitioning = true;
            jumpAt = Time.unscaledTime + (message != null ? 1.2f : 0.2f);
            return;
        }
        NextStep();
    }

    void NextStep()
    {
        step = Mathf.Min(StepCount - 1, step + 1);
        throws = 0;
        stepTime = 0f;
        ghost = IsHouseStep(step);
        wrongKind = false;
        hoopDunked = false;
    }

    void StartFade(int kind)
    {
        fadeStart = Time.unscaledTime;
        moveKind = kind;
    }

    // Called when the screen is black.
    void FinishMove(GameManager gm, BikeForward bike)
    {
        var sp = gm.Spawner;
        if (moveKind == 1)
        {
            NextStep();
            transitioning = false;
            Paper.DestroyAll();
            if (IsHouseStep(step) && sp != null) bike.MoveTo(sp.TutorialRetryZ(step));
        }
        else if (moveKind == 2)
        {
            if (sp != null)
            {
                bike.MoveTo(sp.TutorialRetryZ(retryK));
                sp.TutorialRearm(retryK);   // after the move: a house behind the bike would count as done
            }
            Paper.DestroyAll();
            wrongKind = false;
            hoopDunked = false;
            ghost = true;
        }
        moveKind = 0;
    }
}
