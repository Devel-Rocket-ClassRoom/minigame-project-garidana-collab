using UnityEngine;

public class MonsterStats : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int _maxHealth = 50;

    private int _currentHealth;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    private Animator _animator;

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        Debug.Log(
            $"{name} took damage. Damage: {damage}, CurrentHealth: {_currentHealth}/{_maxHealth}"
        );

        _animator.SetTrigger("isHit");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died.");
        _animator.SetBool("isDead", true);
        Destroy(gameObject);
    }
}
