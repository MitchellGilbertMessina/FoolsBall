using UnityEngine;
using ChocDino.PartyIO;
using Unity.Mathematics;

public class FoosballKeyboardController : MonoBehaviour
{
    public GameObject wall1;
    public GameObject wall2;

    public GameObject groundPlaneObj;

    public float slideSpeed = 2f;
    public float rotationSpeed = 500f;

    public float wallPadding = 0.025f;

    float slideMin;
    float slideMax;

    public float mouseSensitivity = 1f;

    public Transform player1Rod;
    public Transform player2Rod;

    public int mouse1Index;
    public int mouse2Index;

    // Per-player accumulated "virtual cursor" screen positions (deltas from the mice are added here each frame).
    Vector3 player1Cursor;
    Vector3 player2Cursor;

    float player1TargetX;
    float player2TargetX;

    float player1Rotation;
    float player2Rotation;

    Rigidbody player1RigidBody;
    Rigidbody player2RigidBody;

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

        player1RigidBody = CacheRodBody(player1Rod);
        player2RigidBody = CacheRodBody(player2Rod);

        // Start the slide targets where the rods currently are so nothing snaps on the first frame.
        if (player1Rod != null) player1TargetX = player1Rod.position.x;
        if (player2Rod != null) player2TargetX = player2Rod.position.x;
    }

    Rigidbody CacheRodBody(Transform rod)
    {
        if (rod == null) return null;

        Rigidbody body = rod.GetComponent<Rigidbody>();
        if (body == null)
            Debug.LogWarning($"[FoolsBall] '{rod.name}' has no Rigidbody. Add a kinematic Rigidbody to the rod so it can kick the ball.");
        else if (!body.isKinematic)
            Debug.LogWarning($"[FoolsBall] '{rod.name}' Rigidbody should be marked Is Kinematic.");
        return body;
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

        // TEMP DIAGNOSTIC: shows how many mice are seen and whether movement deltas arrive.
        if (mice.Count > 0)
            Debug.Log($"[FoolsBall] mice={mice.Count} delta={mice[0].PositionDelta} scroll={mice[0].ScrollDelta} p1TargetX={player1TargetX:F4}");
        else
            Debug.Log("[FoolsBall] mice=0 (no device registered yet — wiggle the mouse)");

        // Drive each player only if its mouse has connected yet. A device only appears in
        // the list AFTER that physical mouse first sends input, so wiggle each mouse once.
        if (mice.Count >= mouse1Index)
            HandlePlayer(mice[mouse1Index], player1Rod, ref player1Cursor, ref player1TargetX, ref player1Rotation);

		if (mice.Count >= mouse2Index)
			HandlePlayer(mice[mouse2Index], player2Rod, ref player2Cursor, ref player2TargetX, ref player2Rotation);
	}

    // Read input every frame (so no movement is missed) but only record TARGETS here;
    // the actual motion is applied through physics in FixedUpdate.
    void HandlePlayer(MouseDevice mouse, Transform rod, ref Vector3 cursor, ref float targetX, ref float rotation)
    {
        if (mouse == null || rod == null) return;

        cursor += new Vector3(mouse.PositionDelta.x, -mouse.PositionDelta.y, 0f) * mouseSensitivity;

        // Keep the virtual cursor on screen.
        cursor.x = Mathf.Clamp(cursor.x, 0f, Screen.width);
        cursor.y = Mathf.Clamp(cursor.y, 0f, Screen.height);

        // --- Slide target: raycast the virtual cursor onto the ground plane ---
        Ray ray = cam.ScreenPointToRay(cursor);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            targetX = Mathf.Clamp(hit.x, slideMin, slideMax);
        }

        // --- Rotation target: accumulate this mouse's scroll wheel ---
        rotation += mouse.ScrollDelta.y * rotationSpeed * Time.deltaTime;
    }

    // Move the kinematic Rigidbodies here so the feet impart real velocity to the ball.
    void FixedUpdate()
    {
        ApplyRod(player1RigidBody, player1Rod, player1TargetX, player1Rotation);
        ApplyRod(player2RigidBody, player2Rod, player2TargetX, player2Rotation);
    }

    void ApplyRod(Rigidbody body, Transform rod, float targetX, float rotation)
    {
        if (body == null || rod == null) return;

        // Slide along X, keep the rod's current Y/Z.
        Vector3 pos = body.position;
        pos.x = targetX;
        body.MovePosition(pos);

        // Rotation is authored in local space; convert to world for MoveRotation.
        Quaternion localTarget = Quaternion.Euler(-rotation, 0f, 90f);
        Quaternion worldTarget = rod.parent != null ? rod.parent.rotation * localTarget : localTarget;
        body.MoveRotation(worldTarget);
    }
}
