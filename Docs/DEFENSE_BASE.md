# Map Sprout Lands và base phòng thủ

## Chạy và kiểm tra

Mở `Assets/_Game/Scenes/Main.unity`, Play. Map dùng Sprout Lands; Player/bò/drone và gameplay sản xuất hiện có được giữ lại. Bán qua đơn hàng trong Main không còn kết thúc sau 5 đơn; scene thử cũ vẫn mặc định chế độ demo 5 đơn.

Map cũ 24×14 ô được thay bằng map đất 64×48 ô (3072 ô, diện tích khoảng 9 lần), có nước viền ngoài. Map chia 5 khu:

| Khu | Phạm vi | Cấp mở |
| --- | --- | --- |
| CentralFarm | x [-16,16], y [-12,12] | 1 |
| WestPasture | x [-32,-16], y [-12,12] | 2 |
| EastWorkshop | x [16,32], y [-12,12] | 2 |
| NorthKeep | x [-32,32], y [12,24] | 3 |
| SouthFields | x [-32,32], y [-24,-12] | 3 |

Khu khóa có lớp che và collider; lên cấp tự mở. Camera theo Player, giới hạn trong phần map đã mở. Công trình gameplay cũ giữ vị trí; `PrototypeMap/Grid`, `Obstacles`, `WorldArt` được tắt trong scene để giữ bản cũ đối chiếu. Bản scene trước migration nằm tại `output/backups/Main-before-expanded-map.unity` trên máy thực hiện.

Menu `Tools → PolishCow → Expanded Map`:

- **Build Sprout Lands Map and Foundation**: tạo map/base một lần trong Main đã lưu; chạy lại chỉ verify, không ghi đè map đã chỉnh.
- **Verify Map and Foundation**: kiểm tra số khu, slot, waypoint, dữ liệu và base duy nhất.
- **Run Foundation Play Mode Test**: kiểm tra mở đất theo EXP, kho mới, giao dịch xây, timer, pause và thua; không lưu trạng thái thử vào scene.
- **Prepare Defense Test Scenes**: tạo `CoreTest`, `EnemyTest`, `TowerTest` nếu chưa có, không ghi đè scene thành viên. Mỗi scene có một kho/Core/HUD, nền, 4 ô tháp và một route mẫu trong `FeatureWork`. Chưa có quái/tháp thật hoặc Player trong các rig tối thiểu này.

Trong Play, chọn `GameSystems`: context menu `CastleProgress → Foundation Test` thêm EXP; `CastleHealth` gây 25 sát thương; `WaveTimer` bắt đầu đếm ngược. Đây là công cụ thử trong Inspector, chưa phải nút gameplay.

## Hợp đồng Core

Base làm song song cho bốn người, scene workspace và hợp đồng mục tiêu mới: [TEAM_4_WORKSTREAMS](TEAM_4_WORKSTREAMS.md). Các composition root nhận DefenseSceneContext qua DefenseModuleInstaller; Main chỉ ghép prefab do từng người sở hữu.

- `IDamageable.TakeDamage(int)`: thành/quái có thể triển khai chung.
- `CastleProgress.AddExperience(int)`: ngưỡng 495 và **3210** (làm tròn lên từ 3209.4); tối đa cấp 3; EXP không tràn int. Phát `OnExpChanged(int)` và `OnCastleLevelUp(int)`.
- `CastleHealth`: HP 120/200/320; lên cấp cộng phần sức chứa tăng thêm, giữ sát thương đã nhận. Phát `OnCastleDamaged(current,max)` và `OnCastleDestroyed` một lần.
- `GameManager`: Preparing/Fighting/Paused/Won/Lost. `SetPaused(bool)` dừng timeScale, khóa điều khiển; khôi phục timeScale cũ khi tiếp tục. Kết thúc khóa Player.
- `BeginNextWave()` phát `OnWaveStarted(wave)` một lần.
- `CompleteWave(wave)` chỉ gọi sau khi spawn xong và không còn quái sống; từ chối sai wave hoặc hoàn tất lặp. Phát `OnWaveEnded(wave)`.
- `ReportEnemyKilled(exp,credits,isFinalBoss)` gọi đúng một lần khi quái chết; Core cộng EXP/tiền và phát `OnEnemyKilled`. Không tự cộng lần nữa trong EnemyController. Không gọi khi quái lọt tới thành hoặc trả về pool.
- Boss cuối bị hạ ở wave 10 thì thắng ngay. Thành hết HP thì chuyển Lost ngay trong base; hiệu ứng nổ/màn thua hoàn thiện do module VFX/UI ghép thêm.
- `WaveTimer.StartCountdown()`: đọc wave kế tiếp từ mảng cấu hình; bắt đầu wave khi hết giờ và bắt đầu đếm tiếp sau CompleteWave.

**Main chưa tự chạy wave**: `startAutomatically` đang tắt vì chưa có WaveSpawner thật. Chỉ bật sau khi spawner đã đăng ký sự kiện; tránh timer bắt đầu chiến đấu không có quái. Đây là base tích hợp, chưa phải game phòng thủ hoàn chỉnh.

