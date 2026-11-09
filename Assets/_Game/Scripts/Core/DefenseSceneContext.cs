using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DefenseSceneContext : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private GameManager game;
    [SerializeField] private CastleProgress progress;
    [SerializeField] private CastleHealth castleHealth;
    [SerializeField] private WaveTimer timer;
    [SerializeField] private Transform castleTarget;
    [SerializeField] private DefenseBuildingSlot[] slots;
    [SerializeField] private EnemyRoute[] routes;
    [SerializeField] private WaveDefinition[] waves;
    private readonly List<IEnemyTarget> enemies = new List<IEnemyTarget>();
    private readonly List<ICowDiseaseTarget> cows = new List<ICowDiseaseTarget>();
    private IReadOnlyList<IEnemyTarget> enemyView;
    private IReadOnlyList<ICowDiseaseTarget> cowView;
    public InventoryManager Inventory => inventory;
    public GameManager Game => game;
    public CastleProgress Progress => progress;
    public CastleHealth CastleHealth => castleHealth;
    public WaveTimer Timer => timer;
    public Transform CastleTarget => castleTarget;
    public IReadOnlyList<DefenseBuildingSlot> Slots => slots;
    public IReadOnlyList<EnemyRoute> Routes => routes;
    public IReadOnlyList<WaveDefinition> Waves => waves;
    public IReadOnlyList<IEnemyTarget> Enemies => enemyView ?? (enemyView = enemies.AsReadOnly());
    public IReadOnlyList<ICowDiseaseTarget> Cows => cowView ?? (cowView = cows.AsReadOnly());
    public bool IsReady => inventory != null && game != null && progress != null && castleHealth != null && timer != null && castleTarget != null;
    public event Action OnTargetsChanged;

    public void RegisterEnemy(IEnemyTarget target)
    { if (IsValid(target) && !enemies.Contains(target)) { enemies.Add(target); OnTargetsChanged?.Invoke(); } }
    public void UnregisterEnemy(IEnemyTarget target)
    { if (enemies.Remove(target)) OnTargetsChanged?.Invoke(); }
    public void RegisterCow(ICowDiseaseTarget target)
    { if (IsValid(target) && !cows.Contains(target)) { cows.Add(target); OnTargetsChanged?.Invoke(); } }
    public void UnregisterCow(ICowDiseaseTarget target)
    { if (cows.Remove(target)) OnTargetsChanged?.Invoke(); }
    private static bool IsValid(object target)
    { return target != null && (!(target is UnityEngine.Object obj) || obj != null); }
}
