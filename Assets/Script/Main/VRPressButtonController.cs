using UnityEngine;

public class VRPressButtonController : MonoBehaviour
{
    [Header("Button Color")]
    public Color normalColor = Color.red;
    public Color hoverColor = Color.yellow;
    public Color pressedColor = Color.green;

    [Header("Target Objects")]
    public GameObject screenObject;      // 켜질 TV 화면 Plane
    public GameObject modeSelectPanel;   // 뜰 모드 선택 패널
    public GameObject attentionEffect;   // 화살표/파티클 효과, 없으면 비워도 됨

    [Header("Press Settings")]
    public bool turnOffAfterPress = true;

    private Renderer buttonRenderer;
    private bool isPressed = false;

    private void Awake()
    {
        buttonRenderer = GetComponent<Renderer>();

        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = normalColor;
        }

        if (screenObject != null)
        {
            screenObject.SetActive(false);
        }

        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPressed) return;

        if (IsHandOrController(other))
        {
            ChangeButtonColor(hoverColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isPressed) return;

        if (IsHandOrController(other))
        {
            ChangeButtonColor(normalColor);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isPressed) return;

        if (IsHandOrController(other))
        {
            // 손이나 컨트롤러가 버튼에 닿으면 버튼이 눌린 것으로 처리
            PressButton();
        }
    }

    private void PressButton()
    {
        isPressed = true;

        ChangeButtonColor(pressedColor);

        if (screenObject != null)
        {
            screenObject.SetActive(true);
        }

        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(true);
        }

        if (attentionEffect != null)
        {
            attentionEffect.SetActive(false);
        }

        if (turnOffAfterPress)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private void ChangeButtonColor(Color color)
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = color;
        }
    }

    private bool IsHandOrController(Collider other)
    {
        return other.CompareTag("Hand") ||
               other.CompareTag("Controller") ||
               other.CompareTag("Player");
    }
}