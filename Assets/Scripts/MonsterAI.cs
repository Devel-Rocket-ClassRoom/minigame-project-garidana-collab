using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private string _playerTag = "Player";

    [Header("Detection")]
    [SerializeField]
    private float _detectionRadius = 8f;

    [SerializeField]
    private float _attackRange = 1.5f;

    [Header("Movement")]
    [SerializeField]
    private float _moveSpeed = 2.5f;

    [SerializeField]
    private float _turnSpeed = 12f;

    [Header("Attack")]
    [SerializeField]
    private int _attackDamage = 10;

    [SerializeField]
    private float _attackCooldown = 1.2f;

    [SerializeField]
    private float _attackHitDelay = 0.25f;

    [Header("Animation")]
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private string _isMovingParameter = "IsMoving";

    [SerializeField]
    private string _speedParameter = "Speed";

    [SerializeField]
    private string _attackTriggerParameter = "Attack";

    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private IDamageable _playerDamageable;
    private float _nextAttackTime;
    private float _pendingHitTime = -1f;
    private bool _hasPendingHit;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        ConfigureAgent();
    }

    private void Start()
    {
        FindPlayer();
        TryPlaceOnNavMesh();
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
            StopMoving();
            return;
        }

        HandlePendingHit();

        if (_agent == null || !_agent.isOnNavMesh)
        {
            if (!TryPlaceOnNavMesh())
            {
                StopMoving();
                return;
            }
        }

        Vector3 toPlayer = GetFlatDirectionToPlayer();
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > _detectionRadius)
        {
            StopMoving();
            return;
        }

        FaceDirection(toPlayer);

        if (distanceToPlayer <= _attackRange)
        {
            StopMoving();
            TryStartAttack();
            return;
        }

        MoveTowardPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindWithTag(_playerTag);
        if (player == null) return;

        _playerTransform = player.transform;
        _playerDamageable = player.GetComponentInParent<IDamageable>();
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        Vector3 direction = _playerTransform.position - transform.position;
        direction.y = 0f;
        return direction;
    }

    private void MoveTowardPlayer()
    {
        if (_agent == null || !_agent.isOnNavMesh || _playerTransform == null)
        {
            StopMoving();
            return;
        }

        if (!NavMesh.SamplePosition(
                _playerTransform.position,
                out NavMeshHit targetHit,
                5f,
                NavMesh.AllAreas
            ))
        {
            StopMoving();
            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(targetHit.position);
        SetMoving(true);
    }

    private void StopMoving()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        SetMoving(false);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _turnSpeed * Time.deltaTime
        );
    }

    private void TryStartAttack()
    {
        if (Time.time < _nextAttackTime) return;

        _nextAttackTime = Time.time + _attackCooldown;
        _pendingHitTime = Time.time + _attackHitDelay;
        _hasPendingHit = true;

        if (_animator != null && !string.IsNullOrWhiteSpace(_attackTriggerParameter))
        {
            _animator.SetTrigger(_attackTriggerParameter);
        }
    }

    private void HandlePendingHit()
    {
        if (!_hasPendingHit || Time.time < _pendingHitTime) return;

        _hasPendingHit = false;

        if (_playerTransform == null) return;

        float distanceToPlayer = GetFlatDirectionToPlayer().magnitude;
        if (distanceToPlayer > _attackRange) return;

        if (_playerDamageable == null)
        {
            _playerDamageable = _playerTransform.GetComponentInParent<IDamageable>();
        }

        _playerDamageable?.TakeDamage(_attackDamage);
    }

    private void SetMoving(bool isMoving)
    {
        if (_animator == null) return;

        float speed = isMoving ? _moveSpeed : 0f;
        if (_agent != null && _agent.isOnNavMesh)
        {
            speed = _agent.velocity.magnitude;
        }

        if (!string.IsNullOrWhiteSpace(_isMovingParameter))
        {
            _animator.SetBool(_isMovingParameter, isMoving);
        }

        if (!string.IsNullOrWhiteSpace(_speedParameter))
        {
            _animator.SetFloat(_speedParameter, speed);
        }
    }

    private void ConfigureAgent()
    {
        if (_agent == null) return;

        _agent.speed = _moveSpeed;
        _agent.stoppingDistance = _attackRange;
        _agent.updateRotation = false;
    }

    private bool TryPlaceOnNavMesh()
    {
        if (_agent == null) return false;
        if (_agent.isOnNavMesh) return true;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            return false;
        }

        return _agent.Warp(hit.position);
    }

    private void OnValidate()
    {
        _detectionRadius = Mathf.Max(0f, _detectionRadius);
        _attackRange = Mathf.Max(0.1f, _attackRange);
        _moveSpeed = Mathf.Max(0f, _moveSpeed);
        _turnSpeed = Mathf.Max(0f, _turnSpeed);
        _attackDamage = Mathf.Max(0, _attackDamage);
        _attackCooldown = Mathf.Max(0.1f, _attackCooldown);
        _attackHitDelay = Mathf.Max(0f, _attackHitDelay);

        if (_attackRange > _detectionRadius)
        {
            _detectionRadius = _attackRange;
        }

        ConfigureAgent();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
