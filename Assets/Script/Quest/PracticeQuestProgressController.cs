using UnityEngine;
using TMPro;

public class PracticeQuestProgressController : MonoBehaviour
{
    public enum PracticeType
    {
        Hammer,
        Drill,
        Grinder
    }

    [Header("Practice Type")]
    [SerializeField] private PracticeType practiceType = PracticeType.Hammer;

    [Header("Quest UI")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questProgressText;

    [Header("Hammer Quest Settings")]
    [SerializeField] private string hammerQuestTitle = "메인 퀘스트";
    [SerializeField] private string hammerQuestContent = "못 박기";
    [SerializeField] private int hammerTargetCount = 5;

    [Header("Drill Quest Settings")]
    [SerializeField] private string drillQuestTitle = "메인 퀘스트";
    [SerializeField] private string drillQuestContent = "드릴 실습";
    [SerializeField] private int drillTargetCount = 1;

    [Header("Grinder Quest Settings")]
    [SerializeField] private string grinderQuestTitle = "메인 퀘스트";
    [SerializeField] private string grinderQuestContent = "그라인더 실습";
    [SerializeField] private int grinderTargetCount = 1;

    private int currentCount = 0;
    private int targetCount = 1;
    private string currentQuestTitle;
    private string currentQuestContent;

    private void Start()
    {
        SetupQuestByPracticeType();
        UpdateQuestUI();
    }

    private void SetupQuestByPracticeType()
    {
        switch (practiceType)
        {
            case PracticeType.Hammer:
                currentQuestTitle = hammerQuestTitle;
                currentQuestContent = hammerQuestContent;
                targetCount = hammerTargetCount;
                break;

            case PracticeType.Drill:
                currentQuestTitle = drillQuestTitle;
                currentQuestContent = drillQuestContent;
                targetCount = drillTargetCount;
                break;

            case PracticeType.Grinder:
                currentQuestTitle = grinderQuestTitle;
                currentQuestContent = grinderQuestContent;
                targetCount = grinderTargetCount;
                break;
        }

        if (targetCount <= 0)
        {
            targetCount = 1;
        }
    }

    public void AddProgress()
    {
        currentCount++;

        if (currentCount > targetCount)
        {
            currentCount = targetCount;
        }

        UpdateQuestUI();
    }

    public void SetProgress(int count)
    {
        currentCount = Mathf.Clamp(count, 0, targetCount);
        UpdateQuestUI();
    }

    public void ResetProgress()
    {
        currentCount = 0;
        UpdateQuestUI();
    }

    private void UpdateQuestUI()
    {
        int progressPercent = Mathf.RoundToInt((float)currentCount / targetCount * 100f);

        if (questTitleText != null)
        {
            questTitleText.text = currentQuestTitle;
        }

        if (questProgressText != null)
        {
            questProgressText.text =
                $"{currentQuestContent} {currentCount} / {targetCount}\n달성률 {progressPercent}%";
        }
    }

    public bool IsQuestComplete()
    {
        return currentCount >= targetCount;
    }
}