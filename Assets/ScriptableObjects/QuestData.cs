using UnityEngine;

public enum QuestObjectiveType
{
    KillMonster,
    CollectItem,
    Interact
}

[System.Serializable]
public class QuestReward
{
    [SerializeField]
    private int gold;

    [SerializeField]
    private int level;

    [SerializeField]
    private int exp;

    public int Gold => gold;
    public int Level => level;
    public int Exp => exp;
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("Info")]
    [SerializeField]
    private string questId;

    [SerializeField]
    private string questTitle;

    [TextArea]
    [SerializeField]
    private string description;

    [Header("Objective")]
    [SerializeField]
    private QuestObjectiveType objectiveType;

    [SerializeField]
    private string targetId;

    [SerializeField]
    private int requiredAmount = 1;

    [Header("Progression")]
    [SerializeField]
    private QuestData prerequisiteQuest;

    [SerializeField]
    private QuestData nextQuest;

    [Header("Reward")]
    [SerializeField]
    private QuestReward reward;

    public string QuestId => questId;
    public string QuestTitle => questTitle;
    public string Description => description;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public string TargetId => targetId;
    public int RequiredAmount => requiredAmount;
    public QuestData PrerequisiteQuest => prerequisiteQuest;
    public QuestData NextQuest => nextQuest;
    public QuestReward Reward => reward;

    private void OnValidate()
    {
        requiredAmount = Mathf.Max(1, requiredAmount);
    }
}
