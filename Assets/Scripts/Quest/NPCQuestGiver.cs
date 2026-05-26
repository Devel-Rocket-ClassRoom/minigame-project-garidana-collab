using UnityEngine;

public class NPCQuestGiver : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string npcName = "Quest NPC";

    [SerializeField]
    private QuestData questData;

    [SerializeField]
    private QuestUi questUi;

    [Header("State Icons")]
    [SerializeField]
    private GameObject availableIcon;

    [SerializeField]
    private GameObject readyToCompleteIcon;

    [SerializeField]
    private GameObject lockedIcon;

    public string InteractionPrompt
    {
        get
        {
            QuestState state = GetState();

            if (state == QuestState.ReadyToComplete)
            {
                return "퀘스트 완료";
            }

            if (state == QuestState.Available)
            {
                return "퀘스트 확인";
            }

            return npcName;
        }
    }

    public Transform Transform => transform;

    private void Awake()
    {
        if (questUi == null)
        {
            questUi = FindFirstObjectByType<QuestUi>();
        }
    }

    private void OnEnable()
    {
        SubscribeQuestEvents();
        RefreshIcon();
    }

    private void OnDisable()
    {
        UnsubscribeQuestEvents();
    }

    public bool CanInteract(GameObject interactor)
    {
        QuestState state = GetState();
        return state == QuestState.Available
            || state == QuestState.InProgress
            || state == QuestState.ReadyToComplete;
    }

    public void Interact(GameObject interactor)
    {
        QuestManager questManager = QuestManager.Instance;

        if (questManager == null || questData == null)
        {
            return;
        }

        if (questManager.CanComplete(questData))
        {
            questManager.CompleteQuest(questData);
            RefreshIcon();
            return;
        }

        if (questManager.CanAccept(questData))
        {
            if (questUi != null)
            {
                questUi.Open(this, questData);
            }
            else
            {
                questManager.AcceptQuest(questData);
            }

            RefreshIcon();
        }
    }

    public void AcceptQuest()
    {
        QuestManager questManager = QuestManager.Instance;

        if (questManager == null)
        {
            return;
        }

        questManager.AcceptQuest(questData);
        RefreshIcon();
    }

    private QuestState GetState()
    {
        if (QuestManager.Instance == null)
        {
            return QuestState.Locked;
        }

        return QuestManager.Instance.GetQuestState(questData);
    }

    private void SubscribeQuestEvents()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestAccepted += HandleQuestChanged;
        QuestManager.Instance.QuestReadyToComplete += HandleQuestChanged;
        QuestManager.Instance.QuestCompleted += HandleQuestChanged;
    }

    private void UnsubscribeQuestEvents()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestAccepted -= HandleQuestChanged;
        QuestManager.Instance.QuestReadyToComplete -= HandleQuestChanged;
        QuestManager.Instance.QuestCompleted -= HandleQuestChanged;
    }

    private void HandleQuestChanged(QuestData quest)
    {
        RefreshIcon();
    }

    private void RefreshIcon()
    {
        QuestState state = GetState();

        SetActive(availableIcon, state == QuestState.Available);
        SetActive(readyToCompleteIcon, state == QuestState.ReadyToComplete);
        SetActive(lockedIcon, state == QuestState.Locked);
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
