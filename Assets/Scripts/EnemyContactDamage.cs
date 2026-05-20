using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField]
    private int _damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        
        damageable.TakeDamage(_damage);
    }
}