using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FoosballGameManager : MonoBehaviour
{
    public static FoosballGameManager Instance;

    public GameObject playerRed;
    public GameObject playerBlue;

    [Header("Ball Setup")]
    public GameObject ballPrefab;
    public Transform spawnLeft;
    public Transform spawnRight;

    [Header("Spawn Settings")]
    public float spawnForce = 5f;
    public Vector2 spawnAngleRange;

    [Header("Shake Settings")]
    public float shakeForce = 0.02f;
    public Transform leftWall;
    public Transform rightWall;

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

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBalls();
        }


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
            float angle = Random.Range(spawnAngleRange.x, spawnAngleRange.y);

            Vector3 finalDir = (Quaternion.AngleAxis(angle, Vector3.up) * direction).normalized;

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

    public void ResetBalls()
    {
        for (int i =0; i< activeBalls.Count; i++)
        {
            Destroy(activeBalls[i]);
        }

        bool left = UnityEngine.Random.value > 0.5f;

        if (left)
        {
            SpawnBall(spawnLeft, Vector3.right);
        }
        else 
        {
            SpawnBall(spawnRight, Vector3.left);
        }
    }

    public void ShakeBalls()
    {
        if (leftWall == null || rightWall == null)
        {
            Debug.LogWarning("[FoolsBall] SHAKE needs leftWall and rightWall assigned.");
            return;
        }

        float minX = Mathf.Min(leftWall.position.x, rightWall.position.x);
        float maxX = Mathf.Max(leftWall.position.x, rightWall.position.x);

        foreach (GameObject ball in activeBalls)
        {
            if (ball == null)
                continue;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            Transform player = NearestPlayer(ball.transform.position);
            if (player == null)
                continue;

            // Rods slide along X at a fixed Z, so a player's reachable line is z = player.z between the walls.
            Vector3 target = new Vector3(Random.Range(minX, maxX), ball.transform.position.y, player.position.z);

            Vector3 toTarget = target - ball.transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance < 0.001f)
                continue;

            // Under linear damping a ball coasts v0 / damping before stopping, so this lands it near the line.
            float damping = rb.linearDamping > 0f ? rb.linearDamping : 1f;
            rb.linearVelocity = (toTarget / distance) * distance * damping * shakeForce;
        }

        Debug.Log($"SHAKE → kicked {activeBalls.Count} ball(s)");
    }

    Transform NearestPlayer(Vector3 ballPosition)
    {
        Transform nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (GameObject player in new[] { playerRed, playerBlue })
        {
            if (player == null)
                continue;

            Vector3 offset = player.transform.position - ballPosition;
            offset.y = 0f;

            float sqr = offset.sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = player.transform;
            }
        }

        return nearest;
    }

}