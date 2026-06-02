using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

/// <summary>
/// Toggles a world-space pause menu when the Vive menu button is pressed.
/// Attach to any persistent GameObject in the scene and wire up the serialized fields.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] XRInputValueReader<bool> m_MenuButtonInput = new XRInputValueReader<bool>("Menu Button");

    [Header("Menu")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] float menuDistance = 1.5f;

    [Header("Scene Names")]
    [SerializeField] string mainSceneName = "MainScene";

    [Header("Camera")]
    [SerializeField] Camera xrCamera;

    bool m_PrevPressed;
    bool m_IsOpen;

    void OnEnable() => m_MenuButtonInput?.EnableDirectActionIfModeUsed();
    void OnDisable() => m_MenuButtonInput?.DisableDirectActionIfModeUsed();

    void Start()
    {
        if (xrCamera == null)
            xrCamera = Camera.main;

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    void Update()
    {
        bool pressed = m_MenuButtonInput?.ReadValue() ?? false;

        if (pressed && !m_PrevPressed)
            ToggleMenu();

        m_PrevPressed = pressed;
    }

    void ToggleMenu()
    {
        m_IsOpen = !m_IsOpen;

        if (m_IsOpen && menuPanel != null && xrCamera != null)
            PlaceMenuInFrontOfCamera();

        menuPanel?.SetActive(m_IsOpen);
    }

    void PlaceMenuInFrontOfCamera()
    {
        Transform cam = xrCamera.transform;
        // Keep the menu level (no pitch), just face the player horizontally
        Vector3 forward = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
        if (forward == Vector3.zero)
            forward = Vector3.forward;

        menuPanel.transform.position = cam.position + forward * menuDistance;
        menuPanel.transform.rotation = Quaternion.LookRotation(forward);
    }

    // Call from the Respawn button's OnClick event
    public void Respawn()
    {
        m_IsOpen = false;
        menuPanel?.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Call from the Main Menu button's OnClick event
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}
