using UnityEngine;
using UnityEngine.UI;

public class MainTitleUi : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _donationButton;
    [SerializeField] private Button _deleteSaveButton;



    private void Start()
    {
        _startButton.onClick.AddListener(OnStartGame);
        _quitButton.onClick.AddListener(OnQuit);
        _donationButton.onClick.AddListener(OnClickSiteButton);
        if (_deleteSaveButton != null)
        {
            _deleteSaveButton.onClick.AddListener(OnDeleteSave);
        }

        RefreshDeleteSaveButton();
    }

    private void OnStartGame()
    {
        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.Game);
    }

    private void OnClickSiteButton()
    {
        Application.OpenURL("https://www.notion.so/Donation-3749e10e86f78020908afe430ac49e2a?source=copy_link");
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDeleteSave()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.DeleteSaveFile();
        RefreshDeleteSaveButton();
    }

    private void RefreshDeleteSaveButton()
    {
        if (_deleteSaveButton == null)
        {
            return;
        }

        _deleteSaveButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSaveData();
    }

}
