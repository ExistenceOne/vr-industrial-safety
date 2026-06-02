using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SafetyPracticeManager : MonoBehaviour
{
    public static SafetyPracticeManager Instance { get; private set; }

    private enum PracticeType
    {
        None,
        Hammer,
        Drill,
        Grinder
    }

    [Header("Practice Type")]
    [SerializeField] private PracticeType practiceType = PracticeType.None;

    [Header("Safety Gear State")]
    [SerializeField] private bool isGloveEquipped = false;

    [Header("UI")]
    [SerializeField] private ToastMessageController toastController;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Quest UI")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questProgressText;

    [Header("Quest Complete Success Panel - Hammer / Grinder Only")]
    [SerializeField] private bool showSuccessPanelOnQuestComplete = true;
    [SerializeField] private float delayBeforeQuestSuccessPanel = 1f;

    [Tooltip("성공 패널을 닫기 버튼으로 닫을 예정이면 크게 유지합니다.")]
    [SerializeField] private float questSuccessPanelShowTime = 9999f;

    [SerializeField, TextArea(4, 12)]
    private string hammerQuestCompleteMessage =
        "망치 실습 완료!\n\n" +
        "못 박기 작업을 성공적으로 완료했습니다.\n\n" +
        "작업 중에는 항상 손의 위치를 확인하고,\n" +
        "공구를 정확한 방향으로 사용해야 합니다.";

    [SerializeField, TextArea(4, 12)]
    private string grinderQuestCompleteMessage =
        "그라인더 실습 완료!\n\n" +
        "그라인더 작업을 안전하게 완료했습니다.\n\n" +
        "작업 전 보호구 착용, 안전거리 확보,\n" +
        "절단 방향 확인을 반드시 지켜야 합니다.";

    [Header("Quest Complete Scene Load")]
    [SerializeField] private bool returnToMainSceneOnQuestComplete = true;
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private float delayBeforeReturnToMainScene = 5f;

    [Header("Hammer Quest")]
    [SerializeField] private string hammerQuestTitle = "메인 퀘스트";
    [SerializeField] private string hammerQuestContent = "못 박기";
    [SerializeField] private int hammerTargetCount = 5;

    [Header("Drill Quest")]
    [SerializeField] private string drillQuestTitle = "메인 퀘스트";
    [SerializeField] private string drillQuestContent = "드릴 실습";
    [SerializeField] private int drillTargetCount = 1;

    [Header("Grinder Quest")]
    [SerializeField] private string grinderQuestTitle = "메인 퀘스트";
    [SerializeField] private string grinderQuestContent = "그라인더 실습";
    [SerializeField] private int grinderTargetCount = 1;

    [Header("Hammer Guide")]
    [SerializeField] private string hammerGuideMessage = "보호구 착용 완료! 못을 정확히 타격하십시오.";
    [SerializeField] private float delayBeforeHammerGuideToast = 2.2f;

    [Header("Drill Guide")]
    [SerializeField] private string drillGuideMessage = "보호구 착용 완료! 드릴을 작동한 뒤 작업물을 천천히 접촉하십시오.";
    [SerializeField] private float delayBeforeDrillGuideToast = 2.2f;

    [Header("Grinder Guide")]
    [SerializeField] private string grinderGuideMessage = "보호구 착용 완료! 방호덮개를 확인하고 그라인더를 사용하십시오.";
    [SerializeField] private float delayBeforeGrinderGuideToast = 2.2f;

    [Header("Failure Settings")]
    [SerializeField, TextArea(8, 20)]
    private string noGloveFailMessage = "보호구 위반";

    [SerializeField] private float toastShowTime = 2.0f;
    [SerializeField] private float delayBeforeFade = 1.0f;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Failure Toast Sound")]
    [SerializeField] private AudioSource failToastAudioSource;
    [SerializeField] private AudioClip failToastSoundClip;

    [Header("Failure Fade BGM")]
    [SerializeField] private AudioSource failFadeBgmAudioSource;
    [SerializeField] private AudioClip failFadeBgmClip;
    [SerializeField] private bool stopFadeBgmBeforeReload = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private bool isFailing = false;

    private Coroutine guideRoutine;
    private Coroutine returnToMainSceneRoutine;
    private Coroutine questSuccessRoutine;

    private bool hasQuestSuccessStarted = false;

    private int currentQuestCount = 0;
    private int targetQuestCount = 1;
    private string currentQuestTitle = "메인 퀘스트";
    private string currentQuestContent = "실습 진행";

    private float liveQuestProgressPercent = 0f;

    public bool IsGloveEquipped => isGloveEquipped;
    public bool IsFailing => isFailing;

    private void Awake()
    {
        Instance = this;

        SetupQuest();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        if (failFadeBgmAudioSource != null)
        {
            failFadeBgmAudioSource.playOnAwake = false;
            failFadeBgmAudioSource.loop = true;

            if (failFadeBgmClip != null)
            {
                failFadeBgmAudioSource.clip = failFadeBgmClip;
            }
        }

        if (failToastAudioSource != null)
        {
            failToastAudioSource.playOnAwake = false;
            failToastAudioSource.loop = false;
        }
    }

    private void Start()
    {
        UpdateQuestUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SetupQuest()
    {
        switch (practiceType)
        {
            case PracticeType.Hammer:
                currentQuestTitle = hammerQuestTitle;
                currentQuestContent = hammerQuestContent;
                targetQuestCount = hammerTargetCount;
                break;

            case PracticeType.Drill:
                currentQuestTitle = drillQuestTitle;
                currentQuestContent = drillQuestContent;
                targetQuestCount = drillTargetCount;
                break;

            case PracticeType.Grinder:
                currentQuestTitle = grinderQuestTitle;
                currentQuestContent = grinderQuestContent;
                targetQuestCount = grinderTargetCount;
                break;

            default:
                currentQuestTitle = "메인 퀘스트";
                currentQuestContent = "실습 진행";
                targetQuestCount = 1;
                break;
        }

        if (targetQuestCount <= 0)
        {
            targetQuestCount = 1;
        }
    }

    public void AddQuestProgress()
    {
        if (isFailing)
            return;

        if (IsQuestComplete())
            return;

        currentQuestCount++;

        if (currentQuestCount > targetQuestCount)
        {
            currentQuestCount = targetQuestCount;
        }

        liveQuestProgressPercent = Mathf.Max(
            liveQuestProgressPercent,
            (float)currentQuestCount / targetQuestCount * 100f
        );

        UpdateQuestUI();

        if (showDebugLog)
        {
            Debug.Log($"[SafetyPracticeManager] 퀘스트 진행도: {currentQuestCount} / {targetQuestCount}");
        }

        HandleQuestComplete();
    }

    public void SetLiveQuestProgressByObject(float objectProgress01)
    {
        if (isFailing)
            return;

        if (targetQuestCount <= 0)
            return;

        objectProgress01 = Mathf.Clamp01(objectProgress01);

        float baseProgress = (float)currentQuestCount / targetQuestCount * 100f;
        float oneObjectProgress = objectProgress01 / targetQuestCount * 100f;

        liveQuestProgressPercent = Mathf.Clamp(baseProgress + oneObjectProgress, 0f, 100f);

        // 드릴은 DrillDepthQuest에서 100% 이후 1초 판정을 직접 처리함.
        // 따라서 여기서 currentQuestCount를 올리거나 완료 처리하면 안 됨.
        if (practiceType != PracticeType.Drill && liveQuestProgressPercent >= 100f)
        {
            currentQuestCount = targetQuestCount;
            liveQuestProgressPercent = 100f;
            UpdateQuestUI();
            HandleQuestComplete();
            return;
        }

        UpdateQuestUI();
    }

    public void AddHammerNailProgress()
    {
        if (practiceType != PracticeType.Hammer)
            return;

        AddQuestProgress();
    }

    public void SetQuestProgress(int count)
    {
        currentQuestCount = Mathf.Clamp(count, 0, targetQuestCount);

        liveQuestProgressPercent = Mathf.Max(
            liveQuestProgressPercent,
            (float)currentQuestCount / targetQuestCount * 100f
        );

        if (currentQuestCount >= targetQuestCount)
        {
            liveQuestProgressPercent = 100f;
        }

        UpdateQuestUI();

        HandleQuestComplete();
    }

    public bool IsQuestComplete()
    {
        return currentQuestCount >= targetQuestCount;
    }

    private void HandleQuestComplete()
    {
        if (!returnToMainSceneOnQuestComplete)
            return;

        if (isFailing)
            return;

        if (!IsQuestComplete())
            return;

        if (practiceType == PracticeType.Drill)
        {
            StartReturnToMainSceneRoutine();
            return;
        }

        if (practiceType == PracticeType.Hammer || practiceType == PracticeType.Grinder)
        {
            StartQuestSuccessPanelRoutine();
            return;
        }

        StartReturnToMainSceneRoutine();
    }

    private void StartQuestSuccessPanelRoutine()
    {
        if (hasQuestSuccessStarted)
            return;

        if (!showSuccessPanelOnQuestComplete)
        {
            StartReturnToMainSceneRoutine();
            return;
        }

        hasQuestSuccessStarted = true;

        currentQuestCount = targetQuestCount;
        liveQuestProgressPercent = 100f;
        UpdateQuestUI();

        if (questSuccessRoutine != null)
        {
            StopCoroutine(questSuccessRoutine);
        }

        questSuccessRoutine = StartCoroutine(QuestSuccessPanelRoutine());
    }

    private IEnumerator QuestSuccessPanelRoutine()
    {
        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 망치/그라인더 퀘스트 100% 달성. "
                      + delayBeforeQuestSuccessPanel.ToString("F1")
                      + "초 후 성공 패널 표시");
        }

        yield return new WaitForSeconds(delayBeforeQuestSuccessPanel);

        if (isFailing)
            yield break;

        string successMessage = GetQuestCompleteMessage();

        if (toastController != null)
        {
            toastController.ShowSuccessToast(successMessage, questSuccessPanelShowTime);

            if (showDebugLog)
            {
                Debug.Log("[SafetyPracticeManager] 퀘스트 완료 성공 패널 표시");
            }
        }
        else
        {
            Debug.LogWarning("[SafetyPracticeManager] toastController가 null입니다. 바로 메인 씬으로 이동합니다.");
            LoadMainScene();
        }

        questSuccessRoutine = null;
    }

    private string GetQuestCompleteMessage()
    {
        if (practiceType == PracticeType.Hammer)
            return hammerQuestCompleteMessage;

        if (practiceType == PracticeType.Grinder)
            return grinderQuestCompleteMessage;

        return "실습 완료!";
    }

    private void UpdateQuestUI()
    {
        int progressPercent = Mathf.RoundToInt(liveQuestProgressPercent);

        if (questTitleText != null)
        {
            questTitleText.text = currentQuestTitle;
        }

        if (questProgressText != null)
        {
            questProgressText.text = $"{currentQuestContent} {currentQuestCount} / {targetQuestCount}\n달성률 {progressPercent}%";
        }
    }

    public void EquipGlove()
    {
        if (isFailing)
            return;

        if (isGloveEquipped)
            return;

        isGloveEquipped = true;

        if (toastController != null)
        {
            toastController.ShowSuccessToast("보호구 착용 완료! 실습을 진행하십시오.", 2f);
        }

        if (guideRoutine != null)
        {
            StopCoroutine(guideRoutine);
        }

        guideRoutine = StartCoroutine(ShowGuideAfterGloveRoutine());

        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 보호장갑 착용 완료");
        }
    }

    private IEnumerator ShowGuideAfterGloveRoutine()
    {
        if (practiceType == PracticeType.Hammer)
        {
            yield return new WaitForSeconds(delayBeforeHammerGuideToast);

            if (toastController != null)
            {
                toastController.ShowNormalToast(hammerGuideMessage, toastShowTime);
            }

            if (showDebugLog)
            {
                Debug.Log("[SafetyPracticeManager] 망치 실습 안내 토스트 출력");
            }
        }
        else if (practiceType == PracticeType.Drill)
        {
            yield return new WaitForSeconds(delayBeforeDrillGuideToast);

            if (toastController != null)
            {
                toastController.ShowNormalToast(drillGuideMessage, toastShowTime);
            }

            if (showDebugLog)
            {
                Debug.Log("[SafetyPracticeManager] 드릴 실습 안내 토스트 출력");
            }
        }
        else if (practiceType == PracticeType.Grinder)
        {
            yield return new WaitForSeconds(delayBeforeGrinderGuideToast);

            if (toastController != null)
            {
                toastController.ShowNormalToast(grinderGuideMessage, toastShowTime);
            }

            if (showDebugLog)
            {
                Debug.Log("[SafetyPracticeManager] 그라인더 실습 안내 토스트 출력");
            }
        }

        guideRoutine = null;
    }

    public bool TryFailIfNoGlove()
    {
        if (isFailing)
            return true;

        if (isGloveEquipped)
            return false;

        StartCoroutine(FailRoutine());
        return true;
    }

    public void ShowPassMessage(string message, float showTime = 3f)
    {
        if (toastController != null)
        {
            toastController.ShowSuccessToast(message, showTime);
        }
    }

    public bool TryFailAlways(string overrideMessage = null)
    {
        if (isFailing)
            return true;

        StartCoroutine(FailRoutine(overrideMessage));
        return true;
    }

    private IEnumerator FailRoutine(string overrideMessage = null)
    {
        isFailing = true;

        string message = overrideMessage ?? noGloveFailMessage;

        Debug.Log("[SafetyPracticeManager] 1. FailRoutine 시작");
        Debug.Log("[SafetyPracticeManager] 실패 메시지: " + message);

        PlayFailToastSound();

        Debug.Log("[SafetyPracticeManager] 2. delayBeforeFade 대기 시작");
        yield return new WaitForSecondsRealtime(delayBeforeFade);
        Debug.Log("[SafetyPracticeManager] 3. delayBeforeFade 대기 완료");

        PlayFailFadeBgm();

        Debug.Log("[SafetyPracticeManager] 4. FadeOutRoutine 시작");
        yield return StartCoroutine(FadeOutRoutine());
        Debug.Log("[SafetyPracticeManager] 5. FadeOutRoutine 완료");

        Debug.Log("[SafetyPracticeManager] 6. 실패 패널 표시 시도");

        if (toastController != null)
        {
            toastController.ShowFailToast(message, toastShowTime);
            Debug.Log("[SafetyPracticeManager] 7. ShowFailToast 호출 완료");
        }
        else
        {
            Debug.LogWarning("[SafetyPracticeManager] toastController가 null입니다.");
            OnFailDialogClosed();
        }
    }

    public void OnFailDialogClosed()
    {
        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 실패 패널 닫힘. 현재 씬을 다시 로드합니다.");
        }

        if (stopFadeBgmBeforeReload)
        {
            StopFailFadeBgm();
        }

        ReloadCurrentScene();
    }

    public void OnSuccessDialogClosed()
    {
        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 성공 패널 닫힘. 메인 씬으로 이동합니다.");
        }

        LoadMainScene();
    }

    private void PlayFailToastSound()
    {
        if (failToastAudioSource == null)
        {
            Debug.LogWarning("[SafetyPracticeManager] failToastAudioSource가 연결되지 않았습니다.");
            return;
        }

        AudioClip clipToPlay = failToastSoundClip;

        if (clipToPlay == null)
        {
            clipToPlay = failToastAudioSource.clip;
        }

        if (clipToPlay == null)
        {
            Debug.LogWarning("[SafetyPracticeManager] 재생할 실패 효과음 클립이 없습니다. Fail Toast Sound Clip 또는 AudioSource Clip 중 하나를 연결하세요.");
            return;
        }

        failToastAudioSource.mute = false;
        failToastAudioSource.volume = 1f;
        failToastAudioSource.spatialBlend = 0f;

        failToastAudioSource.PlayOneShot(clipToPlay);

        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 실패 효과음 재생 / 클립: " + clipToPlay.name);
        }
    }

    private void PlayFailFadeBgm()
    {
        if (failFadeBgmAudioSource == null)
        {
            Debug.LogWarning("[SafetyPracticeManager] failFadeBgmAudioSource가 연결되지 않았습니다.");
            return;
        }

        AudioClip clipToPlay = failFadeBgmClip;

        if (clipToPlay == null)
        {
            clipToPlay = failFadeBgmAudioSource.clip;
        }

        if (clipToPlay == null)
        {
            Debug.LogWarning("[SafetyPracticeManager] Fail Fade BGM Clip 또는 AudioSource Clip 중 하나를 연결해야 합니다.");
            return;
        }

        failFadeBgmAudioSource.Stop();
        failFadeBgmAudioSource.clip = clipToPlay;
        failFadeBgmAudioSource.loop = true;
        failFadeBgmAudioSource.mute = false;
        failFadeBgmAudioSource.volume = 1f;
        failFadeBgmAudioSource.spatialBlend = 0f;

        failFadeBgmAudioSource.Play();

        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 실패 페이드 BGM 재생 / 클립: " + clipToPlay.name
                      + " / isPlaying: " + failFadeBgmAudioSource.isPlaying);
        }
    }

    private void StopFailFadeBgm()
    {
        if (failFadeBgmAudioSource == null)
            return;

        if (failFadeBgmAudioSource.isPlaying)
        {
            failFadeBgmAudioSource.Stop();
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        Debug.Log("[SafetyPracticeManager] FadeOutRoutine 진입");

        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SafetyPracticeManager] fadeCanvasGroup이 null입니다.");
            yield break;
        }

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;

        float elapsedTime = 0f;

        if (fadeDuration <= 0f)
        {
            fadeCanvasGroup.alpha = 1f;
            Debug.Log("[SafetyPracticeManager] fadeDuration이 0 이하라 즉시 암전 처리");
            yield break;
        }

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);

            Debug.Log(
                "[SafetyPracticeManager] FadeOut 진행 중 / elapsed: " +
                elapsedTime.ToString("F2") +
                " / alpha: " +
                fadeCanvasGroup.alpha.ToString("F2")
            );

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        Debug.Log("[SafetyPracticeManager] FadeOut 완료");
    }

    private void StartReturnToMainSceneRoutine()
    {
        if (returnToMainSceneRoutine != null)
            return;

        returnToMainSceneRoutine = StartCoroutine(ReturnToMainSceneRoutine());
    }

    private IEnumerator ReturnToMainSceneRoutine()
    {
        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 퀘스트 완료. "
                      + delayBeforeReturnToMainScene.ToString("F1")
                      + "초 후 메인 씬으로 이동합니다.");
        }

        yield return new WaitForSeconds(delayBeforeReturnToMainScene);

        if (isFailing)
            yield break;

        LoadMainScene();
    }

    private void LoadMainScene()
    {
        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogWarning("[SafetyPracticeManager] Main Scene Name이 비어 있어 메인 씬으로 이동할 수 없습니다.");
            return;
        }

        SceneManager.LoadScene(mainSceneName);
    }

    private void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}