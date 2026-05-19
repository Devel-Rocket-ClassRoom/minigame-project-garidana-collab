using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 5f;

    [SerializeField]
    private float _rotateSpeed = 10f;

    private Vector3 _playerDirection;
    private PlayerInputReader _playerInput;
    private Rigidbody _playerRigidbody;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInputReader>();
        _playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        _playerDirection = new Vector3(_playerInput.MoveInput.x, 0f, _playerInput.MoveInput.y).normalized;
        Rotate();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector3 nextPosition =
            _playerRigidbody.position + _playerDirection * _moveSpeed * Time.fixedDeltaTime;

        _playerRigidbody.MovePosition(nextPosition);
    }

    private void Rotate()
    {
        if (_playerDirection == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(_playerDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotateSpeed * Time.deltaTime
        );
    }
}
