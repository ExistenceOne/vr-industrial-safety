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

    [Header("사운드 설정")]
    [Tooltip("비트 파손 시 재생될 금속 부러지는 효과음 (DrillBitSnap)")]
    [SerializeField] private AudioClip bitSnapSound;
    [Tooltip("피 연출 시 재생될 효과음 (BloodSquirt)")]
    [SerializeField] private AudioClip bloodSquirtSound;
    [Tooltip("페이드아웃 되며 쓰러질 때 재생될 비명 소리 (ScreamMale)")]
    [SerializeField] private AudioClip screamSound;
    [Tooltip("화면이 어두워질 때 함께 재생되는 페이드아웃 효과음 (FadeOut)")]
    [SerializeField] private AudioClip fadeOutSound;
    [Tooltip("모든 사고 효과음 볼륨 (0~1)")]
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.5f;

    [Header("사고 설정값")]
    [Tooltip("복셀과 드릴의 각도가 이 수치(도)를 넘어가면 비트 파손 발생")]
    [SerializeField] private float maxSafeAngle = 15f;
    [Tooltip("각도를 검사할 복셀 오브젝트를 연결하세요")]
    [SerializeField] private VoxelObject targetVoxel;
    [Tooltip("사고 발생 시 표시할 실패 메시지")]
    [SerializeField, TextArea(4, 10)]
    private string failMessage =
    "비트 파손!\n" +
    "파손된 드릴 비트 파편이 눈을 가격했습니다.";

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
        if (isAccidentTriggered || !isDrillRunning || drillTip == null || playerHead == null || targetVoxel == null) return;

        Debug.DrawRay(drillTip.position, drillTip.forward * 0.1f, Color.red);

        var col = targetVoxel.GetComponent<Collider>();
        if (col == null) return;

        if (Physics.Raycast(drillTip.position, drillTip.forward, out RaycastHit hit, 0.1f))
        {
            if (hit.collider != col) return;

            // 복셀 표면 수직 방향(-transform.forward)과 드릴 방향 사이 각도
            float angle = Vector3.Angle(-targetVoxel.transform.forward, drillTip.forward);

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

        // 피 분출 이펙트 생성 (눈 부상 연출)
        if (bloodEffectPrefab != null)
        {
            Instantiate(bloodEffectPrefab, playerHead.position, playerHead.rotation);
        }

        // 사운드 재생
        if (bitSnapSound != null)
            AudioSource.PlayClipAtPoint(bitSnapSound, drillTip.position, soundVolume);
        if (bloodSquirtSound != null)
            AudioSource.PlayClipAtPoint(bloodSquirtSound, playerHead.position, soundVolume);
        if (screamSound != null)
            AudioSource.PlayClipAtPoint(screamSound, playerHead.position, soundVolume);
        if (fadeOutSound != null)
            AudioSource.PlayClipAtPoint(fadeOutSound, playerHead.position, soundVolume);

        // 실패 처리 (토스트 + 페이드아웃 + 씬 재시작)
        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways(failMessage);
    }
}