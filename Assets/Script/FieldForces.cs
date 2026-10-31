using UnityEngine;

/// <summary>
/// The "shape" of the table, done with forces instead of geometry:
///
///  - Edge slope: real foosball tables are raised along the sides so the ball
///    rolls back into reach of the figures. Within edgeBand of a side wall the
///    ball is pushed back toward the middle, harder the closer it gets.
///  - Corner slope: same idea at the four corners, pushing diagonally out.
///  - Stall nudge: a ball that sits nearly still for stallTime gets a small
///    random push toward the centre, so nothing stays parked out of reach.
///
/// Created automatically if it isn't in the scene. Add it to a GameObject
/// yourself to tweak the numbers; select it in Play to see the bands drawn.
/// Field axes: X runs along the rods (up/down the screen), Z runs goal to goal.
/// </summary>
public class FieldForces : MonoBehaviour
{
    [Header("Field bounds")]
    [Tooltip("Object whose collider/renderer marks the playing surface. Found by name 'Field' if empty.")]
    public Transform field;
    [Tooltip("Shrink the bounds by this much on each side (to account for wall thickness).")]
    public float inset = 0.0f;

    [Header("Edge slope (sides)")]
    [Tooltip("How far in from the side walls the slope reaches (metres).")]
    public float edgeBand = 0.06f;
    [Tooltip("Push at the wall itself (m/s²). ~1.7 is like a 10° slope.")]
    public float edgeAcceleration = 2f;

    [Header("Corner slope")]
    [Tooltip("How far from each end wall counts as the corner zone (metres).")]
    public float cornerBand = 0.08f;
    [Tooltip("Extra push out of corners, toward the table centre (m/s²).")]
    public float cornerAcceleration = 3f;

    [Header("Stall nudge")]
    [Tooltip("Below this speed (m/s) a ball counts as stalled.")]
    public float stallSpeed = 0.04f;
    [Tooltip("Seconds stalled before it gets nudged.")]
    public float stallTime = 2f;
    [Tooltip("Speed of the nudge (m/s).")]
    public float stallNudge = 0.4f;

    Bounds bounds;
    bool hasBounds;
    readonly System.Collections.Generic.Dictionary<BallBehaviour, float> stalledSince =
        new System.Collections.Generic.Dictionary<BallBehaviour, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (FindAnyObjectByType<FieldForces>() != null) return;
        new GameObject("FieldForces (auto)").AddComponent<FieldForces>();
    }

    void Start()
    {
        if (field == null)
        {
            var go = GameObject.Find("Field");
            if (go != null) field = go.transform;
        }

        if (field != null)
        {
            if (field.TryGetComponent(out Collider col)) { bounds = col.bounds; hasBounds = true; }
            else if (field.TryGetComponent(out Renderer rend)) { bounds = rend.bounds; hasBounds = true; }
        }

        if (!hasBounds)
            Debug.LogWarning("FieldForces: couldn't find the field's bounds; set 'field' in the Inspector.");
    }

    void FixedUpdate()
    {
        if (!hasBounds) return;

        float minX = bounds.min.x + inset, maxX = bounds.max.x - inset;
        float minZ = bounds.min.z + inset, maxZ = bounds.max.z - inset;
        Vector3 centre = bounds.center;

        foreach (var ball in BallBehaviour.Active)
        {
            if (ball == null || !ball.InPlay) continue;
            Rigidbody rb = ball.Body;
            if (rb == null || rb.isKinematic) continue;

            Vector3 p = rb.position;
            Vector3 push = Vector3.zero;

            // ---- side edges (along X) ----
            float dLow = p.x - minX, dHigh = maxX - p.x;
            if (dLow < edgeBand) push.x += edgeAcceleration * (1f - Mathf.Clamp01(dLow / edgeBand));
            if (dHigh < edgeBand) push.x -= edgeAcceleration * (1f - Mathf.Clamp01(dHigh / edgeBand));

            // ---- corners: near a side AND near an end ----
            bool nearSide = dLow < edgeBand || dHigh < edgeBand;
            bool nearEnd = (p.z - minZ) < cornerBand || (maxZ - p.z) < cornerBand;
            if (nearSide && nearEnd)
            {
                Vector3 toCentre = centre - p;
                toCentre.y = 0f;
                push += toCentre.normalized * cornerAcceleration;
            }

            if (push != Vector3.zero)
                rb.AddForce(push, ForceMode.Acceleration);

            // ---- stall nudge ----
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            if (v.magnitude < stallSpeed)
            {
                if (!stalledSince.TryGetValue(ball, out float since))
                {
                    stalledSince[ball] = Time.time;
                }
                else if (Time.time - since > stallTime)
                {
                    Vector3 dir = centre - p;
                    dir.y = 0f;
                    dir = (dir.normalized + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f))).normalized;
                    rb.AddForce(dir * stallNudge, ForceMode.VelocityChange);
                    stalledSince.Remove(ball);
                }
            }
            else
            {
                stalledSince.Remove(ball);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!hasBounds) return;
        float minX = bounds.min.x + inset, maxX = bounds.max.x - inset;
        float minZ = bounds.min.z + inset, maxZ = bounds.max.z - inset;
        float y = bounds.max.y + 0.005f;
        float lenZ = maxZ - minZ, lenX = maxX - minX;

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawCube(new Vector3(minX + edgeBand / 2f, y, (minZ + maxZ) / 2f), new Vector3(edgeBand, 0.001f, lenZ));
        Gizmos.DrawCube(new Vector3(maxX - edgeBand / 2f, y, (minZ + maxZ) / 2f), new Vector3(edgeBand, 0.001f, lenZ));

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawCube(new Vector3((minX + maxX) / 2f, y, minZ + cornerBand / 2f), new Vector3(lenX, 0.001f, cornerBand));
        Gizmos.DrawCube(new Vector3((minX + maxX) / 2f, y, maxZ - cornerBand / 2f), new Vector3(lenX, 0.001f, cornerBand));
    }
}
