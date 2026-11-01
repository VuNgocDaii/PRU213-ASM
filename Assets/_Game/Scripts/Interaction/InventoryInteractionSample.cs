using UnityEngine;

// Chỗ nối mẫu: đọc kho để thử tham chiếu, chưa có logic tính năng.
public class InventoryInteractionSample : Interactable
{
    // Giữ ô cũ để scene/prefab cũ không mất dữ liệu khi chuyển sang Initialize.
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private ItemType inspectItem = ItemType.Credits;
    [SerializeField] private BuildingConnectionSample buildingTest;

    public override void Interact()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning(name + ": Chưa Initialize FeatureReferences, chưa cho tương tác.", this);
            return;
        }

        InventoryManager sceneInventory = featureReferences.Inventory;
        if (inventory != null && inventory != sceneInventory)
        {
            Debug.LogError(name + ": Ô kho cũ khác kho đã Initialize. Hãy kiểm tra scene.", this);
            return;
        }
        int amount = sceneInventory.GetAmount(inspectItem);
        Debug.Log(name + ": Tương tác mẫu. " + inspectItem + " hiện có: " + amount +
            ". Chưa cộng/trừ tài nguyên.", this);
        if (buildingTest != null)
        {
            buildingTest.RunConnectionTest();
        }
    }
}
