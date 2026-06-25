using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainTitleUi : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _donationButton;
    [SerializeField] private Button _deleteSaveButton;
    [SerializeField] private Button _logoutButton;
    [SerializeField] private TextMeshProUGUI _currentUserId;
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _mainTitlePanel;

    private async UniTaskVoid Start()
    {
        SoundManager.Instance?.PlayBGM(SoundManager.BGMType.MainTitle);

        if (_startButton != null)
        {
            _startButton.onClick.AddListener(() => OnStartGame().Forget());
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnQuit);
        }

        if (_donationButton != null)
        {
            _donationButton.onClick.AddListener(OnClickSiteButton);
        }

        if (_deleteSaveButton != null)
        {
            _deleteSaveButton.onClick.AddListener(() => OnDeleteSave().Forget());
        }

        if (_logoutButton != null)
        {
            _logoutButton.onClick.AddListener(OnLogout);
        }

        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);
        AuthManager.Instance.LoginStatusChanged += HandleLoginStatusChanged;
        await RefreshAuthUiAsync();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.LoginStatusChanged -= HandleLoginStatusChanged;
        }
    }

    private void HandleLoginStatusChanged(bool isLoggedIn)
    {
        RefreshAuthUiAsync().Forget();
    }

    private async UniTask OnStartGame()
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
        {
            await RefreshAuthUiAsync();
            return;
        }

        if (SaveManager.Instance != null)
        {
            await SaveManager.Instance.LoadCloudSaveAsync();
        }

        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.Game);
    }

    private void OnClickSiteButton()
    {
        Application.OpenURL("https://www.notion.so/Donation-3749e10e86f78020908afe430ac49e2a?source=copy_link");
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private async UniTask OnDeleteSave()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        await SaveManager.Instance.DeleteCloudSaveAsync();
        RefreshDeleteSaveButton();
    }

    private void OnLogout()
    {
        AuthManager.Instance?.SignOut();
    }

    private async UniTask RefreshAuthUiAsync()
    {
        bool isLoggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;

        if (isLoggedIn && SaveManager.Instance != null)
        {
            await SaveManager.Instance.LoadCloudSaveAsync();
        }

        if (_loginPanel != null)
        {
            _loginPanel.SetActive(!isLoggedIn);
        }

        if (_mainTitlePanel != null)
        {
            _mainTitlePanel.SetActive(isLoggedIn);
        }

        if (_currentUserId != null)
        {
            _currentUserId.text = isLoggedIn ? AuthManager.Instance.Email : string.Empty;
        }

        RefreshDeleteSaveButton();
    }

    private void RefreshDeleteSaveButton()
    {
        if (_deleteSaveButton == null)
        {
            return;
        }

        _deleteSaveButton.interactable = AuthManager.Instance != null
            && AuthManager.Instance.IsLoggedIn
            && SaveManager.Instance != null
            && SaveManager.Instance.HasSaveData();
    }
}


