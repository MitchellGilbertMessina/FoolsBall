using UnityEngine;
using System.IO.Ports;
using System.Globalization;
using System.Threading;

public class SerialManager : MonoBehaviour
{
    public static SerialManager Instance { get; private set; }

    [Header("Port Settings")]
    [Tooltip("Leave empty if using Boot Scene selector")]
    public string portName;

    public int baudRate = 230400;

    private SerialPort serial;
    private Thread serialThread;
    private bool keepReading = true;

    private readonly object dataLock = new object();
    private float[] parsedValues = new float[6];

    // ---------------- STATE ----------------
    public bool IsConnected { get; private set; }
    public bool HasNewData { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ---------------- AUTO PORT LOAD ----------------
        if (string.IsNullOrEmpty(portName))
        {
            portName = PlayerPrefs.GetString("lastPort", "");
        }

        OpenPort();

        serialThread = new Thread(ReadSerialThread);
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

    public void SetPort(string newPort)
    {
        portName = newPort;
        PlayerPrefs.SetString("lastPort", portName);
        PlayerPrefs.Save();

        ClosePort();
        OpenPort();
    }

    // ---------------- CONNECTION ----------------

    public void OpenPort()
    {
        try
        {
            if (string.IsNullOrEmpty(portName))
            {
                Debug.LogWarning("No serial port selected.");
                return;
            }

            if (serial != null && serial.IsOpen)
                serial.Close();

            serial = new SerialPort(portName, baudRate)
            {
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = 50
            };

            serial.Open();

            IsConnected = true;
            Debug.Log("Serial opened / connected on: " + portName);
        }
        catch (System.Exception e)
        {
            IsConnected = false;
            Debug.LogWarning("Serial open failed: " + e.Message);
        }
    }

    public void ClosePort()
    {
        try
        {
            if (serial != null)
            {
                serial.DtrEnable = false;
                serial.RtsEnable = false;

                if (serial.IsOpen)
                    serial.Close();
            }
        }
        catch { }

        IsConnected = false;
    }

    // ---------------- THREAD ----------------

    void ReadSerialThread()
    {
        while (keepReading)
        {
            try
            {
                if (serial != null && serial.IsOpen)
                {
                    string line = serial.ReadLine();
                    ParseLine(line);

                    IsConnected = true;
                    HasNewData = true;
                }
                else
                {
                    IsConnected = false;
                }
            }
            catch (System.TimeoutException) { }
            catch (System.Exception)
            {
                IsConnected = false;
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

    void OnDestroy()
    {
        keepReading = false;

        if (serialThread != null && serialThread.IsAlive)
            serialThread.Join();

        ClosePort();
    }
}