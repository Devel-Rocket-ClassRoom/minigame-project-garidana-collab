using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GameOverUi : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject gameOverWarningText;
    [SerializeField] private Button titleButton;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private TextMeshProUGUI respawnCountDownText;

    public static bool IsSkyDeath { get; set; }

    private bool _isShown;
    private Coroutine _respawnRoutine;

    private void Awake()
    {
        gameOverPanel.SetActive(false);
        titleButton.interactable = false;
        titleButton.onClick.AddListener(RespawnPlayer);
    }

    private void OnEnable()
    {
        if (playerStats != null)
            playerStats.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.Died -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (_isShown) return;

        bool skyDeath = IsSkyDeath;
        IsSkyDeath = false;

        if (gameOverText != null) gameOverText.SetActive(!skyDeath);
        if (gameOverWarningText != null) gameOverWarningText.SetActive(skyDeath);

        _isShown = true;
        _respawnRoutine = StartCoroutine(ShowAndEnableRespawnAfterDelay());
    }

    private IEnumerator ShowAndEnableRespawnAfterDelay()
    {
        gameOverPanel.SetActive(true);
        titleButton.interactable = false;

        float remaining = respawnDelay;
        while (remaining > 0f)
        {
            respawnCountDownText.text = $"{Mathf.CeilToInt(remaining)}";
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        respawnCountDownText.text = "";
        titleButton.interactable = true;
        _respawnRoutine = null;
    }

    private void RespawnPlayer()
    {
        if (playerStats == null) return;

        Transform spawnPoint = WaypointManager.Instance != null
            ? WaypointManager.Instance.GetRespawnPoint()
            : null;

        Vector3 respawnPosition = spawnPoint != null ? spawnPoint.position : playerStats.transform.position;

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        playerStats.RespawnAt(respawnPosition);

        titleButton.interactable = false;
        respawnCountDownText.text = string.Empty;
        gameOverPanel.SetActive(false);
        _isShown = false;
    }

    public static bool IsAnyOpen()
    {
        GameOverUi[] gameOverUis = FindObjectsByType<GameOverUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < gameOverUis.Length; i++)
        {
            if (gameOverUis[i] != null
                && gameOverUis[i].gameOverPanel != null
                && gameOverUis[i].gameOverPanel.activeSelf)
                return true;
        }
        return false;
    }
}
