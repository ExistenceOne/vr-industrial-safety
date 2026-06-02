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
    [Tooltip("토스트 매니저 연결 (1단계 경고 메시지용)")]
    [SerializeField] private ToastMessageController toastController;

    [Header("사고 설정값")]
    [Tooltip("이 거리(m)보다 드릴이 몸에 가까우면 말림 사고 발생")]
    [SerializeField] private float dangerousDistance = 0.3f;
    [Tooltip("사고가 최종 발동하기 위해 위험 구역에서 버텨야 하는 시간(초)")]
    [SerializeField] private float requiredDuration = 1.5f;

    [Header("안전 확인 오브젝트")]
    [Tooltip("이 오브젝트가 비활성화 상태면 안전한 것으로 판단해 긍정 메시지를 표시하고 사고를 발동하지 않습니다.")]
    [SerializeField] private GameObject safetyCheckObject;
    [Tooltip("안전 확인 시 표시할 긍정 메시지")]
    [SerializeField] private string safetyPassMessage = "안전하게 작업하고 있습니다!";

    [Header("메시지 설정")]
    [Tooltip("1단계 경고 시 표시할 메시지")]
    [SerializeField] private string firstStrikeMessage = "말림 사고 발생!\n드릴을 몸에서 떨어뜨려 사용하세요.";
    [Tooltip("치명상(페이드아웃) 시 표시할 실패 메시지")]
    [SerializeField] private string fatalMessage = "말림 사고!\n드릴에 몸이 감겼습니다.";

    [Header("사운드 설정")]
    [Tooltip("피 연출 시 재생될 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;
    [Tooltip("옷이 드릴에 감겨 찢어지는 효과음 (DrillClothRip)")]
    [SerializeField] private AudioClip clothRipSound;
    [Tooltip("페이드아웃 되며 쓰러질 때 재생될 비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;
    [Tooltip("화면이 어두워질 때 함께 감쇄하는 페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;
    [Tooltip("모든 사고 효과음 볼륨 (0~1)")]
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.5f;

    private XRGrabInteractable grabInteractable;

    private bool hasFirstStrike = false;
    private bool isAccidentComplete = false;
    private float dangerTimer = 0f;

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

    private void OnDrillActivated(ActivateEventArgs args)
    {
        if (playerHead == null || drillTip == null || isAccidentComplete) return;

        float distanceToBody = Vector3.Distance(drillTip.position, playerHead.position);

        if (distanceToBody < dangerousDistance && !hasFirstStrike)
        {
            TriggerFirstStrike();
        }
    }

    void Update()
    {
        if (playerHead == null || drillTip == null || isAccidentComplete) return;

        float distanceToBody = Vector3.Distance(drillTip.position, playerHead.position);

        if (hasFirstStrike)
        {
            if (distanceToBody < dangerousDistance)
            {
                dangerTimer += Time.deltaTime;

                if (dangerTimer >= requiredDuration)
                {
                    TriggerFatalAccident();
                }
            }
            else if (distanceToBody > dangerousDistance + 0.1f)
            {
                ResetAccident();
            }
        }
    }

    private void TriggerFirstStrike()
    {
        if (safetyCheckObject != null && !safetyCheckObject.activeSelf)
        {
            Debug.Log("[DrillEntanglement] 안전 오브젝트 비활성화 — 사고 없음");
            if (toastController != null)
                toastController.ShowSuccessToast(safetyPassMessage, 3f);
            return;
        }

        hasFirstStrike = true;
        Debug.Log("[DrillEntanglement] 1단계: 몸 근처에서 드릴 작동, 피 분출");

        if (toastController != null)
            toastController.ShowFailToast(firstStrikeMessage, 4f);

        if (bloodEffectPrefab != null)
            Instantiate(bloodEffectPrefab, drillTip.position, Quaternion.identity);

        if (clothRipSound != null)
            AudioSource.PlayClipAtPoint(clothRipSound, playerHead.position, soundVolume);
        if (bloodSquirtSound != null)
            AudioSource.PlayClipAtPoint(bloodSquirtSound, playerHead.position, soundVolume);
    }

    private void TriggerFatalAccident()
    {
        isAccidentComplete = true;
        Debug.Log("[DrillEntanglement] 2단계: 위험 구역 유지됨, 페이드아웃 실행");

        if (screamSound != null)
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position, soundVolume);
        if (fadeOutSound != null)
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position, soundVolume);

        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways(fatalMessage);
    }

    private void ResetAccident()
    {
        hasFirstStrike = false;
        dangerTimer = 0f;
        Debug.Log("[DrillEntanglement] 드릴을 치워서 사고 상태가 리셋되었습니다.");
    }
}
