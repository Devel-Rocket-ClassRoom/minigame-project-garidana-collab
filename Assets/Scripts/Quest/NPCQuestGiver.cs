using UnityEngine;

public class NPCQuestGiver : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string npcName = "퀘스트 NPC";

    [SerializeField]
    private Sprite npcPortrait;

    [TextArea]
    [SerializeField]
    private string availableDialogue = "부탁하고 싶은 일이 있습니다.";

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

    private bool subscribed;

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
                return "퀘스트 진행 가능";
            }

            return npcName;
        }
    }

    public string NpcName => npcName;
    public Sprite NpcPortrait => npcPortrait;
    public string AvailableDialogue => availableDialogue;
    public QuestData QuestData => questData;
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

    private void Start()
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
                PlayerInteractor playerInteractor = interactor.GetComponent<PlayerInteractor>();
                float closeDistance = playerInteractor != null ? playerInteractor.InteractRadius : 2f;
                questUi.Open(this, questData, interactor.transform, closeDistance);
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
        if (subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestAccepted += HandleQuestChanged;
        QuestManager.Instance.QuestReadyToComplete += HandleQuestChanged;
        QuestManager.Instance.QuestCompleted += HandleQuestChanged;

        subscribed = true;
    }

    private void UnsubscribeQuestEvents()
    {
        if (!subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestAccepted -= HandleQuestChanged;
        QuestManager.Instance.QuestReadyToComplete -= HandleQuestChanged;
        QuestManager.Instance.QuestCompleted -= HandleQuestChanged;

        subscribed = false;
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

