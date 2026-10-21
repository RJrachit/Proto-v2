using UnityEngine;

// A trigger zone that belongs to a House: the garden, the doorstep mat, the mailbox, the balcony, a window, the table, the hoop, or the dishes on a table.
// The paper reports the hit here; the House decides what it means.
public class HouseTarget : MonoBehaviour
{
    // Door = the doorstep mat. Garden = the big lawn. Path = the path from the footpath to the door.
    public enum Kind { Door, Mailbox, Balcony, Window, Garden, Path, Table, Hoop, Breakable, Lifeguard, Palm }

    public Kind kind;
    public House owner;
    public Vector3 size;   // trigger box size in world units (the aim assist uses it)

    [Tooltip("If set, a paper that hits this zone snaps to this point (and earns the snap bonus).")]
    public Transform snapPoint;
    [Tooltip("The paper shrinks and disappears into the target (mailbox) instead of lying on it.")]
    public bool tuckIn;
    [Tooltip("If above 0, a paper must come down within this distance of the snap point to be caught (a beach cabin's open roof).")]
    public float openingHalf;
    [Tooltip("If above 0, the opening is this wide along the road (z) instead of openingHalf.")]
    public float openingHalfZ;
    [Tooltip("A reverse basket: a paper bounced up from the trampoline beneath scores as it rises through the ring (as well as one that drops in).")]
    public bool reverse;
    [Tooltip("A balcony zone: its place in the house's balcony pool (House.balconies).")]
    public int index;

    public void OnPaperHit(Vector3 pos, bool snapped, bool trick = false)
    {
        if (kind == Kind.Window)
        {
            if (owner != null) owner.HitWindow(this, pos);
            return;
        }
        if (kind == Kind.Breakable)
        {
            if (owner != null) owner.HitBreakable(this, pos);
            return;
        }
        if (kind == Kind.Palm)
        {
            if (owner != null) owner.HandlePalm(this, pos);
            return;
        }
        if (kind == Kind.Balcony && owner != null)
        {
            owner.HandleBalcony(this, pos, snapped);
            return;
        }
        if (kind == Kind.Hoop && owner != null)
        {
            owner.HandleHoop(this, snapped && snapPoint != null ? snapPoint.position : pos, snapped, trick);
            return;
        }
        if (owner != null) owner.HandleHit(kind, snapped && snapPoint != null ? snapPoint.position : pos, snapped);
    }
}
