using UnityEngine;

public class HammerFragmentEyeAccident : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [Tooltip("XR Origin의 Main Camera를 연결하세요 (유저의 눈 위치로 피가 튀게 함)")]
    [SerializeField] private Transform playerHead;
    [Tooltip("피 분출 이펙트 프리팹 (blood_spurt_effect) 연결")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [Tooltip("토스트 매니저 연결")]
    [SerializeField] private ToastMessageController toastController;

    [Header("사운드 설정")]
    [Tooltip("파편이 튀는 금속 충돌 효과음")]
    [SerializeField] private AudioClip fragmentRicochetSound;
    [Tooltip("피 연출 시 재생될 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;
    [Tooltip("페이드아웃 되며 쓰러질 때 재생될 비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;
    [Tooltip("화면이 어두워질 때 함께 재생되는 페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;

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

        if (toastController != null)
        {
            toastController.ShowFailToast("파편 비산!\n파편이 눈을 가격했습니다.", 4f);
        }

        if (bloodEffectPrefab != null && playerHead != null)
        {
            Instantiate(bloodEffectPrefab, playerHead.position, playerHead.rotation);
        }

        if (fragmentRicochetSound != null)
        {
            AudioSource.PlayClipAtPoint(fragmentRicochetSound, transform.position);
        }
        if (bloodSquirtSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(bloodSquirtSound, playerHead.position);
        }
        if (screamSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position);
        }
        if (fadeOutSound != null && playerHead != null)
        {
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position);
        }

        if (PlayFadeOut.Instance != null)
        {
            PlayFadeOut.Instance.StartFade();
        }
    }
}
