# Bàn giao tính năng Xây dựng, Nâng/Bán và Hình công trình (Building)

**Người phụ trách:** Người 2  
**Nhánh:** `feature/building`  
**Scene chính:** `Assets/_Game/Scenes/Workspaces/BuildingWorkspace.unity`  
**Scene thử phụ:** `Assets/_Game/Scenes/Tests/BuildingTest.unity`  

---

## 1. Tổng quan kiến trúc & Phạm vi sở hữu

- **Mã nguồn (`Assets/_Game/Scripts/Features/Building/`):**
  - `BuildingCatalogData.cs`: ScriptableObject định nghĩa danh mục công trình (prefab, loại ô, giá xây, cấp thành yêu cầu, footprint, bảng giá nâng cấp, tỉ lệ hoàn tiền).
  - `BuildingFootprintUtility.cs`: Tiện ích kiểm tra vật cản và footprint diện tích cho prefab trước khi gọi xây (`Physics2D.OverlapBoxAll`, bỏ qua trigger collider).
  - `BuildingController.cs`: Bộ điều khiển giao dịch xây và bán công trình. Nối trực tiếp với `DefenseBuildingSlot` khi có sẵn, đảm bảo chỉ có 1 bộ điều khiển thanh toán.
  - `BuildingUpgradeController.cs`: Bộ điều khiển giao dịch nâng cấp, kiểm tra điều kiện cấp thành (tháp không nâng vượt cấp thành), thực hiện thanh toán và gọi `ApplyLevel`.
  - `BuildingPanel.cs`: UI tương tác xây/nâng/bán công trình, hỗ trợ chọn danh mục catalog, xem thông tin hoàn tiền và hiển thị lý do từ chối rõ ràng.
  - `BuildingSlot.cs`: Component tương tác (`Interactable`) gắn vào các ô xây trên bản đồ để mở `BuildingPanel` khi người chơi nhấn `[E]`.
  - `BuildingFeature.cs`: `DefenseModule` gốc của tính năng, nhận `DefenseSceneContext`, tự động nối tất cả các ô trong `Context.Slots` với catalog và hệ thống tương tác.
  - `CastleVisualController.cs`: Điều khiển hình ảnh thành lũy 3 cấp, lắng nghe sự kiện thăng cấp thành và nổ thành từ Core.
  - `TowerUpgradeBridge.cs`: Đầu mối giao tiếp nâng cấp dành riêng cho UI Towers gọi thông qua UnityEvent/phương thức trung gian.
  - `BuildingWorkspaceValidator.cs`: Bộ công cụ tự động kiểm thử toàn bộ 4 mốc tính năng tại runtime.
- **Ngoại lệ tuân thủ:** KHÔNG sửa `DefenseBuildingSlot.cs`. Mọi tương tác tuân thủ đúng hợp đồng phương thức `TryBuild(prefab, price, kind, unlockCastleLevel)` đã có.
- **Prefab:**
  - `Assets/_Game/Prefabs/Modules/Building/BuildingFeature.prefab`: Composition root của module, chứa `BuildingFeature`, `TowerUpgradeBridge` và liên kết với catalog.
  - `Assets/_Game/Prefabs/Features/Building/BuildingCanvas.prefab`: Canvas UI xây/nâng/bán.
  - `Assets/_Game/Prefabs/Features/Building/CastleVisual.prefab`: Prefab riêng biệt chứa hình ảnh 3 cấp của thành lũy và điểm móc VFX.
- **Dữ liệu:** `Assets/_Game/Data/Features/Building/BuildingCatalog.asset`

---

## 2. Chi tiết 4 nội dung công việc triển khai

### A. Catalog và menu xây
- **Catalog công trình (`BuildingCatalogData` & `BuildingCatalog.asset`):**
  - Chứa đầy đủ 6 loại công trình hỗ trợ:
    1. **Tháp Cung (Arrow Tower):** Ô Tower, giá 80 Credits, mở khóa cấp thành 1, nâng cấp `[60, 120]`.
    2. **Tháp Pháo (Cannon Tower):** Ô Tower, giá 150 Credits, mở khóa cấp thành 2, nâng cấp `[100, 180]`.
    3. **Tháp Băng (Ice Tower):** Ô Tower, giá 120 Credits, mở khóa cấp thành 2, nâng cấp `[90, 150]`.
    4. **Tháp Tỉa (Sniper Tower):** Ô Tower, giá 200 Credits, mở khóa cấp thành 3, nâng cấp `[140, 240]`.
    5. **Chuồng Bò (Cow Barn):** Ô Cow, giá 150 Credits, mở khóa cấp thành 1, nâng cấp `[120, 280]`.
    6. **Máy Đóng Chai (Production Machine):** Ô Machine, giá 300 Credits, mở khóa cấp thành 1, nâng cấp `[150, 250]`.
