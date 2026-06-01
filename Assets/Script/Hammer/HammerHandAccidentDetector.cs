using UnityEngine;

// Hammer 오브젝트(HammerMotionHapticController와 같은 GameObject)에 붙인다.
// 매 프레임 망치머리 위치가 왼손 근처인지 거리로 체크한다.
public class HammerHandAccidentDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HammerMotionHapticController hammerController;

    [Header("Hand Transform — 씬의 왼손 오브젝트를 드래그 앤 드롭")]
    [SerializeField] private Transform leftHandTransform;
    [Tooltip("XR Origin의 Main Camera를 연결하세요 (비명·페이드아웃 위치 기준)")]
    [SerializeField] private Transform playerHead;

    [Header("Blood Effect")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [Tooltip("토스트 매니저 연결")]
    [SerializeField] private ToastMessageController toastController;

    [Header("사운드 설정")]
    [Tooltip("망치가 손을 가격하는 충격음")]
    [SerializeField] private AudioClip hammerImpactSound;
    [Tooltip("피 연출 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;
    [Tooltip("비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;
    [Tooltip("페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;

    [Header("사고 설정값")]
    [Tooltip("망치머리와 왼손 사이의 거리가 이 값(m) 이하이면 타격으로 판정")]
    [SerializeField] private float hitRadius = 0.08f;
    [Tooltip("사고를 발생시키기 위한 최소 망치 속도 (m/s)")]
    [SerializeField] private float minHitSpeed = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasTriggeredAccident = false;

    private void Awake()
    {
        if (hammerController == null)
            hammerController = GetComponent<HammerMotionHapticController>();

        if (hammerController == null)
            Debug.LogWarning("[HammerHandAccidentDetector] HammerMotionHapticController를 찾을 수 없습니다.");
    }

    private void Update()
    {
        if (hasTriggeredAccident) return;
        if (hammerController == null) return;
        if (!hammerController.IsGrabbed) return;
        if (hammerController.CurrentHeadSpeed < minHitSpeed) return;
        if (leftHandTransform == null || hammerController.HammerHead == null) return;

        float distance = Vector3.Distance(hammerController.HammerHead.position, leftHandTransform.position);

        if (distance > hitRadius) return;

        TriggerAccident();
    }

    private void TriggerAccident()
    {
        hasTriggeredAccident = true;

        if (showDebugLog)
            Debug.Log("[HammerHandAccidentDetector] 왼손 타격 사고 발생!");

        if (toastController != null)
            toastController.ShowFailToast("왼손 타격!\n망치가 손을 가격했습니다.", 4f);

        if (bloodEffectPrefab != null && leftHandTransform != null)
            Instantiate(bloodEffectPrefab, leftHandTransform.position, Quaternion.identity);

        if (hammerImpactSound != null && hammerController.HammerHead != null)
            AudioSource.PlayClipAtPoint(hammerImpactSound, hammerController.HammerHead.position);

        if (bloodSquirtSound != null && leftHandTransform != null)
            AudioSource.PlayClipAtPoint(bloodSquirtSound, leftHandTransform.position);

        if (screamSound != null && playerHead != null)
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position);

        if (fadeOutSound != null && playerHead != null)
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position);

        if (PlayFadeOut.Instance != null)
            PlayFadeOut.Instance.StartFade();
    }
}
