using UnityEngine;
using TMPro;

public class ConnectionIndicator : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text statusText;

    void Update()
    {
        if (SerialManager.Instance == null) return;

        if (SerialManager.Instance.IsConnected)
        {
            statusText.text = "Connected: " + SerialManager.Instance.ConnectedPort;
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "Scanning for Arduino...";
            statusText.color = Color.yellow;
        }
    }
}