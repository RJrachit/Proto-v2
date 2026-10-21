using UnityEngine;

// Marks a porch roof that has a basketball ring beside it. Nothing here moves the paper: the roof just leans towards the ring and is slippery
// (see House.LayoutPorchRoof), and ordinary physics does the rest. A paper that slides off the roof and drops through the ring is a trick shot
// like any other that bounced off a surface (see Paper).
public class ChuteSurface : MonoBehaviour
{
    public bool active;   // off on every roof that has no ring beside it
}
