using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private static PauseManager _instance;

    [SerializeField]
    private bool _enableDebugLogs = true;

    private float _baseFixedDeltaTime;

    public static void Pause()
    {
        GetOrCreateInstance().SetPaused(true);
    }

    public static void Resume()
    {
        GetOrCreateInstance().SetPaused(false);
    }

    private static PauseManager GetOrCreateInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        _instance = FindFirstObjectByType<PauseManager>();

        if (_instance != null)
        {
            return _instance;
        }

        GameObject managerObject = new GameObject(nameof(PauseManager));
        _instance = managerObject.AddComponent<PauseManager>();
        DontDestroyOnLoad(managerObject);
        _instance.Log("Created runtime PauseManager instance.");
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _baseFixedDeltaTime = Time.timeScale > 0f ? Time.fixedDeltaTime / Time.timeScale : Time.fixedDeltaTime;
        Log($"Initialized. BaseFixedDeltaTime: {_baseFixedDeltaTime}, TimeScale: {Time.timeScale}");
        DontDestroyOnLoad(gameObject);
    }

    private void SetPaused(bool isPaused)
    {
        float previousTimeScale = Time.timeScale;
        float previousFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = isPaused ? 0f : 1f;
        Time.fixedDeltaTime = isPaused ? 0f : _baseFixedDeltaTime;

        Log(
            $"{(isPaused ? "Paused" : "Resumed")}. " +
            $"TimeScale: {previousTimeScale} -> {Time.timeScale}, " +
            $"FixedDeltaTime: {previousFixedDeltaTime} -> {Time.fixedDeltaTime}"
        );
    }

    private void Log(string message)
    {
        if (!_enableDebugLogs) return;

        Debug.Log($"[PauseManager] {message}");
    }
}