- **Sử dụng `Context.Slots`:** `BuildingFeature` kết nối trực tiếp với danh sách ô `Context.Slots` do map cung cấp, tuyệt đối không tự sinh hệ ô độc lập chồng lấn lên map base.
- **Kiểm tra Footprint & vật cản:** Trước khi gọi `TryBuild`, `BuildingFootprintUtility.IsFootprintClear` kiểm tra vùng chiếm chỗ (`footprintSize`) tại vị trí ô. Nếu phát hiện collider cứng (không phải trigger của slot/vùng), thao tác xây sẽ bị từ chối với lý do *"Vị trí có vật cản"*.
- **Từ chối rõ ràng:** Kiểm tra và thông báo lỗi cụ thể trên UI:
  - Thiếu tiền: *"Không đủ Credits (cần X)"*.
  - Ô đã chiếm: *"Vị trí đã có công trình"*.
  - Sai loại ô: *"Sai loại ô (yêu cầu ô X)"*.
  - Chưa mở khóa theo cấp thành: *"Chưa mở khóa (Yêu cầu thành cấp X)"*.
  - Chưa mở khóa trên bản đồ: *"Ô chưa mở khóa trên bản đồ"*.

### B. Luồng thanh toán xây
- Luồng thực thi: UI chọn công trình -> kiểm tra cấu hình, tiền, cấp, footprint -> gọi `DefenseBuildingSlot.TryBuild` **đúng một lần**.
- `DefenseBuildingSlot.TryBuild` tự trừ tiền Credits từ `InventoryManager`, instantiate prefab, khởi tạo `FeatureReferences` và `DefenseModule`, chiếm giữ ô.
- `BuildingController` nhận biết khi được gắn cùng `DefenseBuildingSlot` và **không tự trừ tiền lần thứ hai**.
- Không gọi `TryRemove(Credits)` ở UI trước khi gọi `TryBuild`.
- **Đề xuất bổ sung cho Base (về API giải phóng ô / bán công trình):**
  > [!NOTE]
  > Trong phiên bản base hiện tại, `DefenseBuildingSlot.cs` có thuộc tính `Occupant { get; private set; }` và chưa cung cấp phương thức giải phóng ô hoặc bán.  
  > Để chuẩn hóa cho các nhánh, đề xuất Người 1 (Core / Tích hợp) bổ sung phương thức vào `DefenseBuildingSlot.cs`:
  > ```csharp
  > public bool TrySell(float refundRatio, out int refundCredits)
  > {
  >     if (Occupant == null || isBuilding) { refundCredits = 0; return false; }
  >     // Hoàn tiền và dọn Occupant
  >     Destroy(Occupant);
  >     Occupant = null;
  >     return true;
  > }
  > ```
  > Hiện tại trên nhánh `feature/building`, `BuildingController` dọn dẹp an toàn instance của công trình, hoàn tiền Credits theo cấu hình và chuyển trạng thái ô sang trống mà không can thiệp vào mã nguồn nội bộ của `DefenseBuildingSlot.cs`.

### C. Nâng cấp
- `BuildingUpgradeController` quản lý giá nâng cấp, cấp hiện tại và giao dịch nâng cấp:
  - Kiểm tra điều kiện hợp lệ và cấp thành trước khi trừ tiền.
  - **Giới hạn cấp thành:** Tháp không thể nâng vượt cấp thành hiện tại (`CurrentLevel + 1 <= CastleProgress.CurrentLevel`).
  - Chỉ gọi `ApplyLevel` sau khi thanh toán trừ Credits thành công.
  - Gọi cùng một cấp hai lần luôn giữ nguyên chỉ số, không cộng dồn/nhân dồn (idempotent).
  - Không nâng vượt quá cấp tối đa (cấp 3).
- **Quy đổi giá nâng bò/máy từ Excel:**
  - Bảng Excel ghi chi phí tích lũy (Cumulative). Module đã chuyển đổi sang giá từng bước (Incremental difference):
    - **Chuồng bò:** Cấp 1 -> 2: `120 - 0 = 120` Credits; Cấp 2 -> 3: `400 - 120 = 280` Credits. Mảng giá: `[120, 280]`.
    - **Máy đóng chai:** Cấp 1 -> 2: `150 - 0 = 150` Credits; Cấp 2 -> 3: `400 - 150 = 250` Credits. Mảng giá: `[150, 250]`.
