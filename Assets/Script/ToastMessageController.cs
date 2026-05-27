using System.Collections;
using UnityEngine;
using TMPro;

public class ToastMessageController : MonoBehaviour
{
    [Header("Normal Toast UI")]
    [SerializeField] private GameObject normalPanel;
    [SerializeField] private TextMeshProUGUI normalText;

    [Header("Fail Toast UI")]
    [SerializeField] private GameObject failPanel;
    [SerializeField] private TextMeshProUGUI failText;

    [Header("Success Toast UI")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private TextMeshProUGUI successText;

    [Header("Toast Settings")]
    [SerializeField] private float defaultShowTime = 3f;

    private Coroutine normalToastRoutine;
    private Coroutine failToastRoutine;
    private Coroutine successToastRoutine;

    private void Awake()
    {
        SetPanelActive(normalPanel, false);
        SetPanelActive(failPanel, false);
        SetPanelActive(successPanel, false);
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

        normalToastRoutine = StartCoroutine(
            ShowToastRoutine(normalPanel, normalText, message, showTime, ToastType.Normal)
        );
    }

    public void ShowFailToast(string message)
    {
        ShowFailToast(message, defaultShowTime);
    }

    public void ShowFailToast(string message, float showTime)
    {
        if (failPanel == null || failText == null)
        {
            Debug.LogWarning("[ToastMessageController] Fail Panel 또는 Fail Text가 연결되지 않았습니다.");
            return;
        }

        if (failToastRoutine != null)
        {
            StopCoroutine(failToastRoutine);
        }

        failToastRoutine = StartCoroutine(
            ShowToastRoutine(failPanel, failText, message, showTime, ToastType.Fail)
        );
    }

    public void ShowSuccessToast(string message)
    {
        ShowSuccessToast(message, defaultShowTime);
    }

    public void ShowSuccessToast(string message, float showTime)
    {
        if (successPanel == null || successText == null)
        {
            Debug.LogWarning("[ToastMessageController] Success Panel 또는 Success Text가 연결되지 않았습니다.");
            return;
        }

        if (successToastRoutine != null)
        {
            StopCoroutine(successToastRoutine);
        }

        successToastRoutine = StartCoroutine(
            ShowToastRoutine(successPanel, successText, message, showTime, ToastType.Success)
        );
    }

    private IEnumerator ShowToastRoutine(
        GameObject targetPanel,
        TextMeshProUGUI targetText,
        string message,
        float showTime,
        ToastType toastType
    )
    {
        targetText.text = message;
        targetPanel.SetActive(true);

        yield return new WaitForSeconds(showTime);

        targetPanel.SetActive(false);

        switch (toastType)
        {
            case ToastType.Normal:
                normalToastRoutine = null;
                break;

            case ToastType.Fail:
                failToastRoutine = null;
                break;

            case ToastType.Success:
                successToastRoutine = null;
                break;
        }
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private enum ToastType
    {
        Normal,
        Fail,
        Success
    }
}