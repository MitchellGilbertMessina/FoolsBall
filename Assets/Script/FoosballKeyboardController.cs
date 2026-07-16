using UnityEngine;
using ChocDino.PartyIO;
using Unity.Mathematics;

public class FoosballKeyboardController : MonoBehaviour
{
    public bool updateMouse;
    public bool invertRotation;

    public float groundPlaneOffset;

    public float slideSpeed = 2f;
    public float rotationSpeed = 500f;

    public float slideMin;
    public float slideMax;

    public float mouseSensitivity = 1f;

    public int mouseIndex;

    Vector3 cursor;

    float targetX;

    float rotation;

    Rigidbody rb;

    Camera cam;
    Plane groundPlane;

    private void Start()
    {
        cam = Camera.main;
        groundPlane = new Plane(Vector3.up, new Vector3(0f, groundPlaneOffset, 0f));

        // Hide the system cursor and lock it so the shared OS pointer doesn't drift/merge the mice.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Start both virtual cursors in the centre of the screen.
        Vector3 centre = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        cursor = centre;

        rb = CacheRodBody();

        // Start the slide targets where the rods currently are so nothing snaps on the first frame.
        targetX = transform.position.x;
    }

    Rigidbody CacheRodBody()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            Debug.LogWarning($"[FoolsBall] '{this.name}' has no Rigidbody. Add a kinematic Rigidbody to the rod so it can kick the ball.");
        else if (!body.isKinematic)
            Debug.LogWarning($"[FoolsBall] '{this.name}' Rigidbody should be marked Is Kinematic.");
        return body;
    }

    void Update()
    {
        if (updateMouse)
            MouseDeviceManager.Instance.Update();

        var mice = MouseDeviceManager.Instance.All;

        if (mice.Count <= mouseIndex) 
        {
            return;
		}

        Debug.Log(this);

        var mouse = mice[mouseIndex];

        Debug.Log(new Vector2(mouse.PositionDelta.x, mouse.PositionDelta.y));

        cursor += new Vector3(mouse.PositionDelta.x, -mouse.PositionDelta.y, 0f) * mouseSensitivity;

        // Keep the  cursor on screen.
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
        float invertRot = invertRotation ? -1 : 1;
        rotation += mouse.ScrollDelta.y * rotationSpeed * Time.deltaTime * invertRot;
    }


    // Move the kinematic Rigidbodies here so the feet impart real velocity to the ball.
    void FixedUpdate()
    {
        // Slide along X, keep the rod's current Y/Z.
        Vector3 pos = transform.position;
        pos.x = targetX;
        rb.MovePosition(pos);

        // Rotation is authored in local space; convert to world for MoveRotation.
        Quaternion localTarget = Quaternion.Euler(-rotation, 0f, 90f);
        Quaternion worldTarget = transform.parent != null ? transform.parent.rotation * localTarget : localTarget;
        rb.MoveRotation(worldTarget);
    }

}
