using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance {get; private set;}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var go = new GameObject("[SceneLoader]");
        Instance = go.AddComponent<SceneLoader>();
        go.AddComponent<DebugSceneSwitcher>();
        DontDestroyOnLoad(go);
    }

    public enum GameScene
    {
        MainTitle,
        Game
    }

    private readonly Dictionary<GameScene, string> sceneNames = new()
    {
        {GameScene.MainTitle, "MainTitle"},
        {GameScene.Game, "SampleScene"}
    };

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(GameScene scene)
    {
        SceneManager.LoadScene(sceneNames[scene]);
    }

    // 로딩 씬
    public void LoadSceneAsync (GameScene scene)
    {
        SceneManager.LoadSceneAsync(sceneNames[scene]);
    }
}


