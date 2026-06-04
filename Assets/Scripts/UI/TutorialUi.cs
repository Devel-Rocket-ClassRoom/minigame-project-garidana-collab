using UnityEngine;

public class TutorialUi : MonoBehaviour
{
    [SerializeField]
    private GameObject root;

    [SerializeField]
    private string hideAfterQuestId = "first";

    private bool subscribed;

    private void Awake()
    {
        Show();
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshVisibility();
    }

    private void Start()
    {
        TrySubscribe();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (!subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestCompleted -= HandleQuestCompleted;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestCompleted += HandleQuestCompleted;
        subscribed = true;
    }

    private void RefreshVisibility()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        foreach (QuestData completedQuest in QuestManager.Instance.CompletedQuests)
        {
            if (completedQuest != null && completedQuest.QuestId == hideAfterQuestId)
            {
                Hide();
                return;
            }
        }

        Show();
    }

    private void HandleQuestCompleted(QuestData quest)
    {
        if (quest != null && quest.QuestId == hideAfterQuestId)
        {
            Hide();
        }
    }

    private void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }
    }

    private void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }
}