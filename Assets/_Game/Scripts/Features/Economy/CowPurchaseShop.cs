using System.Reflection;
using UnityEngine;

/// <summary>
/// Shop mua bò mới vào CowBarn.
/// Nhấn E để mua 1 con bò (giá 100 Credits).
/// Mỗi CowBarn có sức chứa tối đa theo cấp CowUpgrade: Cấp 1 → 2, Cấp 2 → 3, Cấp 3 → 4.
/// </summary>
[DisallowMultipleComponent]
public class CowPurchaseShop : Interactable
{
    [Header("Cấu hình")]
    [SerializeField, Min(1)] private int cowPrice = 100;
    [SerializeField] private GameObject individualCowPrefab; // IndividualCow.prefab
    [SerializeField, Min(0)] private int maxCowsPerBarnOverride = 0; // 0 = auto theo CowUpgrade level

    // Lưu count/max count hiện tại cho hint text
    private int currentBarnCount;
    private int maxBarnCount;
    [SerializeField] private FeatureReferences featureReferences;

    [Header("Vị trí đặt bò trong chuồng")]
    [SerializeField, Min(0.5f)] private float slotSpacing = 1.2f;   // Khoảng cách giữa các bò
    [SerializeField, Range(1, 12)] private int slotAttempts = 8;   // Số lần thử tìm chỗ trống

    private static readonly FieldInfo InteractionTextField =
        typeof(Interactable).GetField("interactionText", BindingFlags.NonPublic | BindingFlags.Instance);

