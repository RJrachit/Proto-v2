using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A thrown newspaper. The result is decided by where the paper ENDS UP, not by what it touches first.
//   - Walls and backboards (BankSurface) bounce it with a scripted, slightly dead bounce.
//   - Floors (ground, lawn, mat, balcony slab) grip it: the first landing only skids a short way.
//   - A mailbox is a container: a paper that comes down into the opening stays there and counts at once.
//   - Otherwise, when the paper comes to rest, the target zone it is inside decides the result
//     (balcony, then doorstep, then path, then garden). Resting anywhere else is a miss.
//   - Windows break on impact.
// There is no bonus for how the paper got there: a bank shot that ends in a target scores like a straight throw.
// The exception is the trick shot: a paper that bounces off anything except a backboard or a ring (a wall, a roof, the ground, a trampoline ...)
// and then goes through a ring (dropping in, or rising through a reverse basket from below) earns a bonus.
public class Paper : MonoBehaviour
{
    public static readonly List<Paper> All = new List<Paper>();

    static float SkidKeep { get { return GameConfig.PaperSkidKeep; } }           // share of sideways speed kept when the paper first lands
    static float LandBounce { get { return GameConfig.PaperLandBounce; } }        // share of downward speed that bounces back up on first landing
    static float MaxLife { get { return GameConfig.PaperMaxLife; } }              // seconds before the paper is judged wherever it is
    static float RestSpeedSqr { get { return GameConfig.PaperRestSpeedSqr; } }      // below this speed (squared) the paper counts as stopped
    static float RestSpinSqr { get { return GameConfig.PaperRestSpinSqr; } }        // and below this spin (squared)
    static float RestTime { get { return GameConfig.PaperRestTime; } }          // for this long
    static float OpeningHalfX { get { return GameConfig.MailboxOpeningHalfX; } }       // size of the mailbox opening (half widths)
    static float OpeningHalfZ { get { return GameConfig.MailboxOpeningHalfZ; } }

    Rigidbody rb;
    Vector3 lastVelocity;                  // velocity before the latest physics step (the impact velocity)
    bool done;                             // the result has been reported
    public bool InFlight { get { return !done; } }   // still travelling (the side-view camera follows it)
    bool landed;                           // has touched a floor
    bool windowHit;
    bool bounced;                          // has bounced off something other than a backboard or ring (a paper that scores after that is a trick shot)
    int trampHops;                         // bounces on trampolines so far (no limit: only the paper's life time ends a paper that keeps bouncing)
    bool scoredInAir;                      // already scored by rising through a reverse basket: it flies on, and is no miss when it lands
    bool hitDestructible;                  // smashed into crates or cans: not a miss, even if it lands nowhere
    bool balconyScored;                    // touched a balcony floor: it counted at once, so where it comes to rest does not matter any more
    float life = MaxLife;
    float restTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.angularDamping = 1.5f;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static void DestroyAll()
    {
        for (int i = All.Count - 1; i >= 0; i--)
            if (All[i] != null) Destroy(All[i].gameObject);
        All.Clear();
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return;
        if (boosting) StepBoost();
        lastVelocity = rb.linearVelocity;
    }

    // ---- the faster start ----
    // The paper flies the same arc, but the first part of it quicker: time is sped up by k (velocity x k, gravity x k*k gives exactly the same path).
    // k starts at `boost`, holds, and eases back to 1 where the boosted share of the flight ends; from there on the physics is the plain physics.
    // A bounce, a landing or the leaves end it early, with the speed turned back to the normal speed, so those behave as they always did.
    bool boosting;
    float boostK = 1f, boostTau, boostT0, boostShare, boostPeak;

