using UnityEngine;

// Put this on ONE empty GameObject in a fresh scene and press Play.
// It creates the managers, bike, camera rig, road spawner and day cycle at runtime, so no other setup is needed.
public class PrototypeBootstrap : MonoBehaviour
{
    void Awake()
    {
        gameObject.AddComponent<DebugOverlay>();   // shows errors and fps on screen (remove for release)
        gameObject.AddComponent<Tutorial>();   // the first-run tutorial (starts from Play the first time, or from Options)
        gameObject.AddComponent<OptionsMenu>();   // player options: delivery-area and grass colours (opened from the menus)
        gameObject.AddComponent<DevMenu>();   // DEV button: tweak throw, bike, world and camera settings in game (remove for release)
        gameObject.AddComponent<Look>();   // ambient light, vignette, optional sun shadows (switched in the DEV menu)
        gameObject.AddComponent<MobileSetup>();   // frame rate, portrait lock, screen-on, cheaper quality

        var managers = new GameObject("Managers");
        managers.AddComponent<Sfx>();
        managers.AddComponent<Juice>();
        managers.AddComponent<PowerUps>();   // timed power-ups: badges on the road (hit them with a paper)
        managers.AddComponent<GameManager>();
        managers.AddComponent<GameUI>();

        var bike = new GameObject("Bike");
        Renderer frame = BuildBikeVisual(bike.transform);
        bike.AddComponent<BikeForward>();
        bike.AddComponent<PaperThrower>();

        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
        cam.fieldOfView = 60f;
        cam.farClipPlane = 300f;
        DisablePostProcessing(cam);
        cam.gameObject.AddComponent<CameraFollow>().target = bike.transform;
        cam.gameObject.AddComponent<DayCycle>();
        new GameObject("Backdrop").AddComponent<Backdrop>().Init(cam, bike.transform);   // sky, sun, clouds, far hills, wide ground

        var spawner = new GameObject("RoadSpawner").AddComponent<SegmentSpawner>();
        spawner.Init(bike.transform);

        GameManager.Instance.Setup(spawner, frame);
    }

    // Searches every loaded assembly, so it works wherever Unity keeps the type, and compiles without URP.
    static System.Type FindType(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName, false);
            if (t != null) return t;
        }
        return null;
    }

    // The Universal 3D template adds a Global Volume (bloom, tonemapping...) that can blow the picture out to white,
    // and the camera has post-processing switched on. The game uses neither, so turn both off.
    static void DisablePostProcessing(Camera cam)
    {
        try
        {
            var volumeType = FindType("UnityEngine.Rendering.Volume");
            if (volumeType != null)
            {
                foreach (var o in Object.FindObjectsByType(volumeType, FindObjectsSortMode.None))
                {
                    var b = o as Behaviour;
                    if (b != null) b.enabled = false;
                }
            }

            var camDataType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (camDataType != null && cam != null)
            {
                var data = cam.GetComponent(camDataType);
                var prop = camDataType.GetProperty("renderPostProcessing");
                if (data != null && prop != null) prop.SetValue(data, false, null);
            }
        }
        catch (System.Exception e) { Debug.LogWarning("Could not disable post-processing: " + e.Message); }
    }

    // Returns the frame's renderer so the shop can recolour the bike.
    static Renderer BuildBikeVisual(Transform bike)
    {
        Renderer frame = Rider.Build(bike);   // the bike and the person sitting on it (Rider.cs)
        if (BlobShadow.Available)
        {
            var blob = BlobShadow.Create(bike);
            blob.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            blob.transform.localScale = new Vector3(1.4f, 2.5f, 1f);
            blob.GetComponent<Renderer>().sharedMaterial = BlobShadow.MatFor(0.35f);
            blob.AddComponent<BlobToggle>();
            blob.AddComponent<BikeShadow>();   // at night the lamps throw the bike's shadow (worked out, not rendered)
        }
        return frame;
    }
}
