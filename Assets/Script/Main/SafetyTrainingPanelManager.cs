using UnityEngine;

public class SafetyArticlePanelController : MonoBehaviour
{
    [Header("Select Panel")]
    public GameObject trainingSelectPanel;   // 작업 선택 패널

    [Header("Article Panels")]
    public GameObject hammerArticlePanel;    // 망치 기사 패널
    public GameObject drillArticlePanel;     // 드릴 기사 패널
    public GameObject grinderArticlePanel;   // 그라인더 기사 패널

    [Header("Select Sound")]
    public AudioSource audioSource;
    public AudioClip workSelectSound;        // 작업 선택 시 기사 뜨기 전 재생
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Debug")]
    public bool showDebugLog = true;

    private void Awake()
    {
        SetupAudioSource();
        CloseAllArticlePanels();
    }

    public void OpenHammerArticle()
    {
        PlaySound(workSelectSound);

        OpenArticlePanel(hammerArticlePanel);
        DebugLog("망치 작업 선택 후 기사 패널 열림");
    }

    public void OpenDrillArticle()
    {
        PlaySound(workSelectSound);

        OpenArticlePanel(drillArticlePanel);
        DebugLog("드릴 작업 선택 후 기사 패널 열림");
    }

    public void OpenGrinderArticle()
    {
        PlaySound(workSelectSound);

        OpenArticlePanel(grinderArticlePanel);
        DebugLog("그라인더 작업 선택 후 기사 패널 열림");
    }

    private void OpenArticlePanel(GameObject targetPanel)
    {
        if (trainingSelectPanel != null)
        {
            trainingSelectPanel.SetActive(false);
        }

        CloseAllArticlePanels();

        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }
    }

    public void CloseAllArticlePanels()
    {
        if (hammerArticlePanel != null)
        {
            hammerArticlePanel.SetActive(false);
        }

        if (drillArticlePanel != null)
        {
            drillArticlePanel.SetActive(false);
        }

        if (grinderArticlePanel != null)
        {
            grinderArticlePanel.SetActive(false);
        }
    }

    public void BackToTrainingSelectPanel()
    {
        CloseAllArticlePanels();

        if (trainingSelectPanel != null)
        {
            trainingSelectPanel.SetActive(true);
        }

        DebugLog("작업 선택 패널로 돌아감");
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // UI 사운드
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip, soundVolume);
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog) return;

        Debug.Log("[SafetyArticlePanelController] " + message, this);
    }
}