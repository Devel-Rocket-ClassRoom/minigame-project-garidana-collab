using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField]
    private PlayerStats playerStats;

    private readonly HashSet<string> completedQuestIds = new HashSet<string>();

    private QuestData currentQuest;
    private int currentAmount;

    public QuestData CurrentQuest => currentQuest;
    public int CurrentAmount => currentAmount;
    public bool HasCurrentQuest => currentQuest != null;

    public event Action<QuestData> QuestAccepted;
    public event Action<QuestData, int, int> QuestProgressChanged;
    public event Action<QuestData> QuestReadyToComplete;
    public event Action<QuestData> QuestCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }
    }

    public QuestState GetQuestState(QuestData quest)
    {
        if (quest == null)
        {
            return QuestState.Locked;
        }

        if (IsCompleted(quest))
        {
            return QuestState.Completed;
        }

        if (currentQuest == quest)
        {
            return IsCurrentQuestComplete() ? QuestState.ReadyToComplete : QuestState.InProgress;
        }

        if (!IsUnlocked(quest) || currentQuest != null)
        {
            return QuestState.Locked;
        }

        return QuestState.Available;
    }

    public bool CanAccept(QuestData quest)
    {
        return GetQuestState(quest) == QuestState.Available;
    }

    public bool AcceptQuest(QuestData quest)
    {
        if (!CanAccept(quest))
        {
            return false;
        }

        currentQuest = quest;
        currentAmount = 0;

        QuestAccepted?.Invoke(currentQuest);
        QuestProgressChanged?.Invoke(currentQuest, currentAmount, currentQuest.RequiredAmount);
        return true;
    }

    public void ReportKill(string monsterId)
    {
        ReportProgress(QuestObjectiveType.KillMonster, monsterId, 1);
    }

    public void ReportItemCollected(string itemId, int amount = 1)
    {
        ReportProgress(QuestObjectiveType.CollectItem, itemId, amount);
    }

    public void ReportInteraction(string targetId)
    {
        ReportProgress(QuestObjectiveType.Interact, targetId, 1);
    }

    public bool CanComplete(QuestData quest)
    {
        return currentQuest == quest && IsCurrentQuestComplete();
    }

    public bool CompleteQuest(QuestData quest)
    {
        if (!CanComplete(quest))
        {
            return false;
        }

        ApplyReward(quest.Reward);
        completedQuestIds.Add(quest.QuestId);

        QuestData completedQuest = currentQuest;
        currentQuest = null;
        currentAmount = 0;

        QuestCompleted?.Invoke(completedQuest);
        return true;
    }

    public bool IsCompleted(QuestData quest)
    {
        return quest != null && completedQuestIds.Contains(quest.QuestId);
    }

    public bool IsCurrentQuestTarget(QuestObjectiveType objectiveType, string targetId)
    {
        return currentQuest != null
            && currentQuest.ObjectiveType == objectiveType
            && currentQuest.TargetId == targetId;
    }

    private void ReportProgress(QuestObjectiveType objectiveType, string targetId, int amount)
    {
        if (!IsCurrentQuestTarget(objectiveType, targetId) || amount <= 0)
        {
            return;
        }

        if (IsCurrentQuestComplete())
        {
            return;
        }

        currentAmount = Mathf.Min(currentAmount + amount, currentQuest.RequiredAmount);
        QuestProgressChanged?.Invoke(currentQuest, currentAmount, currentQuest.RequiredAmount);

        if (IsCurrentQuestComplete())
        {
            QuestReadyToComplete?.Invoke(currentQuest);
        }
    }

    private bool IsUnlocked(QuestData quest)
    {
        QuestData prerequisite = quest.PrerequisiteQuest;
        return prerequisite == null || IsCompleted(prerequisite);
    }

    private bool IsCurrentQuestComplete()
    {
        return currentQuest != null && currentAmount >= currentQuest.RequiredAmount;
    }

    private void ApplyReward(QuestReward reward)
    {
        if (reward == null || playerStats == null)
        {
            return;
        }

        if (reward.Gold > 0)
        {
            playerStats.AddGold(reward.Gold);
        }

        if (reward.Exp > 0)
        {
            playerStats.AddExp(reward.Exp);
        }

        if (reward.Level > 0)
        {
            playerStats.AddLevel(reward.Level);
        }
    }
}
