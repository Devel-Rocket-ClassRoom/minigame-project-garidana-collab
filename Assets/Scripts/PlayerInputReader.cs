using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool Attack { get; private set; }
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

    public void OnAttack()
    {
        
    }
}
