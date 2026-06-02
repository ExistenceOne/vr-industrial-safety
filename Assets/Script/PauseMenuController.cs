using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] XRInputValueReader<float> m_MenuButtonInput = new XRInputValueReader<float>("Menu");

    [Header("Menu")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] float menuDistance = 1.5f;

    [Header("Scene Names")]
    [SerializeField] string mainSceneName = "MainScene";

    [Header("Camera")]
    [SerializeField] Camera xrCamera;

    [Header("Auto Respawn")]
    [SerializeField] float fallThreshold = -10f;

    bool m_PrevPressed;
    bool m_IsOpen;

    void OnEnable() => m_MenuButtonInput?.EnableDirectActionIfModeUsed();
    void OnDisable() => m_MenuButtonInput?.DisableDirectActionIfModeUsed();

    void Start()
    {
        if (xrCamera == null)
            xrCamera = Camera.main;

        if (menuPanel != null && xrCamera != null)
        {
            // Parent the panel to the camera so it always follows the player's view
            menuPanel.transform.SetParent(xrCamera.transform, false);
            menuPanel.transform.localPosition = new Vector3(0f, 0f, menuDistance);
            menuPanel.transform.localRotation = Quaternion.identity;
            menuPanel.SetActive(false);
        }
    }

    void Update()
    {
        bool pressed = (m_MenuButtonInput?.ReadValue() ?? 0f) >= 1f;

        if (pressed && !m_PrevPressed)
            ToggleMenu();

        m_PrevPressed = pressed;

        if (xrCamera != null && xrCamera.transform.position.y < fallThreshold)
            Respawn();
    }

    void ToggleMenu()
    {
        m_IsOpen = !m_IsOpen;
        menuPanel?.SetActive(m_IsOpen);
    }

    public void Respawn()
    {
        m_IsOpen = false;
        menuPanel?.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}
