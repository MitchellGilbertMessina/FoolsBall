using UnityEngine;
using System.Collections;

public class BallBehaviour : MonoBehaviour
{
    private Rigidbody rb;
    public float maxSpeed = 10f;

    [Header("Kick Damping")]
    [Tooltip("Fraction of a rod kick the ball keeps. 1 = full physics kick, 0.5 = half strength, 0 = no kick.")]
    [Range(0f, 1f)]
    public float kickStrength = 0.5f;

    private Vector3 velocityBeforeStep;
    private bool dampedThisStep;

	private void Start()
	{
        rb = GetComponent<Rigidbody>();

        // Sets the built-in PhysX max linear velocity clamp
        rb.maxLinearVelocity = maxSpeed;
    }

    // FixedUpdate runs before the physics step, so this is the ball's velocity going into the collision.
    void FixedUpdate()
    {
        if (rb == null)
            return;

        velocityBeforeStep = rb.linearVelocity;
        dampedThisStep = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        DampKick(collision);
    }

    // The foot keeps sweeping through the ball after first contact, so damp every step it stays in touch.
    void OnCollisionStay(Collision collision)
    {
        DampKick(collision);
    }

    void DampKick(Collision collision)
    {
        if (dampedThisStep || rb == null || rb.isKinematic)
            return;

        // Rods are the only kinematic bodies the ball can hit; walls and the field are static colliders.
        if (collision.rigidbody == null || !collision.rigidbody.isKinematic)
            return;

        // Keep only a fraction of the velocity the solver just added, so weak kicks stay weak.
        Vector3 kick = rb.linearVelocity - velocityBeforeStep;
        rb.linearVelocity = velocityBeforeStep + kick * kickStrength;

        dampedThisStep = true;
    }

	public void OnGoal()
    {
        StartCoroutine(FadeAndFreeze());
    }

    IEnumerator FadeAndFreeze()
{
    Rigidbody rb = GetComponent<Rigidbody>();

    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    Collider col = GetComponent<Collider>();
    if (col != null)
        col.enabled = false;

    Renderer r = GetComponent<Renderer>();
    if (r == null) yield break;

    Color start = r.material.color;

    float t = 0;

    while (t < 1f)
    {
        t += Time.deltaTime;
        r.material.color = Color.Lerp(start, Color.clear, t);
        yield return null;
    }

    Destroy(gameObject);
}
}