using UnityEngine;
using UnityEngine.UI;

public class GuideUIController : MonoBehaviour
{
    [Header("Guide UI")]
    [SerializeField] private GameObject guidePanel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    private void Start()
    {
        ShowGuideUI();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGuideUI);
        }
        else
        {
            Debug.LogWarning("[GuideUIController] Close Button이 연결되지 않았습니다.");
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGuideUI);
        }
    }

    private void ShowGuideUI()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[GuideUIController] Guide Panel이 연결되지 않았습니다.");
        }
    }

    private void CloseGuideUI()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
            Debug.Log("[GuideUIController] 안내 UI 닫힘");
        }
    }
}