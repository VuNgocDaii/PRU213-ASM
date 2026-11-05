using System;
using UnityEngine;

// Owns the build transaction. Feature gameplay is reached only through the base contracts.
[DisallowMultipleComponent]
public class BuildingController : MonoBehaviour
{
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private string buildingName;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(1)] private int buildPrice;

    private GameObject instance;
    private bool busy;
    private int lastTransactionFrame = -1;

    public event Action Changed;
    public InventoryManager Inventory => featureReferences != null ? featureReferences.Inventory : null;
    public string BuildingName => buildingName;
    public int BuildPrice => buildPrice;
    public GameObject Instance => instance;
    public bool IsOccupied => instance != null;
    public bool IsBusy => busy;
    public string LastError { get; private set; }

    public bool CanBuild(out string reason)
    {
        reason = null;
        if (busy) reason = "Đang xử lý";
        else if (IsOccupied) reason = "Vị trí đã có công trình";
        else if (Inventory == null) reason = "Chưa Initialize kho";
        else if (spawnPoint == null || buildPrice <= 0) reason = "Thiếu điểm đặt hoặc giá xây không hợp lệ";
        else if (!ValidRoot(buildingPrefab)) reason = "Prefab cần FeatureReferences và đúng một UpgradeTarget ở gốc";
        else if (Inventory.GetAmount(ItemType.Credits) < buildPrice) reason = "Không đủ Credits";
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
        GameObject pending = null;
        bool paid = false;
        try
        {
            paid = inventory.TryRemove(ItemType.Credits, buildPrice);
            if (!paid) return false;
            // No parent: a cow's collider must never resolve to the slot's Interactable.
            pending = Instantiate(buildingPrefab, spawnPoint.position, spawnPoint.rotation);
            InitializeBuilding(pending, inventory);
            pending.GetComponent<UpgradeTarget>().ApplyLevel(1);
            instance = pending;
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

    // Virtual seam lets the Play test inject a failure AFTER payment without changing Core.
    protected virtual void InitializeBuilding(GameObject building, InventoryManager inventory)
    {
        if (!ValidRoot(building)) throw new InvalidOperationException("Instance thiếu hợp đồng ở gốc");
        FeatureReferences references = building.GetComponent<FeatureReferences>();
        references.Initialize(inventory);
        if (references.Inventory != inventory) throw new InvalidOperationException("Initialize sai kho");
    }
}
