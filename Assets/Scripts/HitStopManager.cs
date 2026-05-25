using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    private static HitStopManager _instance;

    [Header("Hit Stop Settings")]
    [SerializeField, Range(0f, 1f)]
    private float _defaultStopScale = 0.0001f;

    [SerializeField]
    private bool _logRequests = false;

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

    public static void SetGlobalTimeScale (float value)
    {
        GetOrCreateInstance().SetTimeScale(Mathf.Max(0f, value));
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
        DontDestroyOnLoad(gameObject);  
    }

    private void ApplyHitStop (float duration, float stopScale)
    {
        float clampedScale = Mathf.Clamp(stopScale, 0f, 1f);
        float resumeAt = Time.unscaledTime + duration;

        if (_logRequests)
        {
            Debug.Log($"HitStop requested. Duration: {duration}, StopScale: {clampedScale}");
        }

        if (_activeCoroutine == null)
        {
            _restoreTimeScale = Time.timeScale;
            SetTimeScale(clampedScale);
            _resumeAtUnscaledTime = resumeAt;
            _activeCoroutine = StartCoroutine(RestoreAfterDelay(clampedScale));
            return;
        }

        _resumeAtUnscaledTime = Mathf.Max(_resumeAtUnscaledTime, resumeAt);
        SetTimeScale(Mathf.Min(Time.timeScale, clampedScale));
    }

    private IEnumerator RestoreAfterDelay(float applieScale)
    {
        while (Time.unscaledTime < _resumeAtUnscaledTime)
        {
            yield return null;
        }
        
        if (Mathf.Approximately(Time.timeScale, applieScale))
        {
            SetTimeScale(_restoreTimeScale);
        }

        else
        {
            SetTimeScale(Time.timeScale);
        }

        _activeCoroutine = null;
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
        Time.fixedDeltaTime = _baseFixedDeltaTime * value;
    }

}
