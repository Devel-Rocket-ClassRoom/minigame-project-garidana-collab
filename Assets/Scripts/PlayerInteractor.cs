using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField]
    private PlayerInputReader _inputReader;

    [SerializeField]
    private InteractionUi _interactionUi;

    private readonly List<IInteractable> _candidates = new();

    private IInteractable _currentTarget;

    private void Awake()
    {
        if (_inputReader == null)
        {
            _inputReader = GetComponent<PlayerInputReader>();
        }
    }

    private void OnEnable()
    {
        if (_inputReader != null)
        {
            _inputReader.InteractPressed += TryInteract;
        }
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.InteractPressed -= TryInteract; 
        }
    }

    private void Update()
    {
        RefreshCurrentTarget();
        RefreshInteractionUi();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            if (!_candidates.Contains(interactable))
            {
                _candidates.Add(interactable);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            _candidates.Remove(interactable);
        }
    }

    private void TryInteract()
    {
        if (_currentTarget == null) return;

        if (!_currentTarget.CanInteract(gameObject)) return;

        _currentTarget.Interact(gameObject);
    }

    private void RefreshCurrentTarget()
    {
        _currentTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var candidate in _candidates)
        {
            if (candidate == null) continue;

            if (!candidate.CanInteract(gameObject)) continue;

            float distance = Vector3.Distance(transform.position, candidate.Transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                _currentTarget = candidate;
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
}
