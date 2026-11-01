using UnityEngine;

[DisallowMultipleComponent]
public class FeatureReferenceInitializer : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private FeatureReferences[] features;

    private void Awake()
    {
        if (inventory == null)
        {
            Debug.LogError("FeatureReferenceInitializer: Chưa gán kho của scene.", this);
            return;
        }

        if (features == null)
        {
            return;
        }
        for (int index = 0; index < features.Length; index++)
        {
            if (features[index] != null)
            {
                features[index].Initialize(inventory);
            }
        }
    }
}
