using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public int scoringPlayer;

    void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Ball")) return;

    if (FoosballGameManager.Instance == null) return;

    FoosballGameManager.Instance.GoalScored(scoringPlayer, other.gameObject);
}
}