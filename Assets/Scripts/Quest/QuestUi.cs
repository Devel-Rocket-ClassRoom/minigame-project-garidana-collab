using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUi : MonoBehaviour
{
    [SerializeField]
    private GameObject root;

    [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField]
    private TextMeshProUGUI descriptionText;

    [SerializeField]
    private TextMeshProUGUI objectiveText;

    [SerializeField]
    private TextMeshProUGUI rewardText;

    [SerializeField]
    private Button acceptButton;

    [SerializeField]
    private Button closeButton;

    private NPCQuestGiver currentQuestGiver;
    private QuestData currentQuest;

    private void Awake()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(AcceptCurrentQuest);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        Close();
    }

    private void OnDestroy()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveListener(AcceptCurrentQuest);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    public void Open(NPCQuestGiver questGiver, QuestData quest)
    {
        currentQuestGiver = questGiver;
        currentQuest = quest;

        if (root != null)
        {
            root.SetActive(true);
        }

        Refresh();
    }

    public void Close()
    {
        currentQuestGiver = null;
        currentQuest = null;

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void AcceptCurrentQuest()
    {
        if (currentQuestGiver != null)
        {
            currentQuestGiver.AcceptQuest();
        }

        Close();
    }

    private void Refresh()
    {
        if (currentQuest == null)
        {
            return;
        }

        SetText(titleText, currentQuest.QuestTitle);
        SetText(descriptionText, currentQuest.Description);
        SetText(objectiveText, BuildObjectiveText(currentQuest));
        SetText(rewardText, BuildRewardText(currentQuest.Reward));

        if (acceptButton != null)
        {
            acceptButton.interactable = QuestManager.Instance != null
                && QuestManager.Instance.CanAccept(currentQuest);
        }
    }

    private string BuildObjectiveText(QuestData quest)
    {
        string objectiveName = quest.ObjectiveType switch
        {
            QuestObjectiveType.KillMonster => "몬스터 처치",
            QuestObjectiveType.CollectItem => "아이템 수집",
            QuestObjectiveType.Interact => "상호작용",
            _ => "목표"
        };

        return $"{objectiveName}: {quest.TargetId} x {quest.RequiredAmount}";
    }

    private string BuildRewardText(QuestReward reward)
    {
        if (reward == null)
        {
            return "보상 없음";
        }

        return $"보상: {reward.Gold} G / EXP {reward.Exp} / Lv +{reward.Level}";
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
