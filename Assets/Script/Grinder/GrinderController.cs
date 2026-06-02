using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class GrinderController : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    // Blade 트리거 원통 설정
    // ═══════════════════════════════════════════════

    [Header("Trigger 원통 크기 (월드 단위)")]
    public float cylinderRadius = 0.5f;
    public float cylinderHalfHeight = 0.05f;

    [Header("원통 중심 오프셋 (로컬 기준)")]
    public Vector3 centerOffset = Vector3.zero;

    [Header("Grind 깎기 설정")]
    public float grindDepth = 0.05f;
    public float grindInterval = 0.05f;

    [Header("Carve 축 방향 (로컬 기준)")]
    public Vector3 carveAxis = Vector3.forward; // 깎는 중심 방향

    // ═══════════════════════════════════════════════
    // 블레이드 회전 설정
    // ═══════════════════════════════════════════════

    [Header("블레이드 회전 설정")]
    [SerializeField] private Transform bladeTransform;
    [SerializeField] private float bladeRotationSpeed = 1800f;
    [SerializeField] private Vector3 bladeRotationAxis = Vector3.forward;

    // ═══════════════════════════════════════════════
    // 햅틱 설정
    // ═══════════════════════════════════════════════

    [Header("햅틱 진동 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float hapticAmplitude = 0.65f;
    [SerializeField] private float hapticDuration = 0.08f;
    [SerializeField] private float hapticInterval = 0.06f;

    // ═══════════════════════════════════════════════
    // 사운드 설정
    // ═══════════════════════════════════════════════

    [Header("그라인더 사운드 설정")]
    [SerializeField] private AudioClip startClip;
    [SerializeField] private AudioClip loopClip;
    [SerializeField] private AudioClip stopClip;
    [Range(0f, 1f)]
    [SerializeField] private float grindSoundVolume = 0.8f;

    // ═══════════════════════════════════════════════
    // 디버그 설정
    // ═══════════════════════════════════════════════

    [Header("디버그 설정")]
    [SerializeField] private bool showHapticLog = true;
    [SerializeField] private float hapticLogInterval = 0.5f;

    // ═══════════════════════════════════════════════
    // 내부 변수
    // ═══════════════════════════════════════════════

    // XR
    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private InputDevice currentDevice;
    private XRNode currentHandNode = XRNode.RightHand;

    // 상태
    private bool isGrabbed = false;
    private bool isActive = false; // 트리거 누른 상태

    // 타이머
    private float hapticTimer = 0f;
    private float hapticLogTimer = 0f;
    private float carveCooldown = 0f;

    // 사운드
    private AudioSource loopSource1;
    private Coroutine soundCoroutine = null;

    // 카빙 대상
    private List<VoxelObject> targets = new();

    // 월드 기준 원통 중심
    private Vector3 WorldCenter => transform.TransformPoint(centerOffset);

    public bool IsGrabbed => isGrabbed;
    public bool IsActive => isActive;

    // ═══════════════════════════════════════════════
    // 초기화
    // ═══════════════════════════════════════════════

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = grindSoundVolume;

        loopSource1 = gameObject.AddComponent<AudioSource>();
        loopSource1.playOnAwake = false;
        loopSource1.loop = true;
        loopSource1.volume = grindSoundVolume;
        loopSource1.spatialBlend = audioSource.spatialBlend;
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnTriggerPressed);
        grabInteractable.deactivated.AddListener(OnTriggerReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.activated.RemoveListener(OnTriggerPressed);
        grabInteractable.deactivated.RemoveListener(OnTriggerReleased);
    }

    // ═══════════════════════════════════════════════
    // VoxelObject 등록
    // ═══════════════════════════════════════════════

    public void RegisterTarget(VoxelObject target)
    {
        if (!targets.Contains(target))
            targets.Add(target);
    }

    // ═══════════════════════════════════════════════
    // 원통 충돌 판정
    // ═══════════════════════════════════════════════

    public bool IsInsideCylinder(Vector3 worldPos)
    {
        Vector3 diff = worldPos - WorldCenter;
        float alongAxis = Vector3.Dot(diff, transform.forward);
        Vector3 radialVec = diff - alongAxis * transform.forward;
        return Mathf.Abs(alongAxis) < cylinderHalfHeight && radialVec.magnitude < cylinderRadius;
    }

    // ═══════════════════════════════════════════════
    // Update — 블레이드 회전 + 햅틱
    // ═══════════════════════════════════════════════

    private void Update()
    {
        if (!isGrabbed || !isActive) return;

        // 블레이드 Z축 회전
        if (bladeTransform != null)
            bladeTransform.Rotate(bladeRotationAxis.normalized, bladeRotationSpeed * Time.deltaTime, Space.Self);

        // 햅틱 장치 갱신
        if (!currentDevice.isValid)
            currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);

        if (!currentDevice.isValid) return;

        hapticTimer += Time.deltaTime;
        hapticLogTimer += Time.deltaTime;

        if (hapticTimer >= hapticInterval)
        {
            SendHaptic();
            hapticTimer = 0f;
        }
    }

    // ═══════════════════════════════════════════════
    // FixedUpdate — 카빙 (트리거 활성 상태일 때만)
    // ═══════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (!isActive) return;

        carveCooldown -= Time.fixedDeltaTime;
        if (carveCooldown > 0f) return;
        carveCooldown = grindInterval;

        foreach (var target in targets)
        {
            if (target != null)
                target.Carve(WorldCenter, cylinderRadius, grindDepth, transform.TransformDirection(carveAxis.normalized));
        }
    }

    // ═══════════════════════════════════════════════
    // 잡기 / 놓기
    // ═══════════════════════════════════════════════

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        hapticTimer = hapticInterval;
        hapticLogTimer = hapticLogInterval;

        string interactorName = args.interactorObject.transform.name.ToLower();
        if (interactorName.Contains("left"))
            currentHandNode = XRNode.LeftHand;
        else if (interactorName.Contains("right"))
            currentHandNode = XRNode.RightHand;
        else
        {
            currentHandNode = XRNode.RightHand;
            Debug.LogWarning("[GrinderController] 손 방향 판단 실패. Interactor 이름에 Left 또는 Right가 있는지 확인하세요.");
        }

        currentDevice = InputDevices.GetDeviceAtXRNode(currentHandNode);
        Debug.Log("[GrinderController] 그라인더 잡은 손: " + currentHandNode);

        if (currentDevice.isValid)
            Debug.Log("[GrinderController] 입력 장치 연결됨: " + currentDevice.name);
        else
            Debug.LogWarning("[GrinderController] 입력 장치를 찾지 못했습니다: " + currentHandNode);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        bool wasActive = isActive;
        isGrabbed = false;
        isActive = false;
        currentDevice = default;

        if (wasActive)
            StopGrindSound();
        Debug.Log("[GrinderController] 그라인더 놓음");
    }

    // ═══════════════════════════════════════════════
    // 트리거 누름 / 뗌
    // ═══════════════════════════════════════════════

    private void OnTriggerPressed(ActivateEventArgs args)
    {
        if (!isGrabbed) return;

        if (SafetyPracticeManager.Instance != null &&
            SafetyPracticeManager.Instance.TryFailIfNoGlove())
        {
            isActive = false;
            StopGrindSound();

            Debug.Log("[GrinderController] 보호구 미착용으로 그라인더 작업 실패 처리");
            return;
        }

        isActive = true;
        hapticTimer = hapticInterval;
        hapticLogTimer = hapticLogInterval;
        carveCooldown = 0f;

        PlayGrindSound();
        Debug.Log("[GrinderController] 트리거 ON — 블레이드 회전 및 카빙 시작");
    }

    private void OnTriggerReleased(DeactivateEventArgs args)
    {
        isActive = false;

        StopGrindSound();
        Debug.Log("[GrinderController] 트리거 OFF — 블레이드 회전 및 카빙 중단");
    }

    // ═══════════════════════════════════════════════
    // 사운드
    // ═══════════════════════════════════════════════

    private void PlayGrindSound()
    {
        if (audioSource == null) return;

        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);

        soundCoroutine = StartCoroutine(GrindSoundRoutine());
    }

    private void StopGrindSound()
    {
        if (audioSource == null) return;

        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
            soundCoroutine = null;
        }

        soundCoroutine = StartCoroutine(StopSoundRoutine());
    }

    private IEnumerator GrindSoundRoutine()
    {
        audioSource.volume = grindSoundVolume;
        loopSource1.volume = grindSoundVolume;

        double loopStartDsp;

        if (startClip != null)
        {
            double startDsp = AudioSettings.dspTime + 0.05;
            audioSource.loop = false;
            audioSource.clip = startClip;
            audioSource.PlayScheduled(startDsp);
            loopStartDsp = startDsp + startClip.length;
        }
        else
        {
            loopStartDsp = AudioSettings.dspTime + 0.05;
        }

        if (loopClip != null)
        {
            loopSource1.clip = loopClip;
            loopSource1.loop = true;
            loopSource1.PlayScheduled(loopStartDsp);
        }

        yield break;
    }

    private IEnumerator StopSoundRoutine()
    {
        loopSource1.Stop();

        if (stopClip != null)
        {
            audioSource.loop = false;
            audioSource.clip = stopClip;
            audioSource.volume = grindSoundVolume;
            audioSource.Play();
            yield return new WaitForSeconds(stopClip.length);
        }

        audioSource.Stop();
        soundCoroutine = null;
    }

    // ═══════════════════════════════════════════════
    // 햅틱
    // ═══════════════════════════════════════════════

    private void SendHaptic()
    {
        if (!currentDevice.isValid)
        {
            Debug.LogWarning("[GrinderController] 진동 실패: 입력 장치가 유효하지 않습니다.");
            return;
        }

        if (currentDevice.TryGetHapticCapabilities(out HapticCapabilities capabilities))
        {
            if (capabilities.supportsImpulse)
            {
                bool result = currentDevice.SendHapticImpulse(0, hapticAmplitude, hapticDuration);

                if (showHapticLog && hapticLogTimer >= hapticLogInterval)
                {
                    if (result)
                        Debug.Log($"[GrinderController] 진동 전송됨 / 손: {currentHandNode} / 세기: {hapticAmplitude}");
                    else
                        Debug.LogWarning("[GrinderController] 진동 명령 전송 실패 / 손: " + currentHandNode);

                    hapticLogTimer = 0f;
                }
            }
            else
            {
                Debug.LogWarning("[GrinderController] 이 컨트롤러는 Haptic Impulse를 지원하지 않습니다: " + currentDevice.name);
            }
        }
        else
        {
            Debug.LogWarning("[GrinderController] HapticCapabilities를 가져오지 못했습니다: " + currentDevice.name);
        }
    }

    // ═══════════════════════════════════════════════
    // Gizmos — 에디터에서 원통 시각화
    // ═══════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);

        int segments = 32;
        float angleStep = 360f / segments;

        Vector3 frontCenter = WorldCenter + transform.forward * cylinderHalfHeight;
        Vector3 backCenter = WorldCenter + transform.forward * -cylinderHalfHeight;

        Vector3 prevFront = Vector3.zero;
        Vector3 prevBack = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = transform.right * Mathf.Cos(angle) * cylinderRadius
                           + transform.up * Mathf.Sin(angle) * cylinderRadius;

            Vector3 front = frontCenter + offset;
            Vector3 back = backCenter + offset;

            if (i > 0)
            {
                Gizmos.DrawLine(prevFront, front);
                Gizmos.DrawLine(prevBack, back);
            }

            if (i % (segments / 4) == 0)
                Gizmos.DrawLine(front, back);

            prevFront = front;
            prevBack = back;
        }

        // carveAxis 방향 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(WorldCenter, transform.TransformDirection(carveAxis.normalized) * cylinderRadius);
    }
}