using UnityEngine;

[DisallowMultipleComponent]
public class FeatureReferences : MonoBehaviour
{
    public InventoryManager Inventory { get; private set; }

    public void Initialize(InventoryManager inventory)
    {
        if (inventory == null)
        {
            Debug.LogError(name + ": Không thể Initialize bằng kho null.", this);
            return;
        }

        Inventory = inventory;
    }
}
