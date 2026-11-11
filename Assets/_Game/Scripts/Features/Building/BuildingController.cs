using System;
using UnityEngine;

// Owns the build transaction. Bridges to DefenseBuildingSlot when present,
// ensuring only DefenseBuildingSlot processes the payment without double-charging.
[DisallowMultipleComponent]
public class BuildingController : MonoBehaviour
{
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private string buildingName;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(1)] private int buildPrice;
    [SerializeField] private GameObject existingBuilding;
    [SerializeField, Range(1, 3)] private int existingLevel = 1;

    [Header("Defense Slot Bridge")]
    [SerializeField] private DefenseBuildingSlot defenseSlot;
    [SerializeField] private CastleProgress castleProgress;
    [SerializeField, Range(1, 3)] private int unlockCastleLevel = 1;
    [SerializeField] private Vector2 footprintSize = new Vector2(1.2f, 1.2f);
    [SerializeField, Range(0.1f, 1f)] private float refundRatio = 0.5f;

    private GameObject instance;
    private bool busy;
    private bool isSelling;
    private bool wasSold;
    private int lastTransactionFrame = -1;
    private int totalInvestedCredits;

    public event Action Changed;
    public InventoryManager Inventory => featureReferences != null ? featureReferences.Inventory : null;
    public string BuildingName => buildingName;
    public int BuildPrice => buildPrice;
    public GameObject Instance => instance;
    public bool IsOccupied => instance != null || (!wasSold && defenseSlot != null && defenseSlot.Occupant != null);
    public bool IsBusy => busy;
    public int CurrentLevel { get; private set; }
    public string LastError { get; private set; }
    public DefenseBuildingSlot DefenseSlot => defenseSlot;
    public CastleProgress CastleProgress => castleProgress;
    public int UnlockCastleLevel => unlockCastleLevel;
    public Vector2 FootprintSize => footprintSize;
    public float RefundRatio => refundRatio;
    public int TotalInvestedCredits => totalInvestedCredits;

    public void BindDefenseSlot(DefenseBuildingSlot slot, CastleProgress progress = null)
    {
        defenseSlot = slot;
        if (progress != null) castleProgress = progress;
        if (slot != null)
        {
            spawnPoint = slot.transform;
            if (slot.Occupant != null && instance == null)
            {
                instance = slot.Occupant;
                CurrentLevel = 1;
                totalInvestedCredits = buildPrice;
                wasSold = false;
            }
        }
    }

    public void SetFeatureReferences(FeatureReferences refs)
    {
        if (refs != null) featureReferences = refs;
    }

    public void SetBuildConfig(GameObject prefab, int price, string name, Vector2 footprint, int unlockLevel = 1, float refund = 0.5f)
    {
        buildingPrefab = prefab;
        buildPrice = price;
        buildingName = name;
        footprintSize = footprint;
        unlockCastleLevel = unlockLevel;
        refundRatio = Mathf.Clamp(refund, 0.1f, 1f);
    }

    public bool CanBuild(out string reason)
    {
        reason = null;
        if (busy) reason = "Đang xử lý";
        else if (IsOccupied || existingBuilding != null) reason = "Vị trí đã có công trình";
        else if (Inventory == null) reason = "Chưa Initialize kho";
        else if (spawnPoint == null || buildPrice <= 0) reason = "Thiếu điểm đặt hoặc giá xây không hợp lệ";
        else if (!ValidRoot(buildingPrefab)) reason = "Prefab cần FeatureReferences và đúng một UpgradeTarget ở gốc";
        else if (castleProgress != null && castleProgress.CurrentLevel < unlockCastleLevel)
            reason = $"Chưa mở khóa (Yêu cầu thành cấp {unlockCastleLevel})";
        else if (defenseSlot != null && !defenseSlot.IsUnlocked)
            reason = "Ô chưa mở khóa trên bản đồ";
        else if (defenseSlot != null && defenseSlot.Occupant != null)
            reason = "Vị trí đã có công trình";
        else if (Inventory.GetAmount(ItemType.Credits) < buildPrice)
            reason = "Không đủ Credits";
        else if (!BuildingFootprintUtility.IsFootprintClear(spawnPoint.position, footprintSize, transform))
            reason = "Vị trí có vật cản";
        return reason == null;
    }

    internal static bool ValidRoot(GameObject root)
    {
        if (root == null || root.GetComponent<FeatureReferences>() == null ||
            root.GetComponents<UpgradeTarget>().Length != 1) return false;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.GetComponents<Interactable>().Length > 1) return false;
        return true;
    }

    internal bool BeginTransaction()
    {
        if (busy || lastTransactionFrame == Time.frameCount) return false;
        busy = true;
        lastTransactionFrame = Time.frameCount;
        LastError = null;
        return true;
    }

    internal void EndTransaction()
    {
        busy = false;
        Changed?.Invoke();
    }

    internal void ReportError(string message)
    {
        LastError = message;
        Debug.LogWarning(name + ": " + message, this);
    }

    public bool TryBuild()
    {
        if (!Application.isPlaying) return false;
        if (!CanBuild(out string reason))
        {
            if (!busy) ReportError(reason);
            return false;
        }
        if (!BeginTransaction()) return false;

        InventoryManager inventory = Inventory;

        // Bridge to DefenseBuildingSlot if wired:
        // DefenseBuildingSlot owns payment and occupancy. Do NOT subtract Credits in this controller.
        if (defenseSlot != null)
        {
            try
            {
                bool built = defenseSlot.TryBuild(buildingPrefab, buildPrice, defenseSlot.SlotKind, unlockCastleLevel);
                if (!built)
                {
                    ReportError("DefenseBuildingSlot từ chối xây dựng");
                    return false;
                }
                instance = defenseSlot.Occupant;
                CurrentLevel = 1;
                totalInvestedCredits = buildPrice;
                wasSold = false;
                instance?.GetComponent<UpgradeTarget>()?.ApplyLevel(1);
                return true;
            }
            catch (Exception exception)
            {
                ReportError("Lỗi xây dựng qua DefenseBuildingSlot: " + exception.Message);
                return false;
            }
            finally { EndTransaction(); }
        }

        // Standalone fallback (used in BuildingTest and isolated smoke fixtures)
        GameObject pending = null;
        bool paid = false;
        try
        {
            paid = inventory.TryRemove(ItemType.Credits, buildPrice);
            if (!paid) return false;
            pending = Instantiate(buildingPrefab, spawnPoint.position, spawnPoint.rotation);
            InitializeBuilding(pending, inventory);
            pending.GetComponent<UpgradeTarget>().ApplyLevel(1);
            instance = pending;
            CurrentLevel = 1;
            totalInvestedCredits = buildPrice;
            wasSold = false;
            return true;
        }
        catch (Exception exception)
        {
            if (pending != null)
            {
                pending.SetActive(false);
                Destroy(pending);
            }
            if (paid) inventory.Add(ItemType.Credits, buildPrice);
            ReportError("Xây thất bại, đã hoàn Credits: " + exception.Message);
            return false;
        }
        finally { EndTransaction(); }
    }

    public int CalculateRefund()
    {
        int baseAmount = totalInvestedCredits > 0 ? totalInvestedCredits : buildPrice;
        return Mathf.Max(0, Mathf.RoundToInt(baseAmount * refundRatio));
    }

    public bool TrySell(out int refundedAmount)
    {
        refundedAmount = 0;
        if (!Application.isPlaying) return false;
        if (busy || isSelling || !IsOccupied)
        {
            ReportError("Không thể bán: không có công trình hoặc đang xử lý giao dịch");
            return false;
        }

        if (!BeginTransaction()) return false;
        isSelling = true;
        try
        {
            refundedAmount = CalculateRefund();
            GameObject targetToDestroy = instance != null ? instance : (defenseSlot != null ? defenseSlot.Occupant : null);
            if (targetToDestroy != null)
            {
                targetToDestroy.SetActive(false);
                Destroy(targetToDestroy);
            }

            instance = null;
            CurrentLevel = 0;
            totalInvestedCredits = 0;
            wasSold = true;

            if (refundedAmount > 0 && Inventory != null)
            {
                Inventory.Add(ItemType.Credits, refundedAmount);
            }

            return true;
        }
        catch (Exception exception)
        {
            ReportError("Lỗi khi bán công trình: " + exception.Message);
            return false;
        }
        finally
        {
            isSelling = false;
            EndTransaction();
        }
    }

    public void RecordUpgradeInvestment(int cost)
    {
        if (cost > 0) totalInvestedCredits += cost;
    }

    private void Start()
    {
        if (existingBuilding != null) RegisterExistingBuilding();
        else if (defenseSlot != null && defenseSlot.Occupant != null && instance == null)
        {
            instance = defenseSlot.Occupant;
            CurrentLevel = 1;
            totalInvestedCredits = buildPrice;
        }
    }

    public bool RegisterExistingBuilding()
    {
        if (!Application.isPlaying || busy || IsOccupied) return false;
        if (Inventory == null || !ValidRoot(existingBuilding) || existingLevel < 1 || existingLevel > 3 ||
            existingBuilding.transform.IsChildOf(transform) ||
            existingBuilding.GetComponent<FeatureReferences>().Inventory != Inventory)
        {
            ReportError("Công trình đặt sẵn không hợp lệ; đăng ký Features cùng kho và đặt ngoài slot");
            return false;
        }
        busy = true;
        try
        {
            existingBuilding.GetComponent<UpgradeTarget>().ApplyLevel(existingLevel);
            instance = existingBuilding;
            CurrentLevel = existingLevel;
            totalInvestedCredits = buildPrice;
            wasSold = false;
            return true;
        }
        catch (Exception exception)
        {
            ReportError("Không thể nhận công trình đặt sẵn: " + exception.Message);
            return false;
        }
        finally { EndTransaction(); }
    }

    internal void SetLevel(int level) { CurrentLevel = level; }

    protected virtual void InitializeBuilding(GameObject building, InventoryManager inventory)
    {
        if (!ValidRoot(building)) throw new InvalidOperationException("Instance thiếu hợp đồng ở gốc");
        FeatureReferences references = building.GetComponent<FeatureReferences>();
        references.Initialize(inventory);
        if (references.Inventory != inventory) throw new InvalidOperationException("Initialize sai kho");
    }
}
