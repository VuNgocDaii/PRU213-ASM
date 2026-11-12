using UnityEngine;

[DisallowMultipleComponent]
public class MilkShop : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Cấu hình Shop Sữa")]
    [SerializeField, Min(1)] private int milkSellPrice = 25;    // Nhận 25 Credits
    [SerializeField, Min(1)] private int milkBatchSize = 1;     // Cần 1 BottledMilk

    private void Awake()
    {
        if (featureReferences == null)
        {
            featureReferences = GetComponentInParent<FeatureReferences>();
        }
        UpdateHintText();
    }

    private void Update()
    {
        UpdateHintText();
    }

    public override void Interact()
    {
        var inventory = GetInventory();
        if (inventory == null)
        {
            Debug.LogError($"{name}: MilkShop chưa được Initialize FeatureReferences!", this);
            return;
        }

        // Kiểm tra đủ BottledMilk để bán
        if (inventory.GetAmount(ItemType.BottledMilk) < milkBatchSize)
        {
            Debug.LogWarning("MilkShop: Không có BottledMilk để bán!", this);
            return;
        }

        int reward = milkBatchSize * milkSellPrice;

        // Kiểm tra tràn số lượng Credits trong kho
        if (inventory.GetAmount(ItemType.Credits) > int.MaxValue - reward)
        {
            Debug.LogWarning("MilkShop: Kho Credits đã đạt giới hạn tối đa!", this);
            return;
        }

        // Trừ BottledMilk và cộng Credits
        if (inventory.TryRemove(ItemType.BottledMilk, milkBatchSize))
        {
            inventory.Add(ItemType.Credits, reward);
            Debug.Log($"MilkShop: Đã bán thành công {milkBatchSize} BottledMilk, nhận được {reward} Credits!");
            UpdateHintText();
        }
    }

    private void UpdateHintText()
    {
        var field = typeof(Interactable).GetField("interactionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return;

        var inv = GetInventory();
        if (inv == null)
        {
            field.SetValue(this, "Cửa hàng bán sữa");
            return;
        }

        bool hasBottled = inv.GetAmount(ItemType.BottledMilk) >= milkBatchSize;
        if (hasBottled)
        {
            field.SetValue(this, $"Bán {milkBatchSize} BottledMilk (+{milkBatchSize * milkSellPrice} Credits)");
        }
        else
        {
            field.SetValue(this, "Cửa hàng bán sữa (Không có BottledMilk để bán)");
        }
    }

    private InventoryManager GetInventory()
    {
        if (featureReferences != null && featureReferences.Inventory != null)
        {
            return featureReferences.Inventory;
        }

        return InventoryManager.Instance;
    }
}
