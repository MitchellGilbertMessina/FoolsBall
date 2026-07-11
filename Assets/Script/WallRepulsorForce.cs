using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WallRepulsorForce : MonoBehaviour
{
    [Header("Push direction")]
    [Tooltip("Rotates the push direction around the wall's up axis. 0 = straight out along transform.forward.")]
    public float angle = 0f;

    [Header("Force settings")]
    [Tooltip("Max force applied right at the wall surface.")]
    public float forcePower = 5f;

    [Tooltip("How far into the field the push effect reaches.")]
    public float effectRange = 1f;

    [Tooltip("If true, force falls off linearly with distance. If false, force is constant within range.")]
    public bool useFalloff = true;

    private Collider triggerZone;
    private Vector3 PushDirection =>
        (Quaternion.AngleAxis(angle, transform.up) * transform.forward).normalized;

    void Awake()
    {
        triggerZone = GetComponent<Collider>();
        triggerZone.isTrigger = true; // this collider is sensor-only, not physical collision
    }

    void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        Vector3 direction = PushDirection;

        // distance from the wall's plane, measured along the push direction
        Vector3 offset = other.transform.position - transform.position;
        float distance = Vector3.Dot(offset, direction);

        // ignore anything behind the wall or outside the effect range
        if (distance <= 0f || distance > effectRange) return;

        float strength = forcePower;
        if (useFalloff)
            strength *= 1f - (distance / effectRange);

        rb.AddForce(direction * strength, ForceMode.Force);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 dir = PushDirection;
        Gizmos.DrawLine(transform.position, transform.position + dir * effectRange);
        Gizmos.DrawWireSphere(transform.position + dir * effectRange, 0.05f);
    }
}