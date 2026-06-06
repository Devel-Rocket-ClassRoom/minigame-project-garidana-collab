using UnityEngine;

/// <summary>
/// 구역(Zone)별 라이팅 무드 프리셋.
/// 디렉셔널 라이트, 환경광(Ambient), 포그 설정을 하나의 에셋으로 묶는다.
/// </summary>
[CreateAssetMenu(fileName = "LightingZoneProfile", menuName = "KA Origin Lighting/Lighting Zone Profile")]
public class LightingZoneProfile : ScriptableObject
{
    [Header("Directional Light (메인광)")]
    public Color directionalColor = Color.white;

    [Range(0f, 3f)]
    public float directionalIntensity = 1f;

    [Header("Ambient (환경광, Flat 모드 기준)")]
    public Color ambientColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Fog (Exponential Squared)")]
    public bool fogEnabled = true;

    public Color fogColor = Color.gray;

    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;
}