- **Đầu mối cho UI Towers:**
  - Cung cấp component `TowerUpgradeBridge` và các sự kiện `UnityEvent<int> onUpgradeSuccess`, `UnityEvent<string> onUpgradeFailed` trên `BuildingUpgradeController`. UI Towers có thể gọi trực tiếp `RequestUpgrade()` hoặc `TryUpgrade(slot)` mà không cần quan tâm đến logic trừ tiền.

### D. Bán và hình công trình
- **Bán công trình & Hoàn tiền:**
  - Khi chọn công trình đã xây, UI hiển thị số Credits sẽ được hoàn lại: `CalculateRefund() = round(totalInvested * refundRatio)`.
  - Tỉ lệ hoàn tiền cấu hình qua `refundRatio` (mặc định 0.5 = 50%, có thể điều chỉnh trên Inspector). Nhóm có thể thống nhất chốt số này.
  - Cơ chế chống bán trùng lặp: cờ `isSelling`, `wasSold` và kiểm tra `!IsOccupied` ngăn chặn hoàn tiền hai lần.
  - Sau khi bán, công trình bị hủy, tiền được hoàn vào kho `InventoryManager`, slot trở về trạng thái trống để có thể xây lại.
- **Hình thành 3 cấp (`CastleVisualController`):**
  - Tạo prefab `Assets/_Game/Prefabs/Features/Building/CastleVisual.prefab` với 3 giai đoạn hiển thị tương ứng với cấp thành 1, 2, 3.
  - Lắng nghe trực tiếp sự kiện `CastleProgress.OnCastleLevelUp(int level)` để cập nhật hình ảnh.
  - Gọi `ApplyCastleLevel` nhiều lần cùng một cấp không gây trùng lặp hiệu ứng.
  - Tự động gắn và cập nhật tại vị trí `CastleTarget` thông qua `BuildingFeature`.
- **Điểm móc VFX:**
  - Cung cấp `Transform vfxHookPoint` tại vị trí (0, 1.5, 0).
  - Có sẵn các slot `ParticleSystem levelUpVFX`, `destructionVFX` và các sự kiện `onLevelUp`, `onDestroyed` để người tích hợp gắn VFX khói/nổ/mở đất.
- **Quy tắc bản đồ:** Không sửa đổi `World/Main`, `MapZone`, camera hay số khu của bản đồ trên nhánh này.

---

## 3. Giới hạn số lượng ô theo cấp thành (Tham chiếu Core)

Theo thiết kế hệ thống của `CastleDefinition`:
- **Cấp 1:** 4 ô tháp / 3 ô bò / 1 ô máy.
- **Cấp 2:** 8 ô tháp / 5 ô bò / 2 ô máy.
- **Cấp 3:** 12 ô tháp / 8 ô bò / 4 ô máy.

*Lưu ý:* Thành lên cấp bằng EXP (thông qua `CastleProgress.AddExperience`), không bán hay nâng cấp thành bằng Credits.

---

## 4. Các mốc triển khai & Tương ứng kiểm thử

| Mốc | Nội dung công việc | Kiểm thử trong `BuildingWorkspaceValidator` |
|---|---|---|
| **Mốc 1** | Xây prefab qua `DefenseBuildingSlot.TryBuild`, không trừ tiền 2 lần, không cho xây trùng trên ô đã chiếm. | `Assert(feature.TryBuildOnSlot(...))` -> kiểm tra Credits trừ 1 lần -> `Assert(!controller.TryBuild())` không trừ tiền thêm. |
| **Mốc 2** | Catalog/menu, preview và kiểm tra footprint. | `Assert(BuildingFootprintUtility.IsFootprintClear(...))` trên ô trống; phát hiện vật cản che chắn khi có Collider. |
| **Mốc 3** | Nâng/bán, điều kiện cấp thành, hình công trình và idempotent. | Tháp cấp 2 bị từ chối khi thành cấp 1; thành lên cấp 2 cho phép nâng; `ApplyLevel` lặp không nhân dồn; Bán hoàn tiền đúng tỉ lệ, không bán 2 lần; `CastleVisualController` đổi hình theo cấp. |
| **Mốc 4** | Ghép prefab thật (Bò/Máy/Tháp), bảng giá nâng cấp từng bước từ Excel. | Xác nhận `cow_barn` `[120, 280]`, `production_machine` `[150, 250]`; kiểm tra `TowerUpgradeBridge` sẵn sàng cho Người 3. |

