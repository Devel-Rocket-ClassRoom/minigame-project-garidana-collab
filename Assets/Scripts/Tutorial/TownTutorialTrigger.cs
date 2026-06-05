using UnityEngine;

public class TownTutorialTrigger : MonoBehaviour
{
    [SerializeField]
    private TownTutorialUi _tutorialUi;

    [SerializeField]
    private bool _showOnlyOnce = true;

    private bool _hasShown;
    private bool _completed;

    public bool IsCompleted => _completed;

    private void OnEnable()
    {
        if (_tutorialUi != null)
        {
            _tutorialUi.TutorialCompleted += HandleTutorialCompleted;
        }

        RestoreCompletedState(SaveManager.Instance != null && SaveManager.Instance.IsTownTutorialCompleted);
    }

    private void OnDisable()
    {
        if (_tutorialUi != null)
        {
            _tutorialUi.TutorialCompleted -= HandleTutorialCompleted;
        }
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_showOnlyOnce && (_hasShown || _completed))
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (_tutorialUi == null)
        {
            Debug.LogWarning($"{name} has no TownTutorialUi assigned.", this);
            return;
        }

        _hasShown = true;
        _tutorialUi.Open();
    }

    public void RestoreCompletedState(bool completed)
    {
        _completed = completed;

        if (completed)
        {
            _hasShown = true;
        }
    }

    private void HandleTutorialCompleted()
    {
        RestoreCompletedState(true);
        SaveManager.Instance?.SaveGame();
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Player"))
        {
            return true;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerStats>() != null;
    }
}
