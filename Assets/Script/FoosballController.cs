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

    // ---------------- CALIBRATION EDGE DETECTION ----------------
    private bool prevLimitA = false;
    private bool prevLimitB = false;

    // ---------------- LERP OVERRIDE ----------------
    private bool lerpOverrideActive = false;
    private float lerpTarget;
    private float lerpSpeed = 20f;

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

        UpdateRod();
    }

    void UpdateRod()
    {
        transform.localRotation = Quaternion.Euler(-rotationAngle, 0, 90);
    }
}