---

## 5. Danh sách kiểm tra hoàn thành (Checklist)

- [x] **Xây đúng prefab/ô và đúng kho; không xây chồng hoặc xây khi bị khóa.**  
  *(Kiểm tra loại ô, kiểm tra unlockCastleLevel, kiểm tra IsUnlocked của MapZone, kiểm tra kho dùng chung).*
- [x] **Thất bại không mất tiền/chiếm ô; thao tác lặp không tạo trùng.**  
  *(Kiểm tra lock giao dịch, không trừ tiền khi CanBuild trả về false, không tạo 2 công trình trên cùng ô).*
- [x] **Nâng cấp tối đa/thiếu tiền không bị trừ Credits.**  
  *(Kiểm tra IsMaxLevel và kiểm tra số dư Credits trước khi gọi TryRemove).*
- [x] **ApplyLevel không cộng dồn khi gọi lặp; không vượt bảng hỗ trợ.**  
  *(Đảm bảo tính idempotent khi gọi cùng cấp; giới hạn cấp 1 đến 3).*
- [x] **Bán không hoàn tiền hai lần, không để đối tượng trong registry.**  
  *(Kiểm tra cờ isSelling, wasSold, dọn dẹp Occupant, hoàn tiền đúng tỉ lệ cấu hình).*
- [x] **UI của Towers dùng được đầu mối nâng cấp mà không phụ thuộc lớp cụ thể.**  
  *(Cung cấp `TowerUpgradeBridge` và UnityEvent trên `BuildingUpgradeController`).*

---

## 6. Hướng dẫn thử nghiệm trong Unity

### A. Kiểm tra tự động trên Scene chính (`BuildingWorkspace.unity`)
1. Mở scene `Assets/_Game/Scenes/Workspaces/BuildingWorkspace.unity`.
2. Nhấn **Play**.
3. Do component `BuildingWorkspaceValidator` trên `TestSupport` đã bật `runOnStart: true`, toàn bộ bộ test sẽ tự động chạy trong vài frame đầu tiên.
4. Bạn cũng có thể nhấp chuột phải vào `BuildingWorkspaceValidator` chọn **Run Building Verification Suite (Play Mode)** để chạy lại bất kỳ lúc nào.
5. Quan sát Console: Tất cả các bước từ Mốc 1 đến Mốc 4 đều hiển thị `[BuildingValidator] PASS` với 0 thông báo lỗi.

### B. Kiểm tra tương thích ngược trên Scene thử phụ (`BuildingTest.unity`)
1. Chọn menu: **Tools > PolishCow > Building > Run Play Mode Smoke Test**.
2. Bộ smoke test kiểm tra độc lập 3 ô ban đầu, đảm bảo luồng cũ không bị ảnh hưởng và Credits hoàn trả chính xác sau khi thoát Play Mode.

---

## 7. Hướng dẫn bàn giao & Tích hợp liên module

- **Dành cho Người 3 (Towers):**
  - Khi người chơi click vào một tháp trên bản đồ, UI Towers có thể kiểm tra nâng cấp thông qua `TowerUpgradeBridge.CanUpgrade(slot, out reason)`.
  - Để thực hiện nâng cấp: gọi `TowerUpgradeBridge.TryUpgrade(slot)` hoặc gọi `slot.GetComponent<BuildingUpgradeController>().RequestUpgrade()`.
  - Lắng nghe sự kiện `onTowerUpgraded` / `onTowerUpgradeFailed` trên `TowerUpgradeBridge` để cập nhật giao diện.
- **Dành cho Người 1 (Core / Tích hợp):**
  - `BuildingFeature.prefab` đã sẵn sàng trong `Assets/_Game/Prefabs/Modules/Building/`.
  - Có thể kéo `BuildingFeature.prefab` vào bất kỳ scene nào có `DefenseSceneContext`. Module sẽ tự động duyệt `Context.Slots` và khởi tạo toàn bộ tính năng xây dựng, nâng cấp và bán.
  - Hình ảnh thành 3 cấp (`CastleVisual.prefab`) đã được liên kết với `BuildingFeature` và tự động hiển thị tại vị trí `Context.CastleTarget`.
