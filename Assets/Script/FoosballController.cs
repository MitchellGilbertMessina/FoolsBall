using UnityEngine;

public class FoosballController : MonoBehaviour
{
    public int rotationIndex = 0;
    public int slideIndex    = 1;
    public int buttonIndex   = 4;

    public float slideMin   = 77f;
    public float slideMax   = 185f;
    public float slideRange = 0.3f;

    private float rotationAngle;
    private float rotationOffset;
    private bool  rotationOffsetSet = false;

    public float rotationDirection = 1f;

    private bool buttonPressed = false;

    // Physics-driven state: input writes these targets in Update, FixedUpdate applies them.
    private float targetLocalX;

    private Rigidbody body;
    private Transform rodParent;
    private Vector3 baseLocalPos;   // resting local position; only X changes when sliding

    void Start()
    {
        rodParent = transform.parent;
        baseLocalPos = transform.localPosition;
        targetLocalX = baseLocalPos.x;

        body = GetComponent<Rigidbody>();
        if (body == null)
            Debug.LogWarning($"[FoolsBall] '{name}' has no Rigidbody. Add a kinematic Rigidbody to the rod so it can kick the ball.");
        else if (!body.isKinematic)
            Debug.LogWarning($"[FoolsBall] '{name}' Rigidbody should be marked Is Kinematic.");
    }

    // Read the serial input every frame, but only record TARGETS here.
    void Update()
    {
        if (SerialManager.Instance == null) return;

        // ---------------- ROTATION ----------------
        float rawRotation = SerialManager.Instance.GetValue(rotationIndex);

        if (!rotationOffsetSet)
        {
            rotationOffset = rawRotation;
            rotationOffsetSet = true;
        }

        rotationAngle = (rawRotation - rotationOffset) * rotationDirection;

        if (rotationAngle < -180) rotationAngle += 360;
        if (rotationAngle > 180) rotationAngle -= 360;

        // ---------------- SLIDE ----------------
        float rawSlide = SerialManager.Instance.GetValue(slideIndex);
        float t = Mathf.InverseLerp(slideMin, slideMax, rawSlide);
        targetLocalX = Mathf.Lerp(-slideRange, slideRange, t);

        // ---------------- BUTTON ----------------
        buttonPressed = SerialManager.Instance.GetValue(buttonIndex) == 1f;
    }

    // Apply motion through physics so the kinematic Rigidbody imparts velocity to the ball.
    void FixedUpdate()
    {
        if (body == null) return;

        // Slide along local X, keep the rod's resting local Y/Z; convert to world for MovePosition.
        Vector3 localPos = baseLocalPos;
        localPos.x = targetLocalX;
        Vector3 worldPos = rodParent != null ? rodParent.TransformPoint(localPos) : localPos;
        body.MovePosition(worldPos);

        // Rotation is authored in local space; convert to world for MoveRotation.
        Quaternion localRot = Quaternion.Euler(-rotationAngle, 0f, 90f);
        Quaternion worldRot = rodParent != null ? rodParent.rotation * localRot : localRot;
        body.MoveRotation(worldRot);
    }
}
