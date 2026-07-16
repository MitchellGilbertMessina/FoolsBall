using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class BallMouseDrag : MonoBehaviour
{
    Rigidbody rb;
    Camera cam;
    Plane groundPlane;

    Vector3 target;


    void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        groundPlane = new Plane(Vector3.up, transform.position);
        target = transform.position;
    }

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
            target = ray.GetPoint(enter);
    }

    void FixedUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = target.x;
        pos.z = target.z;
        rb.MovePosition(pos);
    }
}
