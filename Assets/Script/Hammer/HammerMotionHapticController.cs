using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class HammerMotionHapticController : MonoBehaviour
{
    [Header("Hammer Head")]
    [SerializeField] private Transform hammerHead;

    [Header("Hit Detection Settings")]
    [SerializeField] private float minHitSpeed = 1.5f;
    [SerializeField] private float hitCooldown = 0.3f;
    [SerializeField] private bool onlyHitTargetTag = false;
    [SerializeField] private string targetTag = "HammerTarget";

    [Header("Hammer Haptic Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float hapticAmplitude = 1.0f;
    [SerializeField] private float hapticDuration = 0.15f;

    [Header("Hammer Sound Settings")]
    [SerializeField] private AudioClip hammerHitSoundClip;
    [Range(0f, 1f)]
    [SerializeField] private float hammerSoundVolume = 0.9f;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLog = true;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;

    private InputDevice currentDevice;
    private XRNode currentHandNode = XRNode.RightHand;

    private bool isGrabbed = false;

    private Vector3 previousHeadPosition;
    private float currentHeadSpeed = 0f;
    private float lastHitTime = -999f;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = hammerSoundVolume;

        if (hammerHitSoundClip != null)
        {
            audioSource.clip = hammerHitSoundClip;
        }

        if (hammerHead == null)
        {
            hammerHead = transform;
            Debug.LogWarning("[HammerMotionHapticController] Hammer Head가 연결되지 않아 Hammer Root를 기준으로 속도를 계산합니다.");
        }

        previousHeadPosition = hammerHead.position;
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void Update()
    {
        UpdateHammerHeadSpeed();

        if (isGrabbed && !currentDevice.isValid)
        {
            currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);
        }
    }

    private void UpdateHammerHeadSpeed()
    {
        if (hammerHead == null)
            return;

        Vector3 currentPosition = hammerHead.position;
        currentHeadSpeed = (currentPosition - previousHeadPosition).magnitude / Time.deltaTime;
        previousHeadPosition = currentPosition;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

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
            Debug.LogWarning("[HammerMotionHapticController] 손 방향 판단 실패. Interactor 이름에 Left 또는 Right가 있는지 확인하세요.");
        }

        currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);

        if (showDebugLog)
        {
            Debug.Log("[HammerMotionHapticController] 망치 잡은 손: " + currentHandNode);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentDevice = default;

        if (showDebugLog)
        {
            Debug.Log("[HammerMotionHapticController] 망치 놓음");
        }
    }

    public void TryHammerHit(Collider hitCollider)
    {
        if (!isGrabbed)
            return;

        if (Time.time - lastHitTime < hitCooldown)
            return;

        if (onlyHitTargetTag && !hitCollider.CompareTag(targetTag))
            return;

        if (currentHeadSpeed < minHitSpeed)
        {
            if (showDebugLog)
            {
                Debug.Log("[HammerMotionHapticController] 타격 속도 부족: " + currentHeadSpeed.ToString("F2"));
            }

            return;
        }

        lastHitTime = Time.time;

        PlayHammerHitSound();
        SendHammerHitHaptic();

        if (showDebugLog)
        {
            Debug.Log("[HammerMotionHapticController] 망치 타격 성공 / 속도: "
                      + currentHeadSpeed.ToString("F2")
                      + " / 대상: "
                      + hitCollider.name);
        }
    }

    private void PlayHammerHitSound()
    {
        if (audioSource == null)
            return;

        if (hammerHitSoundClip == null && audioSource.clip == null)
        {
            Debug.LogWarning("[HammerMotionHapticController] 망치 타격 사운드 클립이 연결되지 않았습니다.");
            return;
        }

        if (hammerHitSoundClip != null)
        {
            audioSource.PlayOneShot(hammerHitSoundClip, hammerSoundVolume);
        }
        else
        {
            audioSource.PlayOneShot(audioSource.clip, hammerSoundVolume);
        }

        if (showDebugLog)
        {
            Debug.Log("[HammerMotionHapticController] 망치 타격 사운드 재생");
        }
    }

    private void SendHammerHitHaptic()
    {
        if (!currentDevice.isValid)
        {
            Debug.LogWarning("[HammerMotionHapticController] 진동 실패: 입력 장치가 유효하지 않습니다.");
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

                if (showDebugLog)
                {
                    if (result)
                    {
                        Debug.Log("[HammerMotionHapticController] 망치 타격 진동 전송됨 / 손: " + currentHandNode);
                    }
                    else
                    {
                        Debug.LogWarning("[HammerMotionHapticController] 진동 명령 전송 실패 / 손: " + currentHandNode);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[HammerMotionHapticController] 이 컨트롤러는 Haptic Impulse를 지원하지 않습니다.");
            }
        }
    }
}