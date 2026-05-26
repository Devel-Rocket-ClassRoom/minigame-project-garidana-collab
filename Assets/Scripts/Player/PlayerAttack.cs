using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerAttackUpgrade))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField]
    private float[] _comboDamageMultipliers = { 1.0f, 1.2f, 1.8f };

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

    // 콘보 히트스탑 시간
    [SerializeField]
    private float[] _hitStopDurations = { 0.03f, 0.04f, 0.06f };

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
    private PlayerStats _playerStats;
    private PlayerAttackUpgrade _attackUpgrade;
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
        _playerStats = GetComponent<PlayerStats>();
        _attackUpgrade = GetComponent<PlayerAttackUpgrade>();
        _animator = GetComponent<Animator>();

        if (_playerStats == null)
        {
            _playerStats = gameObject.AddComponent<PlayerStats>();
            Debug.LogWarning("PlayerStats was missing on PlayerAttack object, so it was added at runtime.");
        }

        if (_attackUpgrade == null)
        {
            _attackUpgrade = gameObject.AddComponent<PlayerAttackUpgrade>();
            Debug.LogWarning("PlayerAttackUpgrade was missing on PlayerAttack object, so it was added at runtime.");
        }
    }

    private void Update()
    {
        if (_playerStats != null && _playerStats.IsDead)
        {
            _playerInput.UseAttackInput();
            return;
        }

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

        Debug.Log(
            $"Player attack started. ComboStep: {_comboStep}, AttackStage: {GetAttackStageName()}, AttackPower: {GetAttackPower()}, ExpectedDamage: {CalculateDamage(_comboStep)}"
        );

        _attackUpgrade.PlayAttackEffect();

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
        float hitRange = GetHitRange();
        float hitRadius = GetHitRadius();
        Vector3 hitCenter = transform.position + transform.forward * hitRange;
        int hitCount = Physics.OverlapSphereNonAlloc(
            hitCenter,
            hitRadius,
            _hitResults,
            _hitLayers,
            QueryTriggerInteraction.Ignore
        );

        int validTargetCount = 0;
        bool dealtDamage = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider target = _hitResults[i];
            if (target == null || target.transform.root == transform.root)
            {
                continue;
            }

            validTargetCount++;
            int damage = CalculateDamage(comboStep);
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                Debug.Log(
                    $"Player hit collider, but target is not damageable. Target: {target.name}, ComboStep: {comboStep}, Damage: {damage}"
                );
                continue;
            }

            Debug.Log(
                $"Player hit damageable target. Target: {target.name}, ComboStep: {comboStep}, AttackStage: {GetAttackStageName()}, AttackPower: {GetAttackPower()}, Damage: {damage}"
            );
            damageable.TakeDamage(damage);
            dealtDamage = true;
        }

        if (dealtDamage)
        {
            HitStopManager.Request(GetComboValue(_hitStopDurations, comboStep, 0.03f));
        }

        if (validTargetCount == 0)
        {
            Debug.Log(
                $"Player attack found no valid targets. ComboStep: {comboStep}, HitCenter: {hitCenter}, HitRadius: {hitRadius}, RawHitCount: {hitCount}"
            );
        }
    }

    private int CalculateDamage(int comboStep)
    {
        float attackPower = GetAttackPower();
        float comboMultiplier = GetComboValue(_comboDamageMultipliers, comboStep, 1.0f);
        float stageMultiplier = _attackUpgrade != null ? _attackUpgrade.CurrentDamageMultiplier : 1f;
        return Mathf.RoundToInt(attackPower * comboMultiplier * stageMultiplier);
    }

    private float GetAttackPower()
    {
        return _playerStats != null ? _playerStats.AttackPower : 10f;
    }

    private float GetHitRange()
    {
        return _attackUpgrade != null ? _attackUpgrade.CurrentHitRange : _hitRange;
    }

    private float GetHitRadius()
    {
        return _attackUpgrade != null ? _attackUpgrade.CurrentHitRadius : _hitRadius;
    }

    private string GetAttackStageName()
    {
        return _attackUpgrade != null ? _attackUpgrade.CurrentStageName : "Normal";
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
        float hitRange = Application.isPlaying ? GetHitRange() : _hitRange;
        float hitRadius = Application.isPlaying ? GetHitRadius() : _hitRadius;
        Vector3 hitCenter = transform.position + transform.forward * hitRange;
        Gizmos.DrawWireSphere(hitCenter, hitRadius);
    }
}