## Dữ liệu

`Assets/_Game/Data/Defense/` chứa `Castle`, 5 `EnemyDefinition`, 10 `WaveDefinition`, 4 `TowerDefinition` theo bảng Excel. Chỉnh asset trong Inspector, không sửa số ở code runtime. Các prefab quái/tháp trong definition để trống cho người phụ trách gán khi hoàn thành.

Giá nâng tháp lưu theo từng bước; dữ liệu quái gồm HP, tốc độ, sát thương thành, EXP, Credits. Wave có nhóm quái, số lượng, khoảng cách spawn, hệ số HP và tiền thưởng hoàn thành. `WaveSpawner` tương lai chịu trách nhiệm thưởng hoàn thành wave đúng một lần; GameManager hiện chỉ thưởng quái chết.

## Điểm nối map/xây dựng

- `MapZone` nghe cấp thành, bật nội dung và bỏ khóa; không tắt chính GameObject chứa MapZone để tránh mất sự kiện.
- Mỗi khu có `Content`; đặt công trình/slot/spawn cần khóa vào đúng khu.
- Tổng slot: tháp 12, bò 8, máy 4. Theo cấp: **4/3/1 → 8/5/2 → 12/8/4**. Chuồng và máy ban đầu đã chiếm 2 slot tương ứng.
- `DefenseBuildingSlot.TryBuild(prefab,price,kind,unlockCastleLevel)`: kiểm tra kho/slot/cấp/loại, thanh toán, Instantiate rồi Initialize; khóa giao dịch để callback OnChanged không xây hai lần. Thất bại tạo instance thì hoàn tiền. Prefab phải có FeatureReferences và đúng một UpgradeTarget ở gốc.
- UI/catalog phải truyền prefab, giá và cấp mở từ cấu hình đáng tin cậy; slot không tự chọn hoặc định giá vật phẩm. Module xây/tháp dùng cùng phương thức này, không tự trừ tiền thêm.
- `EnemyRoute` có mảng Waypoints theo thứ tự từ spawn tới thành. Có 6 route, nằm trong Content của khu tương ứng. Spawner chỉ chọn route đang active; Fly bỏ waypoint và chọn bò qua registry của module bò sau này.
- `CentralFarm/Content/CastleTarget` là điểm đích để quái/VFX gắn với thành.

## Kho và UI

`Medicine`, `SpoiledMilk` thêm cuối ItemType để giữ giá trị enum cũ. InventoryManager hỗ trợ GetAmount/Add/TryRemove với hai item mới. `CastleHUD` riêng hiển thị HP, cấp, EXP, wave và số thuốc/sữa hỏng; không thay font/icon kho đang được chỉnh riêng.

Main dùng đơn hàng liên tục như bước chuyển tiếp. **Chưa có điểm bán giá cố định riêng hoặc đơn thưởng ×1.5**; module kinh tế triển khai tiếp.

## Phần còn lại cho từng module

| Module | Phần đã lên base | Phần cần triển khai tiếp |
| --- | --- | --- |
| Core | State, damage, EXP, HP, timer và sự kiện | Lưu/tải, menu hoàn chỉnh, điều phối hiệu ứng kết thúc |
| Economy | Kho mới, Main bỏ giới hạn đơn | CowHealth, thuốc, sữa hỏng, bán trực tiếp, shop và giá nâng |
| Building | Map 5 khu, camera, slot và TryBuild | Catalog/menu đặt, kiểm tra footprint, nâng/bán, hình thành theo cấp |
| Enemy | Enemy/WaveDefinition, waypoint, sự kiện Core | EnemyController, WaveSpawner, ruồi, HP bar, pooling |
| Tower/UI | TowerDefinition, 12 slot, CastleHUD | TowerController, đạn, slow/AOE, shop khóa cấp, animation, âm thanh |

Chưa nhập các sprite trong `Docs/New/Enemy` vào gameplay. Chưa đổi nhân vật/bò. Quái/tháp/bệnh không được trình bày là tính năng đã hoàn thành.

## Asset production tìm được

- Solaria Farming & Crafting: https://jamiebrownhill.itch.io/solaria-farming-crafting — bản đầy đủ có cheese press, cask, keg và nhiều máy; pixel art 16×16; trang niêm yết 10 USD cho bản đầy đủ. Bản miễn phí có một số bàn chế tạo nhưng không bao gồm toàn bộ máy đó.
- Farmstead Artisan Workshop: https://saczland.itch.io/farmstead-artisan-workshop — mẫu miễn phí có butter churn; dùng phi thương mại, có animation. Sprite cell 128×128, cần kiểm tra tỉ lệ và phong cách khi ghép Sprout Lands.

Chưa mua/tải hai pack này; chưa xác nhận có máy đóng chai sữa chuyên dụng. Chỉ là ứng viên thay hình ProductionMachine; gameplay sản xuất hiện tại đã có.
