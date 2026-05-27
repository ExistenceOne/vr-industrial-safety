using UnityEngine;

// 불꽃 파티클이 있는 오브젝트에 부착.
// 그라인더 작동 중 파티클이 "Face" 레이어 오브젝트에 충돌하면 사고 발생.
// ParticleSystem Collision 모듈에서 Send Collision Messages 반드시 체크 필요.
[RequireComponent(typeof(ParticleSystem))]
public class GrinderSparkAccidentDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrinderController grinderController;

    [Header("Face Layer")]
    [Tooltip("FaceCollider 오브젝트가 속한 레이어를 선택. Inspector 드롭다운으로 직접 지정.")]
    [SerializeField] private LayerMask faceLayerMask;

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
            if (!sparkSystem.isPlaying) sparkSystem.Play();
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

        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways();
        else
            Debug.LogWarning("[GrinderSparkAccidentDetector] SafetyPracticeManager 인스턴스 없음.");
    }
}
