using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillDepthQuest : MonoBehaviour, IQuest
{
    [System.Serializable]
    public class DepthEntry
    {
        public string itemName;
        public VoxelObject voxelObject;

        [Header("100%로 인정할 깎임 비율")]
        [Tooltip("0.08 = 전체 복셀 값 기준 8% 감소 시 퀘스트 달성률 100%")]
        [Range(0.01f, 1f)]
        public float targetCarvedRatio = 0.08f;

        [HideInInspector] public bool isAchieved;
        [HideInInspector] public bool isFailed;
    }

    [Header("퀘스트 시작 시 활성화할 오브젝트 목록")]
    [SerializeField] private List<GameObject> activateOnStart = new();

    [Header("드릴 깊이 항목 목록")]
    [SerializeField] private List<DepthEntry> entries = new();

    [Header("진행률 보정")]
    [Tooltip("퀘스트 달성률 상승 속도 배율입니다. 1 = 기본, 2 = 2배 빠르게, 10 = 10배 빠르게")]
    [SerializeField, Range(0.1f, 100f)]
    private float progressSpeedMultiplier = 1f;

    [Tooltip("진행률 최소 증가 단위입니다. 1이면 UI상 1% 단위처럼 보이게 됩니다.")]
    [SerializeField, Range(0f, 10f)]
    private float progressStepPercent = 1f;

    [Header("100% 달성 후 판정 설정")]
    [Tooltip("100% 달성 후 이 시간 뒤에 성공/실패를 판정합니다.")]
    [SerializeField] private float successDelay = 1f;

    [Tooltip("체크 시 100% 이후에도 드릴 트리거가 눌려 있으면 과천공 실패로 처리합니다.")]
    [SerializeField] private bool failIfTriggerHeldAfterTarget = true;

    [Header("성공 메시지")]
    [SerializeField, TextArea(3, 8)]
    private string successMessage =
        "안전 깊이 천공 완료!\n" +
        "지정된 깊이까지만 천공하여\n" +
        "벽 내부 전선 손상을 방지했습니다.";

    [Header("과천공 실패 사운드")]
    [SerializeField] private AudioClip overDrillFailSound;

    [Header("과천공 실패 메시지")]
    [SerializeField, TextArea(4, 10)]
    private string overDrillFailMessage =
        "지정 깊이를 초과하여 천공했습니다.\n" +
        "벽 내부 전선을 손상시켜 감전 사고가 발생했습니다.\n\n" +
        "최근 공개된 국내 전기재해 통계에 따르면\n" +
        "2024년 감전사고 인명피해는 371명,\n" +
        "이 중 28명이 사망했습니다.\n\n" +
        "벽 내부 전선은 육안으로 확인하기 어렵기 때문에,\n" +
        "깊이 확인 없는 천공은 작은 부주의도\n" +
        "치명적인 감전 사고로 이어질 수 있습니다.";

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    public event System.Action OnQuestCompleted;

    private bool isCompleted = false;
    private bool isFailed = false;
    private bool isSuccessPending = false;

    private bool isDrillTriggerHeld = false;

    private Coroutine successRoutine;

    private void Awake()
    {
        this.enabled = false;
    }

    public void StartQuest()
    {
        isCompleted = false;
        isFailed = false;
        isSuccessPending = false;
        isDrillTriggerHeld = false;

        this.enabled = true;

        if (successRoutine != null)
        {
            StopCoroutine(successRoutine);
            successRoutine = null;
        }

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            entry.isAchieved = false;
            entry.isFailed = false;
        }

        foreach (var obj in activateOnStart)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        UpdateQuestProgress();

        if (showDebugLog)
        {
            Debug.Log($"[DrillDepthQuest] {gameObject.name} 퀘스트 시작");
        }
    }

    private void Update()
    {
        if (isCompleted || isFailed)
            return;

        UpdateQuestProgress();
    }

    private void UpdateQuestProgress()
    {
        if (entries == null || entries.Count == 0)
            return;

        float highestProgress01 = 0f;

        foreach (var entry in entries)
        {
            if (entry == null || entry.voxelObject == null)
                continue;

            float carvedRatio = entry.voxelObject.GetCarvedDensityRatio();
            float targetRatio = Mathf.Max(0.01f, entry.targetCarvedRatio);

            float rawProgress01 = carvedRatio / targetRatio;
            float boostedProgress01 = rawProgress01 * Mathf.Max(0.1f, progressSpeedMultiplier);
            float progress01 = Mathf.Clamp01(boostedProgress01);

            if (progressStepPercent > 0f)
            {
                float progressPercent = progress01 * 100f;
                progressPercent = Mathf.Floor(progressPercent / progressStepPercent) * progressStepPercent;
                progress01 = Mathf.Clamp01(progressPercent / 100f);
            }

            if (progress01 > highestProgress01)
            {
                highestProgress01 = progress01;
            }

            if (showDebugLog)
            {
                Debug.Log(
                    $"[DrillDepthQuest] {entry.itemName} 깎임 비율: {(carvedRatio * 100f):F2}% / " +
                    $"목표 깎임 비율: {(targetRatio * 100f):F1}% / " +
                    $"진행률 배율: {progressSpeedMultiplier:F1} / " +
                    $"퀘스트 달성률: {(progress01 * 100f):F1}% / " +
                    $"트리거 상태: {(isDrillTriggerHeld ? "ON" : "OFF")}"
                );
            }

            if (progress01 >= 1f)
            {
                entry.isAchieved = true;

                if (!isSuccessPending)
                {
                    StartSuccessDelay();
                }
            }
        }

        SafetyPracticeManager.Instance?.SetLiveQuestProgressByObject(highestProgress01);
    }

    private void StartSuccessDelay()
    {
        if (isSuccessPending || isCompleted || isFailed)
            return;

        isSuccessPending = true;

        if (successRoutine != null)
        {
            StopCoroutine(successRoutine);
        }

        successRoutine = StartCoroutine(SuccessDelayRoutine());

        if (showDebugLog)
        {
            Debug.Log($"[DrillDepthQuest] 100% 달성. {successDelay:F1}초 후 트리거 상태로 성공/실패 판정");
        }
    }

    private IEnumerator SuccessDelayRoutine()
    {
        yield return new WaitForSeconds(successDelay);

        if (isCompleted || isFailed)
            yield break;

        if (failIfTriggerHeldAfterTarget && isDrillTriggerHeld)
        {
            if (showDebugLog)
            {
                Debug.Log("[DrillDepthQuest] 100% 이후에도 드릴 트리거 유지 → 과천공 실패");
            }

            FailByOverDrill(null, 100f);
            yield break;
        }

        if (entries.TrueForAll(e => e.isAchieved))
        {
            CompleteQuest();
        }
        else
        {
            isSuccessPending = false;
        }

        successRoutine = null;
    }

    public void SetDrillTriggerHeld(bool isHeld)
    {
        if (isCompleted || isFailed)
            return;

        isDrillTriggerHeld = isHeld;

        if (showDebugLog)
        {
            Debug.Log($"[DrillDepthQuest] 드릴 트리거 상태 변경: {(isHeld ? "ON" : "OFF")}");
        }
    }

    public void NotifyDrillingContact()
    {
        if (isCompleted || isFailed)
            return;

        if (!isSuccessPending)
            return;

        if (showDebugLog)
        {
            Debug.Log("[DrillDepthQuest] 100% 도달 후 드릴 접촉 감지");
        }
    }

    private void CompleteQuest()
    {
        if (isCompleted || isFailed)
            return;

        isCompleted = true;

        SafetyPracticeManager.Instance?.SetLiveQuestProgressByObject(1f);

        if (SafetyPracticeManager.Instance != null)
        {
            SafetyPracticeManager.Instance.ShowPassMessage(successMessage, 3f);
            SafetyPracticeManager.Instance.AddQuestProgress();
        }

        OnQuestCompleted?.Invoke();
        this.enabled = false;

        if (showDebugLog)
        {
            Debug.Log("[DrillDepthQuest] 드릴 깊이 퀘스트 완료");
        }
    }

    private void FailByOverDrill(DepthEntry entry, float currentPercent)
    {
        if (isFailed)
            return;

        isFailed = true;

        if (successRoutine != null)
        {
            StopCoroutine(successRoutine);
            successRoutine = null;
        }

        if (entry != null)
        {
            entry.isFailed = true;
        }

        if (showDebugLog)
        {
            Debug.Log($"[DrillDepthQuest] 과천공 실패 발생 / 현재 진행률: {currentPercent:F1}%");
        }

        if (overDrillFailSound != null)
            AudioSource.PlayClipAtPoint(overDrillFailSound, transform.position);

        SafetyPracticeManager.Instance?.TryFailAlways(overDrillFailMessage);

        this.enabled = false;
    }
}