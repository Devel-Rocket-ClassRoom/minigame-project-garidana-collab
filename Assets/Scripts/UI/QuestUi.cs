using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuestUi : MonoBehaviour
{
    [SerializeField]
    private GameObject root;

    [Header("NPC")]
    [SerializeField]
    private TextMeshProUGUI npcNameText;

    [SerializeField]
    private Image npcPortraitImage;

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [Header("Quest")]
    [SerializeField]
    private TextMeshProUGUI chapterText;

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

    [SerializeField]
    private TextMeshProUGUI acceptButtonText;

    private NPCQuestGiver currentQuestGiver;
    private QuestData currentQuest;
    private Transform currentInteractor;
    private float closeDistance;

    private void Awake()
    {
        if (acceptButtonText == null && acceptButton != null)
        {
            acceptButtonText = acceptButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

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

    private void Update()
    {
        if (!IsOpen())
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (keyboard != null
            && ((keyboard.enterKey != null && keyboard.enterKey.wasPressedThisFrame)
            || (keyboard.numpadEnterKey != null && keyboard.numpadEnterKey.wasPressedThisFrame)))
        {
            AcceptCurrentQuest();
            return;
        }

        if (IsInteractorOutOfRange())
        {
            Close();
        }
    }

    public void Open(NPCQuestGiver questGiver, QuestData quest)
    {
        Open(questGiver, quest, null, 0f);
    }

    public void Open(NPCQuestGiver questGiver, QuestData quest, Transform interactor, float interactionRange)
    {
        currentQuestGiver = questGiver;
        currentQuest = quest;
        currentInteractor = interactor;
        closeDistance = interactionRange;

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
        currentInteractor = null;
        closeDistance = 0f;

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void AcceptCurrentQuest()
    {
        if (currentQuestGiver == null || currentQuest == null || QuestManager.Instance == null)
        {
            Close();
            return;
        }

        if (QuestManager.Instance.CanComplete(currentQuest))
        {
            QuestManager.Instance.CompleteQuest(currentQuest);
        }
        else if (QuestManager.Instance.CanAccept(currentQuest))
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

        bool canComplete = QuestManager.Instance != null && QuestManager.Instance.CanComplete(currentQuest);
        bool canAccept = QuestManager.Instance != null && QuestManager.Instance.CanAccept(currentQuest);

        SetText(npcNameText, currentQuestGiver != null ? currentQuestGiver.NpcName : string.Empty);
        SetText(dialogueText, currentQuestGiver != null ? currentQuestGiver.GetDialogueForCurrentState() : string.Empty);
        SetPortrait(currentQuestGiver != null ? currentQuestGiver.NpcPortrait : null);

        SetText(chapterText, BuildChapterText(currentQuest));
        SetText(titleText, currentQuest.QuestTitle);
        SetText(descriptionText, canComplete ? currentQuest.CompletionText : currentQuest.Description);
        SetText(objectiveText, BuildObjectiveText(currentQuest));
        SetText(rewardText, BuildRewardText(currentQuest.Reward));

        if (acceptButton != null)
        {
            acceptButton.interactable = canAccept || canComplete;
        }

        SetText(acceptButtonText, canComplete ? "[Enter] 완료" : "[Enter] 수락");
    }

    private bool IsOpen()
    {
        return root != null && root.activeSelf;
    }

    private bool IsInteractorOutOfRange()
    {
        if (currentQuestGiver == null || currentInteractor == null || closeDistance <= 0f)
        {
            return false;
        }

        return Vector3.Distance(currentInteractor.position, currentQuestGiver.Transform.position) > closeDistance;
    }

    private string BuildChapterText(QuestData quest)
    {
        if (quest == null)
        {
            return string.Empty;
        }

        return $"챕터 {quest.Chapter}";
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

    private void SetPortrait(Sprite portrait)
    {
        if (npcPortraitImage == null)
        {
            return;
        }

        npcPortraitImage.sprite = portrait;
        npcPortraitImage.enabled = portrait != null;
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    public static bool IsAnyOpen()
    {
        QuestUi[] questUis = FindObjectsByType<QuestUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < questUis.Length; i++)
        {
            if (questUis[i] != null && questUis[i].IsOpen())
            {
                return true;
            }
        }

        return false;
    }
}
