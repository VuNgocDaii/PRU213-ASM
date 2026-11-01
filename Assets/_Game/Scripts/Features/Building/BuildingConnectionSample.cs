using UnityEngine;

// Chỉ thử Instantiate, Initialize và nâng cấp; không mua/xây thật.
public class BuildingConnectionSample : MonoBehaviour
{
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(1)] private int testLevel = 2;

    private GameObject spawnedBuilding;

    [ContextMenu("Test Instantiate and Upgrade (Play Mode)")]
    public void RunConnectionTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Hãy Play trước khi thử tạo công trình giả.", this);
            return;
        }
        if (featureReferences == null || featureReferences.Inventory == null ||
            buildingPrefab == null || spawnPoint == null || testLevel < 1)
        {
            Debug.LogError("Chưa Initialize hoặc thiếu prefab/điểm tạo/cấp hợp lệ.", this);
            return;
        }
        InventoryManager inventory = featureReferences.Inventory;
        int creditsBefore = inventory.GetAmount(ItemType.Credits);
        if (spawnedBuilding == null)
        {
            if (buildingPrefab.GetComponent<FeatureReferences>() == null ||
                buildingPrefab.GetComponent<UpgradeTarget>() == null)
            {
                Debug.LogError("Prefab cần FeatureReferences và UpgradeTarget ở gốc.", this);
                return;
            }
            spawnedBuilding = Instantiate(buildingPrefab, spawnPoint.position, spawnPoint.rotation);
            // Awake/OnEnable của prefab không chạy gameplay cần kho.
            FeatureReferences spawnedReferences = spawnedBuilding.GetComponent<FeatureReferences>();
            spawnedReferences.Initialize(inventory);
        }

        FeatureReferences references = spawnedBuilding.GetComponent<FeatureReferences>();
        UpgradeTarget target = spawnedBuilding.GetComponent<UpgradeTarget>();
        if (references == null || references.Inventory != inventory || target == null)
        {
            Debug.LogError("Công trình giả không dùng đúng kho hoặc thiếu UpgradeTargetSample.", this);
            return;
        }
        target.ApplyLevel(testLevel);
        target.ApplyLevel(testLevel);
        UpgradeTargetSample sample = target as UpgradeTargetSample;
        if (sample != null)
        {
            Debug.Assert(sample.CurrentLevel == testLevel,
                "ApplyLevel cùng cấp hai lần phải giữ cùng cấp.", this);
        }
        Debug.Assert(inventory.GetAmount(ItemType.Credits) == creditsBefore,
            "Mẫu nối tham chiếu không được trừ Credits.", this);
        Debug.Log("THỬ KẾT NỐI: kho đúng, đã gọi ApplyLevel(" + testLevel +
            ") hai lần; không trừ tiền. Kiểm tra chỉ số thực trong component tính năng.", this);
    }
}
