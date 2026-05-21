using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField]
    private float _maxHealth = 100;

    [SerializeField]
    private float _attackPower = 10;

    [SerializeField]
    private float _maxStamina = 100f;

    [SerializeField]
    private float _staminaRegenDelay = 2f;

    [SerializeField]
    private float _staminaRegenRate = 40f;

    [SerializeField]
    private float _damageInvincibleDuration = 0.5f;

    [SerializeField]
    private int _expToLevelUp = 100;

    private int _level = 1;
    private int _currentExp = 0;
    private int _gold = 0;
    private float _currentStamina = 30f;
    private float _lastStaminaUsedTime = -999f;
    
    private float _lastDamagedTime = -999f;
    private float _currentHealth;
    private Animator _animator;
    private bool _isDead = false;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public float AttackPower => _attackPower;

    public bool IsDead => _isDead;
    public float MaxStamina => _maxStamina;
    public float CurrentStamina => _currentStamina;
    public bool HasStamina => _currentStamina > 0f;
    public int Level => _level;
    public int CurrentExp => _currentExp;
    public int ExpToLevelUp => _expToLevelUp;
    public int Gold => _gold;


    private void Awake()
    {
        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        StaminaRegen();
    }

    // 스태미나 회복 함수
    private void StaminaRegen()
    {
        if (Time.time < _lastStaminaUsedTime + _staminaRegenDelay)
        {
            return;
        }

        if (_currentStamina < _maxStamina)
        {
            _currentStamina = Mathf.Min(
                _maxStamina,
                _currentStamina + _staminaRegenRate * Time.deltaTime
            );
        }
    }

    // 스태미나 사용 
    public bool TryUseStamina(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina = Mathf.Max(0f, _currentStamina - amount);
        _lastStaminaUsedTime = Time.time;
        return true;
    }


    public void IncreaseAttackPower(float amount)
    {
        _attackPower += amount;
    }

    public void IncreaseMaxHealth(float amount)
    {
        _maxHealth += amount;
        _currentHealth += amount;
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        if (Time.time < _lastDamagedTime + _damageInvincibleDuration)
        {
            return;
        }
        _lastDamagedTime = Time.time;
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        if (_currentHealth <= 0)
        {
            Die();
            return;
        }

        _animator.SetTrigger("isHit");
        Debug.Log(
            $"{name} took damage. Damage: {damage}, CurrentHealth: {_currentHealth}/{_maxHealth}"
        );

        
    }

    public void AddGold(int amount)
    {
        _gold += amount;
        Debug.Log($"골드 :{amount}, 현재 골드: {_gold}");   
    }

    public bool SpendGold (int amount)
    {
        if (_gold < amount)
        {
            Debug.Log("골드 부족");
            return false;
        }
        _gold -= amount;
        // 후에 UI 업데이트
        return true;
    }

    public void AddExp (int amount)
    {
        if (_level >= 50)
        {
            return;
        }

        _currentExp += amount;
        Debug.Log($"경험치 +{amount}, 현재 경험치: {_currentExp}/{_expToLevelUp}");
        
        while (_currentExp >= _expToLevelUp && _level < 50)
        {
            _currentExp -= _expToLevelUp;
            LevelUp();
        }
    }

    public void AddLevel(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (_level >= 50) return;
            LevelUp();
        }
    }

    public void LevelUp()
    {
        _level++;
        Debug.Log($"레벨업! 현재 레벨: {_level}");
        // 나중에 레벨업 이펙트 UI 추가
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _currentHealth = 0f;

        Debug.Log($"{name} died.");
        _animator.SetTrigger("Dead");
    }
}
