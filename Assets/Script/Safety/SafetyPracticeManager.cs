using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SafetyPracticeManager : MonoBehaviour
{
    public static SafetyPracticeManager Instance { get; private set; }

    private enum PracticeType
    {
        None,
        Hammer,
        Drill
    }

    [Header("Practice Type")]
    [SerializeField] private PracticeType practiceType = PracticeType.None;

    [Header("Safety Gear State")]
    [SerializeField] private bool isGloveEquipped = false;

    [Header("UI")]
    [SerializeField] private ToastMessageController toastController;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Hammer Nail Guide")]
    [SerializeField] private NailHitController nailHitController;
    [SerializeField] private float delayBeforeNailGuideToast = 2.2f;

    [Header("Drill Guide")]
    [SerializeField] private string drillGuideMessage = "보호구 착용 완료! 드릴을 작동한 뒤 작업물을 천천히 접촉하십시오.";
    [SerializeField] private float delayBeforeDrillGuideToast = 2.2f;

    [Header("Failure Settings")]
    [SerializeField] private string noGloveFailMessage = "보호구 미착용! 안전수칙 위반으로 실습에 실패했습니다.";
    [SerializeField] private float toastShowTime = 2.0f;
    [SerializeField] private float delayBeforeFade = 1.0f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float delayBeforeReload = 0.5f;

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

    public bool IsGloveEquipped => isGloveEquipped;
    public bool IsFailing => isFailing;

    private void Awake()
    {
        Instance = this;

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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
            yield return new WaitForSeconds(delayBeforeNailGuideToast);

            if (nailHitController != null)
            {
                nailHitController.ShowStartMessage();
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

    private IEnumerator FailRoutine()
    {
        isFailing = true;

        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 보호구 미착용으로 실습 실패");
        }

        if (toastController != null)
        {
            toastController.ShowFailToast(noGloveFailMessage, toastShowTime);
        }

        PlayFailToastSound();

        yield return new WaitForSeconds(delayBeforeFade);

        PlayFailFadeBgm();

        yield return StartCoroutine(FadeOutRoutine());

        yield return new WaitForSeconds(delayBeforeReload);

        if (stopFadeBgmBeforeReload)
        {
            StopFailFadeBgm();
        }

        ReloadCurrentScene();
    }

    private void PlayFailToastSound()
    {
        if (failToastAudioSource == null)
            return;

        if (failToastSoundClip != null)
        {
            failToastAudioSource.PlayOneShot(failToastSoundClip);
        }
        else if (failToastAudioSource.clip != null)
        {
            failToastAudioSource.PlayOneShot(failToastAudioSource.clip);
        }

        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 실패 토스트 효과음 재생");
        }
    }

    private void PlayFailFadeBgm()
    {
        if (failFadeBgmAudioSource == null)
            return;

        if (failFadeBgmClip != null)
        {
            failFadeBgmAudioSource.clip = failFadeBgmClip;
        }

        if (failFadeBgmAudioSource.clip == null)
            return;

        failFadeBgmAudioSource.loop = true;
        failFadeBgmAudioSource.Play();

        if (showDebugLog)
        {
            Debug.Log("[SafetyPracticeManager] 실패 페이드 BGM 재생");
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
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}