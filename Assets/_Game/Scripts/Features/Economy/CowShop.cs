using UnityEngine;

/// <summary>
/// Cửa hàng mua bò mới vào chuồng (CowBarn).
/// Sức chứa tối đa của chuồng dựa theo cấp CowUpgrade (Cấp 1: 2 con, Cấp 2: 3 con, Cấp 3: 4 con).
/// </summary>
[DisallowMultipleComponent]
public class CowShop : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private GameObject individualCowPrefab;

    [Header("Cấu hình Shop Bò")]
    [SerializeField, Min(1)] private int cowPrice = 100; // Cần 100 Credits
    [SerializeField, Min(0)] private int maxCowsPerBarnOverride = 0;

    [Header("Vị trí đặt bò trong chuồng")]
    [SerializeField, Min(0.5f)] private float slotSpacing = 1.2f;
    [SerializeField, Range(1, 12)] private int slotAttempts = 8;

    private float hintTimer = 0f;

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
        hintTimer -= Time.deltaTime;
        if (hintTimer <= 0f)
        {
            hintTimer = 0.25f;
            UpdateHintText();
        }
    }

    public override void Interact()
    {
        var inventory = GetInventory();
        if (inventory == null)
        {
            Debug.LogError($"{name}: CowShop chưa kết nối được với Inventory!", this);
            return;
        }

        if (individualCowPrefab == null)
        {
            Debug.LogWarning("CowShop: Chưa gán IndividualCow prefab vào Inspector!", this);
            return;
        }

        // Tìm chuồng bò còn chỗ trống
        var (barnGO, currentCount, maxCount) = FindTargetBarn();
        if (barnGO == null)
        {
            Debug.Log("CowShop: Tất cả chuồng bò đã đầy! Hãy nâng cấp CowBarn để mua thêm bò.", this);
            return;
        }

        // Kiểm tra đủ tiền
        if (inventory.GetAmount(ItemType.Credits) < cowPrice)
        {
            Debug.LogWarning($"CowShop: Không đủ Credits để mua bò! Cần {cowPrice} Credits, hiện có {inventory.GetAmount(ItemType.Credits)}.");
            return;
        }

        // Trừ tiền và tạo bò mới vào chuồng
        if (inventory.TryRemove(ItemType.Credits, cowPrice))
        {
            SpawnCowInBarn(barnGO);
            Debug.Log($"CowShop: Đã mua 1 con bò với giá {cowPrice} Credits! Sức chứa chuồng: {currentCount + 1}/{maxCount}.");
            UpdateHintText();
        }
    }

    private void UpdateHintText()
    {
        var field = typeof(Interactable).GetField("interactionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, BuildCurrentText());
    }

    private string BuildCurrentText()
    {
        var inventory = GetInventory();
        int credits = inventory != null ? inventory.GetAmount(ItemType.Credits) : 0;

        var (barnGO, currentCount, maxCount) = FindTargetBarn();
        if (barnGO == null)
        {
            return $"Mua bò ({cowPrice} Credits) | Chuồng đã đầy!";
        }

        if (credits < cowPrice)
        {
            return $"Mua bò: {cowPrice} Credits | Bạn có {credits} Credits | Chuồng: {currentCount}/{maxCount}";
        }

        return $"Mua bò ({cowPrice} Credits) | Chuồng: {currentCount}/{maxCount}";
    }

    private void SpawnCowInBarn(GameObject barnGO)
    {
        GameObject newCow = Instantiate(individualCowPrefab, barnGO.transform);

        // Khởi tạo CowController
        var ctrl = newCow.GetComponent<CowController>();
        if (ctrl == null) ctrl = newCow.AddComponent<CowController>();
        var upgrade = barnGO.GetComponent<CowUpgrade>();
        ctrl.Initialize(barnGO.GetComponent<FeatureReferences>(), upgrade, newCow.GetComponent<CowHealth>());

        // Đặt vị trí trong vùng RoamingArea của chuồng
        var area = barnGO.GetComponentInChildren<RoamingArea>();
        if (area != null)
        {
            newCow.transform.position = CalculateSpawnPosition(area, newCow.transform, barnGO);

            var movement = newCow.GetComponent<CowMovement>();
            if (movement != null)
            {
                movement.SetRoamingArea(area);
            }
        }
        else
        {
            newCow.transform.position = barnGO.transform.position;
        }

        // Đăng ký bệnh cho bò mới
        var health = newCow.GetComponent<CowHealth>();
        if (health != null)
        {
            var ctx = FindAnyObjectByType<DefenseSceneContext>();
            if (ctx != null) health.RegisterWith(ctx);
        }

        // Gắn nhãn UI hiển thị trạng thái bệnh
        var healthUI = newCow.GetComponent<CowHealthUI>();
        if (healthUI == null)
        {
            newCow.AddComponent<CowHealthUI>();
        }
    }

    private Vector3 CalculateSpawnPosition(RoamingArea area, Transform newCowTransform, GameObject barn)
    {
        for (int i = 0; i < slotAttempts; i++)
        {
            float step = (i + 1) / 2f;
            float sign = (i % 2 == 1) ? -1f : 1f;
            float offsetX = sign * step * slotSpacing;
            Vector3 candidate = area.transform.position + new Vector3(offsetX, 0f, 0f);

            if (IsPositionFree(candidate, newCowTransform, barn, area))
            {
                return candidate;
            }
        }

        return area.transform.position;
    }

    private bool IsPositionFree(Vector3 candidate, Transform newCowTransform, GameObject barn, RoamingArea area)
    {
        // Kiểm tra khoảng cách với các con bò khác trong chuồng
        foreach (var existing in barn.GetComponentsInChildren<CowController>(true))
        {
            if (existing == null) continue;
            float dist = Vector2.Distance(existing.transform.position, candidate);
            if (dist < slotSpacing * 0.5f)
            {
                return false;
            }
        }

        // Kiểm tra không vượt ngoài biên chuồng
        if (area != null)
        {
            float halfWidth = area.WorldBounds.extents.x - 0.35f;
            float dx = Mathf.Abs(candidate.x - area.transform.position.x);
            if (dx > halfWidth)
            {
                return false;
            }
        }

        return true;
    }

    private (GameObject barn, int currentCount, int maxCount) FindTargetBarn()
    {
        var upgrades = FindObjectsByType<CowUpgrade>();
        foreach (var upgrade in upgrades)
        {
            // Chỉ chuồng bò mới sở hữu RoamingArea
            if (upgrade.GetComponentInChildren<RoamingArea>() == null) continue;

            Transform barnRoot = upgrade.transform;
            int currentCount = barnRoot.GetComponentsInChildren<CowController>(true).Length;
            int maxCount = GetMaxCows(upgrade);

            if (currentCount < maxCount)
            {
                return (barnRoot.gameObject, currentCount, maxCount);
            }
        }

        return (null, 0, 0);
    }

    private int GetMaxCows(CowUpgrade upgrade)
    {
        if (maxCowsPerBarnOverride > 0) return maxCowsPerBarnOverride;
        if (upgrade == null) return 2;
        return upgrade.Capacity;
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
