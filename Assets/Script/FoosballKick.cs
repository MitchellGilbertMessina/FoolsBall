using UnityEngine;

public class FoosballKick : MonoBehaviour
{
    public float kickForce = 0f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {

			Rigidbody ball = collision.gameObject.GetComponent<Rigidbody>();

			if (ball != null)
			{
				Vector3 kickDirection = transform.forward;
				ball.AddForce(kickDirection * kickForce, ForceMode.Impulse);
			}
		}
    }
}