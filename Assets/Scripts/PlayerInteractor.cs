using UnityEngine;
using System.Collections.Generic;

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
        RefreshCurrentTarget();
        RefreshInteractionUi();
    }

    private void TryInteract()
    {
        if (_playerStats != null && _playerStats.IsDead) return;

        if (_currentTarget == null) return;

        if (!_currentTarget.CanInteract(gameObject)) return;

        _currentTarget.Interact(gameObject);
        Debug.Log("TryInteract working");
    }

    private void HandlePlayerDied()
    {
        ClearInteraction();
        enabled = false;
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
                interactable = hit.GetComponent<IInteractable>();
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
    private void RefreshInteractionUi()
    {
        if (_interactionUi == null) return; 

        if (_currentTarget == null)
        {
            _interactionUi.Hide();
            return;
        }
        
        _interactionUi.Show(_currentTarget.InteractionPrompt);
    }

    private void ClearInteraction()
    {
        _currentTarget = null;

        if (_interactionUi != null)
        {
            _interactionUi.Hide();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}

