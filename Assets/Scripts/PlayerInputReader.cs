using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool AttackRequested { get; private set; }
    public bool DashRequested { get; private set; }
    public bool IsSprinting { get; private set; }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        IsSprinting = value.isPressed;
        
        // Dash is requested only on the initial press
        if (value.isPressed)
        {
            DashRequested = true;
        }
    }

    public void UseDashInput()
    {
        DashRequested = false;
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
}
