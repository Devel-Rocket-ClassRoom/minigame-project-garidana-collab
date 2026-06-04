using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 내 모든 Button에 클릭음을 자동으로 등록합니다.
/// SoundManager와 함께 DontDestroyOnLoad로 유지됩니다.
/// </summary>
public class UiSoundHandler : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameObject go = new GameObject("[UiSoundHandler]");
        go.AddComponent<UiSoundHandler>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllButtons();
    }

    // 씬 로드 직후 비활성 오브젝트 포함 전체 버튼에 클릭음 등록
    private static void RegisterAllButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    private static void PlayButtonClick()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.SFXType.ButtonClick);
    }
}
