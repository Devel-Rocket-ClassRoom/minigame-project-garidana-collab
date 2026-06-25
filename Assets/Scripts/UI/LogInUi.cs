using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogInUi : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _mainTitlePanel;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signupButton;
    [SerializeField] private Button _quitButton;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _errorText;

    private async UniTaskVoid Start()
    {
        if (_loginButton != null)
        {
            _loginButton.onClick.AddListener(() => OnLoginClicked().Forget());
        }

        if (_signupButton != null)
        {
            _signupButton.onClick.AddListener(() => OnSignupClicked().Forget());
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(QuitGame);
        }

        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);
        AuthManager.Instance.LoginStatusChanged += HandleLoginStatusChanged;
        await RefreshPanelsAsync();
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
        RefreshPanelsAsync().Forget();
    }

    private async UniTask OnLoginClicked()
    {
        string email = _emailInput != null ? _emailInput.text.Trim() : string.Empty;
        string password = _passwordInput != null ? _passwordInput.text : string.Empty;

        if (!ValidateInput(email, password))
        {
            return;
        }

        SetButtonsInteractable(false);
        ClearError();

        var (success, error) = await AuthManager.Instance.SignInUserWithEmailAsync(email, password);
        if (!success)
        {
            ShowError(error);
            SetButtonsInteractable(true);
            return;
        }

        ShowError("로그인 성공 !");
        await RefreshPanelsAsync();
        SetButtonsInteractable(true);
    }

    private async UniTask OnSignupClicked()
    {
        string email = _emailInput != null ? _emailInput.text.Trim() : string.Empty;
        string password = _passwordInput != null ? _passwordInput.text : string.Empty;

        if (!ValidateInput(email, password))
        {
            return;
        }

        SetButtonsInteractable(false);
        ClearError();

        var (success, error) = await AuthManager.Instance.CreateUserWithEmailAsync(email, password);
        if (!success)
        {
            ShowError(error);
            SetButtonsInteractable(true);
            return;
        }

        ShowError("로그인 성공 !");
        await RefreshPanelsAsync();
        SetButtonsInteractable(true);
    }

    private async UniTask RefreshPanelsAsync()
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
    }

    private bool ValidateInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("이메일과 비밀번호를 입력해주세요.");
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        if (_errorText != null)
        {
            _errorText.text = message;
        }
    }

    private void ClearError()
    {
        ShowError(string.Empty);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_loginButton != null)
        {
            _loginButton.interactable = interactable;
        }

        if (_signupButton != null)
        {
            _signupButton.interactable = interactable;
        }
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}



