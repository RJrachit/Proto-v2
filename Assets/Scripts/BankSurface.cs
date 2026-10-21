using UnityEngine;

// Put this on a collider (a house wall, a pillar, a fence) to make papers bounce off it.
// The bounce is scripted so it is predictable: the paper loses energy, so it feels like paper and not a rubber ball.
public class BankSurface : MonoBehaviour
{
    [Tooltip("Share of the speed straight into the surface that comes back out. Lower = deader bounce.")]
    [Range(0f, 1f)] public float restitution = GameConfig.WallBounce;

    [Tooltip("Share of the sideways (sliding) speed the paper keeps.")]
    [Range(0f, 1f)] public float tangentKeep = GameConfig.WallSlideKeep;

    [Tooltip("A basketball backboard or ring. Hitting these before going in the ring does not make a trick shot (a bounce off anything else does).")]
    public bool hoopPart;

    [Tooltip("The tutorial's basket shot only: a paper that touches this backboard is guided into this ring (Paper reads it). Null everywhere else.")]
    public HouseTarget guideRing;
}
