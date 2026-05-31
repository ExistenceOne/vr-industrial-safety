using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class DrillEntanglementAccident : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [Tooltip("XR Origin의 Main Camera를 연결하세요 (유저의 머리/몸체)")]
    [SerializeField] private Transform playerHead;
    [Tooltip("드릴의 끝부분(비트) 위치를 나타내는 빈 오브젝트를 연결하세요")]
    [SerializeField] private Transform drillTip;
    [Tooltip("피 분출 이펙트 프리팹(blood_spurt_effect) 연결")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [Tooltip("토스트 매니저 연결")]
    [SerializeField] private ToastMessageController toastController;

    [Header("사고 설정값")]
    [Tooltip("이 거리(m)보다 드릴이 몸에 가까우면 말림 사고 발생")]
    [SerializeField] private float dangerousDistance = 0.3f;
    [Tooltip("사고가 최종 발동하기 위해 위험 구역에서 버텨야 하는 시간(초)")]
    [SerializeField] private float requiredDuration = 1.5f;

    [Header("사운드 설정")]
    [Tooltip("피 연출 시 재생될 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;
    [Tooltip("옷이 드릴에 감겨 찢어지는 효과음 (DrillClothRip)")]
    [SerializeField] private AudioClip clothRipSound;
    [Tooltip("페이드아웃 되며 쓰러질 때 재생될 비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;
    [Tooltip("화면이 어두워질 때 함께 감쇄하는 페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;

    private XRGrabInteractable grabInteractable;

    // 상태 관리 변수
    private bool hasFirstStrike = false;        // 1단계: 최초 피격 여부
    private bool isAccidentComplete = false;    // 2단계: 기절(페이드아웃) 완료 여부
    private float dangerTimer = 0f;             // 유지 시간 타이머

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.activated.AddListener(OnDrillActivated);
    }

    private void OnDisable()
    {
        grabInteractable.activated.RemoveListener(OnDrillActivated);
    }

    // 드릴을 작동하는 순간 거리를 체크하여 피 연출
    private void OnDrillActivated(ActivateEventArgs args)
    {
        if (playerHead == null || drillTip == null || isAccidentComplete) return;

        float distanceToBody = Vector3.Distance(drillTip.position, playerHead.position);

        // 위험 거리 안에서 작동시켰고, 아직 첫 피격이 일어나지 않았다면 즉시 실행
        if (distanceToBody < dangerousDistance && !hasFirstStrike)
        {
            TriggerFirstStrike();
        }
    }

    void Update()
    {
        if (playerHead == null || drillTip == null || isAccidentComplete) return;

        // 머리와 드릴 팁 사이의 거리 계산
        float distanceToBody = Vector3.Distance(drillTip.position, playerHead.position);

        // 한 번 피 연출이 발생한 상태에서 거리를 실시간으로 체크
        if (hasFirstStrike)
        {
            if (distanceToBody < dangerousDistance)
            {
                dangerTimer += Time.deltaTime;

                if (dangerTimer >= requiredDuration)
                {
                    TriggerFatalAccident(); // 1.5초 도달 시 페이드아웃
                }
            }
            else if (distanceToBody > dangerousDistance + 0.1f)
            {
                // 치명상에 도달하기 전에 드릴을 멀리 치우면 상태가 초기화 (재테스트 가능)
                ResetAccident();
            }
        }
    }

    // [연출] 최초 피격
    private void TriggerFirstStrike()
    {
        hasFirstStrike = true;
        Debug.Log("[DrillEntanglement] 1단계: 몸 근처에서 드릴 작동, 피 분출");

        if (toastController != null)
        {
            toastController.ShowFailToast("말림 사고 발생!\n드릴을 몸에서 떨어뜨려 사용하세요.", 4f);
        }

        if (bloodEffectPrefab != null)
        {
            Instantiate(bloodEffectPrefab, drillTip.position, Quaternion.identity);
        }

        if (clothRipSound != null)
        {
            AudioSource.PlayClipAtPoint(clothRipSound, playerHead.position);
        }
        if (bloodSquirtSound != null)
        {
            AudioSource.PlayClipAtPoint(bloodSquirtSound, playerHead.position);
        }
    }

    // [연출] 치명상 (페이드아웃)
    private void TriggerFatalAccident()
    {
        isAccidentComplete = true;
        Debug.Log("[DrillEntanglement] 2단계: 위험 구역 1.5초 유지됨, 페이드아웃 실행");

        if (PlayFadeOut.Instance != null)
        {
            PlayFadeOut.Instance.StartFade();
        }

        if (screamSound != null)
        {
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position);
        }
        if (fadeOutSound != null)
        {
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position);
        }
    }

    // [초기화] 드릴을 멀리 떨어뜨렸을 때
    private void ResetAccident()
    {
        hasFirstStrike = false;
        dangerTimer = 0f;
        Debug.Log("[DrillEntanglement] 드릴을 치워서 사고 상태가 리셋되었습니다.");
    }
}