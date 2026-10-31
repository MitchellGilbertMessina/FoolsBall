using UnityEngine;
using System.Collections;

public class BallBehaviour : MonoBehaviour
{
    // Every ball currently in play, so table-wide effects (FieldForces) can find them
    static readonly System.Collections.Generic.List<BallBehaviour> active = new System.Collections.Generic.List<BallBehaviour>();
    public static System.Collections.Generic.IReadOnlyList<BallBehaviour> Active => active;

    public Rigidbody Body { get; private set; }
    public bool InPlay { get; private set; } = true;

    void Awake() => Body = GetComponent<Rigidbody>();
    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => active.Clear();

    public void OnGoal()
    {
        InPlay = false;
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