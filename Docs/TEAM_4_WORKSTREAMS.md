# Base cho 4 người làm độc lập

Bạn giữ vai trò tích hợp/Core. Bốn thành viên làm bốn module dưới đây. Tài liệu này thay phân công Cows/Production/Orders riêng của giai đoạn cũ; các hợp đồng kho, Initialize và ApplyLevel vẫn giữ.

## 1. Phạm vi mỗi người

Mở nhanh trong Unity: **Tools → PolishCow → Team Base → Open Team Workspaces**. Mỗi phần có nút mở scene riêng và chọn prefab module. Scene Economy đã gắn máy chế biến và thùng giao hàng Farm & Ranch Life; logic sản xuất/đơn hàng hiện có giữ nguyên.

| Người | Nhánh | Công việc | Scene làm việc chính | Prefab bàn giao |
| --- | --- | --- | --- | --- |
| 1 | feature/economy | Bò, bệnh/thuốc, máy, bán sữa/đơn hàng/shop Feed | Workspaces/EconomyWorkspace | Prefabs/Modules/Economy/EconomyFeature |
| 2 | feature/building | Catalog và UI xây/nâng/bán; bố trí công trình và hình thành | Workspaces/BuildingWorkspace | Prefabs/Modules/Building/BuildingFeature |
| 3 | feature/enemies | Quái, ruồi, boss, wave, pooling và thưởng wave | Workspaces/EnemiesWorkspace | Prefabs/Modules/Enemies/EnemiesFeature |
| 4 | feature/towers | Tháp, đạn, slow/AOE, battle UI và âm thanh chiến đấu | Workspaces/TowersWorkspace | Prefabs/Modules/Towers/TowersFeature |

Đường dẫn scene tính từ `Assets/_Game/Scenes/`; prefab từ `Assets/_Game/`. Mỗi workspace có đúng một Player, kho, GameManager và DefenseSceneContext. Economy có vòng nông trại hiện tại; ba workspace còn lại có nền/slot/route, mục tiêu giả và bảng điều khiển thử ở góc màn hình.

**Không cùng sửa Main.** Main đã tham chiếu bốn prefab module rỗng ở `FeatureModules`; mỗi người phát triển prefab của mình. Module không mang thêm Player, camera, kho, shared HUD hoặc EventSystem. Prefab tính năng hiện có của Economy tiếp tục được dùng; khi chuyển chúng vào module, người tích hợp sẽ gỡ instance cũ để tránh nhân đôi gameplay.

## 2. Quyền sở hữu file

- Economy: `Scripts/Features/{Economy,Cows,Production,Orders}`, prefab Cows/Production/Orders hiện có; scene CowTest/ProductionTest/OrderTest.
- Building: `Scripts/Features/Building`, prefab Building và scene BuildingTest. `DefenseBuildingSlot.cs` nằm trong Core là base đóng băng; `BuildingSlot.cs` trong Features/Building là điểm tương tác của module Building đã có trên main.
- Enemies: `Scripts/Features/Enemies`, EnemyTest; asset `Data/Defense/Enemy*.asset` và `Wave*.asset`.
- Towers: `Scripts/Features/Towers`, TowerTest; asset `Data/Defense/Tower*.asset`.
- Mỗi người sở hữu `Prefabs/Modules/<Role>`, `Data/Features/<Role>`, `Art/Features/<Role>` và `Docs/Features/<Role>.md` tương ứng.
- UI/âm thanh/Editor hỗ trợ của một module đặt trong thư mục tính năng riêng, không cùng sửa `Scripts/UI` hoặc công cụ Editor chung. Script Editor phải nằm trong thư mục con `Editor`.
- Bạn tích hợp giữ: Main, Core, World, Interaction, Inventory, Scripts/UI, TestSupport, Editor chung, Shared, SproutLands và dữ liệu nền chung, ProjectSettings/Packages, tài liệu/quy tắc chung.
- Building làm giao diện/chọn catalog/kiểm tra footprint trong module; MapZone/camera nền chung do tích hợp sửa. Towers làm UI chiến đấu trong canvas riêng của prefab; không chỉnh CastleHUD chung.

