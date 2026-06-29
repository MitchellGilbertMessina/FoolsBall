using UnityEngine;

public class ExitManager : MonoBehaviour
{
    public float doublePressTime = 0.5f;
    private float lastEscTime = -10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.time - lastEscTime < doublePressTime)
            {
                Debug.Log("Exiting game...");
                Application.Quit();
            }
            else
            {
                lastEscTime = Time.time;
                Debug.Log("Press ESC again to quit");
            }
        }
    }
}