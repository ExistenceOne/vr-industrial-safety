using UnityEngine;

// AngleGrinder 오브젝트(GrinderController와 같은 GameObject)에 붙인다.
// 매 프레임 손 위치가 블레이드 원통 내부인지 IsInsideCylinder()로 체크한다.
// Trigger Collider나 별도 오브젝트 추가 불필요.
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

        SpawnBloodEffect(hand.position);

        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways();
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
