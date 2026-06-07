using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EndGameUi : MonoBehaviour
{
    [SerializeField] private QuestData finalQuest;
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private Button titleButton;

    private PlayerInput playerInput;
    private Coroutine showRoutine;
    private bool subscribed;
    private bool isShown;

    private void Awake()
    {
        playerInput = FindFirstObjectByType<PlayerInput>();

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(ReturnToTitle);
        }
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
        Unsubscribe();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(ReturnToTitle);
        }
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

    private void Unsubscribe()
    {
        if (!subscribed || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestCompleted -= HandleQuestCompleted;
        subscribed = false;
    }

    private void HandleQuestCompleted(QuestData completedQuest)
    {
        if (isShown
            || completedQuest == null
            || completedQuest != finalQuest
            || (SaveManager.Instance != null && SaveManager.Instance.IsApplyingSave))
        {
            return;
        }

        if (showRoutine == null)
        {
            showRoutine = StartCoroutine(ShowAfterQuestUiCloses());
        }
    }

    private IEnumerator ShowAfterQuestUiCloses()
    {
        yield return null;

        showRoutine = null;
        isShown = true;

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        playerInput?.DeactivateInput();
        PauseManager.Pause();
    }

    private void ReturnToTitle()
    {
        PauseManager.Resume();
        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.MainTitle);
    }

    public static bool IsAnyOpen()
    {
        EndGameUi[] endGameUis = FindObjectsByType<EndGameUi>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < endGameUis.Length; i++)
        {
            if (endGameUis[i] != null
                && endGameUis[i].endGamePanel != null
                && endGameUis[i].endGamePanel.activeSelf)
            {
                return true;
            }
        }

        return false;
    }
}
