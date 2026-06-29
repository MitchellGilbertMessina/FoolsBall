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

    void Update()
    {
        if (SerialManager.Instance == null) return;

        // --- Debug ---
        Debug.Log($"{gameObject.name} | rot:{SerialManager.Instance.GetValue(rotationIndex)} slide:{SerialManager.Instance.GetValue(slideIndex)}");

        // ---------------- ROTATION (unchanged) ----------------
        float rawRotation = SerialManager.Instance.GetValue(rotationIndex);

        if (!rotationOffsetSet)
        {
            rotationOffset = rawRotation;
            rotationOffsetSet = true;
        }

        rotationAngle = (rawRotation - rotationOffset) * rotationDirection;

        if (rotationAngle < -180) rotationAngle += 360;
        if (rotationAngle > 180) rotationAngle -= 360;

        // ---------------- SLIDE (FIXED) ----------------
        float rawSlide = SerialManager.Instance.GetValue(slideIndex);

        float t = Mathf.InverseLerp(slideMin, slideMax, rawSlide);

        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Lerp(-slideRange, slideRange, t);
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