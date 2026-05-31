using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputReader : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";

    private PlayerInput _playerInput;

    public Vector2 MoveInput { get; private set; }
    public bool AttackRequested { get; private set; }
    public bool DashRequested { get; private set; }
    public bool HealRequested {get; private set; }

    public event Action InteractPressed;
    public event Action InventoryPressed;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        EnablePlayerActionMap();
    }

    private void EnablePlayerActionMap()
    {
        if (_playerInput == null || _playerInput.actions == null)
        {
            return;
        }

        if (_playerInput.currentActionMap != null && _playerInput.currentActionMap.name == PlayerActionMapName)
        {
            return;
        }

        _playerInput.SwitchCurrentActionMap(PlayerActionMapName);
    }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            DashRequested = true;
        }
    }

    public void UseDashInput()
    {
        DashRequested = false;
    }

    public void UseHealInput()
    {
        HealRequested = false;
    }

    public bool PreviousRequested { get; private set; }
    public bool NextRequested { get; private set; }

    public void OnPrevious(InputValue value)
    {
        if (value.isPressed)
        {
            PreviousRequested = true;
        }
    }

    public void OnNext(InputValue value)
    {
        if (value.isPressed)
        {
            NextRequested = true;
        }
    }

    public void UsePreviousInput()
    {
        PreviousRequested = false;
    }

    public void UseNextInput()
    {
        NextRequested = false;
    }

    public void UseAttackInput()
    {
        AttackRequested = false;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            AttackRequested = true;
        }
    }

    public void OnInteract (InputValue value)
    {
        if (value.isPressed)
        {
            InteractPressed?.Invoke();
            // Debug.Log ("OnInteract working");
        }
    }

    public void OnHeal(InputValue value)
    {
        if (value.isPressed)
        {
            HealRequested = true;
        }
    }

    public void OnInventory(InputValue value)
    {
        if (value.isPressed)
        {
            InventoryPressed?.Invoke();
        }
    }
}
