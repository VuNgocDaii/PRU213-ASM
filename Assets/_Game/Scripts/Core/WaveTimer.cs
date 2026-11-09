using System;
using UnityEngine;

public class WaveTimer : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private WaveDefinition[] waves;
    [SerializeField] private bool startAutomatically;
    public float RemainingSeconds { get; private set; }
    public bool IsCounting { get; private set; }
    public event Action<float> OnTimerChanged;
    private void OnEnable() { if (game != null) game.OnWaveEnded += AfterWave; }
    private void OnDisable() { if (game != null) game.OnWaveEnded -= AfterWave; }
    private void Start() { if (startAutomatically) StartCountdown(); }
    private void AfterWave(int wave) { StartCountdown(); }
    public bool StartCountdown()
    {
        if (game == null || game.State != GameState.Preparing || IsCounting || waves == null || game.CurrentWave >= waves.Length) return false;
        WaveDefinition next = waves[game.CurrentWave];
        if (next == null) return false;
        RemainingSeconds = Mathf.Max(0f, next.preparationSeconds);
        IsCounting = true;
        OnTimerChanged?.Invoke(RemainingSeconds);
        return true;
    }
    private void Update()
    {
        if (!IsCounting || game == null) return;
        if (game.State == GameState.Paused) return;
        if (game.State != GameState.Preparing) { IsCounting = false; return; }
        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
        if (RemainingSeconds <= 0f) { IsCounting = false; game.BeginNextWave(); }
        OnTimerChanged?.Invoke(RemainingSeconds);
    }
    [ContextMenu("Foundation Test/Start Wave Countdown")]
    private void TestStart() { if (Application.isPlaying) StartCountdown(); }
}
