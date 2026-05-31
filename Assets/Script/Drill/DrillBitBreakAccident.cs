using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class DrillBitBreakAccident : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [Tooltip("XR Origin의 Main Camera를 연결하세요 (유저의 눈 위치로 피가 튀게 함)")]
    [SerializeField] private Transform playerHead;
    [Tooltip("드릴의 끝부분(비트) 위치를 나타내는 빈 오브젝트를 연결하세요")]
    [SerializeField] private Transform drillTip;
    [Tooltip("피 분출 이펙트 프리팹 (blood_spurt_effect) 연결")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [Tooltip("토스트 매니저 연결")]
    [SerializeField] private ToastMessageController toastController;

    [Header("사운드 설정")]
    [Tooltip("비트 파손 시 재생될 금속 부러지는 효과음 (DrillBitSnap)")]
    [SerializeField] private AudioClip bitSnapSound;
    [Tooltip("피 연출 시 재생될 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;
    [Tooltip("페이드아웃 되며 쓰러질 때 재생될 비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;
    [Tooltip("화면이 어두워질 때 함께 재생되는 페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;

    [Header("사고 설정값")]
    [Tooltip("벽과 드릴의 각도가 이 수치(도)를 넘어가면 비트 파손 발생")]
    [SerializeField] private float maxSafeAngle = 15f;
    [Tooltip("벽이나 나무판자의 Layer를 선택하세요")]
    [SerializeField] private LayerMask targetLayer;

    private XRGrabInteractable grabInteractable;
    private bool isDrillRunning = false;
    private bool isAccidentTriggered = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.activated.AddListener(OnDrillActivated);
        grabInteractable.deactivated.AddListener(OnDrillDeactivated);
    }

    private void OnDisable()
    {
        grabInteractable.activated.RemoveListener(OnDrillActivated);
        grabInteractable.deactivated.RemoveListener(OnDrillDeactivated);
    }

    private void OnDrillActivated(ActivateEventArgs args) { isDrillRunning = true; }
    private void OnDrillDeactivated(DeactivateEventArgs args) { isDrillRunning = false; }

    void Update()
    {
        if (isAccidentTriggered || !isDrillRunning || drillTip == null || playerHead == null) return;

        // 드릴 끝부분 테스트
        Debug.DrawRay(drillTip.position, drillTip.forward * 0.1f, Color.red);

        // 드릴 끝에서 앞(Forward)으로 10cm(0.1f) 길이의 레이저를 쏴서 목표물(Layer) 검출
        if (Physics.Raycast(drillTip.position, drillTip.forward, out RaycastHit hit, 0.1f, targetLayer))
        {
            // 벽의 수직 방향(Normal)의 반대 방향과 드릴의 방향 사이의 각도 계산
            float angle = Vector3.Angle(-hit.normal, drillTip.forward);

            if (angle > maxSafeAngle)
            {
                TriggerAccident();
            }
        }
    }

    private void TriggerAccident()
    {
        isAccidentTriggered = true;
        isDrillRunning = false;
        Debug.Log("[DrillBitBreakAccident] 비트 파손! 파편이 눈으로 튀었습니다.");

        // 토스트 메시지 호출
        if (toastController != null)
        {
            toastController.ShowFailToast("비트 파손!\n파편이 눈을 가격했습니다.", 4f);
        }

        // 피 분출 이펙트 생성 (눈 부상 연출)
        if (bloodEffectPrefab != null)
        {
            Instantiate(bloodEffectPrefab, playerHead.position, playerHead.rotation);
        }

        // 사운드 재생
        if (bitSnapSound != null)
        {
            AudioSource.PlayClipAtPoint(bitSnapSound, drillTip.position);
        }
        if (bloodSquirtSound != null)
        {
            AudioSource.PlayClipAtPoint(bloodSquirtSound, playerHead.position);
        }
        if (screamSound != null)
        {
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position);
        }
        if (fadeOutSound != null)
        {
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position);
        }

        // 화면 페이드 아웃 연출
        if (PlayFadeOut.Instance != null)
        {
            PlayFadeOut.Instance.StartFade();
        }
    }
}