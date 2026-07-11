using UnityEngine;
using ChocDino.PartyIO;
using Unity.Mathematics;

public class FoosballKeyboardController : MonoBehaviour
{
    public Transform player1Rod;
    public Transform player2Rod;

    public GameObject wall1;
    public GameObject wall2;

    public GameObject groundPlaneObj;

    public float slideSpeed = 2f;
    public float rotationSpeed = 500f;

    public float wallPadding = 0.025f;

    float slideMin;
    float slideMax;

    [Header("ChocDino Mouse Party")]
    [Tooltip("Multiplier applied to each mouse's raw pixel movement before it is added to that player's virtual cursor.")]
    public float mouseSensitivity = 1f;

    // Per-player accumulated "virtual cursor" screen positions (deltas from the mice are added here each frame).
    Vector3 player1Cursor;
    Vector3 player2Cursor;

    float player1Rotation;
    float player2Rotation;

    Camera cam;
    Plane groundPlane;

    private void Start()
    {
        cam = Camera.main;
        groundPlane = new Plane(Vector3.up, new Vector3(0f, groundPlaneObj.transform.position.y, 0f));

        // Hide the system cursor and lock it so the shared OS pointer doesn't drift/merge the mice.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Start both virtual cursors in the centre of the screen.
        Vector3 centre = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        player1Cursor = centre;
        player2Cursor = centre;

        float wall1X = wall1.transform.position.x;
        float wall2X = wall2.transform.position.x;

        slideMin = math.min(wall1X, wall2X)+ wallPadding;
        slideMax = math.max(wall1X, wall2X) - wallPadding;
    }

    void OnEnable()
    {
        // Log every device as it connects/disconnects so you can confirm the plugin sees your mice.
        MouseDeviceManager.ChangedConnectionState += OnMouseConnectionChanged;
    }

    void OnDisable()
    {
        MouseDeviceManager.ChangedConnectionState -= OnMouseConnectionChanged;
    }

    void OnMouseConnectionChanged(MouseDevice mouse)
    {
        Debug.Log($"[FoolsBall] Mouse {mouse.ConnectionState}: id={mouse.DeviceId} name=\"{mouse.FriendlyName}\" (total now {MouseDeviceManager.Instance.All.Count})");
    }

    void Update()
    {
        MouseDeviceManager.Instance.Update();

        var mice = MouseDeviceManager.Instance.All;

        // Drive each player only if its mouse has connected yet. A device only appears in
        // the list AFTER that physical mouse first sends input, so wiggle each mouse once.
        if (mice.Count > 0)
            HandlePlayer(mice[0], player1Rod, ref player1Cursor, ref player1Rotation);

        if (mice.Count > 1)
            HandlePlayer(mice[1], player2Rod, ref player2Cursor, ref player2Rotation);
    }

    void HandlePlayer(MouseDevice mouse, Transform rod, ref Vector3 cursor, ref float rotation)
    {
        if (mouse == null || rod == null) return;

        cursor += new Vector3(mouse.PositionDelta.x, -mouse.PositionDelta.y, 0f) * mouseSensitivity;

        // Keep the virtual cursor on screen.
        cursor.x = Mathf.Clamp(cursor.x, 0f, Screen.width);
        cursor.y = Mathf.Clamp(cursor.y, 0f, Screen.height);

        // --- Slide: raycast the virtual cursor onto the ground plane, drive rod X ---
        Ray ray = cam.ScreenPointToRay(cursor);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            float x = Mathf.Clamp(hit.x, slideMin, slideMax);
            rod.position = new Vector3(x, rod.position.y, rod.position.z);
        }

        // --- Rotation: this mouse's scroll wheel ---
        float scroll = mouse.ScrollDelta.y;
        rotation += scroll * rotationSpeed * Time.deltaTime;

        rod.localRotation = Quaternion.Euler(-rotation, 0, 90);
    }
}
