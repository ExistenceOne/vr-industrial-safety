using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class GrinderSparkAccidentDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrinderController grinderController;

    [Header("Face Layer")]
    [Tooltip("FaceCollider 오브젝트가 속한 레이어를 선택. Inspector 드롭다운으로 직접 지정.")]
    [SerializeField] private LayerMask faceLayerMask;

    [Header("안전 확인 오브젝트")]
    [Tooltip("이 오브젝트가 비활성화 상태면 안전한 것으로 판단해 긍정 메시지를 표시합니다.")]
    [SerializeField] private GameObject safetyCheckObject;

    [Tooltip("안전 확인 시 표시할 긍정 메시지")]
    [TextArea(3, 8)]
    [SerializeField] private string safetyPassMessage = "안전하게 작업하고 있습니다!";

    [Header("메시지 설정")]
    [Tooltip("사고 발생 시 표시할 실패 메시지")]
    [TextArea(4, 12)]
    [SerializeField] private string failMessage = "불꽃 비산 사고!\n불꽃 파편이 얼굴에 충돌했습니다.";

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private ParticleSystem sparkSystem;
    private bool hasTriggeredAccident = false;
    private bool wasActive = false;

    private void Awake()
    {
        sparkSystem = GetComponent<ParticleSystem>();

        if (grinderController == null)
            grinderController = GetComponentInParent<GrinderController>();

        if (grinderController == null)
            Debug.LogWarning("[GrinderSparkAccidentDetector] GrinderController를 찾을 수 없습니다.");

        var emission = sparkSystem.emission;
        emission.enabled = false;
    }

    private void Update()
    {
        if (grinderController == null) return;

        bool shouldEmit = grinderController.IsGrabbed && grinderController.IsActive;

        if (shouldEmit && !wasActive)
        {
            var emission = sparkSystem.emission;
            emission.enabled = true;

            if (!sparkSystem.isPlaying)
                sparkSystem.Play();

            wasActive = true;
        }
        else if (!shouldEmit && wasActive)
        {
            var emission = sparkSystem.emission;
            emission.enabled = false;
            wasActive = false;
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (hasTriggeredAccident) return;
        if ((faceLayerMask.value & (1 << other.layer)) == 0) return;

        hasTriggeredAccident = true;

        if (showDebugLog)
            Debug.Log($"[GrinderSparkAccidentDetector] 불꽃 파편이 얼굴에 충돌: {other.name} — 사고 발생");

        if (safetyCheckObject != null && !safetyCheckObject.activeSelf)
        {
            if (showDebugLog)
                Debug.Log("[GrinderSparkAccidentDetector] 안전 오브젝트 비활성화 — 사고 없음");

            if (SafetyPracticeManager.Instance != null)
                SafetyPracticeManager.Instance.ShowPassMessage(safetyPassMessage);

            hasTriggeredAccident = false;
            return;
        }

        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways(failMessage);
        else
            Debug.LogWarning("[GrinderSparkAccidentDetector] SafetyPracticeManager 인스턴스 없음.");
    }
}