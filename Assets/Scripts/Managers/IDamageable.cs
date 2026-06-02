using UnityEngine;

public readonly struct DamageHitInfo
{
    public DamageHitInfo(float damage)
    {
        Damage = damage;
        HitPoint = Vector3.zero;
        HitDirection = Vector3.zero;
        AttackStage = null;
        HasHitPoint = false;
    }

    public DamageHitInfo(
        float damage,
        Vector3 hitPoint,
        Vector3 hitDirection,
        AttackStageData attackStage = null)
    {
        Damage = damage;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
        AttackStage = attackStage;
        HasHitPoint = true;
    }

    public float Damage { get; }
    public Vector3 HitPoint { get; }
    public Vector3 HitDirection { get; }
    public AttackStageData AttackStage { get; }
    public bool HasHitPoint { get; }

    public static DamageHitInfo FromDamage(float damage)
    {
        return new DamageHitInfo(damage);
    }
}

public interface IDamageable
{
    void TakeDamage(float damage);
    void TakeHit(DamageHitInfo hitInfo);
}
