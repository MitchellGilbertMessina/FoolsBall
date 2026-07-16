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

    [Header("Shake Settings")]
    public float shakeForce = 0.02f;

    private List<GameObject> activeBalls = new List<GameObject>();

    private int player1Score = 0;
    private int player2Score = 0;

    // --- edge detection (prevents spam) ---
    private bool prevB1 = false;
    private bool prevB2 = false;
    [Header("UI")]
public TMP_Text player1Text;
public TMP_Text player2Text;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (SerialManager.Instance == null) return;

        // ---- read buttons ----
        bool b1 = (SerialManager.Instance.GetValue(4) == 1f) || (Input.GetKey(KeyCode.T));
        bool b2 = SerialManager.Instance.GetValue(5) == 1f || (Input.GetKey(KeyCode.Y));

        // ---- EDGE TRIGGER SPAWNING ----
        if (b1 && !prevB1)
        {
            SpawnBall(spawnLeft, Vector3.right);
        }

        if (b2 && !prevB2)
        {
            SpawnBall(spawnRight, Vector3.left);
        }

        prevB1 = b1;
        prevB2 = b2;

        // ---- cleanup null references ----
        activeBalls.RemoveAll(ball => ball == null);

        if (Input.GetKeyDown(KeyCode.U))
        {
            ShakeBalls();
        }

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

    public void ShakeBalls()
    {
        foreach (GameObject ball in activeBalls)
        {
            if (ball == null)
                continue;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            Vector2 dir = Random.insideUnitCircle.normalized;
            rb.AddForce(new Vector3(dir.x, 0f, dir.y) * shakeForce, ForceMode.Impulse);
        }

        Debug.Log($"SHAKE → nudged {activeBalls.Count} ball(s)");
    }

}