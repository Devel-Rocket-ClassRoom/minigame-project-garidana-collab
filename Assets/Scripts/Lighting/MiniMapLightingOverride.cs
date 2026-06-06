using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 미니맵 카메라가 렌더링하는 동안만 라이팅을 고정값으로 덮어쓴다.
/// 구역별 라이팅/포그 변화가 미니맵에 영향을 주지 않게 함.
/// MiniMapCamera 오브젝트에 부착하고 Directional Light를 연결할 것.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MiniMapLightingOverride : MonoBehaviour
{
    [SerializeField]
    private Light directionalLight;

    [Header("미니맵 전용 고정 라이팅")]
    [SerializeField]
    private Color fixedLightColor = Color.white;

    [SerializeField]
    private float fixedLightIntensity = 1.2f;

    [SerializeField]
    private Color fixedAmbientColor = new Color(0.75f, 0.75f, 0.75f);

    private Camera miniMapCamera;

    // 복원용 백업
    private bool savedFog;
    private Color savedAmbient;
    private Color savedLightColor;
    private float savedLightIntensity;
    private bool overriding;

    private void Awake()
    {
        miniMapCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != miniMapCamera)
        {
            return;
        }

        savedFog = RenderSettings.fog;
        savedAmbient = RenderSettings.ambientLight;

        RenderSettings.fog = false;
        RenderSettings.ambientLight = fixedAmbientColor;

        if (directionalLight != null)
        {
            savedLightColor = directionalLight.color;
            savedLightIntensity = directionalLight.intensity;
            directionalLight.color = fixedLightColor;
            directionalLight.intensity = fixedLightIntensity;
        }

        overriding = true;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != miniMapCamera || !overriding)
        {
            return;
        }

        RenderSettings.fog = savedFog;
        RenderSettings.ambientLight = savedAmbient;

        if (directionalLight != null)
        {
            directionalLight.color = savedLightColor;
            directionalLight.intensity = savedLightIntensity;
        }

        overriding = false;
    }
}
