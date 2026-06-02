using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class ToastMessageController : MonoBehaviour
{
    [Header("Normal Toast UI")]
    [SerializeField] private GameObject normalPanel;
    [SerializeField] private TextMeshProUGUI normalText;

    [Header("Fail Dialog UI")]
    [SerializeField] private GameObject failPanel;
    [SerializeField] private TextMeshProUGUI failTitleText;
    [SerializeField] private TextMeshProUGUI failBodyText;
    [SerializeField] private Button failCloseButton;

    [Header("Success Dialog UI")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private TextMeshProUGUI successTitleText;
    [SerializeField] private TextMeshProUGUI successBodyText;
    [SerializeField] private Button successCloseButton;

    [Header("Toast Settings")]
    [SerializeField] private float defaultShowTime = 3f;

    [Header("Fail Dialog Close Event")]
    [SerializeField] private UnityEvent onFailDialogClosed;

    private Coroutine normalToastRoutine;

    private void Awake()
    {
        SetPanelActive(normalPanel, false);
        SetPanelActive(failPanel, false);
        SetPanelActive(successPanel, false);

        if (failCloseButton != null)
        {
            failCloseButton.onClick.RemoveListener(CloseFailDialog);
            failCloseButton.onClick.AddListener(CloseFailDialog);
        }

        if (successCloseButton != null)
        {
            successCloseButton.onClick.RemoveListener(CloseSuccessDialog);
            successCloseButton.onClick.AddListener(CloseSuccessDialog);
        }
    }

    public void ShowNormalToast(string message)
    {
        ShowNormalToast(message, defaultShowTime);
    }

    public void ShowNormalToast(string message, float showTime)
    {
        if (normalPanel == null || normalText == null)
        {
            Debug.LogWarning("[ToastMessageController] Normal Panel 또는 Normal Text가 연결되지 않았습니다.");
            return;
        }

        if (normalToastRoutine != null)
        {
            StopCoroutine(normalToastRoutine);
        }

        normalToastRoutine = StartCoroutine(ShowNormalToastRoutine(message, showTime));
    }

    public void ShowFailToast(string message)
    {
        ShowFailToast("실패", message);
    }

    // 기존 코드 호환용
    // ShowFailToast("메시지", 3f) 형태 유지
    public void ShowFailToast(string message, float showTime)
    {
        Debug.Log("[ToastMessageController] ShowFailToast(message, float) 호출됨");
        Debug.Log("[ToastMessageController] message: " + message);
        Debug.Log("[ToastMessageController] failPanel 연결 여부: " + (failPanel != null));
        Debug.Log("[ToastMessageController] failTitleText 연결 여부: " + (failTitleText != null));
        Debug.Log("[ToastMessageController] failBodyText 연결 여부: " + (failBodyText != null));

        if (failPanel == null || failTitleText == null || failBodyText == null)
        {
            Debug.LogWarning("[ToastMessageController] Fail Dialog UI가 연결되지 않았습니다.");
            return;
        }

        HideDialogPanels();

        failTitleText.text = "실패";
        failBodyText.text = message;
        failPanel.SetActive(true);

        Debug.Log("[ToastMessageController] Fail Panel SetActive(true) 완료");
        Debug.Log("[ToastMessageController] failPanel.activeSelf: " + failPanel.activeSelf);
        Debug.Log("[ToastMessageController] failPanel.activeInHierarchy: " + failPanel.activeInHierarchy);
    }

    // 새 방식
    // ShowFailToast("제목", "내용")
    public void ShowFailToast(string title, string message)
    {
        if (failPanel == null || failTitleText == null || failBodyText == null)
        {
            Debug.LogWarning("[ToastMessageController] Fail Dialog UI가 연결되지 않았습니다.");
            return;
        }

        HideDialogPanels();

        failTitleText.text = title;
        failBodyText.text = message;
        failPanel.SetActive(true);
    }

    public void ShowSuccessToast(string message)
    {
        ShowSuccessToast("성공", message);
    }

    // 기존 코드 호환용
    // ShowSuccessToast("메시지", 3f) 형태 유지
    public void ShowSuccessToast(string message, float showTime)
    {
        ShowSuccessToast("성공", message);
    }

    // 새 방식
    // ShowSuccessToast("제목", "내용")
    public void ShowSuccessToast(string title, string message)
    {
        if (successPanel == null || successTitleText == null || successBodyText == null)
        {
            Debug.LogWarning("[ToastMessageController] Success Dialog UI가 연결되지 않았습니다.");
            return;
        }

        HideDialogPanels();

        successTitleText.text = title;
        successBodyText.text = message;
        successPanel.SetActive(true);
    }

    private IEnumerator ShowNormalToastRoutine(string message, float showTime)
    {
        normalText.text = message;
        normalPanel.SetActive(true);

        yield return new WaitForSeconds(showTime);

        normalPanel.SetActive(false);
        normalToastRoutine = null;
    }

    private void CloseFailDialog()
    {
        SetPanelActive(failPanel, false);

        if (onFailDialogClosed != null)
        {
            onFailDialogClosed.Invoke();
        }
    }

    private void CloseSuccessDialog()
    {
        SetPanelActive(successPanel, false);
    }

    private void HideDialogPanels()
    {
        SetPanelActive(failPanel, false);
        SetPanelActive(successPanel, false);
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}