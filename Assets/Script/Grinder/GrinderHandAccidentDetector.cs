using UnityEngine;

public class GrinderHandAccidentDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrinderController grinderController;

    [Header("Hand Transforms — 씬의 손 오브젝트를 드래그 앤 드롭")]
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;

    [Header("Blood Effect")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private AudioSource bloodAudioSource;
    [SerializeField] private AudioClip bloodSoundClip;

    [Header("안전 확인 오브젝트")]
    [Tooltip("이 오브젝트가 비활성화 상태면 안전한 것으로 판단해 긍정 메시지를 표시합니다.")]
    [SerializeField] private GameObject safetyCheckObject;
    [Tooltip("안전 확인 시 표시할 긍정 메시지")]
    [SerializeField] private string safetyPassMessage = "안전하게 작업하고 있습니다!";

    [Header("메시지 설정")]
    [Tooltip("사고 발생 시 표시할 실패 메시지")]
    [SerializeField] private string failMessage = "손 절단 사고!\n그라인더 날에 손이 접촉했습니다.";

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasTriggeredAccident = false;

    private void Awake()
    {
        if (grinderController == null)
            grinderController = GetComponent<GrinderController>();

        if (grinderController == null)
            Debug.LogWarning("[GrinderHandAccidentDetector] GrinderController를 찾을 수 없습니다.");
    }

    private void Update()
    {
        if (hasTriggeredAccident) return;
        if (grinderController == null) return;
        if (!grinderController.IsGrabbed || !grinderController.IsActive) return;

        CheckHand(leftHandTransform);
        CheckHand(rightHandTransform);
    }

    private void CheckHand(Transform hand)
    {
        if (hand == null) return;
        if (!grinderController.IsInsideCylinder(hand.position)) return;

        hasTriggeredAccident = true;

        if (showDebugLog)
            Debug.Log($"[GrinderHandAccidentDetector] 손({hand.name}) 블레이드 접촉 — 사고 발생");

        if (safetyCheckObject != null && !safetyCheckObject.activeSelf)
        {
            if (showDebugLog)
                Debug.Log("[GrinderHandAccidentDetector] 안전 오브젝트 비활성화 — 사고 없음");

            if (SafetyPracticeManager.Instance != null)
                SafetyPracticeManager.Instance.ShowPassMessage(safetyPassMessage);

            hasTriggeredAccident = false;
            return;
        }

        SpawnBloodEffect(hand.position);

        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways(failMessage);
        else
            Debug.LogWarning("[GrinderHandAccidentDetector] SafetyPracticeManager 인스턴스 없음.");
    }

    private void SpawnBloodEffect(Vector3 position)
    {
        if (bloodEffectPrefab != null)
            Instantiate(bloodEffectPrefab, position, Quaternion.identity);

        if (bloodAudioSource != null && bloodSoundClip != null)
            bloodAudioSource.PlayOneShot(bloodSoundClip);
        else if (bloodAudioSource != null && bloodAudioSource.clip != null)
            bloodAudioSource.PlayOneShot(bloodAudioSource.clip);
    }
}
