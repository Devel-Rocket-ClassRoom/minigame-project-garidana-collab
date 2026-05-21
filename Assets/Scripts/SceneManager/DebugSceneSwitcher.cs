using UnityEngine;


public class DebugSceneSwitcher : MonoBehaviour
{
    [SerializeField]
    private bool showDebugPanel = false;
    [SerializeField]
    private KeyCode toggleKey = KeyCode.F1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugPanel = !showDebugPanel;
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SceneLoader.Instance.LoadScene(SceneLoader.GameScene.MainTitle);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SceneLoader.Instance.LoadScene(SceneLoader.GameScene.Game);
            }
        }
    }

    private void OnGUI()
    {
        if (!showDebugPanel) return; 

        GUI.Box (new Rect (10, 10, 200, 140), "Scene Switcher (F1)");
        
        if (GUI.Button (new Rect(20, 40, 180, 25), "1. Main Title (메인)"))
        {
            SceneLoader.Instance.LoadScene(SceneLoader.GameScene.MainTitle);
        }
        if (GUI.Button (new Rect(20, 70, 180, 25), "2. GamePlay (월드)"))
        {
            SceneLoader.Instance.LoadScene(SceneLoader.GameScene.Game);
        }
    }
}