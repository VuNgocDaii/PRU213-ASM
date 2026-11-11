using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class MilkShop : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Bán BottledMilk (0 = tắt)")]
    [SerializeField, Min(0)] private int bottledMilkSellPrice = 25;
    [SerializeField, Min(1)] private int bottledMilkBatchSize = 1;

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

    public override void Interact()
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogError($"{name}: MilkShop chưa kết nối được với Inventory!", this);
            return;
        }

        // Chỉ bán BottledMilk
        if (bottledMilkSellPrice > 0 && TrySell(inventory, ItemType.BottledMilk, bottledMilkBatchSize, bottledMilkSellPrice))
        {
            UpdateHintText();
            return;
        }

        Debug.LogWarning("MilkShop: Không có BottledMilk để bán!", this);
    }

    private bool TrySell(InventoryManager inv, ItemType milk, int batch, int pricePerUnit)
    {
        if (inv.GetAmount(milk) < batch) return false;

        int reward = batch * pricePerUnit;
        if (inv.GetAmount(ItemType.Credits) > int.MaxValue - reward) return false;

        if (!inv.TryRemove(milk, batch)) return false;

        inv.Add(ItemType.Credits, reward);
        Debug.Log($"MilkShop: Đã bán {batch} {milk} → +{reward} Credits.");
        return true;
    }

    private void UpdateHintText()
    {
        if (InteractionTextField == null) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            InteractionTextField.SetValue(this, "Cửa hàng bán sữa");
            return;
        }

        bool hasBottled = bottledMilkSellPrice > 0 && inv.GetAmount(ItemType.BottledMilk) >= bottledMilkBatchSize;

        if (hasBottled)
        {
            InteractionTextField.SetValue(this, $"Bán {bottledMilkBatchSize} BottledMilk (+{bottledMilkBatchSize * bottledMilkSellPrice} Credits)]");
        }
        else
        {
            InteractionTextField.SetValue(this, "Cửa hàng bán sữa (Không có BottledMilk để bán)");
        }
    }

    [ContextMenu("Test/Sell Milk Now")]
    public void TestSellMilk()
    {
        Interact();
    }
}
