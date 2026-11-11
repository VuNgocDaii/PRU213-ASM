using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thả quái theo wave (Data/Defense/Wave01..10).
// Thả hết và không còn quái sống -> thưởng completionCredits 1 lần rồi CompleteWave.
// Quái đã biến mất được dùng lại (pool) thay vì tạo mới.
public class WaveSpawner : DefenseModule
{
    private readonly List<EnemyController> pool = new List<EnemyController>();
    private WaveDefinition wave;
    private int waveNumber;
    private bool isSpawning;
    private int aliveCount;

    public int AliveCount => aliveCount;

    protected override void OnConnected() { Context.Game.OnWaveStarted += StartWave; }
    protected override void OnDisconnected() { Context.Game.OnWaveStarted -= StartWave; }

    private void StartWave(int number)
    {
        if (number < 1 || number > Context.Waves.Count) return;
        wave = Context.Waves[number - 1];
        waveNumber = number;
        StopAllCoroutines();   // wave cũ còn đang thả dở thì dừng, không để 2 wave thả chồng nhau
        StartCoroutine(SpawnWave(wave));
    }

    // Thả lần lượt từng nhóm, mỗi con cách nhau spawnInterval giây.
    // Tạm dừng thì timeScale = 0 nên WaitForSeconds cũng đứng; thắng/thua thì ngừng thả.
    private IEnumerator SpawnWave(WaveDefinition current)
    {
        isSpawning = true;
        foreach (WaveDefinition.Group group in current.groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                if (Context.Game.State == GameState.Won || Context.Game.State == GameState.Lost) yield break;
                SpawnEnemy(group.enemy, current.healthMultiplier);
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
        isSpawning = false;
        CheckWaveFinished();
    }

    // Public để phím F (EnemyControlKeys) cũng thả được ruồi
    public EnemyController SpawnEnemy(EnemyDefinition type, float healthMultiplier)
    {
        EnemyController enemy = pool.Find(e => !e.gameObject.activeSelf && e.Definition == type);
        if (enemy == null)
        {
            enemy = Instantiate(type.prefab, transform).GetComponent<EnemyController>();
            enemy.OnDespawned += HandleDespawned;
            pool.Add(enemy);
        }

        EnemyRoute route = PickRoute();
        if (enemy is GroundEnemy ground) ground.SetRoute(route);

        aliveCount++;
        enemy.Spawn(Context, route != null ? route.SpawnPosition : transform.position, healthMultiplier);
        return enemy;
    }

    private void HandleDespawned(EnemyController enemy)
    {
        aliveCount--;
        CheckWaveFinished();
    }

    private void CheckWaveFinished()
    {
        if (wave == null || isSpawning || aliveCount > 0) return;

        Context.Inventory.Add(ItemType.Credits, wave.completionCredits);   // Add tự từ chối nếu tràn int
        Context.Game.CompleteWave(waveNumber);
        wave = null;   // đánh dấu đã thưởng, không thưởng lần 2
    }

    // Chọn ngẫu nhiên 1 đường đang mở (khu chưa mở thì đường bị tắt)
    private EnemyRoute PickRoute()
    {
        List<EnemyRoute> open = new List<EnemyRoute>();
        foreach (EnemyRoute route in Context.Routes)
            if (route != null && route.gameObject.activeInHierarchy) open.Add(route);
        return open.Count > 0 ? open[Random.Range(0, open.Count)] : null;
    }
}
