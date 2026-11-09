using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CastleHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private CastleProgress progress;
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public event Action<int, int> OnCastleDamaged;
    public event Action OnCastleDestroyed;
    public bool IsDestroyed => CurrentHealth <= 0;

    private void Awake()
    {
        MaxHealth = progress != null && progress.Definition != null ? progress.Definition.GetLevel(progress.CurrentLevel).maxHealth : 120;
        CurrentHealth = MaxHealth;
    }
    private void OnEnable() { if (progress != null) progress.OnCastleLevelUp += ChangeLevel; }
    private void OnDisable() { if (progress != null) progress.OnCastleLevelUp -= ChangeLevel; }
    private void ChangeLevel(int level)
    {
        if (IsDestroyed) return;
        int newMax = progress.Definition.GetLevel(level).maxHealth;
        // Keep damage already taken; leveling grants only the extra health capacity.
        CurrentHealth = Mathf.Clamp(CurrentHealth + newMax - MaxHealth, 1, newMax);
        MaxHealth = newMax;
        OnCastleDamaged?.Invoke(CurrentHealth, MaxHealth);
    }
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDestroyed) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        bool destroyed = IsDestroyed;
        OnCastleDamaged?.Invoke(CurrentHealth, MaxHealth);
        if (destroyed) OnCastleDestroyed?.Invoke();
    }
    [ContextMenu("Foundation Test/Take 25 Damage")]
    private void TestDamage() { if (Application.isPlaying) TakeDamage(25); }
}
