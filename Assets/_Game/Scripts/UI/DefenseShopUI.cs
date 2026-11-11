using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefenseShopUI : MonoBehaviour
{
    [System.Serializable]
    public class ShopEntry
    {
        public TowerDefinition definition;
        public Button button;
        public TextMeshProUGUI costText;
        public GameObject lockOverlay;
        public TextMeshProUGUI lockReasonText;
    }

    [SerializeField] private ShopEntry[] entries;
    [SerializeField] private GameObject selectionBorder; // Viền báo hiệu đang chọn tháp nào

    private DefenseSceneContext context;
    private TowerDefinition selectedTower;

    public void Initialize(DefenseSceneContext sceneContext)
    {
        context = sceneContext;

        if (context != null && context.Progress != null)
        {
            context.Progress.OnCastleLevelUp += OnCastleLevelUp;
            UpdateShopState(context.Progress.CurrentLevel);
        }

        for (int i = 0; i < entries.Length; i++)
        {
            int index = i;
            entries[i].button.onClick.AddListener(() => SelectTower(entries[index].definition));
        }
    }

    private void OnDestroy()
    {
        if (context != null && context.Progress != null)
        {
            context.Progress.OnCastleLevelUp -= OnCastleLevelUp;
        }
    }

    private void OnCastleLevelUp(int newLevel)
    {
        UpdateShopState(newLevel);
    }

    public void UpdateShopState(int castleLevel)
    {
        foreach (var entry in entries)
        {
            if (entry.definition == null) continue;

            bool isUnlocked = castleLevel >= entry.definition.unlockCastleLevel;
            entry.button.interactable = isUnlocked;

            if (entry.lockOverlay != null)
            {
                entry.lockOverlay.SetActive(!isUnlocked);
            }

            if (!isUnlocked && entry.lockReasonText != null)
            {
                entry.lockReasonText.text = $"Khóa (Thành Lv{entry.definition.unlockCastleLevel})";
            }

            if (entry.costText != null)
            {
                // Hiển thị cả TÊN LOẠI THÁP và GIÁ TIỀN xuống dòng
                entry.costText.text = $"{entry.definition.kind.ToString().ToUpper()}\n{entry.definition.buildPrice} C";
            }
        }
    }

    public void SelectTower(TowerDefinition def)
    {
        selectedTower = def;
        Debug.Log($"[Shop] Đã chọn mua: {def.kind} ({def.buildPrice} Credits). Bấm vào 1 ô tháp trống để xây!");
    }

    // Khi người chơi bấm vào 1 ô DefenseBuildingSlot
    public void HandleSlotClick(DefenseBuildingSlot slot)
    {
        if (selectedTower == null || slot == null) return;

        if (slot.SlotKind != DefenseBuildingSlot.Kind.Tower)
        {
            Debug.LogWarning("Ô này không phải là ô đặt tháp!");
            return;
        }

        // Gọi trực tiếp hợp đồng chuẩn của Người 1
        bool success = slot.TryBuild(
            selectedTower.prefab,
            selectedTower.buildPrice,
            DefenseBuildingSlot.Kind.Tower,
            selectedTower.unlockCastleLevel
        );

        if (success)
        {
            Debug.Log($"Xây thành công tháp {selectedTower.kind}!");

            // Khởi tạo context cho tháp vừa sinh
            if (slot.Occupant != null && slot.Occupant.TryGetComponent<TowerController>(out var tower))
            {
                tower.SetContext(context);
            }

            selectedTower = null; // Bỏ chọn sau khi xây xong
        }
        else
        {
            Debug.LogWarning("Không thể xây tháp (ô đã có người, thiếu tiền hoặc chưa đủ cấp thành)!");
        }
    }
}