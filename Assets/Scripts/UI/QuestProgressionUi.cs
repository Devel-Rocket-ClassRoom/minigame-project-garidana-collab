using TMPro;
using UnityEngine;

public class QuestProgressionUi : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField]
    private TextMeshProUGUI progressText;

    private CanvasGroup canvasGroup;
    private bool subscribed;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        Hide();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (!subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestAccepted -= HandleQuestAccepted;
        QuestManager.Instance.QuestProgressChanged -= HandleQuestProgressChanged;
        QuestManager.Instance.QuestCompleted -= HandleQuestCompleted;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestAccepted += HandleQuestAccepted;
        QuestManager.Instance.QuestProgressChanged += HandleQuestProgressChanged;
        QuestManager.Instance.QuestCompleted += HandleQuestCompleted;
        subscribed = true;
    }

    private void HandleQuestAccepted(QuestData quest)
    {
        UpdateProgress(quest, 0, quest.RequiredAmount);
    }

    private void HandleQuestProgressChanged(QuestData quest, int currentAmount, int requiredAmount)
    {
        UpdateProgress(quest, currentAmount, requiredAmount);
    }

    private void HandleQuestCompleted(QuestData quest)
    {
        Hide();
    }

    private void UpdateProgress(QuestData quest, int currentAmount, int requiredAmount)
    {
        if (quest == null)
        {
            Hide();
            return;
        }

        Show();
        SetText(titleText, quest.QuestTitle);
        SetText(progressText, $"{currentAmount} / {requiredAmount}");
    }

    private void Show()
    {
        canvasGroup.alpha = 1f;
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
