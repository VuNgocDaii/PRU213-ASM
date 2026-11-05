using UnityEngine;

public class FeedShop : Interactable
{
    [Header("Base Reference")]
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Shop Config")]
    [SerializeField, Min(1)] private int feedCost = 10;     // Cần 10 Credits
    [SerializeField, Min(1)] private int feedAmount = 5;     // Nhận được 5 Feed

    private void Start()
    {
        // Cập nhật text gợi ý khi lại gần
        // Ví dụ: Nhấn E: Mua 5 Feed (10 Credits)
    }

    public override void Interact()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogError(name + ": FeedShop chưa được Initialize FeatureReferences!", this);
            return;
        }

        InventoryManager inventory = featureReferences.Inventory;

        int currentCredits = inventory.GetAmount(ItemType.Credits);
        if (currentCredits < feedCost)
        {
            Debug.LogWarning("FeedShop: Không đủ Credits để mua Feed!");
            return;
        }

        // Kiểm tra tràn số int Feed
        if (inventory.GetAmount(ItemType.Feed) > int.MaxValue - feedAmount)
        {
            Debug.LogWarning("FeedShop: Kho Feed đã đầy!");
            return;
        }

        // Trừ Credits và cộng Feed
        if (inventory.TryRemove(ItemType.Credits, feedCost))
        {
            inventory.Add(ItemType.Feed, feedAmount);
            Debug.Log($"FeedShop: Đã mua thành công {feedAmount} Feed với giá {feedCost} Credits!");
        }
    }
}