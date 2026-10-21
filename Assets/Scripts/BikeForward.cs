using UnityEngine;

// Moves the bike down the road on rails while a run is in progress. The speed follows a profile over the run (see GameConfig.SpeedAt).
public class BikeForward : MonoBehaviour
{
    public float startSpeed = GameConfig.BikeStartSpeed;
    public float maxSpeed = GameConfig.BikeMaxSpeed;

    public static BikeForward Instance { get; private set; }

    float speed;
    public bool Hold;                // true: the bike stands still (the tutorial's intro)
    float speedOverride;            // above 0: a fixed speed (the tutorial)
    float runTime;                  // seconds ridden this run (the speed follows GameConfig.SpeedAt)

    public Vector3 Velocity { get { return transform.forward * speed; } }

    void Awake()
    {
        Instance = this;
        speed = GameConfig.SpeedAt(0f);
    }

    public void ResetRun()
    {
        transform.position = Vector3.zero;
        runTime = 0f;
        speedOverride = 0f;
        Hold = false;
        speed = GameConfig.SpeedAt(0f);
    }

    // The tutorial: start part-way along the road and ride at a slow, fixed speed.
    public void BeginTutorial(float startZ, float fixedSpeed)
    {
        transform.position = new Vector3(0f, 0f, startZ);
        runTime = 0f;
        speedOverride = fixedSpeed;
        speed = fixedSpeed;
    }

    // Puts the bike back along the road (the tutorial's second try at a house).
    public void MoveTo(float z)
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, p.y, z);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.Playing || Hold) return;

        runTime += Time.deltaTime;
        speed = speedOverride > 0f ? speedOverride : GameConfig.SpeedAt(runTime);
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
