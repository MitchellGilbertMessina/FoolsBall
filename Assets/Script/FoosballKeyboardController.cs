using UnityEngine;

public class FoosballKeyboardController : MonoBehaviour
{
    public Transform player1Rod;
    public Transform player2Rod;
    public GameObject groundPlaneObj;

    public float slideSpeed = 2f;
    public float rotationSpeed = 500f;

    public float slideMin = 0f;
    public float slideMax = 0.03f;

    float player1Slide;
    float player2Slide;

    float player1Rotation;
    float player2Rotation;

    Camera cam;

    Plane groundPlane;


	private void Start()
	{
        cam = Camera.main;
        groundPlane = new Plane(Vector3.up, new Vector3(0f, groundPlaneObj.transform.position.y, 0f));
    }

	void Update()
    {
              
        HandlePlayer1();
        HandlePlayer2();
    }

    void HandlePlayer1()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            float x = Mathf.Clamp(hit.x, slideMin, slideMax);

            player1Rod.position = new Vector3(x, player1Rod.transform.position.y, player1Rod.transform.position.z);
        }

        // Trackpad scroll
        float scroll = Input.mouseScrollDelta.y;
        player1Rotation += scroll * rotationSpeed * Time.deltaTime;

        player1Rod.localRotation = Quaternion.Euler(-player1Rotation, 0, 90);
    }

void HandlePlayer2()
{
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            float x = Mathf.Clamp(hit.x, slideMin, slideMax);

            player1Rod.position = new Vector3(x, player1Rod.transform.position.y, player1Rod.transform.position.z);
        }

        // Trackpad scroll
        float scroll = Input.mouseScrollDelta.y;
        player1Rotation += scroll * rotationSpeed * Time.deltaTime;

        player1Rod.localRotation = Quaternion.Euler(-player1Rotation, 0, 90);
    }

}