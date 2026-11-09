using UnityEngine;

// Owned by base. Towers depend on this contract, never on EnemyController.
public interface IEnemyTarget : IDamageable
{
    Transform TargetTransform { get; }
    bool IsAlive { get; }
    bool IsFlying { get; }
}

public interface ISlowable
{
    // 0.7 = 70% of original speed; the enemy module owns stacking/expiry.
    void ApplySlow(float speedMultiplier, float seconds);
}
