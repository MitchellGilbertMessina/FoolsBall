using UnityEngine;

public class FoosballKeyboardController : MonoBehaviour
{
    public Transform player1Rod;
    public Transform player2Rod;

    public float slideSpeed = 2f;
    public float rotationSpeed = 500f;

    public float slideRange = 0.03f;

    float player1Slide;
    float player2Slide;

    float player1Rotation;
    float player2Rotation;

    void Update()
    {
                
        HandlePlayer1();
        HandlePlayer2();

        UpdateRod(player1Rod, player1Slide, player1Rotation);
        UpdateRod(player2Rod, player2Slide, player2Rotation);
    }

    void HandlePlayer1()
    {
        // A / D movement
        float move = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            move = -1f;
        if (Input.GetKey(KeyCode.RightArrow))
            move = 1f;

        player1Slide += move * slideSpeed * Time.deltaTime;
        player1Slide = Mathf.Clamp(player1Slide, -slideRange, slideRange);

        // Trackpad scroll
        float scroll = Input.mouseScrollDelta.y;
        player1Rotation += scroll * rotationSpeed * Time.deltaTime;
    }

void HandlePlayer2()
{
    // Arrow keys slide
    float move = 0f;

    if (Input.GetKey(KeyCode.A))
        move = -1f;
    if (Input.GetKey(KeyCode.D))
        move = 1f;

    player2Slide += move * slideSpeed * Time.deltaTime;
    player2Slide = Mathf.Clamp(player2Slide, -slideRange, slideRange);

    // W / S rotate
    float spin = 0f;

    if (Input.GetKey(KeyCode.W))
        spin = 1f;
    if (Input.GetKey(KeyCode.S))
        spin = -1f;

    player2Rotation += spin * rotationSpeed * Time.deltaTime;
}

    void UpdateRod(Transform rod, float slide, float rotation)
    {
        if (rod == null) return;

        Vector3 pos = rod.localPosition;
        pos.x = slide;
        rod.localPosition = pos;

        rod.localRotation = Quaternion.Euler(-rotation, 0, 90);
    }
}