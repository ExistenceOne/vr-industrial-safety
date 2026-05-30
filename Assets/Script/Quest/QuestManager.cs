using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IQuest를 구현한 스크립트를 순서대로 실행합니다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    [Header("퀘스트 목록 (순서대로 실행)")]
    [SerializeField] private List<MonoBehaviour> quests = new();

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

        var quest = quests[index] as IQuest;
        if (quest == null)
        {
            Debug.LogError($"[QuestManager] quests[{index}] 가 IQuest를 구현하지 않았습니다.");
            return;
        }

        currentQuestIndex = index;
        quest.OnQuestCompleted += OnCurrentQuestCleared;
        quest.StartQuest(); // 명시적으로 퀘스트 시작
        Debug.Log($"[QuestManager] 퀘스트 {currentQuestIndex + 1} 시작");
    }

    private void OnCurrentQuestCleared()
    {
        var quest = quests[currentQuestIndex] as IQuest;
        if (quest != null)
            quest.OnQuestCompleted -= OnCurrentQuestCleared;

        Debug.Log($"[QuestManager] 퀘스트 {currentQuestIndex + 1} 클리어!");
        StartQuest(currentQuestIndex + 1);
    }
}
