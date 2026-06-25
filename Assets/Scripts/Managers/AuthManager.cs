using System;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth _auth;
    private FirebaseUser _currentUser;
    private bool _isInitialized;
    private bool _lastNotifiedSignedIn;

    public FirebaseUser CurrentUser => _currentUser;
    public bool IsInitialized => _isInitialized;
    public bool IsLoggedIn => _currentUser != null;
    public string UserId => _currentUser != null ? _currentUser.UserId : string.Empty;
    public string Email => _currentUser != null ? _currentUser.Email : string.Empty;

    public event Action<bool> LoginStatusChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("[AuthManager]");
        go.AddComponent<AuthManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async UniTaskVoid Start()
    {
        bool isReady = await FirebaseInitializer.Instance.WaitForInitializationAsync();
        if (!isReady)
        {
            _isInitialized = true;
            Debug.LogError("[Auth] Firebase 초기화 실패로 인증을 사용할 수 없습니다.");
            NotifyLoginState(true);
            return;
        }

        _auth = FirebaseInitializer.Instance.Auth;
        _auth.StateChanged += OnAuthStateChanged;
        _currentUser = _auth.CurrentUser;
        _isInitialized = true;
        NotifyLoginState(true);
    }

    private void OnDestroy()
    {
        if (_auth != null)
        {
            _auth.StateChanged -= OnAuthStateChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        _currentUser = _auth.CurrentUser;
        NotifyLoginState(false);
    }

    public async UniTask<(bool success, string error)> CreateUserWithEmailAsync(string email, string password)
    {
        if (!IsReady(out string error))
        {
            return (false, error);
        }

        try
        {
            AuthResult result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            _currentUser = result.User;
            NotifyLoginState(false);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public async UniTask<(bool success, string error)> SignInUserWithEmailAsync(string email, string password)
    {
        if (!IsReady(out string error))
        {
            return (false, error);
        }

        try
        {
            AuthResult result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            _currentUser = result.User;
            NotifyLoginState(false);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public void SignOut()
    {
        if (_auth == null)
        {
            return;
        }

        _auth.SignOut();
        _currentUser = null;
        NotifyLoginState(false);
    }

    private bool IsReady(out string error)
    {
        if (!_isInitialized || _auth == null)
        {
            error = _isInitialized ? "Firebase 인증을 사용할 수 없습니다." : "Firebase 인증 초기화 중입니다.";
            return false;
        }

        error = null;
        return true;
    }

    private void NotifyLoginState(bool force)
    {
        bool signedIn = IsLoggedIn;
        if (!force && signedIn == _lastNotifiedSignedIn)
        {
            return;
        }

        _lastNotifiedSignedIn = signedIn;
        Debug.Log(signedIn ? $"[Auth] 로그인 상태: {Email}" : "[Auth] 로그아웃 상태");
        LoginStatusChanged?.Invoke(signedIn);
    }

    private string ParseFirebaseError(string error)
    {
        Debug.LogWarning($"[Auth] Firebase 에러 원문: {error}");

        string lower = error.ToLowerInvariant();

        if (lower.Contains("already in use") || lower.Contains("email-already"))
        {
            return "이미 사용 중인 이메일입니다.";
        }
        if (lower.Contains("at least 6") || lower.Contains("weak") || lower.Contains("password is invalid"))
        {
            return "비밀번호는 6자 이상이어야 합니다.";
        }
        if (lower.Contains("badly formatted") || lower.Contains("invalid-email"))
        {
            return "이메일 형식이 올바르지 않습니다.";
        }
        if (lower.Contains("network"))
        {
            return "네트워크 연결을 확인해주세요.";
        }

        return "이메일 또는 비밀번호를 확인해주세요.";
    }
}


