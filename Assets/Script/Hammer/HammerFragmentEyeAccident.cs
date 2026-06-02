using UnityEngine;

public class HammerFragmentEyeAccident : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [Tooltip("XR Origin의 Main Camera를 연결하세요 (유저의 눈 위치로 피가 튀게 함)")]
    [SerializeField] private Transform playerHead;

    [Tooltip("피 분출 이펙트 프리팹 (blood_spurt_effect) 연결")]
    [SerializeField] private GameObject bloodEffectPrefab;

    [Header("메시지 설정")]
    [Tooltip("사고 발생 시 표시할 실패 메시지")]
    [TextArea(4, 12)]
    [SerializeField]
    private string failMessage =
        "파편 비산 사고 발생!\n\n" +
        "못이 기울어진 상태에서 망치로 타격하여\n" +
        "금속 파편이 눈 방향으로 튀었습니다.\n\n" +
        "작업 중 눈 부상의 상당수는 날아오는 파편이나\n" +
        "충격으로 발생하며, 작은 파편도 각막 손상이나\n" +
        "시력 저하로 이어질 수 있습니다.\n\n" +
        "망치 작업 전에는 반드시 보안경을 착용하고,\n" +
        "못을 수직으로 고정한 상태에서 타격해야 합니다.";

    [Header("사운드 설정")]
    [Tooltip("파편이 튀는 금속 충돌 효과음")]
    [SerializeField] private AudioClip fragmentRicochetSound;

    [Tooltip("피 연출 시 재생될 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;

    [Tooltip("페이드아웃 되며 쓰러질 때 재생될 비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;

    [Tooltip("화면이 어두워질 때 함께 재생되는 페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;

    [Tooltip("모든 사고 효과음 볼륨 (0~1)")]
    [SerializeField]
    [Range(0f, 1f)]
    private float soundVolume = 0.5f;

    [Header("사고 설정값")]
    [Tooltip("못의 기울기가 이 각도(도)를 초과한 상태에서 타격 시 파편 사고 발생")]
    [SerializeField] private float minTiltAngleToTrigger = 20f;

    private bool isAccidentTriggered = false;

    public void TryTriggerAccident(float currentTiltAngle)
    {
        if (isAccidentTriggered) return;
        if (currentTiltAngle < minTiltAngleToTrigger) return;

        TriggerAccident();
    }

    private void TriggerAccident()
    {
        isAccidentTriggered = true;

        Debug.Log("[HammerFragmentEyeAccident] 파편 비산! 파편이 눈으로 튀었습니다.");

        if (bloodEffectPrefab != null && playerHead != null)
        {
            Instantiate(bloodEffectPrefab, playerHead.position, playerHead.rotation);
        }

        if (fragmentRicochetSound != null)
        {
            AudioSource.PlayClipAtPoint(fragmentRicochetSound, transform.position, soundVolume);
        }

        if (bloodSquirtSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(bloodSquirtSound, playerHead.position, soundVolume);
        }

        if (screamSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position, soundVolume);
        }

        if (fadeOutSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position, soundVolume);
        }

        if (SafetyPracticeManager.Instance != null)
        {
            SafetyPracticeManager.Instance.TryFailAlways(failMessage);
        }
        else
        {
            Debug.LogWarning("[HammerFragmentEyeAccident] SafetyPracticeManager 인스턴스 없음.");
        }
    }
}