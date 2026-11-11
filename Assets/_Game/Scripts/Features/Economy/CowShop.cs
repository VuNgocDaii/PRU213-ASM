using System.Reflection;
using UnityEngine;

/// <summary>
/// Shop mua bò mới vào CowBarn.
/// Nhấn E để mua 1 con bò (giá 100 Credits).
/// Mỗi CowBarn có sức chứa tối đa theo cấp CowUpgrade: Cấp 1 → 2, Cấp 2 → 3, Cấp 3 → 4.
/// </summary>
[DisallowMultipleComponent]
public class CowShop : Interactable
{
    [Header("Cấu hình")]
    [SerializeField, Min(1)] private int cowPrice = 100;
    [SerializeField] private GameObject individualCowPrefab;
    [SerializeField, Min(0)] private int maxCowsPerBarnOverride = 0;

    [SerializeField] private FeatureReferences featureReferences;

    [Header("Vị trí đặt bò trong chuồng")]
    [SerializeField, Min(0.5f)] private float slotSpacing = 1.2f;
    [SerializeField, Range(1, 12)] private int slotAttempts = 8;

    private static readonly FieldInfo InteractionTextField =
        typeof(Interactable).GetField("interactionText", BindingFlags.NonPublic | BindingFlags.Instance);

    private float hintTimer = 0f;
    private string cachedText = "";

    private void Awake()
    {
        if (featureReferences == null)
            featureReferences = GetComponentInParent<FeatureReferences>();
        UpdateHintText();
    }

    private void Update()
    {
        hintTimer -= Time.deltaTime;
        if (hintTimer > 0f) return;
        hintTimer = 0.25f;
        UpdateHintText();
    }

    public override void Interact()
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[CowShop] Chưa kết nối được với Inventory.", this);
            return;
        }

        if (individualCowPrefab == null)
        {
            Debug.LogWarning("[CowShop] Chưa gán IndividualCow prefab vào Inspector!", this);
            return;
        }

        var (barnGO, currentCount, maxCount) = FindTargetBarn();
        if (barnGO == null)
        {
            Debug.Log("[CowShop] Tất cả chuồng bò đã đầy! Hãy nâng cấp CowBarn để mua thêm bò.", this);
            return;
        }

        if (inventory.GetAmount(ItemType.Credits) < cowPrice)
        {
            Debug.Log($"[CowShop] Không đủ Credits! Cần {cowPrice} Credits để mua bò. Hiện có: {inventory.GetAmount(ItemType.Credits)}.", this);
            return;
        }

        if (!inventory.TryRemove(ItemType.Credits, cowPrice))
        {
            Debug.LogWarning("[CowShop] Không thể trừ Credits.", this);
            return;
        }

        GameObject newCow = Object.Instantiate(individualCowPrefab, barnGO.transform);
        var ctrl = newCow.GetComponent<CowController>();
        if (ctrl == null) ctrl = newCow.AddComponent<CowController>();
        var upgrade = barnGO.GetComponent<CowUpgrade>();
        ctrl.Initialize(featureReferences, upgrade);

        var area = barnGO.GetComponentInChildren<RoamingArea>();
        if (area != null)
        {
            bool placed = false;
            for (int i = 0; i < slotAttempts && !placed; i++)
            {
                float step = (i + 1) / 2f;
                float sign = (i % 2 == 1) ? -1f : 1f;
                float offsetX = sign * step * slotSpacing;
                Vector3 candidate = area.transform.position + new Vector3(offsetX, 0f, 0f);
                if (IsPositionFree(candidate, newCow.transform, barnGO))
                {
                    newCow.transform.position = candidate;
                    placed = true;
                }
            }
            if (!placed)
                newCow.transform.position = area.transform.position;
        }
        else
        {
            newCow.transform.position = barnGO.transform.position;
        }

        var movement = newCow.GetComponent<CowMovement>();
        if (movement != null && area != null)
            movement.SetRoamingArea(area);

        var health = newCow.GetComponent<CowHealth>();
        if (health != null)
        {
            var ctx = FindAnyObjectByType<DefenseSceneContext>();
            if (ctx != null) health.RegisterWith(ctx);
        }

        var healthUI = newCow.GetComponent<CowHealthUI>();
        if (healthUI == null)
            healthUI = newCow.AddComponent<CowHealthUI>();

        Debug.Log($"[CowShop] Đã mua bò cho {barnGO.name} (-{cowPrice} Credits). Barn: {currentCount + 1}/{maxCount}.", this);
        UpdateHintText();
    }

    private void UpdateHintText()
    {
        if (InteractionTextField == null) return;
        cachedText = BuildCurrentText();
        InteractionTextField.SetValue(this, cachedText);
    }

    private string BuildCurrentText()
    {
        InventoryManager inventory = InventoryManager.Instance;
        int credits = inventory != null ? inventory.GetAmount(ItemType.Credits) : 0;

        var (barnGO, currentCount, maxCount) = FindTargetBarn();
        if (barnGO == null)
            return $"Mua bò ({cowPrice} Credits) | Chuồng đã đầy!";

        if (credits < cowPrice)
            return $"Mua bò: {cowPrice} Credits | Bạn có {credits} Credits | Chuồng: {currentCount}/{maxCount}";

        return $"Mua bò ({cowPrice} Credits) | Chuồng: {currentCount}/{maxCount}";
    }

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