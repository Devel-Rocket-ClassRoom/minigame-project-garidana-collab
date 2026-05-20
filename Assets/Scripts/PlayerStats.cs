using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int _maxHealth = 100;

    [SerializeField]
    private int _attackPower = 10;

    [SerializeField]
    private float _damageInvincibleDuration = 0.5f;
    
    private float _lastDamagedTime = -999f;

    private int _currentHealth;
    private Animator _animator;
    private bool _isDead = false;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int AttackPower => _attackPower;


    private void Awake()
    {
        _currentHealth = _maxHealth;
        _animator = GetComponent<Animator>();
    }

    public void IncreaseAttackPower(int amount)
    {
        _attackPower += amount;
    }

    public void IncreaseMaxHealth(int amount)
    {
        _maxHealth += amount;
        _currentHealth += amount;
    }

    public void TakeDamage(int damage)
    {
        if (Time.time < _lastDamagedTime + _damageInvincibleDuration)
        {
            return;
        }
        _lastDamagedTime = Time.time;
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        _animator.SetTrigger("isHit");
        Debug.Log(
            $"{name} took damage. Damage: {damage}, CurrentHealth: {_currentHealth}/{_maxHealth}"
        );

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died.");
        _isDead = true;
        _animator.SetTrigger("Dead");
    }
}
