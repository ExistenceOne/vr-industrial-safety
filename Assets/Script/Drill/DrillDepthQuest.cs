using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DrillDepthQuest : MonoBehaviour, IQuest
{
    [System.Serializable]
    public class DepthEntry
    {
        public string itemName;
        public VoxelObject voxelObject;
        [Range(0f, 100f)] public float minPercent = 30f;
        [Range(0f, 100f)] public float maxPercent = 80f;
        [HideInInspector] public bool isAchieved;
    }

    [Header("퀘스트 시작 시 활성화할 오브젝트 목록")]
    [SerializeField] private List<GameObject> activateOnStart = new();

    [Header("드릴 깊이 항목 목록")]
    [SerializeField] private List<DepthEntry> entries = new();

    [Header("레이어당 깎인 것으로 인정할 최소 복셀 수")]
    [SerializeField] private int minCarvedPerLayer = 4;

    [Header("출력 텍스트")]
    [SerializeField] private TextMeshProUGUI displayText;

    public event System.Action OnQuestCompleted;

    private bool isCompleted = false;

    private void Awake()
    {
        this.enabled = false;
    }

    public void StartQuest()
    {
        isCompleted = false;
        this.enabled = true;

        foreach (var obj in activateOnStart)
            if (obj != null) obj.SetActive(true);

        UpdateDisplay();
        Debug.Log($"[DrillDepthQuest] {gameObject.name} 퀘스트 시작");
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

            float ratio = entry.voxelObject.GetCarvedDepthRatio(-entry.voxelObject.transform.forward, minCarvedPerLayer) * 100f;
            bool inRange = ratio >= entry.minPercent && ratio <= entry.maxPercent;

            if (inRange) entry.isAchieved = true;

            string status = entry.isAchieved ? "달성" : $"{entry.minPercent:F0}~{entry.maxPercent:F0}% 목표";
            sb.AppendLine($"{entry.itemName} 깊이 - [{ratio:F1}% : {status}]");
        }
        displayText.text = sb.ToString().TrimEnd();
    }
}
