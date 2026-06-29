using UnityEngine;
using System.Collections;

public class BallBehaviour : MonoBehaviour
{
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