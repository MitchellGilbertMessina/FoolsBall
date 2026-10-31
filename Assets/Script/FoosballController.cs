using UnityEngine;

public enum FoosRow { Back = 0, Front = 1 }

/// <summary>
/// One foosball rod. Reads a RodState from RodInputHub (Arduino rods or
/// mouse/keyboard; it doesn't know or care which) and moves/rotates itself.
///
/// The rod is driven as a kinematic Rigidbody in FixedUpdate, so the physics
/// engine knows how fast the figures are moving: a fast flick hits the ball
/// hard, a slow push nudges it. Interpolation keeps it visually smooth.
/// Hardware mapping and calibration live in SerialRodSource.
/// </summary>
public class FoosballController : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique per rod. Input sources look rods up by this id.")]
    public int rodId = 0;
    public FoosTeam team = FoosTeam.Red;
    [Tooltip("Back = goalie row (nearest your own goal), Front = strikers.")]
    public FoosRow row = FoosRow.Back;

    [Header("Motion")]
    [Tooltip("Half-width of the rod's travel along local X. Power-ups may change this at runtime.")]
    public float slideRange = 0.3f;

    [Tooltip("1 or -1. Flips spin for rods that face the other way.")]
    public float rotationDirection = 1f;

    [Header("Smoothing")]
    [Tooltip("Seconds to catch up with the input. 0 = raw; ~0.02–0.05 = buttery; higher = floaty.")]
    public float slideSmoothTime = 0.025f;
    public float spinSmoothTime = 0.015f;

    /// <summary>The input this rod used this frame.</summary>
    public RodState Current { get; private set; }

    Rigidbody body;
    float baseY, baseZ;           // the rod's resting local position (it only slides on X)
    float slide, slideVel;        // smoothed local X
    float angle, angleVel;        // smoothed spin, degrees

    void Awake()
    {
        Vector3 p = transform.localPosition;
        baseY = p.y;
        baseZ = p.z;
        slide = p.x;

        // Kinematic: we drive it, physics just reads its motion. Child colliders
        // (the figures) become one compound collider on this body.
        if (!TryGetComponent(out body)) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void OnEnable() => RodInputHub.Register(this);
    void OnDisable() => RodInputHub.Unregister(this);

    void Update()
    {
        Current = RodInputHub.GetRod(rodId);
    }

    void FixedUpdate()
    {
        RodState s = Current;
        float dt = Time.fixedDeltaTime;

        float targetSlide = s.slide * slideRange;
        float targetAngle = s.angle * rotationDirection;

        slide = slideSmoothTime > 0f
            ? Mathf.SmoothDamp(slide, targetSlide, ref slideVel, slideSmoothTime, Mathf.Infinity, dt)
            : targetSlide;

        angle = spinSmoothTime > 0f
            ? Mathf.SmoothDampAngle(angle, targetAngle, ref angleVel, spinSmoothTime, Mathf.Infinity, dt)
            : targetAngle;

        Vector3 localPos = new Vector3(slide, baseY, baseZ);
        Quaternion localRot = Quaternion.Euler(-angle, 0, 90);

        Transform parent = transform.parent;
        Vector3 worldPos = parent != null ? parent.TransformPoint(localPos) : localPos;
        Quaternion worldRot = parent != null ? parent.rotation * localRot : localRot;

        body.MovePosition(worldPos);
        body.MoveRotation(worldRot);
    }
}
