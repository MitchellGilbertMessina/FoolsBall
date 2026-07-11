using UnityEngine;

public class FoosballController : MonoBehaviour
{
    public int rotationIndex = 0;
    public int slideIndex    = 1;
    public int buttonIndex   = 4;

    [Header("Slide Calibration Indices")]
    [Tooltip("Serial index for this player's limit A trigger")]
    public int limitAIndex = 6;
    [Tooltip("Serial index for this player's limit B trigger")]
    public int limitBIndex = 7;

    [Tooltip("If true, limit A = slideMax and limit B = slideMin (reversed mapping)")]
    public bool invertLimitMapping = false;

    [Header("Slide Settings")]
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

// Read the serial input every frame, but only record TARGETS here.
    // ---------------- CALIBRATION EDGE DETECTION ----------------
    private bool prevLimitA = false;
    private bool prevLimitB = false;

    // ---------------- LERP OVERRIDE ----------------
    private bool lerpOverrideActive = false;
    private float lerpTarget;
    private float lerpSpeed = 20f;

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

    void Update()
    {
        if (SerialManager.Instance == null) return;

        // --- Debug ---
        Debug.Log($"{gameObject.name} | rot:{SerialManager.Instance.GetValue(rotationIndex)} slide:{SerialManager.Instance.GetValue(slideIndex)}");

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
        
        // ---------------- SLIDE CALIBRATION ----------------
        float rawSlide = SerialManager.Instance.GetValue(slideIndex);
        float t = Mathf.InverseLerp(slideMin, slideMax, rawSlide);
        targetLocalX = Mathf.Lerp(-slideRange, slideRange, t);

        bool limitA = SerialManager.Instance.GetValue(limitAIndex) == 1f;
        bool limitB = SerialManager.Instance.GetValue(limitBIndex) == 1f;

        if (limitA && !prevLimitA)
        {
            if (invertLimitMapping)
            {
                slideMax = rawSlide;
                lerpTarget = slideRange;
            }
            else
            {
                slideMin = rawSlide;
                lerpTarget = -slideRange;
            }

            lerpOverrideActive = true;
            Debug.Log($"{gameObject.name} | Calibrated limit A → raw {rawSlide}");
        }

        if (limitB && !prevLimitB)
        {
            if (invertLimitMapping)
            {
                slideMin = rawSlide;
                lerpTarget = -slideRange;
            }
            else
            {
                slideMax = rawSlide;
                lerpTarget = slideRange;
            }

            lerpOverrideActive = true;
            Debug.Log($"{gameObject.name} | Calibrated limit B → raw {rawSlide}");
        }

        prevLimitA = limitA;
        prevLimitB = limitB;

        // ---------------- SLIDE POSITION ----------------
        Vector3 pos = transform.localPosition;

        if (lerpOverrideActive)
        {
            pos.x = Mathf.MoveTowards(pos.x, lerpTarget, lerpSpeed * Time.deltaTime);

            if (Mathf.Approximately(pos.x, lerpTarget))
                lerpOverrideActive = false;
        }
        else
        {
            float t = Mathf.InverseLerp(slideMin, slideMax, rawSlide);
            pos.x = Mathf.Lerp(-slideRange, slideRange, t);
        }

        transform.localPosition = pos;

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
