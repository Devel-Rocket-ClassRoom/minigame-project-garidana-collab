using UnityEngine;
using UnityEngine.UI;

public class MainTitleUi : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _donationButton;



    private void Start()
    {
        _startButton.onClick.AddListener(OnStartGame);
        _quitButton.onClick.AddListener(OnQuit);
        _donationButton.onClick.AddListener(OnClickSiteButton);
    }

    private void OnStartGame()
    {
        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.Game);
    }

    private void OnClickSiteButton()
    {
        Application.OpenURL("https://www.notion.so/rockdo4/Donation-367ff6a084d180318b7ff93701cbaf9c?source=copy_link");
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}