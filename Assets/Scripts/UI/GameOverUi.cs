using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private bool _isShown;

    private void Awake()
    {
        gameOverPanel.SetActive(false);

        titleButton.onClick.AddListener(GoTitle);
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
        StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);

        gameOverPanel.SetActive(true);
        PauseManager.Pause();
    }

    private void GoTitle()
    {
        PauseManager.Resume();
        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.MainTitle);
    }
}
