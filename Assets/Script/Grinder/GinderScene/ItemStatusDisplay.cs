using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 아이템 착용 상태를 감지해서 TextMeshPro에 표시합니다.
/// </summary>
public class ItemStatusDisplay : MonoBehaviour, IQuest
{
    [System.Serializable]
    public class ItemEntry
    {
        public GameObject target;
        public string itemName;
        [HideInInspector]
        public bool isEquipped;
    }

    [Header("아이템 목록")]
    [SerializeField] private List<ItemEntry> items = new();

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
        Debug.Log($"[ItemStatusDisplay] {gameObject.name} 퀘스트 시작");
    }

    private void Update()
    {
        if (isCompleted) return;

        UpdateStatus();
        UpdateDisplay();

        if (items.TrueForAll(item => item.isEquipped))
        {
            isCompleted = true;
            OnQuestCompleted?.Invoke();
            this.enabled = false;
        }
    }

    private void UpdateStatus()
    {
        foreach (var item in items)
        {
            if (item.target == null) continue;
            if (!item.target.activeSelf)
                item.isEquipped = true;
        }
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;

        var sb = new System.Text.StringBuilder();
        foreach (var item in items)
        {
            string status = item.isEquipped ? "착용" : "미착용";
            sb.AppendLine($"{item.itemName} 착용하기 - (상태 : {status})");
        }
        displayText.text = sb.ToString().TrimEnd();
    }
}
