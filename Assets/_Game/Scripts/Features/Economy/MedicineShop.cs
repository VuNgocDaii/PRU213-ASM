using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class MedicineShop : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Cấu hình Shop Thuốc")]
    [SerializeField, Min(1)] private int medicineCost = 20;     // 20 Credits
    [SerializeField, Min(1)] private int medicineAmount = 1;   // 1 Medicine

    private static readonly FieldInfo InteractionTextField =
        typeof(Interactable).GetField("interactionText", BindingFlags.NonPublic | BindingFlags.Instance);

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

    public InventoryManager GetInventory()
    {
        if (featureReferences != null && featureReferences.Inventory != null)
        {
            return featureReferences.Inventory;
        }

        var module = GetComponentInParent<EconomyFeature>();
        if (module != null && module.Context != null && module.Context.Inventory != null)
        {
            return module.Context.Inventory;
        }

        var ctx = FindAnyObjectByType<DefenseSceneContext>();
        if (ctx != null && ctx.Inventory != null)
        {
            return ctx.Inventory;
        }

        return FindAnyObjectByType<InventoryManager>();
    }

    public override void Interact()
    {
        InventoryManager inventory = GetInventory();
        if (inventory == null)
        {
            Debug.LogError($"{name}: MedicineShop chưa kết nối được với Inventory!", this);
            return;
        }

        int currentCredits = inventory.GetAmount(ItemType.Credits);
        if (currentCredits < medicineCost)
        {
            Debug.LogWarning("MedicineShop: Không đủ Credits để mua thuốc!");
            return;
        }

        if (inventory.GetAmount(ItemType.Medicine) > int.MaxValue - medicineAmount)
        {
            Debug.LogWarning("MedicineShop: Kho thuốc đã đầy!");
            return;
        }

        if (inventory.TryRemove(ItemType.Credits, medicineCost))
        {
            inventory.Add(ItemType.Medicine, medicineAmount);
            Debug.Log($"MedicineShop: Đã mua thành công {medicineAmount} Medicine với giá {medicineCost} Credits!");
        }
    }

    private void UpdateHintText()
    {
        if (InteractionTextField != null)
        {
            InteractionTextField.SetValue(this, $"Mua {medicineAmount} Thuốc ({medicineCost} Credits)");
        }
    }

    [ContextMenu("Test/Buy Medicine Now")]
    public void TestBuyMedicine()
    {
        Interact();
    }
}
