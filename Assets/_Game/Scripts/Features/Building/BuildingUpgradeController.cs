using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BuildingUpgradeController : MonoBehaviour
{
    [SerializeField] private BuildingController building;
    [SerializeField] private int[] upgradePrices = new int[0];
    [SerializeField] private CastleProgress castleProgress;
    [SerializeField] private bool limitByCastleLevel;

    [Header("UI Events for Towers & Modular HUDs")]
    [SerializeField] private UnityEvent<int> onUpgradeSuccess;
    [SerializeField] private UnityEvent<string> onUpgradeFailed;

    public event Action<int> OnUpgraded;
    public BuildingController Building => building;
    public CastleProgress CastleProgress => castleProgress;
    public bool LimitByCastleLevel => limitByCastleLevel;

    public int MaxLevel => upgradePrices == null ? 1 : upgradePrices.Length + 1;
    public int NextPrice => building != null && building.CurrentLevel >= 1 && building.CurrentLevel < MaxLevel
        ? upgradePrices[building.CurrentLevel - 1] : 0;
    public bool IsMaxLevel => building != null && building.IsOccupied && building.CurrentLevel >= MaxLevel;

    public void Configure(BuildingController targetBuilding, int[] prices, CastleProgress progress = null, bool limitByCastle = false)
    {
        building = targetBuilding;
        upgradePrices = prices ?? new int[0];
        castleProgress = progress;
        limitByCastleLevel = limitByCastle;
    }

    public void SetCastleProgress(CastleProgress progress, bool limitByCastle = false)
    {
        castleProgress = progress;
        limitByCastleLevel = limitByCastle;
    }

    public void SetUpgradePrices(int[] prices)
    {
        upgradePrices = prices ?? new int[0];
    }

    public bool CanUpgrade(out string reason)
    {
        reason = null;
        if (building == null || building.Inventory == null) reason = "Chưa Initialize kho";
        else if (building.IsBusy) reason = "Đang xử lý";
        else if (!building.IsOccupied) reason = "Chưa có công trình";
        else if (MaxLevel > 3 || building.CurrentLevel < 1) reason = "Bảng cấp không hợp lệ (chỉ hỗ trợ 1–3)";
        else if (IsMaxLevel) reason = "Cấp tối đa";
        else if (NextPrice <= 0) reason = "Giá nâng cấp phải lớn hơn 0";
        else if (limitByCastleLevel && castleProgress != null && (building.CurrentLevel + 1) > castleProgress.CurrentLevel)
            reason = $"Tháp không thể nâng vượt cấp thành (Thành cấp {castleProgress.CurrentLevel})";
        else if (!BuildingController.ValidRoot(building.Instance) ||
            building.Instance.GetComponent<FeatureReferences>().Inventory != building.Inventory)
            reason = "Công trình thiếu hợp đồng nâng cấp hoặc sai kho";
        else if (building.Inventory.GetAmount(ItemType.Credits) < NextPrice) reason = "Không đủ Credits";
        return reason == null;
    }

    // Accessible via Inspector / UnityEvent so Towers and external UI can trigger upgrades
    public void RequestUpgrade()
    {
        TryUpgrade();
    }

    public bool TryUpgrade()
    {
        if (!Application.isPlaying) return false;
        if (!CanUpgrade(out string reason))
        {
            if (building != null && !building.IsBusy) building.ReportError(reason);
            onUpgradeFailed?.Invoke(reason);
            return false;
        }
        if (!building.BeginTransaction()) return false;

        InventoryManager inventory = building.Inventory;
        int price = NextPrice;
        int previousLevel = building.CurrentLevel;
        UpgradeTarget target = building.Instance.GetComponent<UpgradeTarget>();
        bool paid = false;
        try
        {
            paid = inventory.TryRemove(ItemType.Credits, price);
            if (!paid)
            {
                onUpgradeFailed?.Invoke("Không đủ Credits");
                return false;
            }

            int nextLevel = previousLevel + 1;
            target.ApplyLevel(nextLevel);
            building.SetLevel(nextLevel);
            building.RecordUpgradeInvestment(price);

            OnUpgraded?.Invoke(nextLevel);
            onUpgradeSuccess?.Invoke(nextLevel);
            return true;
        }
        catch (Exception exception)
        {
            // A throwing target may have changed its state before throwing. Restore the absolute level.
            if (paid)
            {
                try { target.ApplyLevel(previousLevel); }
                catch (Exception restoreError) { Debug.LogException(restoreError, this); }
                inventory.Add(ItemType.Credits, price);
            }
            building.ReportError("Nâng cấp thất bại, đã hoàn Credits: " + exception.Message);
            onUpgradeFailed?.Invoke(exception.Message);
            return false;
        }
        finally { building.EndTransaction(); }
    }
}
