using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BattleUI : MonoBehaviour
{
    [Header("Banner Lên Cấp Thành")]
    [SerializeField] private GameObject levelUpBanner;
    [SerializeField] private TextMeshProUGUI txtLevelUp;

    [Header("Màn hình Kết thúc")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI txtGameOverTitle;
    [SerializeField] private Button btnRestart;

    [Header("Menu Tạm dừng (Pause)")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button btnResume;

    private DefenseSceneContext context;

    public void Initialize(DefenseSceneContext sceneContext)
    {
        context = sceneContext;

        if (levelUpBanner != null) levelUpBanner.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        if (context != null)
        {
            if (context.Progress != null)
                context.Progress.OnCastleLevelUp += ShowLevelUpBanner;

            if (context.Game != null)
                context.Game.OnGameOver += HandleGameOver;
        }

        if (btnResume != null)
            btnResume.onClick.AddListener(() => TogglePause(false));
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            if (context.Progress != null)
                context.Progress.OnCastleLevelUp -= ShowLevelUpBanner;

            if (context.Game != null)
                context.Game.OnGameOver -= HandleGameOver;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            bool isPaused = pausePanel != null && pausePanel.activeSelf;
            TogglePause(!isPaused);
        }
    }

    private void TogglePause(bool pause)
    {
        if (context == null || context.Game == null) return;
        context.Game.SetPaused(pause);
        if (pausePanel != null) pausePanel.SetActive(pause);
    }

    private void ShowLevelUpBanner(int newLevel)
    {
        if (levelUpBanner == null) return;

        txtLevelUp.text = $"THÀNH ĐẠT CẤP {newLevel}!\nMở khóa khu đất và tháp phòng thủ mới!";
        levelUpBanner.SetActive(true);
        CancelInvoke(nameof(HideLevelUpBanner));
        Invoke(nameof(HideLevelUpBanner), 3.5f);
    }

    private void HideLevelUpBanner()
    {
        if (levelUpBanner != null) levelUpBanner.SetActive(false);
    }

    private void HandleGameOver(bool won)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);
        txtGameOverTitle.text = won ? "<color=green>CHIẾN THẮNG!</color>\nĐã bảo vệ thành công trang trại!"
                                    : "<color=red>THẤT BẠI!</color>\nThành trì đã bị phá hủy!";
    }
}