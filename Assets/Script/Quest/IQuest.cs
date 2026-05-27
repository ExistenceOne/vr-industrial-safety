using System;

/// <summary>
/// 모든 퀘스트가 구현해야 하는 인터페이스
/// </summary>
public interface IQuest
{
    event Action OnQuestCompleted;
    void StartQuest(); // QuestManager가 명시적으로 시작시킴
}
