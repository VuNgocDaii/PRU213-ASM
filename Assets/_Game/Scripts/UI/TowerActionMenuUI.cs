using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerActionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI txtTitle;
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
        selectedTower = tower;
        selectedSlot = slot;

        if (selectedTower == null)
        {
            CloseMenu();
            return;
        }

        menuPanel.SetActive(true);
        RefreshUI();
    }

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
        selectedTower = null;
        selectedSlot = null;
    }

    private void RefreshUI()
    {
        if (selectedTower == null) return;

        TowerDefinition def = selectedTower.Definition;
        int lvl = selectedTower.CurrentLevel;
        txtTitle.text = $"{def.kind} (Cấp {lvl})";

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
            txtUpgradeCost.text = $"{upgradePrice} C";
        }

        // Bán tháp (hoàn lại 60% tiền)
        int refund = Mathf.RoundToInt(def.buildPrice * 0.6f);
        txtSellRefund.text = $"+{refund} C";
    }

    private void OnUpgradeClicked()
    {
        if (selectedTower == null || context == null) return;

        int lvl = selectedTower.CurrentLevel;
        int price = selectedTower.Definition.upgradePrices[lvl - 1];

        if (context.Inventory.TryRemove(ItemType.Credits, price))
        {
            selectedTower.ApplyLevel(lvl + 1);
            RefreshUI();
        }
    }

    private void OnSellClicked()
    {
        if (selectedTower == null || context == null) return;

        int refund = Mathf.RoundToInt(selectedTower.Definition.buildPrice * 0.6f);
        context.Inventory.Add(ItemType.Credits, refund);

        Destroy(selectedTower.gameObject);
        CloseMenu();
    }
}