    private float hintTimer = 0f;
    private string cachedText = "";

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
        if (hintTimer > 0f) return;
        hintTimer = hintRefreshInterval;
        UpdateHintText();
    }

    private float hintRefreshInterval = 0.25f;

    public InventoryManager GetInventory()
    {
        if (featureReferences != null && featureReferences.Inventory != null)
        {
            return featureReferences.Inventory;
        }

        InventoryManager inv = null;
        var module = GetComponentInParent<EconomyFeature>();
        if (module != null && module.Context != null && module.Context.Inventory != null)
        {
            inv = module.Context.Inventory;
        }

        if (inv == null)
        {
            var ctx = FindAnyObjectByType<DefenseSceneContext>();
            if (ctx != null && ctx.Inventory != null)
            {
                inv = ctx.Inventory;
            }
        }

        if (inv == null)
            inv = FindAnyObjectByType<InventoryManager>();

        if (inv != null && featureReferences != null && featureReferences.Inventory == null)
        {
            featureReferences.Initialize(inv);
        }

        return inv;
    }

    // ─── Mua bò ──────────────────────────────────────────────────────────

    public override void Interact()
    {
        InventoryManager inventory = GetInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[CowPurchaseShop] Chưa kết nối được với Inventory.", this);
            return;
        }

        if (individualCowPrefab == null)
        {
            Debug.LogWarning("[CowPurchaseShop] Chưa gán IndividualCow prefab vào Inspector!", this);
            return;
        }

        var (barnGO, currentCount, maxCount) = FindTargetBarn();
        currentBarnCount = currentCount;
        maxBarnCount = maxCount;
        if (barnGO == null)
        {
            Debug.Log("[CowPurchaseShop] Tất cả chuồng bò đã đầy! Hãy nâng cấp CowBarn để mua thêm bò.", this);
            return;
        }

        if (inventory.GetAmount(ItemType.Credits) < cowPrice)
        {
            Debug.Log($"[CowPurchaseShop] Không đủ Credits! Cần {cowPrice} Credits để mua bò. Hiện có: {inventory.GetAmount(ItemType.Credits)}.", this);
            return;
        }

        if (!inventory.TryRemove(ItemType.Credits, cowPrice))
        {
            Debug.LogWarning("[CowPurchaseShop] Không thể trừ Credits.", this);
            return;
        }

        GameObject newCow = Object.Instantiate(individualCowPrefab, barnGO.transform);
        var ctrl = newCow.GetComponent<CowController>();
        if (ctrl == null) ctrl = newCow.AddComponent<CowController>();
        var upgrade = barnGO.GetComponent<CowUpgrade>();
        ctrl.Initialize(featureReferences, upgrade);

        // Đặt bò tại vị trí trong chuồng (giống bò cũ trên RoamingArea)
        var area = barnGO.GetComponentInChildren<RoamingArea>();
        if (area != null)
        {
            float offsetX = 0f;
            bool placed = false;
            for (int i = 0; i < slotAttempts && !placed; i++)
            {
                float step = (i + 1) / 2f;
                float sign = (i % 2 == 1) ? -1f : 1f;
                offsetX = sign * step * slotSpacing;
                Vector3 candidate = area.transform.position + new Vector3(offsetX, 0f, 0f);
                if (IsPositionFree(candidate, newCow.transform, barnGO))
                {
                    newCow.transform.position = candidate;
                    placed = true;
                }
            }
            if (!placed)
            {
                newCow.transform.position = area.transform.position;
            }
        }
        else
        {
            newCow.transform.position = barnGO.transform.position;
        }

        var movement = newCow.GetComponent<CowMovement>();
        if (movement != null && area != null)
        {
            movement.SetRoamingArea(area);
        }

        var health = newCow.GetComponent<CowHealth>();
        if (health != null)
        {
            var ctx = FindAnyObjectByType<DefenseSceneContext>();
            if (ctx != null) health.RegisterWith(ctx);
        }

        // Thêm CowHealthUI nếu chưa có (tự tạo nhãn disease label)
        var healthUI = newCow.GetComponent<CowHealthUI>();
        if (healthUI == null)
        {
            healthUI = newCow.AddComponent<CowHealthUI>();
        }

        Debug.Log($"[CowPurchaseShop] Đã mua bò cho {barnGO.name} (-{cowPrice} Credits). Barn: {currentCount + 1}/{maxCount}.", this);
        UpdateHintText();
    }

    // ─── Hint Text ──────────────────────────────────────────────────────────

    private void UpdateHintText()
    {
        if (InteractionTextField == null) return;
        cachedText = BuildCurrentText();
        InteractionTextField.SetValue(this, cachedText);
    }

    private string BuildCurrentText()
    {
        InventoryManager inventory = GetInventory();
        int credits = inventory != null ? inventory.GetAmount(ItemType.Credits) : 0;

        var barnInfo = FindTargetBarn();
        if (barnInfo.barn == null)
        {
            return $"Mua bò ({cowPrice} Credits) | Chuồng đã đầy!";
        }

        if (credits < cowPrice)
        {
            return $"Mua bò: {cowPrice} Credits | Bạn có {credits} Credits | Chuồng: {currentBarnCount}/{maxBarnCount}";
        }

        return $"Nhấn E: Mua bò ({cowPrice} Credits) | Chuồng: {currentBarnCount}/{maxBarnCount}";
    }

    [ContextMenu("Test/Press E (Buy Cow)")]
    private void TestBuyCow()
    {
        Interact();
    }

    // ─── Private helpers ───────────────────────────────────────────────────

    private bool IsPositionFree(Vector3 candidate, Transform newCowTransform, GameObject barn)
    {
        // Kiểm tra không trùng bò khác trong chuồng
        foreach (var existing in barn.GetComponentsInChildren<CowController>(true))
        {
            if (existing == null) continue;
            float dist = Vector2.Distance(existing.transform.position, candidate);
            if (dist < slotSpacing * 0.5f)
            {
                return false;
            }
        }

        // Kiểm tra không ra ngoài ranh giới của Chuồng
        var area = barn.GetComponentInChildren<RoamingArea>();
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
        var upgrades = FindObjectsByType<CowUpgrade>(FindObjectsInactive.Include);
        foreach (var upgrade in upgrades)
        {
            Transform barnRoot = upgrade.transform.parent != null ? upgrade.transform.parent : upgrade.transform;
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
        return 1 + upgrade.CurrentLevel;
    }
}