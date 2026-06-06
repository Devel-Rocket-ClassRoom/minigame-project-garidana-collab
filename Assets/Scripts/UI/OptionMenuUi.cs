using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionMenuUi : MonoBehaviour
{
    [SerializeField]
    private GameObject optionPanel;
    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button quitButton;
    [SerializeField]
    private Button titleButton;

    [Header("볼륨 슬라이더")]
    [SerializeField]
    private Slider masterVolumeSlider;
    [SerializeField]
    private Slider bgmVolumeSlider;
    [SerializeField]
    private Slider sfxVolumeSlider;


    private void Awake()
    {
        optionPanel.SetActive(false);

        resumeButton.onClick.AddListener(CloseMenu);
        quitButton.onClick.AddListener(QuitGame);
        titleButton.onClick.AddListener(OnToTitle);

        InitVolumeSliders();
    }

    private void InitVolumeSliders()
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SoundManager.Instance.GetMasterVolume();
            masterVolumeSlider.onValueChanged.AddListener(SoundManager.Instance.SetMasterVolume);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = SoundManager.Instance.GetBGMVolume();
            bgmVolumeSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SoundManager.Instance.GetSFXVolume();
            sfxVolumeSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameOverUi.IsAnyOpen() || ShopUi.BlocksGlobalShortcuts || InventoryUi.IsAnyOpen() || QuestUi.IsAnyOpen() || TownTutorialUi.IsAnyOpen())
            {
                return;
            }

            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        bool nextActive = !optionPanel.activeSelf;
        optionPanel.SetActive(nextActive);
        PlayMenuSound(nextActive);

        // 메뉴 열리면 게임 멈춤 기능
        if (nextActive)
        {
            PauseManager.Pause();
        }
        else
        {
            PauseManager.Resume();
        }
    }

    private void CloseMenu()
    {
        bool wasOpen = optionPanel.activeSelf;
        optionPanel.SetActive(false);
        if (wasOpen)
        {
            PlayMenuSound(false);
        }
        PauseManager.Resume();
    }

    private static void PlayMenuSound(bool opening)
    {
        SoundManager.Instance?.PlaySFX(
            opening ? SoundManager.SFXType.OptionMenuOpen : SoundManager.SFXType.OptionMenuClose);
    }

    private void QuitGame()
    {
        PauseManager.Resume();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnToTitle()
    {
        PauseManager.Resume();
        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.MainTitle);
    }

    public static bool IsAnyOpen()
    {
        OptionMenuUi[] optionMenuUis = FindObjectsByType<OptionMenuUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < optionMenuUis.Length; i++)
        {
            if (optionMenuUis[i] != null && optionMenuUis[i].optionPanel != null && optionMenuUis[i].optionPanel.activeSelf)
            {
                return true;
            }
        }

        return false;
    }
}
