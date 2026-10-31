using UnityEngine;

/// <summary>
/// One foosball rod. Reads a RodState from RodInputHub (Arduino rods or
/// mouse/keyboard; it doesn't know or care which) and moves/rotates itself.
/// Hardware mapping and calibration now live in SerialRodSource.
/// </summary>
public class FoosballController : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique per rod. Input sources look rods up by this id.")]
    public int rodId = 0;
    public FoosTeam team = FoosTeam.Red;

    [Header("Motion")]
    [Tooltip("Half-width of the rod's travel along local X. Power-ups may change this at runtime.")]
    public float slideRange = 0.3f;

    [Tooltip("1 or -1. Flips spin for rods that face the other way.")]
    public float rotationDirection = 1f;

    /// <summary>The input this rod used this frame.</summary>
    public RodState Current { get; private set; }

    void OnEnable() => RodInputHub.Register(this);
    void OnDisable() => RodInputHub.Unregister(this);

    void Update()
    {
        RodState s = RodInputHub.GetRod(rodId);
        Current = s;

        Vector3 pos = transform.localPosition;
        pos.x = s.slide * slideRange;
        transform.localPosition = pos;

        transform.localRotation = Quaternion.Euler(-s.angle * rotationDirection, 0, 90);
    }
}
