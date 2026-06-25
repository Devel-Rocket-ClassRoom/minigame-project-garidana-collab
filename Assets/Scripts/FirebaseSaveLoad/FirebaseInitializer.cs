using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    public enum InitState
    {
        Pending,
        Ready,
        Failed
    }

    public static FirebaseInitializer Instance { get; private set; }

    public InitState State { get; private set; } = InitState.Pending;
    public bool IsReady => State == InitState.Ready;
    public string LastError { get; private set; }
    public FirebaseApp App { get; private set; }
    public FirebaseAuth Auth { get; private set; }
    public FirebaseDatabase Database { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("[FirebaseInitializer]");
        go.AddComponent<FirebaseInitializer>();
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
        InitializeFirebaseAsync().Forget();
    }

    private async UniTaskVoid InitializeFirebaseAsync()
    {
        Debug.Log("[Firebase] 초기화 시작");

        try
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (status != DependencyStatus.Available)
            {
                Fail($"의존성 오류: {status}");
                return;
            }

            App = FirebaseApp.DefaultInstance;
            Auth = FirebaseAuth.GetAuth(App);
            Database = GetDatabase(App);
            State = InitState.Ready;
            Debug.Log("[Firebase] 초기화 완료");
        }
        catch (System.Exception ex)
        {
            Fail(ex.Message);
        }
    }

    private FirebaseDatabase GetDatabase(FirebaseApp app)
    {
        FirebaseConfig config = Resources.Load<FirebaseConfig>("FirebaseConfig");
        if (config != null && config.IsValid)
        {
            return FirebaseDatabase.GetInstance(app, config.databaseUrl);
        }

        return FirebaseDatabase.GetInstance(app);
    }

    private void Fail(string error)
    {
        LastError = error;
        State = InitState.Failed;
        Debug.LogError($"[Firebase] 초기화 실패: {error}");
    }

    public async UniTask<bool> WaitForInitializationAsync()
    {
        await UniTask.WaitUntil(() => State != InitState.Pending);
        return State == InitState.Ready;
    }
}
