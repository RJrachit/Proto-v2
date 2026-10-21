using UnityEngine;

// One crate or can in a stack on a lot without a customer. A paper that hits it damages it:
//   crates take a couple of hits (they wobble and darken first), cans go flying at once.
// Units above a broken one drop down. Breaking units can knock their neighbours (a chain). Clearing a whole stack pays papers back.
// The stack logic (rewards, chains, what is left standing) lives in House.StackHit.
public class Destructible : MonoBehaviour
{
    public House owner;
    public bool crate;
    public Renderer rend;
    public int level;                       // 0 = bottom row of the stack
    public int group;                       // which pyramid on the lot (a lot can hold two); -1 = a billboard (see House.LayoutBlocker); -2 = a shop prop
    public bool glass;                      // a shop window (group -2): breaks with a glass crash
    public bool fixedPos;                   // does not drop down when units below it are gone
    public int hp;
    public Color color;

    Collider col;
    Vector3 baseScale = Vector3.one;
    Color crackColor;
    float punch;
    bool dying;
    float dyingTime;
    Vector3 vel;
    Vector3 spin;

    public bool Standing { get { return hp > 0 && !dying; } }

    void Awake() { col = GetComponent<Collider>(); }

    // Sets the unit up for a new stack (the same objects are re-used every time the road recycles).
    public void Place(int lvl, int hits, Vector3 localPos, Vector3 scale, Color c)
    {
        level = lvl;
        hp = hits;
        color = c;
        crackColor = new Color(c.r * 0.65f, c.g * 0.65f, c.b * 0.65f, 1f);
        baseScale = scale;
        dying = false;
        punch = 0f;
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.identity;
        transform.localScale = scale;
        if (rend != null) rend.material.color = c;
        gameObject.SetActive(true);
        if (col == null) col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    // A paper hit this unit. `dir` is the paper's direction of travel (world). A chain hit does not start another chain.
    public void Hit(Vector3 dir, bool chain = false)
    {
        if (!Standing) return;
        hp--;
        if (hp > 0)
        {
            punch = 1f;
            if (rend != null) rend.material.color = crackColor;
            if (owner != null) owner.StackHit(this, false, chain);
            return;
        }

        dying = true;
        dyingTime = 0f;
        if (col != null) col.enabled = false;
        Vector3 d = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
        vel = d * 2.5f + Vector3.up * 3.5f + Random.insideUnitSphere * 1.2f;
        spin = Random.insideUnitSphere * 540f;
        if (owner != null) owner.StackHit(this, true, chain);
    }

    void Update()
    {
        if (dying)
        {
            dyingTime += Time.deltaTime;
            vel += Vector3.down * 12f * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime, Space.Self);
            if (dyingTime > 0.8f) gameObject.SetActive(false);
            return;
        }

        // drop down when what was under us is gone
        if (owner != null && !fixedPos)
        {
            Vector3 p = transform.localPosition;
            p.y = Mathf.MoveTowards(p.y, owner.StackY(level, group), 5f * Time.deltaTime);
            transform.localPosition = p;
        }

        if (punch > 0f)
        {
            punch = Mathf.MoveTowards(punch, 0f, 4f * Time.deltaTime);
            transform.localScale = baseScale * (1f + 0.2f * punch);
            transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(Time.time * 40f) * 8f * punch);
        }
    }
}
