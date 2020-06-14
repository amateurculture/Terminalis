using UnityEngine;
using UnityStandardAssets.Vehicles.Aeroplane;

public class AirDiagnosticsInputHandler : MonoBehaviour
{
    public GameObject diagnosticsPanel;
    public AeroplaneController aircraftController;

    private void Start()
    {
        diagnosticsPanel.SetActive(false);
        aircraftController = GetComponent<AeroplaneController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Fire3"))
        {
            diagnosticsPanel.SetActive(!diagnosticsPanel.activeSelf);

            if (!diagnosticsPanel.activeSelf)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1.0f;
            }
            else
            {
                Time.timeScale = 0.0f;
                // Cursor lock recovery handled in UnityInput.cs LateUpdate()
            }
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
