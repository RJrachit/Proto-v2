using UnityEngine;

// A row of plain buildings and trees behind the houses of one side of a road segment, so the area behind the houses is not bare grass.
// Decoration only: no colliders, no targets. Layout() re-rolls it every time the segment is recycled.
public class BackRow
{
    const int Buildings = 3;
    const int Trees = 26;

    int side;
    Transform holderT;
    readonly Transform[] bodies = new Transform[Buildings];
    readonly Renderer[] bodyR = new Renderer[Buildings];
    readonly Transform[] roofs = new Transform[Buildings];
    readonly Renderer[] roofR = new Renderer[Buildings];
    readonly Transform[] tanks = new Transform[Buildings];
    readonly Transform[] trunks = new Transform[Trees];
    readonly Transform[] crowns = new Transform[Trees];
    readonly MeshFilter[] crownF = new MeshFilter[Trees];
    readonly Renderer[] crownR = new Renderer[Trees];
    readonly Renderer[] trunkR = new Renderer[Trees];

    static readonly Color[] Walls =
    {
        new Color(0.86f, 0.78f, 0.68f), new Color(0.78f, 0.82f, 0.86f), new Color(0.88f, 0.74f, 0.72f),
        new Color(0.74f, 0.82f, 0.74f), new Color(0.90f, 0.88f, 0.76f),
    };

    public static BackRow Build(Transform parent, int side)
    {
        var row = new BackRow();
        row.side = side;
        var holder = new GameObject(side < 0 ? "BackLeft" : "BackRight").transform;
        holder.SetParent(parent, false);
        row.holderT = holder;

        for (int i = 0; i < Buildings; i++)
        {
            var body = Prim.Make(PrimitiveType.Cube, "BackBuilding", holder, Vector3.zero, Vector3.one, Color.white);
            var roof = Prim.Make(PrimitiveType.Cube, "BackRoof", holder, Vector3.zero, Vector3.one, Color.white);
            var tank = Prim.Make(PrimitiveType.Cylinder, "BackTank", holder, Vector3.zero, Vector3.one, new Color(0.45f, 0.52f, 0.60f));
            row.bodies[i] = body.transform;
            row.bodyR[i] = body.GetComponent<Renderer>();
            row.roofs[i] = roof.transform;
            row.roofR[i] = roof.GetComponent<Renderer>();
            row.tanks[i] = tank.transform;
        }
        // the trees are the same low-poly trees as on the lawns (LowPolyTree), a lighter copy: coarse shared crown and trunk meshes, shared materials, no colliders
        for (int i = 0; i < Trees; i++)
        {
            var trunk = new GameObject("BackTrunk");
            trunk.transform.SetParent(holder, false);
            trunk.AddComponent<MeshFilter>().sharedMesh = LowPolyTree.BackTrunkMesh();
            var tr = trunk.AddComponent<MeshRenderer>();
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var crown = new GameObject("BackCrown");
            crown.transform.SetParent(holder, false);
            var cf = crown.AddComponent<MeshFilter>();
            var cr = crown.AddComponent<MeshRenderer>();
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            row.trunks[i] = trunk.transform;
            row.trunkR[i] = tr;
            row.crowns[i] = crown.transform;
            row.crownF[i] = cf;
            row.crownR[i] = cr;
        }
        return row;
    }

    public void SetVisible(bool on) { if (holderT != null) holderT.gameObject.SetActive(on); }

