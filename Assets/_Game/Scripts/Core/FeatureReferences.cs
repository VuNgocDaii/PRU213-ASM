using UnityEngine;

[DisallowMultipleComponent]
public class FeatureReferences : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;

    public InventoryManager Inventory { get => inventory; private set => inventory = value; }

    public void Initialize(InventoryManager inventory)
    {
        if (inventory == null)
        {
            Debug.LogError(name + ": Không thể Initialize bằng kho null.", this);
            return;
        }

        this.inventory = inventory;
    }
}