Script kiểm tra phạm vi dùng manifest `Tools/FeatureScopes.json`. Đây là kiểm tra file trong PR, không thay thế review logic hoặc quyền nhánh GitHub.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Test-FeatureScope.ps1 -Role economy -BaseRef origin/main -IncludeWorkingTree
```

Đổi Role thành building/enemies/towers. Chạy trên nhánh tính năng sau khi nhận commit base chung. Không chạy để đánh giá toàn bộ working tree của người đang dựng base vì base cố ý sửa file dùng chung. Script trả mã 1 nếu chạm ngoài phạm vi; rename kiểm tra cả đường dẫn cũ và mới.

## 3. Điểm nối không cần đợi code của nhau

`DefenseSceneContext` được gán bằng Inspector trong scene. Nó cung cấp Inventory, Game, Progress, CastleHealth, Timer, CastleTarget, Slots, Routes, Waves và danh sách mục tiêu. Không singleton, không Find kho hoặc scan toàn scene mỗi frame.

Mỗi composition root kế thừa `DefenseModule`. `DefenseModuleInstaller.Start` gọi Initialize sau khi mọi Awake của scene đã hoàn tất. Gọi lại Initialize cùng context không lặp đăng ký. Module runtime mới xây được DefenseBuildingSlot truyền cả kho và context. Instance do spawner/pool tạo tự gọi Initialize(context) sau Instantiate, trước khi bắt đầu gameplay.

```csharp
public class MyFeature : DefenseModule
{
    protected override void OnConnected()
    {
        Context.Game.OnWaveStarted += HandleWave;
    }
    protected override void OnDisconnected()
    {
        Context.Game.OnWaveStarted -= HandleWave;
    }
    private void HandleWave(int wave) { /* module logic */ }
}
```

Awake chỉ lấy component nội bộ. Khi override OnEnable/OnDisable của DefenseModule, gọi base hoặc dùng OnConnected/OnDisconnected để giữ vòng đời đăng ký.

| Bên gọi | Hợp đồng chung | Bên triển khai |
| --- | --- | --- |
| Tháp/đạn | IEnemyTarget: TargetTransform, IsAlive, IsFlying, TakeDamage | Quái |
| Tháp băng | ISlowable.ApplySlow(speedMultiplier,seconds) | Quái |
| Ruồi | ICowDiseaseTarget: TargetTransform, IsHealthy, CanBeInfected, Infect | CowHealth của Economy |
| Quái | Context.CastleHealth.TakeDamage, Context.CastleTarget | Core |
| WaveSpawner | Context.Game.OnWaveStarted, CompleteWave | Core |
| UI xây / UI tháp | DefenseBuildingSlot.TryBuild | Base |
| Shop/UI/module | Inventory.GetAmount/Add/TryRemove | Base |
| Công trình/tháp | UpgradeTarget.ApplyLevel | Module tương ứng |

Enemies đăng ký `Context.RegisterEnemy(this)` khi active, Unregister khi chết/disable/trả pool. CowHealth tương tự RegisterCow/UnregisterCow. Khi duyệt danh sách mà gây damage có thể làm quái chết, chọn mục tiêu trước rồi gây damage; không xóa registry ngay trong vòng foreach đang duyệt. Các danh sách trả read-only để bên khác không sửa trực tiếp.

Towers không tham chiếu EnemyController; Enemies không tham chiếu CowHealth. Các dummy trong TestSupport triển khai cùng interface để mỗi người thử trước khi module thật hoàn tất. Main không chứa dummy/test panel.

## 4. Tránh hai người cùng xử lý một giao dịch

Code Building của Tuan trên main được giữ nguyên (BuildingController, BuildingUpgradeController, BuildingPanel và BuildingSlot). Lớp ô xây của map mới được đặt tên DefenseBuildingSlot trong Core để tránh trùng tên/GUID. Module Building cần nối phần UI/catalog hiện có với slot của map mới khi tích hợp; không để hai bộ điều khiển cùng thanh toán cho một lần xây.

- **Xây:** UI chọn prefab/giá → TryBuild một lần. Chỉ DefenseBuildingSlot trừ tiền/tạo instance/chiếm slot. Không trừ Credits ở UI trước đó.
- **Nâng cấp:** Building sở hữu thao tác thanh toán/cấp; module bò/máy/tháp chỉ nhận ApplyLevel. Towers hiển thị nút nâng, gọi bộ điều khiển nâng cấp của Building qua prefab/UnityEvent khi ghép; không tự viết giao dịch nâng thứ hai.
- **Chết:** EnemyController gọi ReportEnemyKilled đúng một lần. Core cộng Credits/EXP; không cộng ở đạn hoặc tháp.
- **Wave:** WaveSpawner giữ số quái sống, kể cả ruồi; sau khi spawn xong và mọi quái được xử lý, thưởng completionCredits một lần và gọi CompleteWave. Quái chạm thành/ruồi chạm bò không cho EXP.
- **Bệnh:** Ruồi chỉ gọi Infect; Economy sở hữu mức bệnh, tiến trình sữa, thuốc và kho sữa hỏng.
- **Kết thúc:** Core sở hữu Won/Lost; module VFX/UI nghe sự kiện, không tự dừng timer hoặc đổi trạng thái lần nữa.
- **Timer:** giữ startAutomatically=false trong Main đến khi WaveSpawner thật được ghép. Workspace có nút thử Begin wave; không cần chờ tích hợp.

## 5. Handoff

1. Mỗi người tạo nhánh từ cùng commit base đã được người tích hợp commit/push.
2. Chỉ mở workspace/scene thử của mình, làm prefab trong Prefab Mode. Kho/context của scene không được Apply về prefab.
3. Đưa asset mới vào Art/Features/<Role>, giữ license và `.meta`; không đổi import settings của SproutLands chung.
4. Play luồng thành công và thất bại; ghi kết quả tại Docs/Features/<Role>.md.
5. Chạy Test-FeatureScope; tạo PR vào nhánh tích hợp đã thống nhất. Nếu nhóm vẫn dùng main thì PR vào main; không tự tạo/sử dụng develop khi nhóm chưa thống nhất.
6. Người tích hợp nhận prefab, gán references/context trong Main, chạy test base + gameplay. Nếu prefab Economy đã bao gồm bò/máy/shop, gỡ instance cũ khi ghép để không tạo hai bản.

Hợp đồng Core bị đóng băng cho đợt này. Nếu thiếu API, ghi yêu cầu thay đổi vào PR riêng cho base, không sửa Core ngay trong nhánh module.

## 6. Kiểm tra base

Menu `Tools → PolishCow → Team Base → Prepare Four Workspaces` tạo phần thiếu, giữ workspace/prefab đã tồn tại. Base chạy một lần và commit kết quả; thành viên không chạy setup lại hằng ngày.

Menu **Verify Four Workspaces** xác nhận các điểm nối tĩnh. Menu **Run Workspace Contract Tests** Play từng workspace, kiểm tra module nhận context, registry/dummy, thanh toán và truyền context khi xây; không lưu dữ liệu thử. Log tại `Logs/TeamWorkspaceSmokeTest.txt`.

Phần đã có là hạ tầng và mock để làm song song, chưa phải triển khai hoàn chỉnh bốn module.
