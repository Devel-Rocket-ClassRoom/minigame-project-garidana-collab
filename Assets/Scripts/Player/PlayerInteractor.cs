using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField]
    private PlayerInputReader _inputReader;

    [SerializeField]
    private PlayerStats _playerStats;

    [SerializeField]
    private InteractionUi _interactionUi;

    [SerializeField]
    private float _interactRadius = 2f;

    [SerializeField]
    private LayerMask _interactableLayer;

    private IInteractable _currentTarget;
    private WaypointInteractable _activeWaypointSelectionTarget;
    private List<WaypointNode> _activeWaypointDestinations = new();

    private void Awake()
    {
        if (_inputReader == null)
        {
            _inputReader = GetComponent<PlayerInputReader>();
        }

        if (_playerStats == null)
        {
            _playerStats = GetComponent<PlayerStats>();
        }
    }

    private void OnEnable()
    {
        if (_inputReader != null)
        {
            _inputReader.InteractPressed += TryInteract;
        }

        if (_playerStats != null)
        {
            _playerStats.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.InteractPressed -= TryInteract; 
        }

        if (_playerStats != null)
        {
            _playerStats.Died -= HandlePlayerDied;
        }

        ClearInteraction();
    }

    private void Update()
    {
        if (_playerStats != null && _playerStats.IsDead)
        {
            ClearInteraction();
            return;
        }

        RefreshCurrentTarget();
        HandleWaypointSelectionInput();
        RefreshInteractionUi();
    }

    private void TryInteract()
    {
        if (_playerStats != null && _playerStats.IsDead) return;

        if (_activeWaypointSelectionTarget != null)
        {
            CloseWaypointSelection();
            return;
        }

        if (_currentTarget == null) return;

        if (!_currentTarget.CanInteract(gameObject)) return;

        if (_currentTarget is WaypointInteractable waypointInteractable
            && waypointInteractable.CanOpenSelectionMenu())
        {
            OpenWaypointSelection(waypointInteractable);
            return;
        }

        _currentTarget.Interact(gameObject);
        Debug.Log("TryInteract working");
    }

    private void HandlePlayerDied()
    {
        ClearInteraction();
    }

    private void RefreshCurrentTarget()
    {
        _currentTarget = null;
        float closestDistance = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _interactRadius,
            _interactableLayer
        );

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable = hit.GetComponentInParent<IInteractable>();
            }

            if (interactable == null)
            {
                continue;
            }

            if (!interactable.CanInteract(gameObject))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, interactable.Transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                _currentTarget = interactable;
            }
        }
    }

    private void HandleWaypointSelectionInput()
    {
        if (_activeWaypointSelectionTarget == null)
        {
            return;
        }

        if (_currentTarget != _activeWaypointSelectionTarget)
        {
            CloseWaypointSelection();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseWaypointSelection();
            return;
        }

        int selectedIndex = GetSelectionIndex(keyboard);
        if (selectedIndex < 0 || selectedIndex >= _activeWaypointDestinations.Count)
        {
            return;
        }

        WaypointNode destination = _activeWaypointDestinations[selectedIndex];
        if (destination == null || WaypointManager.Instance == null)
        {
            CloseWaypointSelection();
            return;
        }

        WaypointManager.Instance.TeleportPlayerTo(destination.WaypointId, gameObject);
        CloseWaypointSelection();
    }

    private void RefreshInteractionUi()
    {
        if (_interactionUi == null) return; 

        if (_currentTarget == null)
        {
            _interactionUi.Hide();
            return;
        }

        if (_activeWaypointSelectionTarget != null)
        {
            List<string> destinationNames = new();
            foreach (WaypointNode destination in _activeWaypointDestinations)
            {
                if (destination == null)
                {
                    continue;
                }

                destinationNames.Add(destination.DisplayName);
            }

            _interactionUi.ShowWaypointSelection(_currentTarget.InteractionPrompt, destinationNames);
            return;
        }

        _interactionUi.Show(_currentTarget.InteractionPrompt);
    }

    private void ClearInteraction()
    {
        _currentTarget = null;
        _activeWaypointSelectionTarget = null;
        _activeWaypointDestinations.Clear();

        if (_interactionUi != null)
        {
            _interactionUi.Hide();
        }
    }

    private void OpenWaypointSelection(WaypointInteractable waypointInteractable)
    {
        _activeWaypointSelectionTarget = waypointInteractable;
        _activeWaypointDestinations = waypointInteractable.GetSelectionDestinations();
    }

    private void CloseWaypointSelection()
    {
        _activeWaypointSelectionTarget = null;
        _activeWaypointDestinations.Clear();
    }

    private int GetSelectionIndex(Keyboard keyboard)
    {
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 3;
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return 4;
        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) return 5;
        if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) return 6;
        if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame) return 7;
        if (keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame) return 8;

        return -1;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}

