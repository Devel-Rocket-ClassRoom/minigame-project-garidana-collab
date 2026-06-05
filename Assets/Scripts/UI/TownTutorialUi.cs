using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class TownTutorialUi : MonoBehaviour
{
    [SerializeField]
    private GameObject _root;

    [SerializeField]
    private GameObject[] _pages;

    [SerializeField]
    private bool _showFirstPageOnAwake;

    private int _currentPageIndex;
    private bool _isOpen;
    private bool _pausedGame;
    private bool _suppressedTutorialGuide;

    public bool IsOpen => _isOpen;

    public event Action TutorialCompleted;

    public static bool IsAnyOpen()
    {
        TownTutorialUi[] townTutorialUis = FindObjectsByType<TownTutorialUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < townTutorialUis.Length; i++)
        {
            if (townTutorialUis[i] != null && townTutorialUis[i].IsOpen)
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        EnsureRoot();

        if (_isOpen)
        {
            return;
        }

        if (_showFirstPageOnAwake)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    private void Update()
    {
        if (!_isOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            ShowNextPage();
        }
    }

    private void OnDisable()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        SetTutorialGuideSuppressed(false);
        ResumeAfterTutorial();
    }

    public void Open()
    {
        EnsureRoot();

        if (_pages == null || _pages.Length == 0)
        {
            Debug.LogWarning($"{name} has no tutorial pages assigned.", this);
            return;
        }

        _isOpen = true;
        PauseForTutorial();
        SetTutorialGuideSuppressed(true);
        _currentPageIndex = 0;

        if (_root != null)
        {
            _root.SetActive(true);
        }

        RefreshPages();
    }

    public void Close()
    {
        EnsureRoot();

        _isOpen = false;

        if (_pages != null)
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].SetActive(false);
                }
            }
        }

        if (_root != null)
        {
            _root.SetActive(false);
        }

        SetTutorialGuideSuppressed(false);
        ResumeAfterTutorial();
    }

    private void ShowNextPage()
    {
        if (_currentPageIndex >= _pages.Length - 1)
        {
            CompleteTutorial();
            return;
        }

        _currentPageIndex++;
        RefreshPages();
    }

    private void RefreshPages()
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] != null)
            {
                _pages[i].SetActive(i == _currentPageIndex);
            }
        }
    }

    private void EnsureRoot()
    {
        if (_root == null)
        {
            _root = gameObject;
        }
    }

    private void PauseForTutorial()
    {
        if (_pausedGame)
        {
            return;
        }

        PauseManager.Pause();
        _pausedGame = true;
    }

    private void ResumeAfterTutorial()
    {
        if (!_pausedGame)
        {
            return;
        }

        PauseManager.Resume();
        _pausedGame = false;
    }

    private void SetTutorialGuideSuppressed(bool suppressed)
    {
        if (_suppressedTutorialGuide == suppressed)
        {
            return;
        }

        TutorialUi.SetSuppressed(suppressed);
        _suppressedTutorialGuide = suppressed;
    }

    private void CompleteTutorial()
    {
        Close();
        TutorialCompleted?.Invoke();
    }
}