    float BoostK(float tau)
    {
        float s = Mathf.Clamp01(tau / Mathf.Max(0.01f, boostT0 * boostShare));
        return Mathf.Lerp(boostPeak, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, s)));
    }

    // v: the launch velocity at normal speed; flightTime: how long the arc takes at normal speed.
    public void BeginBoost(Vector3 v, float flightTime, float boost, float share)
    {
        if (rb == null) return;
        boosting = true;
        boostPeak = Mathf.Max(1f, boost);
        boostT0 = flightTime;
        boostShare = Mathf.Clamp(share, 0.1f, 0.95f);
        boostTau = 0f;
        boostK = BoostK(0f);
        rb.useGravity = false;
        rb.linearVelocity = v * boostK;
    }

    void StepBoost()
    {
        if (boostTau >= boostT0 * boostShare) { EndBoost(); return; }
        float dt = Time.fixedDeltaTime;
        float k = BoostK(boostTau);
        Vector3 v = rb.linearVelocity * (k / boostK) + Physics.gravity * (k * k * dt);
        rb.linearVelocity = v;
        boostK = k;
        boostTau += k * dt;
    }

    void EndBoost()
    {
        if (!boosting) return;
        boosting = false;
        if (rb == null) return;
        rb.useGravity = true;
        float inv = 1f / boostK;
        rb.linearVelocity *= inv;
        lastVelocity *= inv;     // (the impact velocity the bounce code reads is the normal-speed one)
        boostK = 1f;
    }

    void Update()
    {
        if (done) return;

        life -= Time.deltaTime;
        if (landed && rb != null
            && rb.linearVelocity.sqrMagnitude < RestSpeedSqr && rb.angularVelocity.sqrMagnitude < RestSpinSqr)
        {
            restTime += Time.deltaTime;
            if (restTime >= RestTime) { Settle(); return; }
        }
        else
        {
            restTime = 0f;
        }

        if (life <= 0f || transform.position.y < -2f) Settle();
    }

    // ---- bouncing and landing ----

    // Throw check for the Console: where the arc should end (plain physics) against where the paper first touched something.
    public Vector3 ThrowFrom;
    public float ThrowPredicted = -1f;
    public string ThrowNote = "";

    void OnCollisionEnter(Collision collision)
    {
        if (done || rb == null) return;
        EndBoost();
        if (ThrowPredicted >= 0f)
        {
            Vector3 d = transform.position - ThrowFrom; d.y = 0f;
            Debug.Log("[Throw] predicted " + ThrowPredicted.ToString("0.0") + " m, first touch " + d.magnitude.ToString("0.0") + " m (" + collision.collider.name + ") " + ThrowNote);
            ThrowPredicted = -1f;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 n = contact.normal;
        if (Vector3.Dot(n, transform.position - contact.point) < 0f) n = -n;   // make it point away from the surface

        var dest = collision.collider.GetComponent<Destructible>();
        if (dest != null) { hitDestructible = true; dest.Hit(lastVelocity); }

        var bank = collision.collider.GetComponent<BankSurface>();
        if (MaxLife - life > 0.1f && (bank == null || !bank.hoopPart)) bounced = true;   // (the first moments of the throw do not count)

        // a trampoline throws the paper back (as often as it hits one; the speed cap and the paper's life time keep it from going on for ever)
        var tramp = collision.collider.GetComponent<TrampolineSurface>();
        if (tramp != null && (n.y > 0.5f || tramp.vertical))
        {
            trampHops++;
            Bounce(n, tramp.restitution, tramp.tangentKeep);
            if (Vector3.Dot(lastVelocity, n) < 0f)   // (a soft touch still launches: at least TrampolineMinBounce away from the pad)
            {
                float away = Vector3.Dot(rb.linearVelocity, n);
                if (away < GameConfig.TrampolineMinBounce) rb.linearVelocity += n * (GameConfig.TrampolineMinBounce - away);
            }
            if (rb.linearVelocity.magnitude > GameConfig.TrampolineMaxSpeed) rb.linearVelocity = rb.linearVelocity.normalized * GameConfig.TrampolineMaxSpeed;
            tramp.Squash(Mathf.Abs(Vector3.Dot(lastVelocity, n)) / 9f);   // a harder hit makes the pad vibrate more
            Sfx.Play("thunk", 0.9f, 1.8f);
            Haptics.Pulse(10);
            return;
        }

        // A balcony counts the moment the paper touches its floor (from above), not when it comes to rest.
        if (!balconyScored && n.y > 0.5f)
        {
            var floorHouse = collision.collider.GetComponentInParent<House>();
            if (floorHouse != null && floorHouse.TouchBalconyFloor(collision.collider, contact.point)) { balconyScored = true; hitDestructible = true; }
        }

        if (bank != null && bank.guideRing != null && Tutorial.HoopFocus && bank.guideRing.gameObject.activeInHierarchy)
        {
            GuideIntoRing(bank.guideRing);   // (the tutorial's basket shot only: any touch of the board drops the paper into the ring)
            return;
        }

        if (bank != null)
        {
            // A window breaks only when the paper actually hits it, at the moment of impact (not when it flies back off the wall).
            // The glass takes the paper's energy: it hardly bounces and drops straight down, much deader than a hit on the plain wall.
            bool throughGlass = false;
            if (!windowHit)
            {
                var house = collision.collider.GetComponentInParent<House>();
                if (house != null && house.TryBreakWindowAt(contact.point)) { windowHit = true; hitDestructible = true; throughGlass = true; }   // (smashing is fun: not a miss)
            }
            if (throughGlass) Bounce(n, GameConfig.WindowBounce, GameConfig.WindowSlideKeep);
            else Bounce(n, bank.restitution, bank.tangentKeep);
        }
        else if (n.y > 0.7f)
        {
            if (!landed)
            {
                // First landing: paper does not slide or bounce much
                Vector3 v = lastVelocity;
                var sticky = collision.collider.GetComponent<StickySurface>();
                float keep = sticky != null ? sticky.skidKeep : SkidKeep;
                rb.linearVelocity = new Vector3(v.x * keep, Mathf.Abs(v.y) * LandBounce, v.z * keep);
                rb.angularVelocity *= 0.3f;
            }
            landed = true;
        }
    }

    // The tutorial's basket shot: a paper that touches the backboard is sent in a short arc to the middle of the ring and drops through it.
    void GuideIntoRing(HouseTarget ring)
    {
        Vector3 c = ring.transform.position;
        Vector3 target = new Vector3(c.x, c.y + 0.1f, c.z);
        Vector3 p = transform.position;
        const float T = 0.3f;
        float g = -Physics.gravity.y;
        rb.linearVelocity = new Vector3((target.x - p.x) / T, (target.y - p.y) / T + 0.5f * g * T, (target.z - p.z) / T);
        rb.angularVelocity *= 0.3f;
    }

    void Bounce(Vector3 n, float restitution, float tangentKeep)
    {
        Vector3 vin = lastVelocity;
        float vn = Vector3.Dot(vin, n);          // negative when moving into the surface
        if (vn >= 0f) return;                    // already moving away

        Vector3 tangent = vin - vn * n;
        rb.linearVelocity = tangent * tangentKeep - vn * restitution * n;
        rb.angularVelocity = Random.insideUnitSphere * 3f;
    }

    // ---- mailbox container and window ----

    void OnTriggerEnter(Collider other) { CheckZone(other); }
    void OnTriggerStay(Collider other) { CheckZone(other); }
    void OnTriggerExit(Collider other) { if (other == foliageIn) foliageIn = null; }

    // Leaves: a paper that flies into a tree's foliage is caught by it. It loses nearly all its speed and sinks slowly through the leaves
    // (it does not bounce), the leaves rustle and a few fall, and it comes out of the bottom (or ends up stuck on the way if its life runs out).
    Collider foliageIn;

    void PushThroughFoliage(Collider other, TreeFoliage foliage)
    {
        if (rb == null || rb.isKinematic) return;
        if (foliageIn != other)
        {
            foliageIn = other;
            foliage.Rustle(transform.position, rb.linearVelocity.magnitude);
        }
        float dt = Time.fixedDeltaTime;
        rb.linearVelocity *= Mathf.Exp(-GameConfig.FoliageDrag * dt);
        rb.angularVelocity *= Mathf.Exp(-3f * dt);
    }

    void CheckZone(Collider other)
    {
        if (done) return;
        var foliage = other.GetComponent<TreeFoliage>();
        if (foliage != null) { EndBoost(); PushThroughFoliage(other, foliage); return; }
        var heart = other.GetComponent<HeartPickup>();
        if (heart != null) { heart.Collect(); return; }

        var target = other.GetComponent<HouseTarget>();
        if (target == null) return;

        if (target.kind == HouseTarget.Kind.Mailbox || target.kind == HouseTarget.Kind.Hoop || target.kind == HouseTarget.Kind.Lifeguard)
        {
            TryCatch(target);
        }
        else if (target.kind == HouseTarget.Kind.Breakable)
        {
            hitDestructible = true;
            target.OnPaperHit(transform.position, false);   // dishes break on contact (free); this paper is spent but not a miss
        }
    }

    // The mailbox keeps a paper that comes down into its opening.
    void TryCatch(HouseTarget target)
    {
        Vector3 c = target.snapPoint != null ? target.snapPoint.position : target.transform.position;
        Vector3 p = transform.position;
        bool hoop = target.kind == HouseTarget.Kind.Hoop;
        float halfX = hoop ? GameConfig.HoopOpeningHalf : OpeningHalfX;
        float halfZ = hoop ? GameConfig.HoopOpeningHalf : OpeningHalfZ;
        if (Tutorial.Active && target.kind == HouseTarget.Kind.Mailbox) { halfX *= 2f; halfZ *= 2f; }   // (a roomier mailbox while learning)
        if (target.openingHalf > 0f) { halfX = target.openingHalf; halfZ = target.openingHalfZ > 0f ? target.openingHalfZ : target.openingHalf; }
        bool inOpening = Mathf.Abs(p.x - c.x) < halfX && Mathf.Abs(p.z - c.z) < halfZ;
        if (!inOpening || rb == null) return;

        // A reverse basket also takes a paper bounced up from the trampoline beneath: it scores as it rises through the ring, then flies on.
        if (hoop && target.reverse && rb.linearVelocity.y > 0.5f)
        {
            if (scoredInAir || trampHops == 0 || p.y < target.transform.position.y - 0.25f) return;
            scoredInAir = true;
            target.OnPaperHit(p, false, true);
            return;
        }
        if (rb.linearVelocity.y > -0.5f) return;   // must be coming down into it

        done = true;
        target.OnPaperHit(p, true, bounced && hoop);
        GameManager.Instance.PaperDone();
        StartCoroutine(TuckIn(target));
    }

    // The paper slips into the mailbox and disappears.
    IEnumerator TuckIn(HouseTarget target)
    {
        if (rb != null) rb.isKinematic = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        Vector3 fromScale = transform.localScale;

        Vector3 toPos = target.snapPoint != null ? target.snapPoint.position : fromPos;
        Quaternion toRot = Quaternion.Euler(0f, fromRot.eulerAngles.y, 0f);
        Vector3 toScale = fromScale * 0.55f;

        const float duration = 0.5f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            k = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(fromPos, toPos, k);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
            transform.localScale = Vector3.Lerp(fromScale, toScale, k);
            yield return null;
        }
        transform.position = toPos;
        transform.rotation = toRot;
        transform.localScale = toScale;

        Sfx.Play("thunk", 0.9f, Random.Range(0.95f, 1.1f));
        Haptics.Pulse(15);

        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    // ---- result: where the paper ended up ----

    void Settle()
    {
        done = true;

        HouseTarget best = null;
        int bestRank = 0;
        var overlaps = Physics.OverlapSphere(transform.position, 0.1f, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (var col in overlaps)
        {
            var t = col.GetComponent<HouseTarget>();
            if (t == null) continue;
            int rank = Rank(t.kind);
            if (rank > bestRank) { best = t; bestRank = rank; }
        }

        if (balconyScored) best = null;   // (smashing the dishes does not cost the paper its result)

        if (best != null) best.OnPaperHit(transform.position, Scoring.Snaps(best.kind));
        else if (!hitDestructible && !scoredInAir) GameManager.Instance.PaperMissed(transform.position);
        if (best == null && !scoredInAir && !balconyScored) Tutorial.OnPaperFailed();   // (the tutorial's near-shot lesson starts again)

        GameManager.Instance.PaperDone();
        Destroy(gameObject, best != null ? 1.2f : 0.8f);   // it lies where it landed for a moment
    }

    // If a paper rests where zones overlap, the more precise one wins.
    static int Rank(HouseTarget.Kind kind)
    {
        switch (kind)
        {
            case HouseTarget.Kind.Palm:    return 5;   // resting on a palm crown (nothing else is up there)
            case HouseTarget.Kind.Balcony: return 4;
            case HouseTarget.Kind.Door:    return 3;
            case HouseTarget.Kind.Table:   return 3;
            case HouseTarget.Kind.Path:    return 2;
            case HouseTarget.Kind.Garden:  return 1;
            default: return 0;   // mailbox and window are handled on contact, not by where the paper rests
        }
    }
}
