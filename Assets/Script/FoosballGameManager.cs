using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FoosballGameManager : MonoBehaviour
{
    public static FoosballGameManager Instance;

    [Header("Ball Setup")]
    public GameObject ballPrefab;
    public Transform spawnLeft;
    public Transform spawnRight;

    [Header("Spawn Settings")]
    public float spawnForce = 5f;
    public float randomness = 0.3f;

    private List<GameObject> activeBalls = new List<GameObject>();

    private int player1Score = 0;
    private int player2Score = 0;

    [Header("UI")]
public TMP_Text player1Text;
public TMP_Text player2Text;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Rod buttons (Arduino) or click/space (mouse) — RodInputHub handles which.
        if (RodInputHub.GetButtonDown(FoosTeam.Red))
            SpawnBall(spawnLeft, Vector3.right);

        if (RodInputHub.GetButtonDown(FoosTeam.Blue))
            SpawnBall(spawnRight, Vector3.left);

        // ---- cleanup null references ----
        activeBalls.RemoveAll(ball => ball == null);
    }

    // ---------------- SPAWN ----------------

    public void SpawnBall(Transform spawnPoint, Vector3 direction)
    {
        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
        activeBalls.Add(ball);

        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomness, randomness),
                0,
                Random.Range(-randomness, randomness)
            );

            Vector3 finalDir = (direction + randomOffset).normalized;

            rb.linearVelocity = finalDir * spawnForce;
        }
    }

    // ---------------- GOAL ----------------

   public void GoalScored(int scoringPlayer, GameObject ball)
{
    if (ball != null)
    {
        BallBehaviour bb = ball.GetComponent<BallBehaviour>();
        if (bb != null)
            bb.OnGoal();
    }

    activeBalls.Remove(ball);

    if (scoringPlayer == 1)
        player1Score++;
    else
        player2Score++;

    UpdateScoreUI();

    Debug.Log($"Score → P1: {player1Score} | P2: {player2Score}");
}
    // ---------------- RESET ----------------

    public void ResetGame()
    {
        foreach (GameObject ball in activeBalls)
        {
            if (ball != null)
                Destroy(ball);
        }

        activeBalls.Clear();

        Debug.Log("GAME RESET → all balls cleared");
    }

    void UpdateScoreUI()
{
    if (player1Text != null)
        player1Text.text = player1Score + " :Red" ;

    if (player2Text != null)
        player2Text.text = "Blue: " + player2Score;
}
}