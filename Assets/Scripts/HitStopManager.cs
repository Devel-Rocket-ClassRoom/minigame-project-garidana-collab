using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    private static HitStopManager _instance;

    [Header("Hit Stop Settings")]
    [SerializeField, Range(0f, 1f)]
    private float _defaultStopScale = 0.0001f;

    [SerializeField]
    private bool _enableDebugLogs = true;

    private Coroutine _activeCoroutine;
    private float _resumeAtUnscaledTime;
    private float _restoreTimeScale = 1f;
    private float _baseFixedDeltaTime;

    public static void Request (float duration)
    {
        if (duration <= 0f) return;

        HitStopManager instance = GetOrCreateInstance();
        instance.ApplyHitStop(duration, instance._defaultStopScale);
    }

    public static void Request (float duration, float stopScale)
    {
        if (duration <= 0f) return;

        GetOrCreateInstance().ApplyHitStop(duration, stopScale);   
    }

    private static HitStopManager GetOrCreateInstance ()
    {
        if (_instance != null) 
        {
            return _instance;
        }

        _instance = FindFirstObjectByType<HitStopManager>();
        
        if (_instance != null) return _instance;
        
        GameObject managerObject = new GameObject(nameof(HitStopManager));
        _instance = managerObject.AddComponent<HitStopManager>();
        DontDestroyOnLoad(managerObject);
        _instance.Log("Created runtime HitStopManager instance.");
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
        _baseFixedDeltaTime = Time.fixedDeltaTime;
        Log($"Initialized. BaseFixedDeltaTime: {_baseFixedDeltaTime}, TimeScale: {Time.timeScale}");
        DontDestroyOnLoad(gameObject);  
    }

    private void ApplyHitStop (float duration, float stopScale)
    {
        float clampedScale = Mathf.Clamp(stopScale, 0f, 1f);
        float resumeAt = Time.unscaledTime + duration;

        Log($"Request received. Duration: {duration}, StopScale: {clampedScale}, CurrentTimeScale: {Time.timeScale}");

        if (_activeCoroutine == null)
        {
            _restoreTimeScale = Time.timeScale;
            SetTimeScale(clampedScale);
            _resumeAtUnscaledTime = resumeAt;
            Log($"Started. RestoreTimeScale: {_restoreTimeScale}, ResumeAtUnscaledTime: {_resumeAtUnscaledTime}");
            _activeCoroutine = StartCoroutine(RestoreAfterDelay(clampedScale));
            return;
        }

        float previousResumeAt = _resumeAtUnscaledTime;
        _resumeAtUnscaledTime = Mathf.Max(_resumeAtUnscaledTime, resumeAt);
        SetTimeScale(Mathf.Min(Time.timeScale, clampedScale));
        Log($"Extended. PreviousResumeAt: {previousResumeAt}, NewResumeAt: {_resumeAtUnscaledTime}, CurrentTimeScale: {Time.timeScale}");
    }

    private IEnumerator RestoreAfterDelay(float appliedScale)
    {
        while (Time.unscaledTime < _resumeAtUnscaledTime)
        {
            yield return null;
        }
        
        if (Mathf.Approximately(Time.timeScale, appliedScale))
        {
            SetTimeScale(_restoreTimeScale);
            Log($"Restored. TimeScale: {Time.timeScale}, FixedDeltaTime: {Time.fixedDeltaTime}");
        }

        else
        {
            SetTimeScale(Time.timeScale);
            Log($"Restore skipped because TimeScale changed externally. CurrentTimeScale: {Time.timeScale}");
        }

        _activeCoroutine = null;
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
        Time.fixedDeltaTime = _baseFixedDeltaTime * value;
    }

    private void Log(string message)
    {
        if (!_enableDebugLogs) return;

        Debug.Log($"[HitStopManager] {message}");
    }

}
