using UnityEngine;

[DisallowMultipleComponent]
public class MedicineShop : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Cấu hình Shop Thuốc")]
    [SerializeField, Min(1)] private int medicineCost = 20;    // Cần 20 Credits
    [SerializeField, Min(1)] private int medicineAmount = 1;  // Nhận được 1 Medicine

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
            Debug.LogError($"{name}: MedicineShop chưa được Initialize FeatureReferences!", this);
            return;
        }

        int currentCredits = inventory.GetAmount(ItemType.Credits);
        if (currentCredits < medicineCost)
        {
            Debug.LogWarning("MedicineShop: Không đủ Credits để mua thuốc!");
            return;
        }

        // Kiểm tra tràn số lượng Medicine trong kho
        if (inventory.GetAmount(ItemType.Medicine) > int.MaxValue - medicineAmount)
        {
            Debug.LogWarning("MedicineShop: Kho thuốc đã đầy!");
            return;
        }

        // Trừ Credits và cộng Medicine
        if (inventory.TryRemove(ItemType.Credits, medicineCost))
        {
            inventory.Add(ItemType.Medicine, medicineAmount);
            Debug.Log($"MedicineShop: Đã mua thành công {medicineAmount} Medicine với giá {medicineCost} Credits!");
            UpdateHintText();
        }
    }

    private void UpdateHintText()
    {
        var field = typeof(Interactable).GetField("interactionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, $"Mua {medicineAmount} Thuốc ({medicineCost} Credits)");
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
