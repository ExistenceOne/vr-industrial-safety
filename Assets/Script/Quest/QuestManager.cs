using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestEntry
{
    public MonoBehaviour quest;
    public bool showSuccessToast = false;
    [TextArea(1, 2)]
    public string successMessage = "";
}

/// <summary>
/// IQuest를 구현한 스크립트를 순서대로 실행합니다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    [Header("퀘스트 목록 (순서대로 실행)")]
    [SerializeField] private List<QuestEntry> quests = new();

    [Header("Toast")]
    [SerializeField] private ToastMessageController toastController;

    private int currentQuestIndex = 0;

    private void Start()
    {
        StartQuest(0);
    }

    private void StartQuest(int index)
    {
        if (index >= quests.Count)
        {
            Debug.Log("[QuestManager] 모든 퀘스트 완료!");
            return;
        }

        var entry = quests[index];
        var quest = entry.quest as IQuest;
        if (quest == null)
        {
            Debug.LogError($"[QuestManager] quests[{index}] 가 IQuest를 구현하지 않았습니다.");
            return;
        }

        currentQuestIndex = index;
        quest.OnQuestCompleted += OnCurrentQuestCleared;
        quest.StartQuest();
        Debug.Log($"[QuestManager] 퀘스트 {currentQuestIndex + 1} 시작");
    }

    private void OnCurrentQuestCleared()
    {
        var entry = quests[currentQuestIndex];
        var quest = entry.quest as IQuest;
        if (quest != null)
            quest.OnQuestCompleted -= OnCurrentQuestCleared;

        if (entry.showSuccessToast && toastController != null && !string.IsNullOrEmpty(entry.successMessage))
            toastController.ShowSuccessToast(entry.successMessage);

        Debug.Log($"[QuestManager] 퀘스트 {currentQuestIndex + 1} 클리어!");
        StartQuest(currentQuestIndex + 1);
    }
}
