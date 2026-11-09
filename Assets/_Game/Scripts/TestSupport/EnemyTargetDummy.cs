using UnityEngine;

// Test-only fixture. Never included in Main. Enemy/tower workspaces use the same public contracts.
public class EnemyTargetDummy : DefenseModule, IEnemyTarget, ISlowable
{
    [SerializeField] private bool flying;
    [SerializeField, Min(1)] private int health = 100;
    public Transform TargetTransform => transform;
    public bool IsAlive => health > 0 && isActiveAndEnabled;
    public bool IsFlying => flying;
    public float LastSpeedMultiplier { get; private set; } = 1;
    protected override void OnConnected() { Context.RegisterEnemy(this); }
    protected override void OnDisconnected() { Context.UnregisterEnemy(this); }
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        health = Mathf.Max(0,health-amount);
        if (health == 0) { Context.UnregisterEnemy(this); gameObject.SetActive(false); }
    }
    public void ApplySlow(float multiplier,float seconds) { if (seconds > 0) LastSpeedMultiplier = Mathf.Clamp01(multiplier); }
}
