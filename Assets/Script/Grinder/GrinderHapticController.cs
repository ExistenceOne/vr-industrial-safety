using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class GrinderHapticController : MonoBehaviour
{
    [Header("Grinder Haptic Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float hapticAmplitude = 0.75f;

    [SerializeField] private float hapticDuration = 0.05f;
    [SerializeField] private float hapticInterval = 0.04f;

    [Header("Grinder Sound Settings")]
    [SerializeField] private AudioClip grinderSoundClip;

    [Range(0f, 1f)]
    [SerializeField] private float grinderSoundVolume = 0.8f;

    [Header("Debug Settings")]
    [SerializeField] private bool showHapticLog = true;
    [SerializeField] private float hapticLogInterval = 0.5f;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;

    private InputDevice currentDevice;
    private XRNode currentHandNode = XRNode.RightHand;

    private bool isGrabbed = false;
    private bool isGrinderActive = false;

    private float hapticTimer = 0f;
    private float hapticLogTimer = 0f;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = grinderSoundVolume;

        if (grinderSoundClip != null)
        {
            audioSource.clip = grinderSoundClip;
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        grabInteractable.activated.AddListener(OnGrinderActivated);
        grabInteractable.deactivated.AddListener(OnGrinderDeactivated);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);

        grabInteractable.activated.RemoveListener(OnGrinderActivated);
        grabInteractable.deactivated.RemoveListener(OnGrinderDeactivated);
    }

    private void Update()
    {
        if (!isGrabbed)
            return;

        if (!isGrinderActive)
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
            SendGrinderHaptic();
            hapticTimer = 0f;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        isGrinderActive = false;

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
            Debug.LogWarning("[GrinderHapticController] 손 방향 판단 실패. Interactor 이름에 Left 또는 Right가 있는지 확인하세요.");
        }

        currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);

        Debug.Log("[GrinderHapticController] 그라인더 잡은 손: " + currentHandNode);

        if (currentDevice.isValid)
        {
            Debug.Log("[GrinderHapticController] 입력 장치 연결됨: " + currentDevice.name);
        }
        else
        {
            Debug.LogWarning("[GrinderHapticController] 입력 장치를 찾지 못했습니다: " + currentHandNode);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        isGrinderActive = false;
        currentDevice = default;

        StopGrinderSound();

        Debug.Log("[GrinderHapticController] 그라인더 놓음");
    }

    private void OnGrinderActivated(ActivateEventArgs args)
    {
        if (!isGrabbed)
            return;

        isGrinderActive = true;
        hapticTimer = hapticInterval;
        hapticLogTimer = hapticLogInterval;

        PlayGrinderSound();

        Debug.Log("[GrinderHapticController] 그라인더 작동 버튼 눌림: " + currentHandNode);
    }

    private void OnGrinderDeactivated(DeactivateEventArgs args)
    {
        isGrinderActive = false;

        StopGrinderSound();

        Debug.Log("[GrinderHapticController] 그라인더 작동 버튼 해제: " + currentHandNode);
    }

    private void PlayGrinderSound()
    {
        if (audioSource == null)
            return;

        if (audioSource.clip == null)
        {
            Debug.LogWarning("[GrinderHapticController] 그라인더 사운드 클립이 연결되지 않았습니다.");
            return;
        }

        audioSource.volume = grinderSoundVolume;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("[GrinderHapticController] 그라인더 사운드 재생");
        }
    }

    private void StopGrinderSound()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[GrinderHapticController] 그라인더 사운드 정지");
        }
    }

    private void SendGrinderHaptic()
    {
        if (!currentDevice.isValid)
        {
            Debug.LogWarning("[GrinderHapticController] 진동 실패: 입력 장치가 유효하지 않습니다.");
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
                        Debug.Log("[GrinderHapticController] 그라인더 진동 전송됨 / 손: "
                                  + currentHandNode
                                  + " / 세기: "
                                  + hapticAmplitude);
                    }
                    else
                    {
                        Debug.LogWarning("[GrinderHapticController] 진동 명령 전송 실패 / 손: " + currentHandNode);
                    }

                    hapticLogTimer = 0f;
                }
            }
            else
            {
                Debug.LogWarning("[GrinderHapticController] 이 컨트롤러는 Haptic Impulse를 지원하지 않습니다: " + currentDevice.name);
            }
        }
        else
        {
            Debug.LogWarning("[GrinderHapticController] HapticCapabilities를 가져오지 못했습니다: " + currentDevice.name);
        }
    }
}