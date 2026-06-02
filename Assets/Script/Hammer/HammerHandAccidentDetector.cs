using UnityEngine;

public class HammerHandAccidentDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HammerMotionHapticController hammerController;

    [Header("Hand Transform — 씬의 왼손 오브젝트를 드래그 앤 드롭")]
    [SerializeField] private Transform leftHandTransform;

    [Tooltip("XR Origin의 Main Camera를 연결하세요. 비명 위치 기준")]
    [SerializeField] private Transform playerHead;

    [Header("Blood Effect")]
    [SerializeField] private GameObject bloodEffectPrefab;

    [Header("안전 확인 오브젝트")]
    [Tooltip("이 오브젝트가 비활성화 상태면 안전한 것으로 판단해 긍정 메시지를 표시합니다.")]
    [SerializeField] private GameObject safetyCheckObject;

    [Tooltip("안전 확인 시 표시할 긍정 메시지")]
    [SerializeField] private string safetyPassMessage = "안전하게 작업하고 있습니다!";

    [Header("메시지 설정")]
    [Tooltip("사고 발생 시 표시할 실패 메시지")]
    [SerializeField, TextArea(3, 10)]
    private string failMessage = "왼손 타격!\n망치가 손을 가격했습니다.";

    [Header("사운드 설정")]
    [Tooltip("망치가 손을 가격하는 충격음")]
    [SerializeField] private AudioClip hammerImpactSound;

    [Tooltip("피 연출 효과음")]
    [SerializeField] private AudioClip bloodSquirtSound;

    [Tooltip("비명 소리")]
    [SerializeField] private AudioClip screamSound;

    [Tooltip("모든 사고 효과음 볼륨")]
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.5f;

    [Header("사고 설정값")]
    [Tooltip("망치머리와 왼손 사이의 거리가 이 값 이하이면 타격으로 판정")]
    [SerializeField] private float hitRadius = 0.08f;

    [Tooltip("사고를 발생시키기 위한 최소 망치 속도")]
    [SerializeField] private float minHitSpeed = 1.5f;

    [Header("Safety Pass Settings")]
    [SerializeField] private float safetyPassMessageCooldown = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasTriggeredAccident = false;
    private float lastSafetyPassMessageTime = -999f;

    private void Awake()
    {
        if (hammerController == null)
        {
            hammerController = GetComponent<HammerMotionHapticController>();
        }

        if (hammerController == null)
        {
            Debug.LogWarning("[HammerHandAccidentDetector] HammerMotionHapticController를 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (hasTriggeredAccident)
            return;

        if (SafetyPracticeManager.Instance != null && SafetyPracticeManager.Instance.IsFailing)
            return;

        if (hammerController == null)
            return;

        if (!hammerController.IsGrabbed)
            return;

        if (hammerController.CurrentHeadSpeed < minHitSpeed)
            return;

        if (leftHandTransform == null || hammerController.HammerHead == null)
            return;

        float distance = Vector3.Distance(
            hammerController.HammerHead.position,
            leftHandTransform.position
        );

        if (distance > hitRadius)
            return;

        CheckHandHit();
    }

    private void CheckHandHit()
    {
        if (safetyCheckObject != null && !safetyCheckObject.activeSelf)
        {
            ShowSafetyPassMessage();
            return;
        }

        TriggerAccident();
    }

    private void ShowSafetyPassMessage()
    {
        if (Time.time - lastSafetyPassMessageTime < safetyPassMessageCooldown)
            return;

        lastSafetyPassMessageTime = Time.time;

        if (showDebugLog)
        {
            Debug.Log("[HammerHandAccidentDetector] 안전 오브젝트 비활성화 — 사고 없음");
        }

        if (SafetyPracticeManager.Instance != null)
        {
            SafetyPracticeManager.Instance.ShowPassMessage(safetyPassMessage);
        }
    }

    private void TriggerAccident()
    {
        hasTriggeredAccident = true;

        if (showDebugLog)
        {
            Debug.Log("[HammerHandAccidentDetector] 왼손 타격 사고 발생!");
        }

        if (bloodEffectPrefab != null && leftHandTransform != null)
        {
            Instantiate(bloodEffectPrefab, leftHandTransform.position, Quaternion.identity);
        }

        if (hammerImpactSound != null && hammerController.HammerHead != null)
        {
            AudioSource.PlayClipAtPoint(
                hammerImpactSound,
                hammerController.HammerHead.position,
                soundVolume
            );
        }

        if (bloodSquirtSound != null && leftHandTransform != null)
        {
            AudioSource.PlayClipAtPoint(
                bloodSquirtSound,
                leftHandTransform.position,
                soundVolume
            );
        }

        if (screamSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(
                screamSound,
                playerHead.position,
                soundVolume
            );
        }

        if (SafetyPracticeManager.Instance != null)
        {
            SafetyPracticeManager.Instance.TryFailAlways(failMessage);
        }
        else
        {
            Debug.LogWarning("[HammerHandAccidentDetector] SafetyPracticeManager.Instance가 없어 실패 처리를 할 수 없습니다.");
        }
    }
}