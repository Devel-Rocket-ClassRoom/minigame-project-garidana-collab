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


    private void Awake()
    {
        optionPanel.SetActive(false);

        resumeButton.onClick.AddListener(CloseMenu);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (ShopUi.BlocksGlobalShortcuts || InventoryUi.IsAnyOpen() || QuestUi.IsAnyOpen())
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
        optionPanel.SetActive(false);
        PauseManager.Resume();
    }

    private void QuitGame()
    {
        PauseManager.Resume();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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
