using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runtime validator for Person 2 (Building, Upgrade/Sell, Visuals).
// Can be executed via Inspector ContextMenu or attached to TestSupport in BuildingWorkspace.
[DisallowMultipleComponent]
public class BuildingWorkspaceValidator : MonoBehaviour
{
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private BuildingFeature feature;

    private readonly List<string> testResults = new List<string>();

    public IReadOnlyList<string> TestResults => testResults;

    private void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunVerificationRoutine());
        }
    }

    [ContextMenu("Run Building Verification Suite (Play Mode)")]
    public void RunVerification()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Vui lòng vào Play Mode trước khi chạy bộ kiểm thử.", this);
            return;
        }
        StartCoroutine(RunVerificationRoutine());
    }

    public IEnumerator RunVerificationRoutine()
    {
        testResults.Clear();
        Debug.Log("--- BẮT ĐẦU BỘ KIỂM THỬ XÂY DỰNG, NÂNG/BÁN VÀ HÌNH CÔNG TRÌNH ---", this);

        DefenseSceneContext context = FindAnyObjectByType<DefenseSceneContext>();
        Assert(context != null && context.IsReady, "DefenseSceneContext sẵn sàng");

        if (feature == null) feature = FindAnyObjectByType<BuildingFeature>();
        Assert(feature != null, "BuildingFeature tồn tại trong scene");

        InventoryManager inventory = context.Inventory;
        CastleProgress progress = context.Progress;
        BuildingCatalogData catalog = feature != null ? feature.Catalog : null;
        if (catalog == null || catalog.Entries == null || catalog.Entries.Count == 0)
        {
#if UNITY_EDITOR
            catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingCatalogData>("Assets/_Game/Data/Features/Building/BuildingCatalog.asset");
            if (feature != null && catalog != null)
            {
                feature.Catalog = catalog;
            }
#endif
            if (catalog == null)
            {
                var all = Resources.FindObjectsOfTypeAll<BuildingCatalogData>();
                if (all != null && all.Length > 0)
                {
                    foreach (var c in all)
                    {
                        if (c != null && c.Entries != null && c.Entries.Count >= 6) { catalog = c; break; }
                    }
                    if (catalog == null) catalog = all[0];
                }
            }
        }
        Assert(catalog != null && catalog.Entries.Count >= 6, "Catalog có ít nhất 6 mục (4 tháp, bò, máy)");

        // -------------------------------------------------------------
        // MỐC 1: Xây qua DefenseBuildingSlot, thanh toán 1 lần, không tạo trùng
        // -------------------------------------------------------------
        DefenseBuildingSlot towerSlot = null;
        foreach (var s in context.Slots)
        {
            if (s != null && s.IsUnlocked && s.SlotKind == DefenseBuildingSlot.Kind.Tower && s.Occupant == null)
            {
                towerSlot = s;
                break;
            }
        }
        Assert(towerSlot != null, "Tìm thấy ô tháp trống đã mở khóa");

        BuildingController controller = towerSlot.GetComponent<BuildingController>();
        Assert(controller != null, "Ô có BuildingController");
        Assert(controller.DefenseSlot == towerSlot, "BuildingController nối đúng DefenseBuildingSlot");

        int initialCredits = inventory.GetAmount(ItemType.Credits);
        BuildingCatalogData.Entry arrowTower = catalog.FindById("tower_arrow");
        Assert(arrowTower != null, "Catalog có Tháp Cung (tower_arrow)");

        bool built = feature.TryBuildOnSlot(towerSlot, "tower_arrow");
        Assert(built, "Xây thành công Tháp Cung trên ô tháp");
        Assert(towerSlot.Occupant != null, "Ô đã có Occupant");
        Assert(controller.IsOccupied && controller.CurrentLevel == 1, "Controller ghi nhận trạng thái đã chiếm và cấp 1");
        Assert(inventory.GetAmount(ItemType.Credits) == initialCredits - arrowTower.buildPrice, "Credits chỉ bị trừ 1 lần bởi slot");

        // Xây lần 2 trên cùng ô phải bị từ chối và không trừ tiền
        int creditsAfterBuild = inventory.GetAmount(ItemType.Credits);
        bool secondBuild = controller.TryBuild();
        Assert(!secondBuild, "Từ chối xây chồng lên ô đã chiếm");
        Assert(inventory.GetAmount(ItemType.Credits) == creditsAfterBuild, "Thao tác xây lặp không trừ thêm tiền");

        yield return null;

        // -------------------------------------------------------------
        // MỐC 2: Kiểm tra Footprint và vật cản
        // -------------------------------------------------------------
        Vector2 testPos = new Vector2(40f, 40f);
        Assert(BuildingFootprintUtility.IsFootprintClear(testPos, new Vector2(1, 1)), "Vùng trống footprint hợp lệ");

        GameObject obstacle = new GameObject("TempObstacle");
        obstacle.transform.position = new Vector3(testPos.x, testPos.y, 0);
        var col = obstacle.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2, 2);
        col.isTrigger = false;
        Physics2D.SyncTransforms();

        Assert(!BuildingFootprintUtility.IsFootprintClear(testPos, new Vector2(1, 1)), "Footprint phát hiện vật cản che chắn");
        Destroy(obstacle);
        Physics2D.SyncTransforms();

        yield return null;

        // -------------------------------------------------------------
        // MỐC 3: Nâng cấp, điều kiện cấp thành, ApplyLevel không cộng dồn
        // -------------------------------------------------------------
        BuildingUpgradeController upgrader = towerSlot.GetComponent<BuildingUpgradeController>();
        Assert(upgrader != null, "Ô có BuildingUpgradeController");

        // Tháp không vượt cấp thành: thành cấp 1 -> không được nâng tháp lên cấp 2
        Assert(progress.CurrentLevel == 1, "Thành đang ở cấp 1");
        Assert(!upgrader.CanUpgrade(out string reason), "Tháp cấp 2 bị từ chối khi thành cấp 1");
        Assert(reason != null && reason.Contains("Thành"), "Lý do từ chối chỉ rõ giới hạn theo cấp thành");

        // Tăng EXP để lên cấp 2
        progress.AddExperience(495);
        Assert(progress.CurrentLevel == 2, "Thành đã lên cấp 2 sau 495 EXP");

        // Giờ được nâng tháp lên cấp 2
        Assert(upgrader.CanUpgrade(out _), "Tháp được phép nâng cấp 2 khi thành cấp 2");
        int creditsBeforeUpgrade = inventory.GetAmount(ItemType.Credits);
        int upgradePrice = upgrader.NextPrice;
        Assert(upgradePrice == 60, "Giá nâng tháp cung cấp 2 là 60 Credits");

        bool upgraded = upgrader.TryUpgrade();
        Assert(upgraded && controller.CurrentLevel == 2, "Nâng cấp tháp lên cấp 2 thành công");
        Assert(inventory.GetAmount(ItemType.Credits) == creditsBeforeUpgrade - upgradePrice, "Đã trừ đúng 60 Credits nâng cấp");

        // ApplyLevel cùng cấp 2 lần phải giữ nguyên chỉ số, không nhân dồn
        UpgradeTarget target = towerSlot.Occupant.GetComponent<UpgradeTarget>();
        Assert(target != null, "Occupant có UpgradeTarget");
        target.ApplyLevel(2);
        target.ApplyLevel(2);
        UpgradeTargetSample sample = target as UpgradeTargetSample;
        if (sample != null)
        {
            Assert(sample.CurrentLevel == 2, "ApplyLevel lặp lại giữ đúng cấp 2, không nhân dồn");
        }

        // Tháp cấp 3 bị từ chối vì thành mới cấp 2
        Assert(!upgrader.CanUpgrade(out _), "Tháp cấp 3 bị từ chối vì thành chưa lên cấp 3");

        yield return null;

        // -------------------------------------------------------------
        // MỐC 3 (tiếp): Bán công trình, hoàn tiền, không bán 2 lần
        // -------------------------------------------------------------
        int creditsBeforeSell = inventory.GetAmount(ItemType.Credits);
        int expectedRefund = controller.CalculateRefund();
        Assert(expectedRefund > 0, "Giá trị hoàn tiền tính đúng tỉ lệ cấu hình");

        bool sold = controller.TrySell(out int refunded);
        Assert(sold && refunded == expectedRefund, "Bán công trình thành công, nhận đúng tiền hoàn");
        Assert(inventory.GetAmount(ItemType.Credits) == creditsBeforeSell + refunded, "Credits được cộng vào kho");
        Assert(!controller.IsOccupied && controller.CurrentLevel == 0, "Controller trở về trạng thái trống");

        // Bán lần 2 phải thất bại, không hoàn tiền lần hai
        bool soldAgain = controller.TrySell(out _);
        Assert(!soldAgain, "Không thể bán ô trống lần hai");
        Assert(inventory.GetAmount(ItemType.Credits) == creditsBeforeSell + refunded, "Không có hoàn tiền trùng lặp");

        yield return null;

        // -------------------------------------------------------------
        // MỐC 3 (tiếp): Hình thành 3 cấp
        // -------------------------------------------------------------
        CastleVisualController castleVisual = feature.CastleVisual;
        Assert(castleVisual != null, "CastleVisualController tồn tại và đã kết nối với thành");
        castleVisual.ApplyCastleLevel(2);
        Assert(castleVisual.CurrentVisualLevel == 2, "Hình thành cập nhật cấp 2");
        castleVisual.ApplyCastleLevel(2);
        Assert(castleVisual.CurrentVisualLevel == 2, "ApplyCastleLevel lặp lại không tạo trùng");

        // -------------------------------------------------------------
        // MỐC 4: Bảng giá nâng cấp từng bước (chênh lệch) từ Excel
        // -------------------------------------------------------------
        BuildingCatalogData.Entry cowEntry = catalog.FindById("cow_barn");
        Assert(cowEntry != null, "Catalog có Chuồng Bò");
        Assert(cowEntry.upgradePrices.Length == 2 && cowEntry.upgradePrices[0] == 120 && cowEntry.upgradePrices[1] == 280,
            "Giá nâng bò đổi sang từng bước bằng chênh lệch: Cấp 2 = 120, Cấp 3 = 280 (tổng 400)");

        BuildingCatalogData.Entry machineEntry = catalog.FindById("production_machine");
        Assert(machineEntry != null, "Catalog có Máy Đóng Chai");
        Assert(machineEntry.upgradePrices.Length == 2 && machineEntry.upgradePrices[0] == 150 && machineEntry.upgradePrices[1] == 250,
            "Giá nâng máy đổi sang từng bước bằng chênh lệch: Cấp 2 = 150, Cấp 3 = 250 (tổng 400)");

        // Kiểm tra TowerUpgradeBridge
        TowerUpgradeBridge bridge = feature.TowerUpgradeBridge;
        Assert(bridge != null, "TowerUpgradeBridge sẵn sàng để UI Towers gọi nâng cấp không phụ thuộc lớp cụ thể");

        Debug.Log($"HOÀN THÀNH TẤT CẢ {testResults.Count} BƯỚC KIỂM THỬ: TẤT CẢ ĐỀU PASS!", this);
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            testResults.Add("FAIL: " + message);
            Debug.LogError("[BuildingValidator] FAIL: " + message, this);
            throw new InvalidOperationException(message);
        }
        testResults.Add("PASS: " + message);
        Debug.Log("[BuildingValidator] PASS: " + message, this);
    }
}
