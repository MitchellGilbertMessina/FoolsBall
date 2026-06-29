using UnityEngine;

public class ArduinoHandshake : MonoBehaviour
{
    [Header("Reconnect Settings")]
    public float timeoutSeconds = 2f;
    public float reconnectCooldown = 2f;

    private float lastDataTime;
    private float lastReconnectTime;

    void Start()
    {
        lastDataTime = Time.time;
    }

    void Update()
    {
        if (SerialManager.Instance == null) return;

        // ---------------------------
        // Track incoming data
        // ---------------------------
        if (SerialManager.Instance.HasNewData)
        {
            lastDataTime = Time.time;
        }

        // ---------------------------
        // Only trigger reconnect if truly disconnected
        // ---------------------------
        bool timedOut = (Time.time - lastDataTime) > timeoutSeconds;

        if (timedOut)
        {
            TryReconnect();
        }
    }

    void TryReconnect()
    {
        // cooldown prevents spam
        if (Time.time - lastReconnectTime < reconnectCooldown)
            return;

        // already connected → do nothing
        if (SerialManager.Instance != null && SerialManager.Instance.IsConnected)
            return;

        lastReconnectTime = Time.time;

        Debug.Log("Attempting Arduino reconnect...");

        try
        {
            SerialManager.Instance.ClosePort();
            SerialManager.Instance.OpenPort();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Reconnect failed: " + e.Message);
        }
    }
}