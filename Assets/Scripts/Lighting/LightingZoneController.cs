using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 라이팅 존 전환을 실제로 수행하는 싱글톤.
/// LightingZoneTrigger가 요청하면 디렉셔널 라이트 / 환경광 / 포그를 부드럽게 블렌딩하고,
/// 구역별 포스트프로세싱 Volume의 weight를 교차 페이드한다.
/// 씬에 1개 배치하고 메인 디렉셔널 라이트를 연결할 것.
/// </summary>
public class LightingZoneController : MonoBehaviour
{
    public static LightingZoneController Instance { get; private set; }

    [SerializeField]
    private Light directionalLight;

    [SerializeField]
    private LightingZoneProfile defaultProfile;

    private Coroutine blendRoutine;
    private Volume currentVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Flat 앰비언트 기준으로 블렌딩 (프로필 색을 그대로 사용)
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        if (defaultProfile != null)
        {
            ApplyImmediate(defaultProfile);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>프로필을 즉시 적용 (게임 시작/씬 로드 직후용).</summary>
    public void ApplyImmediate(LightingZoneProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
            blendRoutine = null;
        }

        if (directionalLight != null)
        {
            directionalLight.color = profile.directionalColor;
            directionalLight.intensity = profile.directionalIntensity;
        }

        RenderSettings.ambientLight = profile.ambientColor;
        RenderSettings.fog = profile.fogEnabled;
        RenderSettings.fogColor = profile.fogColor;
        RenderSettings.fogDensity = profile.fogDensity;
    }

    /// <summary>프로필로 부드럽게 전환. zoneVolume은 해당 구역의 포스트프로세싱 Volume(선택).</summary>
    public void BlendTo(LightingZoneProfile profile, Volume zoneVolume, float duration)
    {
        if (profile == null)
        {
            return;
        }

        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
        }

        blendRoutine = StartCoroutine(BlendRoutine(profile, zoneVolume, Mathf.Max(0.01f, duration)));
    }

    private IEnumerator BlendRoutine(LightingZoneProfile profile, Volume targetVolume, float duration)
    {
        Color startLightColor = directionalLight != null ? directionalLight.color : Color.white;
        float startLightIntensity = directionalLight != null ? directionalLight.intensity : 1f;
        Color startAmbient = RenderSettings.ambientLight;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;

        // 포그가 꺼져 있던 구역에서 켜질 때는 농도 0부터 시작
        if (profile.fogEnabled && !RenderSettings.fog)
        {
            RenderSettings.fog = true;
            startFogDensity = 0f;
        }

        float targetFogDensity = profile.fogEnabled ? profile.fogDensity : 0f;

        Volume previousVolume = currentVolume;
        currentVolume = targetVolume;

        float startPrevWeight = previousVolume != null ? previousVolume.weight : 0f;
        float startTargetWeight = targetVolume != null ? targetVolume.weight : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startLightColor, profile.directionalColor, t);
                directionalLight.intensity = Mathf.Lerp(startLightIntensity, profile.directionalIntensity, t);
            }

            RenderSettings.ambientLight = Color.Lerp(startAmbient, profile.ambientColor, t);
            RenderSettings.fogColor = Color.Lerp(startFogColor, profile.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, targetFogDensity, t);

            if (previousVolume != null && previousVolume != targetVolume)
            {
                previousVolume.weight = Mathf.Lerp(startPrevWeight, 0f, t);
            }

            if (targetVolume != null)
            {
                targetVolume.weight = Mathf.Lerp(startTargetWeight, 1f, t);
            }

            yield return null;
        }

        if (!profile.fogEnabled)
        {
            RenderSettings.fog = false;
        }

        blendRoutine = null;
    }
}
