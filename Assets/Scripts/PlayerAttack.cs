using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField]
    private int[] _comboDamages = { 10, 12, 18 };

    [SerializeField]
    private float[] _attackDurations = { 0.55f, 0.6f, 0.75f };

    [SerializeField]
    private float[] _hitDelays = { 0.12f, 0.14f, 0.18f };

    [SerializeField]
    private float _comboWindowStartOffset = 0.25f;

    [SerializeField]
    private float _comboWindowEndOffset = 0.5f;

    [SerializeField]
    private float _hitRange = 1.1f;

    [SerializeField]
    private float _hitRadius = 0.9f;

    [SerializeField]
    private LayerMask _hitLayers = ~0;

    private int _comboStep;
    private bool _isAttacking;
    private bool _comboQueued;
    private float _comboWindowStart;
    private float _comboWindowEnd;
    private float _attackEndTime;
    private int _pendingHitComboStep;

    private readonly Collider[] _hitResults = new Collider[12];

    private PlayerInputReader _playerInput;
    private PlayerMovement _playerMovement;
    private Animator _animator;

    private float _pendingHitTime;
    private bool _hasPendingHit;

    private const int MaxComboStep = 3;
    private const string MoveStateName = "Move";
    private static readonly int[] AttackStateHashes =
    {
        Animator.StringToHash("Attack_1"),
        Animator.StringToHash("Attack_2"),
        Animator.StringToHash("Attack_3")
    };

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInputReader>();
        _playerMovement = GetComponent<PlayerMovement>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_playerInput.AttackRequested)
        {
            TryAttack();
            _playerInput.UseAttackInput();
        }

        UpdateCombo();

        if (_hasPendingHit && Time.time >= _pendingHitTime)
        {
            _hasPendingHit = false;
            ApplyHit(_pendingHitComboStep);
        }
    }

    private void TryAttack()
    {
        if (_playerMovement != null && _playerMovement.IsDashing)
        {
            return;
        }

        if (!_isAttacking)
        {
            StartComboAttack(1);
            return;
        }

        if (_comboStep < MaxComboStep && Time.time >= _comboWindowStart && Time.time <= _comboWindowEnd)
        {
            _comboQueued = true;
        }
    }

    private void UpdateCombo()
    {
        if (!_isAttacking || Time.time < _attackEndTime)
        {
            return;
        }

        if (_comboQueued && _comboStep < MaxComboStep)
        {
            StartComboAttack(_comboStep + 1);
            return;
        }

        ResetCombo();
    }

    private void StartComboAttack(int step)
    {
        _comboStep = Mathf.Clamp(step, 1, MaxComboStep);
        _isAttacking = true;
        _comboQueued = false;

        float attackDuration = GetComboValue(_attackDurations, _comboStep, 0.6f);
        float hitDelay = GetComboValue(_hitDelays, _comboStep, 0.12f);

        _attackEndTime = Time.time + attackDuration;
        _comboWindowStart = Time.time + Mathf.Min(_comboWindowStartOffset, attackDuration);
        _comboWindowEnd = Time.time + Mathf.Min(_comboWindowEndOffset, attackDuration);
        _pendingHitTime = Time.time + hitDelay;
        _pendingHitComboStep = _comboStep;
        _hasPendingHit = true;

        if (_animator != null)
        {
            _animator.CrossFadeInFixedTime(AttackStateHashes[_comboStep - 1], 0.05f);
        }
    }

    private void ResetCombo()
    {
        _comboStep = 0;
        _isAttacking = false;
        _comboQueued = false;
        _hasPendingHit = false;

        if (_animator != null)
        {
            _animator.CrossFadeInFixedTime(MoveStateName, 0.1f);
        }
    }

    private void ApplyHit(int comboStep)
    {
        Vector3 hitCenter = transform.position + transform.forward * _hitRange;
        int hitCount = Physics.OverlapSphereNonAlloc(
            hitCenter,
            _hitRadius,
            _hitResults,
            _hitLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider target = _hitResults[i];
            if (target == null || target.transform.root == transform.root)
            {
                continue;
            }

            int damage = GetComboValue(_comboDamages, comboStep, 10);
            target.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private static T GetComboValue<T>(T[] values, int comboStep, T fallback)
    {
        int index = comboStep - 1;
        if (values == null || index < 0 || index >= values.Length)
        {
            return fallback;
        }

        return values[index];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position + transform.forward * _hitRange;
        Gizmos.DrawWireSphere(hitCenter, _hitRadius);
    }
}
