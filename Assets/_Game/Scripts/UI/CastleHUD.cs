using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CastleHUD : MonoBehaviour
{
    [SerializeField] private CastleProgress progress;
    [SerializeField] private CastleHealth health;
    [SerializeField] private GameManager game;
    [SerializeField] private WaveTimer timer;
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image experienceFill;
    private void OnEnable()
    {
        if (progress != null) { progress.OnExpChanged += Exp; progress.OnCastleLevelUp += Exp; }
        if (health != null) health.OnCastleDamaged += Health;
        if (game != null) game.OnStateChanged += State;
        if (timer != null) timer.OnTimerChanged += Timer;
        if (inventory != null) inventory.OnChanged += Refresh;
    }
    private void Start() { Refresh(); }
    private void OnDisable()
    {
        if (progress != null) { progress.OnExpChanged -= Exp; progress.OnCastleLevelUp -= Exp; }
        if (health != null) health.OnCastleDamaged -= Health;
        if (game != null) game.OnStateChanged -= State;
        if (timer != null) timer.OnTimerChanged -= Timer;
        if (inventory != null) inventory.OnChanged -= Refresh;
    }
    private void Exp(int value) { Refresh(); }
    private void Health(int value, int maximum) { Refresh(); }
    private void State(GameState value) { Refresh(); }
    private void Timer(float value) { Refresh(); }
    private void Refresh()
    {
        if (statusText == null || progress == null || progress.Definition == null || health == null || game == null) return;
        string next = progress.CurrentLevel < progress.Definition.MaxLevel
            ? " / " + progress.Definition.GetLevel(progress.CurrentLevel + 1).requiredExp : " (MAX)";
        string wave = timer != null && timer.IsCounting ? $"Đợt tiếp theo: {Mathf.CeilToInt(timer.RemainingSeconds)} giây" : $"Đợt {game.CurrentWave}/10  ·  {StateLabel(game.State)}";
        string items = inventory != null ? $"Thuốc {inventory.GetAmount(ItemType.Medicine)}   ·   Sữa hỏng {inventory.GetAmount(ItemType.SpoiledMilk)}" : "";
        statusText.text = $"<b>THÀNH CẤP {progress.CurrentLevel}</b>    <color=#A9DBB7>{health.CurrentHealth}/{health.MaxHealth} HP</color>\n" +
            $"<color=#9ECDE0>EXP {progress.Experience}{next}</color>\n{wave}\n<color=#C6CFC7>{items}</color>";
        if (healthFill != null) healthFill.fillAmount = health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0;
        if (experienceFill != null)
        {
            int minimum = progress.Definition.GetLevel(progress.CurrentLevel).requiredExp;
            int maximum = progress.CurrentLevel < progress.Definition.MaxLevel ? progress.Definition.GetLevel(progress.CurrentLevel + 1).requiredExp : minimum;
            experienceFill.fillAmount = maximum > minimum ? Mathf.Clamp01((float)(progress.Experience - minimum) / (maximum - minimum)) : 1;
        }
    }
    private static string StateLabel(GameState state)
    {
        switch (state)
        {
            case GameState.Fighting: return "Đang chiến đấu";
            case GameState.Paused: return "Tạm dừng";
            case GameState.Won: return "Chiến thắng";
            case GameState.Lost: return "Thất bại";
            default: return "Chuẩn bị";
        }
    }
}
