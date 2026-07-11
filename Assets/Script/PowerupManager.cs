using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerupManager : MonoBehaviour
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.Return;

    [Header("Goal References")]
    public Transform blueGoal;
    public Transform redGoal;
    public Transform blueWallTop;
    public Transform blueWallBottom;
    public Transform redWallTop;
    public Transform redWallBottom;

    [Header("Player References")]
    public FoosballController redController;
    public FoosballController blueController;
    public Transform redBody;
    public Transform blueBody;

    [Header("Move Goalposts Settings")]
    public float goalLerpSpeed = 8f;

    [Header("Seeing Double Settings")]
    [Tooltip("Local Y offset on rod for each figure (rod's Y maps to world X)")]
    public float figureSpread = 0.3f;
    public float figureLerpSpeed = 10f;

    // ---------------- STATE ----------------
    enum GoalSlot { Bottom, Middle, Top }
    GoalSlot currentGoalState = GoalSlot.Middle;
    bool seeingDoubleActive = false;

    // Cached slot positions (world X) per side
    float blueSlotBottom, blueSlotMiddle, blueSlotTop;
    float redSlotBottom, redSlotMiddle, redSlotTop;

    // Wall parent for local conversion
    Transform wallParent;

    void Start()
    {
        // Goals are root objects — world X is straightforward
        blueSlotMiddle = blueGoal.position.x;
        redSlotMiddle = redGoal.position.x;

        // Walls are children of Wall parent — get world X
        wallParent = blueWallTop.parent;

        blueSlotTop = blueWallTop.position.x;
        blueSlotBottom = blueWallBottom.position.x;

        redSlotTop = redWallTop.position.x;
        redSlotBottom = redWallBottom.position.x;
    }

    void Update()
    {
        if (Input.GetKeyDown(activateKey))
            ActivateRandomPowerup();
    }

    // ---------------- ACTIVATION ----------------

    void ActivateRandomPowerup()
    {
        List<System.Action> available = new List<System.Action>();

        available.Add(MoveGoalposts);

        if (!seeingDoubleActive)
            available.Add(SeeingDouble);

        int index = Random.Range(0, available.Count);
        available[index].Invoke();
    }

    // ================================================================
    //  MOVE GOALPOSTS
    // ================================================================

    void MoveGoalposts()
    {
        List<GoalSlot> options = new List<GoalSlot>();

        if (currentGoalState != GoalSlot.Bottom) options.Add(GoalSlot.Bottom);
        if (currentGoalState != GoalSlot.Middle) options.Add(GoalSlot.Middle);
        if (currentGoalState != GoalSlot.Top) options.Add(GoalSlot.Top);

        GoalSlot newSlot = options[Random.Range(0, options.Count)];
        GoalSlot oldSlot = currentGoalState;
        currentGoalState = newSlot;

        Debug.Log($"Powerup: Move Goalposts → Blue {newSlot}, Red {InvertSlot(newSlot)}");

        // Blue side: goal goes to newSlot, displaced wall goes to oldSlot
        float blueGoalTarget = GetBlueSlotX(newSlot);
        float blueWallTarget = GetBlueSlotX(oldSlot);
        Transform blueDisplacedWall = GetBlueWallAtSlot(newSlot);

        StartCoroutine(LerpGoalX(blueGoal, blueGoalTarget));
        StartCoroutine(LerpWallX(blueDisplacedWall, blueWallTarget));

        // Red side: inverted slot
        GoalSlot redNewSlot = InvertSlot(newSlot);
        GoalSlot redOldSlot = InvertSlot(oldSlot);

        float redGoalTarget = GetRedSlotX(redNewSlot);
        float redWallTarget = GetRedSlotX(redOldSlot);
        Transform redDisplacedWall = GetRedWallAtSlot(redNewSlot);

        StartCoroutine(LerpGoalX(redGoal, redGoalTarget));
        StartCoroutine(LerpWallX(redDisplacedWall, redWallTarget));
    }

    GoalSlot InvertSlot(GoalSlot slot)
    {
        switch (slot)
        {
            case GoalSlot.Bottom: return GoalSlot.Top;
            case GoalSlot.Top: return GoalSlot.Bottom;
            default: return GoalSlot.Middle;
        }
    }

    float GetBlueSlotX(GoalSlot slot)
    {
        switch (slot)
        {
            case GoalSlot.Bottom: return blueSlotBottom;
            case GoalSlot.Top: return blueSlotTop;
            default: return blueSlotMiddle;
        }
    }

    float GetRedSlotX(GoalSlot slot)
    {
        switch (slot)
        {
            case GoalSlot.Bottom: return redSlotBottom;
            case GoalSlot.Top: return redSlotTop;
            default: return redSlotMiddle;
        }
    }

    Transform GetBlueWallAtSlot(GoalSlot slot)
    {
        // Which wall is currently at the target slot?
        float targetX = GetBlueSlotX(slot);
        float distTop = Mathf.Abs(blueWallTop.position.x - targetX);
        float distBottom = Mathf.Abs(blueWallBottom.position.x - targetX);
        return distTop < distBottom ? blueWallTop : blueWallBottom;
    }

    Transform GetRedWallAtSlot(GoalSlot slot)
    {
        float targetX = GetRedSlotX(slot);
        float distTop = Mathf.Abs(redWallTop.position.x - targetX);
        float distBottom = Mathf.Abs(redWallBottom.position.x - targetX);
        return distTop < distBottom ? redWallTop : redWallBottom;
    }

    IEnumerator LerpGoalX(Transform goal, float targetX)
    {
        while (Mathf.Abs(goal.position.x - targetX) > 0.001f)
        {
            Vector3 pos = goal.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, goalLerpSpeed * Time.deltaTime);
            goal.position = pos;
            yield return null;
        }

        Vector3 final = goal.position;
        final.x = targetX;
        goal.position = final;
    }

    IEnumerator LerpWallX(Transform wall, float targetWorldX)
    {
        // Walls are children of wallParent — convert world X to local X
        while (Mathf.Abs(wall.position.x - targetWorldX) > 0.001f)
        {
            Vector3 worldPos = wall.position;
            worldPos.x = Mathf.MoveTowards(worldPos.x, targetWorldX, goalLerpSpeed * Time.deltaTime);
            wall.position = worldPos;
            yield return null;
        }

        Vector3 finalWorld = wall.position;
        finalWorld.x = targetWorldX;
        wall.position = finalWorld;
    }

    // ================================================================
    //  SEEING DOUBLE
    // ================================================================

    void SeeingDouble()
    {
        seeingDoubleActive = true;

        Debug.Log("Powerup: Seeing Double");

        SpawnSecondFigure(redBody, redController);
        SpawnSecondFigure(blueBody, blueController);
    }

    void SpawnSecondFigure(Transform body, FoosballController controller)
    {
        // Clone the body
        Transform clone = Instantiate(body, body.parent);
        clone.localPosition = body.localPosition;
        clone.localRotation = body.localRotation;
        clone.localScale = body.localScale;

        // Offset both along rod's local Y (maps to world X / sliding axis)
        float currentY = body.localPosition.y;

        Vector3 pos1 = body.localPosition;
        pos1.y = currentY + figureSpread;

        Vector3 pos2 = clone.localPosition;
        pos2.y = currentY - figureSpread;

        StartCoroutine(LerpLocalY(body, pos1.y));
        StartCoroutine(LerpLocalY(clone, pos2.y));

        // Reduce slide range to keep figures in bounds
        // Rod's local Y scale maps to world X, so world offset = figureSpread * rod.localScale.y
        float worldOffset = figureSpread * body.parent.localScale.y;
        controller.slideRange -= worldOffset;
    }

    IEnumerator LerpLocalY(Transform target, float targetY)
    {
        while (Mathf.Abs(target.localPosition.y - targetY) > 0.001f)
        {
            Vector3 pos = target.localPosition;
            pos.y = Mathf.MoveTowards(pos.y, targetY, figureLerpSpeed * Time.deltaTime);
            target.localPosition = pos;
            yield return null;
        }

        Vector3 final = target.localPosition;
        final.y = targetY;
        target.localPosition = final;
    }
}