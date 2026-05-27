using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 퀘스트 2: 등록된 물체를 활성화하고 VoxelObject 절단 진행도를 표시합니다.
/// </summary>
public class GrindQuest : MonoBehaviour, IQuest
{
    [System.Serializable]
    public class VoxelEntry
    {
        public string itemName;
        public VoxelObject voxelObject;
        [Range(1f, 100f)]
        public float targetPercent = 50f;
        [HideInInspector]
        public bool isAchieved;
    }

    [Header("퀘스트 시작 시 활성화할 오브젝트 목록")]
    [SerializeField] private List<GameObject> activateOnStart = new();

    [Header("복셀 절단 항목 목록")]
    [SerializeField] private List<VoxelEntry> entries = new();

    [Header("출력 텍스트")]
    [SerializeField] private TextMeshProUGUI displayText;

    public event System.Action OnQuestCompleted;

    private bool isCompleted = false;

    private void Awake()
    {
        // 퀘스트 시작 전까지 비활성화
        this.enabled = false;
    }

    public void StartQuest()
    {
        isCompleted = false;
        this.enabled = true;

        foreach (var obj in activateOnStart)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        UpdateDisplay(); // 첫 프레임 전에 텍스트 즉시 표시
        Debug.Log($"[GrindQuest] {gameObject.name} 퀘스트 시작");
    }

    private void Update()
    {
        if (isCompleted) return;

        UpdateDisplay();

        if (entries.TrueForAll(e => e.isAchieved))
        {
            isCompleted = true;
            OnQuestCompleted?.Invoke();
            this.enabled = false;
        }
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;

        var sb = new System.Text.StringBuilder();
        foreach (var entry in entries)
        {
            if (entry.voxelObject == null) continue;

            float carvedPercent = entry.voxelObject.GetCarvedRatio() * 100f;

            if (carvedPercent >= entry.targetPercent)
                entry.isAchieved = true;

            // 목표 % 기준으로 100분위 환산 (초과 시 100% 고정)
            float displayPercent = Mathf.Min(100f, carvedPercent / entry.targetPercent * 100f);

            string status = entry.isAchieved ? "달성" : "미달성";
            sb.AppendLine($"{entry.itemName} 절단 정도 - [{displayPercent:F1}% : {status}]");
        }
        displayText.text = sb.ToString().TrimEnd();
    }
}
