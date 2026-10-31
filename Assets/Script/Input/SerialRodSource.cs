using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns the Arduino's comma-separated values (via SerialManager) into RodStates.
/// Everything hardware-specific lives here: which index is which, raw slide
/// calibration, limit-switch recalibration, rotation zeroing. When the linear
/// encoder changes, this is the file that changes; gameplay doesn't.
/// </summary>
[DefaultExecutionOrder(-200)]
public class SerialRodSource : RodInputSource
{
    [System.Serializable]
    public class RodMapping
    {
        [Tooltip("Matches FoosballController.rodId")]
        public int rodId;

        [Header("Serial indices (-1 = not wired)")]
        public int rotationIndex;
        public int slideIndex;
        public int buttonIndex = -1;
        public int limitAIndex = -1;
        public int limitBIndex = -1;

        [Header("Slide calibration (raw units)")]
        public float slideMin;
        public float slideMax = 1f;
        [Tooltip("If true, limit A = slideMax and limit B = slideMin")]
        public bool invertLimitMapping;

        [System.NonSerialized] public bool rotationOffsetSet;
        [System.NonSerialized] public float rotationOffset;
        [System.NonSerialized] public bool prevLimitA, prevLimitB;
        [System.NonSerialized] public RodState state;
    }

    [Tooltip("Defaults match the two-rod jam build's Arduino line format.")]
    public List<RodMapping> rods = new List<RodMapping>
    {
        new RodMapping { rodId = 0, rotationIndex = 0, slideIndex = 1, buttonIndex = 4, limitAIndex = 6, limitBIndex = 7, slideMin = 94.6f, slideMax = 990f },
        new RodMapping { rodId = 1, rotationIndex = 2, slideIndex = 3, buttonIndex = 5, limitAIndex = 8, limitBIndex = 9, slideMin = -671f, slideMax = -17f },
    };

    [Tooltip("Treat each rod's angle at the first reading after connecting as upright (0°).")]
    public bool zeroRotationOnConnect = true;

    long connectionId = -1;

    public override string DisplayName =>
        SerialManager.Instance != null && SerialManager.Instance.IsConnected
            ? "Arduino rods (" + SerialManager.Instance.ConnectedPort + ")"
            : "Arduino rods (not connected)";

    public override bool IsAvailable =>
        SerialManager.Instance != null && SerialManager.Instance.IsConnected;

    public override bool TryGetRod(int rodId, out RodState state)
    {
        foreach (var m in rods)
        {
            if (m.rodId == rodId)
            {
                state = m.state;
                return true;
            }
        }
        state = RodState.Neutral;
        return false;
    }

    // Runs every frame (not only when active) so calibration keeps tracking
    // even while you're playing with the mouse.
    void Update()
    {
        var serial = SerialManager.Instance;
        if (serial == null || !serial.IsConnected) return;
        if (serial.LinesReceived == 0) return; // connected but no data yet

        // New connection → re-zero rotation.
        if (serial.ConnectionCount != connectionId)
        {
            connectionId = serial.ConnectionCount;
            foreach (var m in rods) m.rotationOffsetSet = false;
        }

        foreach (var m in rods) UpdateRod(serial, m);
    }

    void UpdateRod(SerialManager serial, RodMapping m)
    {
        // ---------------- ROTATION ----------------
        float rawRotation = serial.GetValue(m.rotationIndex);

        if (!m.rotationOffsetSet)
        {
            m.rotationOffset = zeroRotationOnConnect ? rawRotation : 0f;
            m.rotationOffsetSet = true;
        }

        m.state.angle = Mathf.DeltaAngle(0f, rawRotation - m.rotationOffset);

        // ---------------- SLIDE + LIMIT CALIBRATION ----------------
        float rawSlide = serial.GetValue(m.slideIndex);

        bool limitA = m.limitAIndex >= 0 && serial.GetValue(m.limitAIndex) == 1f;
        bool limitB = m.limitBIndex >= 0 && serial.GetValue(m.limitBIndex) == 1f;

        if (limitA && !m.prevLimitA)
        {
            if (m.invertLimitMapping) m.slideMax = rawSlide; else m.slideMin = rawSlide;
            Debug.Log($"Rod {m.rodId} | calibrated limit A → raw {rawSlide}");
        }

        if (limitB && !m.prevLimitB)
        {
            if (m.invertLimitMapping) m.slideMin = rawSlide; else m.slideMax = rawSlide;
            Debug.Log($"Rod {m.rodId} | calibrated limit B → raw {rawSlide}");
        }

        m.prevLimitA = limitA;
        m.prevLimitB = limitB;

        float t = Mathf.InverseLerp(m.slideMin, m.slideMax, rawSlide);
        m.state.slide = Mathf.Lerp(-1f, 1f, t);

        // ---------------- BUTTON ----------------
        m.state.button = m.buttonIndex >= 0 && serial.GetValue(m.buttonIndex) == 1f;
    }
}
