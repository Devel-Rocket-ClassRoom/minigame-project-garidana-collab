using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 5f;

    [SerializeField]
    private float _dashDistance = 7f;

    [SerializeField]
    private float _dashDuration = 0.15f;

    [SerializeField]
    private float _dashCooldown = 0.5f;

    [SerializeField]
    private float _dashStaminaCost = 20f;

    [SerializeField]
    private float _rotateSpeed = 10f;

    // 대쉬 아이콘 쿨타임 표시용
    public bool IsDashOnCooldown => Time.time < _lastDashTime + _dashCooldown;
    public float DashCooldownProgress
    {
        get
        {
            float elapsed = Time.time - _lastDashTime;
            return Mathf.Clamp01(elapsed / _dashCooldown);
        }
    }


    private Vector3 _playerDirection;
    private PlayerInputReader _playerInput;
    private Rigidbody _playerRigidbody;
    private Animator _animator;
    private PlayerStats _playerStats;

    private bool _isDashing;
    private float _dashTimeRemaining;
    private float _lastDashTime = -100f;
    private Vector3 _dashDirection;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInputReader>();
        _playerRigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (_playerStats != null && _playerStats.IsDead)
        {
            _playerDirection = Vector3.zero;
            return;
        }


        _playerDirection = new Vector3(_playerInput.MoveInput.x, 0f, _playerInput.MoveInput.y).normalized;

        // Dash Logic - Always consume input to prevent "buffering" multiple dashes
        if (_playerInput.DashRequested)
        {
            if (!_isDashing && Time.time >= _lastDashTime + _dashCooldown)
            {
                StartDash();
            }
            _playerInput.UseDashInput();
        }

        Rotate();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (_playerStats != null && _playerStats.IsDead)
        {
            return;
        }

        Move();
    }

    private void StartDash()
    {
        if (_playerStats != null && !_playerStats.TryUseStamina(_dashStaminaCost))
        {
            return;
        }

        _isDashing = true;
        _dashTimeRemaining = _dashDuration;
        _lastDashTime = Time.time;
        
        // Dash in movement direction if input exists, otherwise dash forward
        _dashDirection = _playerDirection != Vector3.zero ? _playerDirection : transform.forward;
        
        // Snap rotation to dash direction immediately for better feel
        if (_dashDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_dashDirection);
        }
    }

    private void Move()
    {
        if (_isDashing)
        {
            // Dash uses a constant high velocity for its duration
            float dashSpeed = _dashDistance / _dashDuration;
            Vector3 nextPosition = _playerRigidbody.position + _dashDirection * dashSpeed * Time.fixedDeltaTime;
            _playerRigidbody.MovePosition(nextPosition);

            _dashTimeRemaining -= Time.fixedDeltaTime;
            if (_dashTimeRemaining <= 0)
            {
                _isDashing = false;
            }
        }
        else
        {
            Vector3 nextPosition =
                _playerRigidbody.position + _playerDirection * _moveSpeed * Time.fixedDeltaTime;

            _playerRigidbody.MovePosition(nextPosition);
        }
    }

    private void Rotate()
    {
        // Don't rotate manually during dash (already snapped in StartDash)
        if (_isDashing || _playerDirection == Vector3.zero)
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

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        float targetSpeed = 0f;
        
        // Use full movement animation speed during dash.
        if (_isDashing)
        {
            targetSpeed = 1.0f;
        }
        else if (_playerDirection.magnitude > 0.1f)
        {
            targetSpeed = 0.5f; // Normal Run
        }

        float currentAnimSpeed = _animator.GetFloat(SpeedHash);
        // Faster lerp for more responsive animation changes
        float smoothedSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * 20f);
        _animator.SetFloat(SpeedHash, smoothedSpeed);
    }

    public bool IsDashing => _isDashing;
}
