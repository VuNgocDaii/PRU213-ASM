using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class MilkSaleShop : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Bán BottledMilk (0 = tắt)")]
    [SerializeField, Min(0)] private int bottledMilkSellPrice = 25;
    [SerializeField, Min(1)] private int bottledMilkBatchSize = 1;

    [Header("Bán RawMilk (0 = tắt)")]
    [SerializeField, Min(0)] private int rawMilkSellPrice = 10;
    [SerializeField, Min(1)] private int rawMilkBatchSize = 1;

    [Header("Hỗ trợ đổ bỏ Sữa hỏng (SpoiledMilk)")]
    [SerializeField] private bool allowDiscardSpoiledMilk = true;

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
            Debug.LogError($"{name}: MilkSaleShop chưa kết nối được với Inventory!", this);
            return;
        }

        // 1. Ưu tiên bán BottledMilk trước nếu có đủ
        if (bottledMilkSellPrice > 0 && TrySell(inventory, ItemType.BottledMilk, bottledMilkBatchSize, bottledMilkSellPrice))
        {
            UpdateHintText();
            return;
        }

        // 2. Nếu không thì bán RawMilk
        if (rawMilkSellPrice > 0 && TrySell(inventory, ItemType.RawMilk, rawMilkBatchSize, rawMilkSellPrice))
        {
            UpdateHintText();
            return;
        }

        // 3. Nếu không có sữa tốt nhưng có SpoiledMilk (sữa hỏng) -> Đổ bỏ giúp người chơi dọn kho
        if (allowDiscardSpoiledMilk && TryDiscardSpoiledMilk(inventory))
        {
            UpdateHintText();
            return;
        }

        Debug.LogWarning("MilkSaleShop: Không có sữa để bán hoặc đổ bỏ!", this);
    }

    private bool TrySell(InventoryManager inv, ItemType milk, int batch, int pricePerUnit)
    {
        if (inv.GetAmount(milk) < batch) return false;

        int reward = batch * pricePerUnit;
        if (inv.GetAmount(ItemType.Credits) > int.MaxValue - reward) return false;

        if (!inv.TryRemove(milk, batch)) return false;

        inv.Add(ItemType.Credits, reward);
        Debug.Log($"MilkSaleShop: Đã bán {batch} {milk} → +{reward} Credits.");
        return true;
    }

    private bool TryDiscardSpoiledMilk(InventoryManager inv)
    {
        int spoiledAmount = inv.GetAmount(ItemType.SpoiledMilk);
        if (spoiledAmount <= 0) return false;

        if (inv.TryRemove(ItemType.SpoiledMilk, spoiledAmount))
        {
            Debug.Log($"MilkSaleShop: Đã đổ bỏ {spoiledAmount} SpoiledMilk hỏng để dọn kho.");
            return true;
        }

        return false;
    }

    private void UpdateHintText()
    {
        if (InteractionTextField == null) return;

        InventoryManager inv = GetInventory();
        if (inv == null)
        {
            InteractionTextField.SetValue(this, "Cửa hàng bán sữa");
            return;
        }

        if (bottledMilkSellPrice > 0 && inv.GetAmount(ItemType.BottledMilk) >= bottledMilkBatchSize)
        {
            InteractionTextField.SetValue(this, $"Bán {bottledMilkBatchSize} BottledMilk (+{bottledMilkBatchSize * bottledMilkSellPrice} Credits)");
        }
        else if (rawMilkSellPrice > 0 && inv.GetAmount(ItemType.RawMilk) >= rawMilkBatchSize)
        {
            InteractionTextField.SetValue(this, $"Bán {rawMilkBatchSize} RawMilk (+{rawMilkBatchSize * rawMilkSellPrice} Credits)");
        }
        else if (allowDiscardSpoiledMilk && inv.GetAmount(ItemType.SpoiledMilk) > 0)
        {
            InteractionTextField.SetValue(this, $"Đổ bỏ {inv.GetAmount(ItemType.SpoiledMilk)} Sữa hỏng (SpoiledMilk)");
        }
        else
        {
            InteractionTextField.SetValue(this, "Cửa hàng bán sữa (Không có sữa để bán)");
        }
    }

    [ContextMenu("Test/Sell Milk Now")]
    public void TestSellMilk()
    {
        Interact();
    }
}