    static float Rand(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    // taper 1: a beach has just ended at the start of this stretch. The sea sweeps away across it (see BeachTail) and the land beside it is a
    // headland of vegetation, so the first two back buildings and the back trees over that part are left out, and the third is lower than usual.
    public void Layout(System.Random r, float segLen, int taper = 0)
    {
        float slot = segLen / Buildings;
        for (int i = 0; i < Buildings; i++)
        {
            bodies[i].gameObject.SetActive(true);
            roofs[i].gameObject.SetActive(true);
            float w = Rand(r, 5f, 8f);      // along x (away from the road)
            float d = Rand(r, 6f, 9f);      // along the road
            float h = Rand(r, 6f, 15f);
            float x = side * (19.5f + w * 0.5f + Rand(r, 0f, 4f));
            float z = slot * (i + 0.5f) + Rand(r, -1.5f, 1.5f);
            if (taper == 1)
            {
                float k = i < 2 ? 0.5f : 0.6f;
                h = Mathf.Max(3.5f, h * k);
                x += side * (1f - k) * 9f;
            }
            Color c = Walls[r.Next(Walls.Length)];
            c = new Color(c.r * 0.92f, c.g * 0.92f, c.b * 0.92f, 1f);

            bodies[i].localPosition = new Vector3(x, h * 0.5f, z);
            bodies[i].localScale = new Vector3(w, h, d);
            bodyR[i].material.color = c;
            roofs[i].localPosition = new Vector3(x, h + 0.2f, z);
            roofs[i].localScale = new Vector3(w + 0.4f, 0.4f, d + 0.4f);
            roofR[i].material.color = new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 1f);
            bool tank = r.NextDouble() < 0.4 && !(taper == 1 && i < 2);
            tanks[i].gameObject.SetActive(tank);
            tanks[i].localPosition = new Vector3(x, h + 1.3f, z);
            tanks[i].localScale = new Vector3(1.6f, 0.9f, 1.6f);
            if (taper == 1 && i < 2)      // the first two are left out (the dice above are still rolled, so the others stay the same)
            {
                bodies[i].gameObject.SetActive(false);
                roofs[i].gameObject.SetActive(false);
                tanks[i].gameObject.SetActive(false);
            }
        }

        float tslot = segLen / Trees;
        for (int i = 0; i < Trees; i++)
        {
            int tier = i % 3;       // 0 close behind the houses, 1 further back, 2 far back (the tiers overlap in the view, so the background is full)
            float x = side * (tier == 0 ? Rand(r, 16.5f, 18.5f) : tier == 1 ? Rand(r, 23f, 31f) : Rand(r, 33f, 44f));
            float z = tslot * (i + 0.5f) + Rand(r, -1.5f, 1.5f);
            float top = tier == 0 ? Rand(r, 7f, 10.5f) : tier == 1 ? Rand(r, 9f, 13f) : Rand(r, 10.5f, 15f);
            int shape = tier == 0 ? 3 + r.Next(3) : r.Next(LowPolyTree.ShapeCount);     // the close ones are slim (they stand between the houses and the back buildings)
            int colour = LowPolyTree.BackColourPool[r.Next(LowPolyTree.BackColourPool.Length)];
            int bark = r.Next(4) == 0 ? 1 : 0;
            float yaw = Rand(r, 0f, 360f);
            bool inSand = taper == 1 && z < 26f;         // the headland's own vegetation stands there instead
            trunks[i].gameObject.SetActive(!inSand);
            crowns[i].gameObject.SetActive(!inSand);

            float rx = LowPolyTree.CrownShapes[shape, 0], ry = LowPolyTree.CrownShapes[shape, 1], rz = LowPolyTree.CrownShapes[shape, 2], cy = LowPolyTree.CrownShapes[shape, 3];
            float sink = GameConfig.TreeSink;
            float grow = (top + sink) / (cy + ry);                                   // one scale for the whole tree, so it reaches the height asked for
            float maxR = tier == 0 ? 2.1f : 3.4f;
            float fw = Mathf.Clamp(maxR / (Mathf.Max(rx, rz) * grow), 0.6f, 1.3f);   // wider when there is room, never into the houses
            float trunkTop = (cy - 0.1f * ry) * grow;
            float thick = Mathf.Max(1.1f, grow * 1.2f);

            trunks[i].localPosition = new Vector3(x, -sink, z);                      // the foot is buried, as on the lawns
            trunks[i].localRotation = Quaternion.Euler(0f, yaw, 0f);
            trunks[i].localScale = new Vector3(thick, trunkTop, thick);
            trunkR[i].sharedMaterial = LowPolyTree.BackTrunkMat(bark);
            crowns[i].localPosition = new Vector3(x, -sink + cy * grow, z);
            crowns[i].localRotation = Quaternion.Euler(0f, yaw, 0f);
            crowns[i].localScale = new Vector3(rx * fw, ry, rz * fw) * grow;
            crownF[i].sharedMesh = LowPolyTree.BackCrownMesh(shape);
            crownR[i].sharedMaterial = LowPolyTree.BackCrownMat(colour);
        }
    }
}
