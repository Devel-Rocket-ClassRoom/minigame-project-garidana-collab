using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GameOverUi : MonoBehaviour
{
    [SerializeField] 
    private PlayerStats playerStats;
    [SerializeField]
    private GameObject gameOverPanel;
    [SerializeField]
    private Button titleButton;
    [SerializeField]
    private float showDelay = 2f;
    [SerializeField]
    private float respawnDelay = 2f;
    [SerializeField]
    private TextMeshProUGUI respawnCountDownText;

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
        {
            playerStats.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.Died -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied()
    {
        if (_isShown) return;

        _isShown = true;
        _respawnRoutine = StartCoroutine(ShowAndEnableRespawnAfterDelay());
    }

    private IEnumerator ShowAndEnableRespawnAfterDelay()
    {
        gameOverPanel.SetActive(true);
        titleButton.interactable = false;
        PauseManager.Pause();

        float remaining = respawnDelay;

        while (remaining > 0f)
        {
            int seconds = Mathf.CeilToInt(remaining);
            respawnCountDownText.text = $"{seconds}";
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        respawnCountDownText.text = "";
        titleButton.interactable = true;
        _respawnRoutine = null;
    }

    private void RespawnPlayer()
    {
        if (playerStats == null)
        {
            return;
        }

        Transform spawnPoint = WaypointManager.Instance != null
            ? WaypointManager.Instance.GetRespawnPoint()
            : null;

        Vector3 respawnPosition = spawnPoint != null ? spawnPoint.position : playerStats.transform.position;

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        PauseManager.Resume();
        playerStats.RespawnAt(respawnPosition);

        titleButton.interactable = false;
        respawnCountDownText.text = string.Empty;
        gameOverPanel.SetActive(false);
        _isShown = false;
    }
}
