using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Phím điều khiển nhanh cho EnemiesWorkspace (không đặt trong Main).
// N: wave tiếp   F: thả 1 ruồi   H: mọi quái mất 10 máu   J: làm chậm mọi quái
public class EnemyControlKeys : DefenseModule
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private EnemyDefinition flyDefinition;

    private void Update()
    {
        Keyboard keys = Keyboard.current;
        if (keys == null || Context == null) return;

        if (keys.nKey.wasPressedThisFrame) Context.Game.BeginNextWave();
        if (keys.fKey.wasPressedThisFrame) spawner.SpawnEnemy(flyDefinition, 1f);

        // Copy danh sách trước, vì quái chết sẽ tự rời Context.Enemies
        foreach (IEnemyTarget enemy in new List<IEnemyTarget>(Context.Enemies))
        {
            if (keys.hKey.wasPressedThisFrame) enemy.TakeDamage(10);
            if (keys.jKey.wasPressedThisFrame && enemy is ISlowable slow) slow.ApplySlow(0.5f, 3f);
        }
    }
}
