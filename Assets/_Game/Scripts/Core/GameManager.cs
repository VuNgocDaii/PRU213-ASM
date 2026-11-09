using System;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    [SerializeField] private CastleProgress castleProgress;
    [SerializeField] private CastleHealth castleHealth;
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private Behaviour[] playerControls;
    [SerializeField, Min(1)] private int totalWaves = 10;
    public GameState State { get; private set; } = GameState.Preparing;
    public int CurrentWave { get; private set; }
    public event Action<GameState> OnStateChanged;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveEnded;
    public event Action<int, int> OnEnemyKilled;
    public event Action<bool> OnGameOver;
    private GameState stateBeforePause;
    private float timeScaleBeforePause = 1f;

    private void OnEnable() { if (castleHealth != null) castleHealth.OnCastleDestroyed += Lose; }
    private void OnDisable()
    {
        if (castleHealth != null) castleHealth.OnCastleDestroyed -= Lose;
        if (State == GameState.Paused) Time.timeScale = timeScaleBeforePause;
    }
    public bool BeginNextWave()
    {
        if (State != GameState.Preparing || CurrentWave >= totalWaves) return false;
        CurrentWave++;
        SetState(GameState.Fighting);
        OnWaveStarted?.Invoke(CurrentWave);
        return true;
    }
    // WaveSpawner calls this only after spawning has ended AND no live enemies remain.
    public bool CompleteWave(int wave)
    {
        if (State != GameState.Fighting || wave != CurrentWave) return false;
        SetState(GameState.Preparing);
        OnWaveEnded?.Invoke(wave);
        return true;
    }
    // Enemy code must call once per death, never on reaching the castle or returning to pool.
    public void ReportEnemyKilled(int exp, int credits, bool isFinalBoss = false)
    {
        if (State != GameState.Fighting) return;
        castleProgress?.AddExperience(Mathf.Max(0, exp));
        inventory?.Add(ItemType.Credits, Mathf.Max(0, credits));
        OnEnemyKilled?.Invoke(Mathf.Max(0, exp), Mathf.Max(0, credits));
        if (isFinalBoss && CurrentWave == totalWaves) Finish(true);
    }
    public void SetPaused(bool paused)
    {
        if (paused && (State == GameState.Preparing || State == GameState.Fighting))
        {
            stateBeforePause = State;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }
        else if (!paused && State == GameState.Paused)
        {
            Time.timeScale = timeScaleBeforePause;
            SetState(stateBeforePause);
        }
    }
    private void Lose() { Finish(false); }
    private void Finish(bool won)
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        if (State == GameState.Paused) Time.timeScale = timeScaleBeforePause;
        SetState(won ? GameState.Won : GameState.Lost);
        OnGameOver?.Invoke(won);
    }
    private void SetState(GameState state)
    {
        State = state;
        if (playerControls != null)
            foreach (Behaviour control in playerControls)
                if (control != null) control.enabled = state == GameState.Preparing || state == GameState.Fighting;
        OnStateChanged?.Invoke(state);
    }
}
