using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerActionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtStats;
    [SerializeField] private Button btnUpgrade;
    [SerializeField] private TextMeshProUGUI txtUpgradeCost;
    [SerializeField] private Button btnSell;
    [SerializeField] private TextMeshProUGUI txtSellRefund;

    private DefenseSceneContext context;
    private TowerController selectedTower;
    private DefenseBuildingSlot selectedSlot;

    public void Initialize(DefenseSceneContext sceneContext)
    {
        context = sceneContext;
        menuPanel.SetActive(false);
        btnUpgrade.onClick.AddListener(OnUpgradeClicked);
        btnSell.onClick.AddListener(OnSellClicked);
    }

    public void OpenMenu(TowerController tower, DefenseBuildingSlot slot)
    {
        if (selectedTower != null) selectedTower.SetRangeVisible(false);
        selectedTower = tower;
        selectedSlot = slot;

        if (selectedTower == null)
        {
            CloseMenu();
            return;
        }

        menuPanel.SetActive(true);
        selectedTower.SetRangeVisible(true);
        RefreshUI();
    }

    public void CloseMenu()
    {
        if (selectedTower != null) selectedTower.SetRangeVisible(false);
        menuPanel.SetActive(false);
        selectedTower = null;
        selectedSlot = null;
    }

    private void RefreshUI()
    {
        if (selectedTower == null) return;

        TowerDefinition def = selectedTower.Definition;
        int lvl = selectedTower.CurrentLevel;
        txtTitle.text = $"{def.kind} {TowerDefinition.LevelLabel(lvl)}";
        if (txtStats != null)
        {
            string damage = $"Sát thương: {selectedTower.CurrentDamage}";
            string range = $"Tầm bắn: {selectedTower.CurrentRange:0.##}";
            if (lvl < 3)
            {
                damage += $" → {def.GetDamage(lvl + 1)}";
                range += $" → {def.GetRange(lvl + 1):0.##}";
            }
            txtStats.text = damage + "\n" + range;
        }

        // Kiểm tra điều kiện nâng cấp
        int castleLvl = context.Progress.CurrentLevel;
        if (lvl >= 3)
        {
            btnUpgrade.interactable = false;
            txtUpgradeCost.text = "Tối đa";
        }
        else if (lvl >= castleLvl)
        {
            // Trần cấp tháp bị khóa bởi cấp thành: Thành Lv1 -> Tháp max Lv1
            btnUpgrade.interactable = false;
            txtUpgradeCost.text = $"Cần Thành Lv{lvl + 1}";
        }
        else
        {
            int upgradePrice = def.upgradePrices[lvl - 1];
            bool hasMoney = context.Inventory.GetAmount(ItemType.Credits) >= upgradePrice;
            btnUpgrade.interactable = hasMoney;
            txtUpgradeCost.text = $"Lên {TowerDefinition.LevelLabel(lvl + 1)} · {upgradePrice} C";
        }

        // Bán tháp (hoàn lại 60% tiền)
        var building = selectedSlot != null ? selectedSlot.GetComponent<BuildingController>() : null;
        int refund = building != null ? building.CalculateRefund() : Mathf.RoundToInt(def.buildPrice * 0.6f);
        txtSellRefund.text = $"Bán · +{refund} C";
    }

    private void OnUpgradeClicked()
    {
        if (selectedTower == null || context == null || selectedSlot == null) return;
        var upgrade = selectedSlot.GetComponent<BuildingUpgradeController>();
        if (upgrade != null) upgrade.TryUpgrade();
        RefreshUI();
    }

    private void OnSellClicked()
    {
        if (selectedTower == null || context == null || selectedSlot == null) return;
        var building = selectedSlot.GetComponent<BuildingController>();
        if (building != null && building.TrySell(out _)) CloseMenu();
    }
}
