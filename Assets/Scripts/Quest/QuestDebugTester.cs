using UnityEngine;

public class QuestDebugTester : MonoBehaviour
{
    [ContextMenu("Report Test Kill")]
    private void ReportTestKill()
    {
        QuestManager.Instance?.ReportKill("test_monster");
        Debug.Log("테스트 처치 보고: test_monster");
    }

    [ContextMenu("Print Current Quest")]
    private void PrintCurrentQuest()
    {
        QuestManager manager = QuestManager.Instance;

        if (manager == null)
        {
            Debug.Log("QuestManager가 없습니다.");
            return;
        }

        if (!manager.HasCurrentQuest)
        {
            Debug.Log("현재 진행 중인 퀘스트가 없습니다.");
            return;
        }

        Debug.Log($"현재 퀘스트: {manager.CurrentQuest.QuestTitle}, 진행도: {manager.CurrentAmount}/{manager.CurrentQuest.RequiredAmount}");
    }

    [ContextMenu("Report Test Kill 10 Times")]
    private void ReportTestKill10Times()
    {
        for (int i = 0; i < 10; i++)
        {
            QuestManager.Instance?.ReportKill("test_monster");
        }

        Debug.Log("테스트 처치 10회 보고: test_monster");
    }
}