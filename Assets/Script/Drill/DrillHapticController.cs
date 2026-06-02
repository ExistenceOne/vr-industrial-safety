using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class DrillHapticController : MonoBehaviour
{
    [Header("Drill Haptic Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float hapticAmplitude = 0.65f;

    [SerializeField] private float hapticDuration = 0.08f;
    [SerializeField] private float hapticInterval = 0.06f;

    [Header("Drill Sound Settings")]
    [SerializeField] private AudioClip drillSoundClip;

    [Range(0f, 1f)]
    [SerializeField] private float drillSoundVolume = 0.8f;

    [Header("Drill Quest 연동")]
    [Tooltip("드릴 깊이 퀘스트를 연결하세요. 100% 이후 트리거 유지 여부 판정에 사용됩니다.")]
    [SerializeField] private DrillDepthQuest drillDepthQuest;

    [Header("Safety Check")]
    [Tooltip("체크 시 보호구 미착용 상태에서 드릴 작동 버튼을 누르면 실패 처리합니다.")]
    [SerializeField] private bool failIfNoGloveOnDrillUse = true;

    [Header("Debug Settings")]
    [SerializeField] private bool showHapticLog = true;
    [SerializeField] private float hapticLogInterval = 0.5f;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;

    private InputDevice currentDevice;
    private XRNode currentHandNode = XRNode.RightHand;

    private bool isGrabbed = false;
    private bool isDrillActive = false;

    private float hapticTimer = 0f;
    private float hapticLogTimer = 0f;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = drillSoundVolume;

        if (drillSoundClip != null)
        {
            audioSource.clip = drillSoundClip;
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        grabInteractable.activated.AddListener(OnDrillActivated);
        grabInteractable.deactivated.AddListener(OnDrillDeactivated);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);

        grabInteractable.activated.RemoveListener(OnDrillActivated);
        grabInteractable.deactivated.RemoveListener(OnDrillDeactivated);
    }

    private void Update()
    {
        if (!isGrabbed)
            return;

        if (!isDrillActive)
            return;

        if (SafetyPracticeManager.Instance != null && SafetyPracticeManager.Instance.IsFailing)
            return;

        if (!currentDevice.isValid)
        {
            currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);
        }

        if (!currentDevice.isValid)
            return;

        hapticTimer += Time.deltaTime;
        hapticLogTimer += Time.deltaTime;

        if (hapticTimer >= hapticInterval)
        {
            SendDrillHaptic();
            hapticTimer = 0f;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        isDrillActive = false;

        hapticTimer = hapticInterval;
        hapticLogTimer = hapticLogInterval;

        string interactorName = args.interactorObject.transform.name.ToLower();

        if (interactorName.Contains("left"))
        {
            currentHandNode = XRNode.LeftHand;
        }
        else if (interactorName.Contains("right"))
        {
            currentHandNode = XRNode.RightHand;
        }
        else
        {
            currentHandNode = XRNode.RightHand;
            Debug.LogWarning("[DrillHapticController] 손 방향 판단 실패. Interactor 이름에 Left 또는 Right가 있는지 확인하세요.");
        }

        currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);

        Debug.Log("[DrillHapticController] 드릴 잡은 손: " + currentHandNode);

        if (currentDevice.isValid)
        {
            Debug.Log("[DrillHapticController] 입력 장치 연결됨: " + currentDevice.name);
        }
        else
        {
            Debug.LogWarning("[DrillHapticController] 입력 장치를 찾지 못했습니다: " + currentHandNode);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        isDrillActive = false;
        currentDevice = default;

        if (drillDepthQuest != null)
        {
            drillDepthQuest.SetDrillTriggerHeld(false);
        }

        StopDrillSound();

        Debug.Log("[DrillHapticController] 드릴 놓음");
    }

    private void OnDrillActivated(ActivateEventArgs args)
    {
        if (!isGrabbed)
            return;

        if (SafetyPracticeManager.Instance != null && SafetyPracticeManager.Instance.IsFailing)
            return;

        if (failIfNoGloveOnDrillUse && SafetyPracticeManager.Instance != null)
        {
            bool failedByNoGlove = SafetyPracticeManager.Instance.TryFailIfNoGlove();

            if (failedByNoGlove)
            {
                isDrillActive = false;

                if (drillDepthQuest != null)
                {
                    drillDepthQuest.SetDrillTriggerHeld(false);
                }

                StopDrillSound();

                Debug.Log("[DrillHapticController] 보호구 미착용 상태에서 드릴 사용 → 실패 처리");
                return;
            }
        }

        isDrillActive = true;
        hapticTimer = hapticInterval;
        hapticLogTimer = hapticLogInterval;

        if (drillDepthQuest != null)
        {
            drillDepthQuest.SetDrillTriggerHeld(true);
        }
        else
        {
            Debug.LogWarning("[DrillHapticController] DrillDepthQuest가 연결되지 않았습니다.");
        }

        PlayDrillSound();

        Debug.Log("[DrillHapticController] 드릴 작동 버튼 눌림: " + currentHandNode);
    }

    private void OnDrillDeactivated(DeactivateEventArgs args)
    {
        isDrillActive = false;

        if (drillDepthQuest != null)
        {
            drillDepthQuest.SetDrillTriggerHeld(false);
        }

        StopDrillSound();

        Debug.Log("[DrillHapticController] 드릴 작동 버튼 해제: " + currentHandNode);
    }

    private void PlayDrillSound()
    {
        if (audioSource == null)
            return;

        if (audioSource.clip == null)
        {
            Debug.LogWarning("[DrillHapticController] 드릴 사운드 클립이 연결되지 않았습니다.");
            return;
        }

        audioSource.volume = drillSoundVolume;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("[DrillHapticController] 드릴 사운드 재생");
        }
    }

    private void StopDrillSound()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[DrillHapticController] 드릴 사운드 정지");
        }
    }

    private void SendDrillHaptic()
    {
        if (!currentDevice.isValid)
        {
            Debug.LogWarning("[DrillHapticController] 진동 실패: 입력 장치가 유효하지 않습니다.");
            return;
        }

        if (currentDevice.TryGetHapticCapabilities(out HapticCapabilities capabilities))
        {
            if (capabilities.supportsImpulse)
            {
                bool result = currentDevice.SendHapticImpulse(
                    0,
                    hapticAmplitude,
                    hapticDuration
                );

                if (showHapticLog && hapticLogTimer >= hapticLogInterval)
                {
                    if (result)
                    {
                        Debug.Log("[DrillHapticController] 드릴 진동 전송됨 / 손: "
                                  + currentHandNode
                                  + " / 세기: "
                                  + hapticAmplitude);
                    }
                    else
                    {
                        Debug.LogWarning("[DrillHapticController] 진동 명령 전송 실패 / 손: " + currentHandNode);
                    }

                    hapticLogTimer = 0f;
                }
            }
            else
            {
                Debug.LogWarning("[DrillHapticController] 이 컨트롤러는 Haptic Impulse를 지원하지 않습니다: " + currentDevice.name);
            }
        }
        else
        {
            Debug.LogWarning("[DrillHapticController] HapticCapabilities를 가져오지 못했습니다: " + currentDevice.name);
        }
    }
}