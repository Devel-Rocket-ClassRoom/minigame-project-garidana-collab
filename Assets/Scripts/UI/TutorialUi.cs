using UnityEngine;

public class TutorialUi : MonoBehaviour
{
    private static int suppressCount;

    [SerializeField]
    private GameObject root;

    [SerializeField]
    private string hideAfterQuestId = "first";

    private bool subscribed;

    public static bool IsSuppressed => suppressCount > 0;

    public static void SetSuppressed(bool suppressed)
    {
        suppressCount = Mathf.Max(0, suppressCount + (suppressed ? 1 : -1));

        TutorialUi[] tutorialUis = FindObjectsByType<TutorialUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tutorialUis.Length; i++)
        {
            if (tutorialUis[i] != null)
            {
                tutorialUis[i].RefreshVisibility();
            }
        }
    }

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
        if (IsSuppressed)
        {
            Hide();
            return;
        }

        if (QuestManager.Instance == null)
        {
            Show();
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
