using UnityEngine;
using System.IO.Ports;
using System.Globalization;
using System.Threading;

public class SerialManager : MonoBehaviour
{
    public static SerialManager Instance { get; private set; }

    [Header("Connection Settings")]
    public int baudRate = 230400;
    public int minExpectedValues = 5;
    public int maxExpectedValues = 10;

    private SerialPort serial;
    private Thread serialThread;
    private volatile bool keepReading = true;

    private readonly object dataLock = new object();
    private float[] parsedValues = new float[6];

    // ---------------- STATE ----------------
    public bool IsConnected { get; private set; }
    public bool HasNewData { get; private set; }
    public string ConnectedPort { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        serialThread = new Thread(SerialLoop);
        serialThread.IsBackground = true;
        serialThread.Start();
    }

    // ---------------- PUBLIC API ----------------

    public float GetValue(int index)
    {
        lock (dataLock)
        {
            if (index < 0 || index >= parsedValues.Length)
                return 0f;

            return parsedValues[index];
        }
    }

    // ---------------- SCANNING ----------------

    string ScanForArduino()
    {
        string[] ports = SerialPort.GetPortNames();

        foreach (string port in ports)
        {
            if (TryHandshake(port))
                return port;
        }

        return null;
    }

    bool TryHandshake(string port)
    {
        SerialPort probe = null;

        try
        {
            probe = new SerialPort(port, baudRate)
            {
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = 1000
            };

            probe.Open();

            // Discard partial/startup lines
            try { probe.ReadLine(); } catch (System.TimeoutException) { return false; }

            // Validate a real data line
            string line = probe.ReadLine().Trim();
            if (IsValidDataLine(line))
            {
                // This is our Arduino — adopt the connection
                probe.ReadTimeout = 50;
                serial = probe;
                return true;
            }
        }
        catch (System.Exception) { }

        // Not the right port — clean up
        try
        {
            if (probe != null && probe.IsOpen)
                probe.Close();

            probe?.Dispose();
        }
        catch { }

        return false;
    }

    bool IsValidDataLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return false;

        string[] parts = line.Split(',');
        if (parts.Length < minExpectedValues || parts.Length > maxExpectedValues) return false;

        foreach (string part in parts)
        {
            if (!float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return false;
        }

        return true;
    }

    // ---------------- THREAD ----------------

    void SerialLoop()
    {
        while (keepReading)
        {
            if (serial == null || !serial.IsOpen)
            {
                IsConnected = false;
                ConnectedPort = null;

                string found = ScanForArduino();

                if (found != null)
                {
                    ConnectedPort = found;
                    IsConnected = true;
                    Debug.Log("Arduino found on: " + found);
                }
                else
                {
                    // Wait before rescanning to avoid busy-loop
                    Thread.Sleep(1000);
                    continue;
                }
            }

            // ---------------- READ LOOP ----------------
            try
            {
                string line = serial.ReadLine();
                ParseLine(line);
                HasNewData = true;
            }
            catch (System.TimeoutException) { }
            catch (System.Exception)
            {
                // Connection lost — close and let the outer loop rescan
                ClosePort();
            }
        }
    }

    // ---------------- PARSING ----------------

    void ParseLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        line = line.Trim();

        // ---------------- COMMANDS ----------------
        if (line == "GAME_RESET")
        {
            FoosballGameManager.Instance?.ResetGame();
            return;
        }

        if (line == "ARDUINO_RESET")
        {
            Debug.Log("Arduino reboot detected");
            return;
        }

        // ---------------- DATA ----------------
        string[] parts = line.Split(',');

        lock (dataLock)
        {
            for (int i = 0; i < parsedValues.Length && i < parts.Length; i++)
            {
                if (float.TryParse(parts[i],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float val))
                {
                    parsedValues[i] = val;
                }
            }
        }
    }

    // ---------------- UPDATE ----------------

    void Update()
    {
        HasNewData = false;
    }

    // ---------------- CLEANUP ----------------

    void ClosePort()
    {
        try
        {
            if (serial != null)
            {
                serial.DtrEnable = false;
                serial.RtsEnable = false;

                if (serial.IsOpen)
                    serial.Close();

                serial.Dispose();
            }
        }
        catch { }

        serial = null;
        IsConnected = false;
        ConnectedPort = null;
    }

    void OnDestroy()
    {
        keepReading = false;

        if (serialThread != null && serialThread.IsAlive)
            serialThread.Join(2000);

        ClosePort();
    }
}