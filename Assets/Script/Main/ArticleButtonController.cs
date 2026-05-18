using UnityEngine;
using UnityEngine.SceneManagement;

public class ArticleButtonController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject trainingSelectPanel;   // 작업 선택 패널
    public GameObject articlePanel;          // 현재 기사 패널

    [Header("Practice Scene")]
    public string practiceSceneName;         // 이동할 실습 씬 이름

    [Header("Debug")]
    public bool showDebugLog = true;

    // 기사 닫기 버튼에 연결
    public void CloseArticle()
    {
        if (articlePanel != null)
        {
            articlePanel.SetActive(false);
            DebugLog("기사 패널 닫힘: " + articlePanel.name);
        }

        if (trainingSelectPanel != null)
        {
            trainingSelectPanel.SetActive(true);
            DebugLog("작업 선택 패널 활성화됨: " + trainingSelectPanel.name);
        }
    }

    // 실습으로 이동 버튼에 연결
    public void MoveToPracticeScene()
    {
        if (string.IsNullOrEmpty(practiceSceneName))
        {
            Debug.LogWarning("[ArticleButtonController] practiceSceneName이 비어 있습니다.", this);
            return;
        }

        DebugLog("실습 씬으로 이동: " + practiceSceneName);
        SceneManager.LoadScene(practiceSceneName);
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog) return;

        Debug.Log("[ArticleButtonController] " + message, this);
    }
}