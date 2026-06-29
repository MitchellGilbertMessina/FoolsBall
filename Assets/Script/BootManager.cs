using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO.Ports;

public class BootManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown portDropdown;
    public Button startButton;
    public GameObject bootUI; // 👈 add this (your overlay panel)

    void Start()
    {
        string[] ports = SerialPort.GetPortNames();

        portDropdown.ClearOptions();
        portDropdown.AddOptions(new System.Collections.Generic.List<string>(ports));

        startButton.onClick.AddListener(StartGame);

        // Optional: auto-hide if no ports found
        if (ports.Length == 0)
        {
            Debug.LogWarning("No serial ports found.");
        }
    }

    void StartGame()
    {
        if (portDropdown.options.Count == 0)
        {
            Debug.LogWarning("No port selected.");
            return;
        }

        string selectedPort = portDropdown.options[portDropdown.value].text;

        SerialManager.Instance.SetPort(selectedPort);

        // 🔥 instead of loading scene, just hide UI
        //if (bootUI != null)
           // bootUI.SetActive(false);

        Debug.Log("Game started with port: " + selectedPort);
    }
}