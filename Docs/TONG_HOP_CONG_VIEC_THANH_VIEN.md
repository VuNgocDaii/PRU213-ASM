# Tổng hợp công việc các thành viên — PolishCow

Cập nhật: 12/11/2026. Nội dung dựa trên lịch sử Git và project hiện tại. Phần hoàn thiện Main được ghi riêng để không nhầm với đóng góp gốc của từng người.

## 1. Công việc, mục đích và luồng xử lý của từng người

Các luồng dưới đây mô tả cách các phần hoạt động trong Main sau khi ghép. Những sửa đổi chung ở mục 2 được tách khỏi phần tính năng gốc.

Đường dẫn bên dưới tính từ **`Assets/_Game/`**; bấm tên file để mở code. Project dùng **Unity và C#**. Script xử lý hành vi; **prefab** là mẫu đối tượng đã gắn thành phần; **asset cấu hình** lưu giá, sát thương, số quái và các thông số khác.

### Đại — Nền project và tích hợp

**Làm gì:** tổ chức project, nhân vật, tương tác E, kho, HUD, hệ thống thành/trận đấu và ghép các tính năng.

**Vì sao cần làm như vậy:** các tính năng đều dùng tiền và vật phẩm. Nếu mỗi phần có một kho hoặc cách tính trạng thái riêng, người chơi có thể thấy tiền trên HUD khác tiền dùng để mua. Vì vậy cần kho dùng chung, quy tắc kết nối chung và một nơi quản lý trạng thái trận.

**Luồng xử lý:**

1. Mở Main → khởi tạo kho và kết nối các phần bò, máy, đơn hàng, xây dựng, quái và tháp.
2. Người chơi di chuyển đến gần đối tượng → hiện gợi ý E → nhấn E để gọi đúng chức năng của đối tượng đó.
3. Chức năng thay đổi tiền/vật phẩm trong kho → HUD cập nhật số lượng.
4. Hệ thống trận đếm thời gian, bắt đầu đợt quái và nhận kết quả chiến đấu. EXP đủ thì thành lên cấp, mở thêm khu vực; thành bị phá hoặc boss cuối chết thì chuyển sang thua/thắng.

**Điểm chính:** chuẩn bị scene làm việc riêng để từng người thử phần mình; khi ghép vào Main phải nối đúng kho, đối tượng và trạng thái trận. Mã tiêu biểu: `InventoryManager`, `PlayerInteraction`, `GameManager`, `DefenseSceneContext`.

**Dùng gì:** kho chung, sự kiện cập nhật dữ liệu, thành phần tương tác, Canvas và TextMeshPro để hiển thị HUD.

| Phần trong luồng | Thư mục | Code cần mở |
| --- | --- | --- |
| Khởi tạo, nối tính năng | `Scripts/Core` | [FeatureReferenceInitializer.cs](../Assets/_Game/Scripts/Core/FeatureReferenceInitializer.cs), [DefenseModuleInstaller.cs](../Assets/_Game/Scripts/Core/DefenseModuleInstaller.cs), [DefenseSceneContext.cs](../Assets/_Game/Scripts/Core/DefenseSceneContext.cs). |
| Thêm/trừ tài nguyên, cập nhật HUD | `Scripts/Inventory`, `Scripts/UI` | [InventoryManager.cs](../Assets/_Game/Scripts/Inventory/InventoryManager.cs), [InventoryHUD.cs](../Assets/_Game/Scripts/UI/InventoryHUD.cs). |
| Tìm đối tượng, nhấn E | `Scripts/Interaction` | [PlayerInteraction.cs](../Assets/_Game/Scripts/Interaction/PlayerInteraction.cs): `FindNearestTarget()`, `UpdateHint()`, gọi `Interact()`. |
| Trận đấu, đếm giờ, cấp và máu thành | `Scripts/Core` | [GameManager.cs](../Assets/_Game/Scripts/Core/GameManager.cs), [WaveTimer.cs](../Assets/_Game/Scripts/Core/WaveTimer.cs), [CastleProgress.cs](../Assets/_Game/Scripts/Core/CastleProgress.cs), [CastleHealth.cs](../Assets/_Game/Scripts/Core/CastleHealth.cs). |
| Mở khu vực, camera | `Scripts/World` | [MapZone.cs](../Assets/_Game/Scripts/World/MapZone.cs), [ExpandedMapCamera.cs](../Assets/_Game/Scripts/World/ExpandedMapCamera.cs). |

**Scene và dữ liệu:** `Scenes/Main.unity` là bản ghép để chơi; `Scenes/Workspaces/` chứa các scene làm riêng. Thông số thành và phòng thủ ở `Data/Defense/`.

### Hoàng — Bò sữa và kinh tế

**Làm gì:** bò đi lại, ăn và tạo sữa; nâng cấp chuồng; bệnh, thuốc, mua bò và bán sữa.

**Vì sao cần làm như vậy:** bò là nguồn nguyên liệu cho máy và đơn hàng. Có thời gian sản xuất, sức chứa chuồng và bệnh sẽ tạo việc chăm sóc, đồng thời buộc người chơi cân nhắc tiền mua bò, thuốc và tháp.

**Luồng xử lý:**

1. Mua bò → kiểm tra tiền và sức chứa của chuồng → trừ tiền, tạo bò trong vùng đi lại của chuồng.
2. Đến bò đang rảnh, cho ăn → trừ thức ăn → bắt đầu đếm thời gian tạo sữa.
3. Hết thời gian → bò chờ thu hoạch → người chơi lấy sữa vào kho → bò trở lại trạng thái rảnh.
4. Nếu bị ruồi gây bệnh, bò có thể tạo sữa chậm hơn, ít hơn hoặc ra sữa hỏng. Người chơi dùng thuốc để chữa; sữa hợp lệ tiếp tục đưa sang máy đóng chai.
5. Nâng cấp chuồng để tăng sức chứa và cải thiện sản xuất; sữa chai có thể bán tại cửa hàng.

**Điểm chính:** tách ba trạng thái rảnh/đang tạo sữa/chờ thu hoạch để tránh cho ăn hoặc lấy sữa lặp. Vùng đi lại thuộc chuồng để bò không chạy ra ngoài. Mã tiêu biểu: `CowController`, `CowMovement`, `CowHealth`, `CowShop`.

**Dùng gì:** trạng thái bò để quản lý sản xuất, `Rigidbody2D` để di chuyển và `RoamingArea` để giới hạn chỗ đi lại. Các cửa hàng dùng kho chung.

| Phần trong luồng | Thư mục | Code cần mở |
| --- | --- | --- |
| Cho ăn, đếm giờ, thu sữa | `Scripts/Features/Cows` | [CowController.cs](../Assets/_Game/Scripts/Features/Cows/CowController.cs): `TryFeed()`, `Update()`, `TryCollectMilk()`; [CowInteraction.cs](../Assets/_Game/Scripts/Features/Cows/CowInteraction.cs) nhận tương tác và chữa bệnh. |
| Đi lại trong chuồng | `Scripts/Features/Cows`, `Scripts/Core` | [CowMovement.cs](../Assets/_Game/Scripts/Features/Cows/CowMovement.cs) chọn vị trí và giới hạn di chuyển qua [RoamingArea.cs](../Assets/_Game/Scripts/Core/RoamingArea.cs). |
| Bệnh và thông số theo cấp | `Scripts/Features/Cows` | [CowHealth.cs](../Assets/_Game/Scripts/Features/Cows/CowHealth.cs), [CowUpgrade.cs](../Assets/_Game/Scripts/Features/Cows/CowUpgrade.cs). |
| Mua bò, thuốc và bán sữa | `Scripts/Features/Economy` | [CowShop.cs](../Assets/_Game/Scripts/Features/Economy/CowShop.cs), [MedicineShop.cs](../Assets/_Game/Scripts/Features/Economy/MedicineShop.cs), [MilkShop.cs](../Assets/_Game/Scripts/Features/Economy/MilkShop.cs). |

**Đối tượng có sẵn:** `Prefabs/Features/Cows/CowBarn.prefab`, `IndividualCow.prefab`; các shop ở `Prefabs/Modules/Economy/`. Scene thử: `Scenes/Workspaces/EconomyWorkspace.unity`.

### Tuấn — Xây dựng và nâng cấp

**Làm gì:** bảng xây dựng, kiểm tra điều kiện, xây/nâng cấp/bán công trình và hình thành theo cấp.

**Vì sao cần làm như vậy:** nhiều loại công trình cùng dùng tiền và ô xây. Dùng một luồng giao dịch chung giúp kiểm tra thống nhất, tránh trừ tiền hai lần, xây đè lên nhau hoặc bán xong vẫn không xây lại được.

**Luồng xử lý qua bảng xây dựng:**

1. Người chơi đến ô xây, nhấn E → bảng hiện công trình có thể xây và giá.
2. Chọn xây → kiểm tra loại ô, cấp mở khóa, tiền, vật cản và ô có đang chứa công trình hay không.
3. Đủ điều kiện → thực hiện thanh toán, tạo công trình và ghi nhận ô đã có chủ. Khi nối với ô phòng thủ, ô đó chịu trách nhiệm thu tiền để tránh thu hai lần.
4. Chọn nâng cấp → kiểm tra cấp và tiền → trừ đúng giá → áp dụng thông số/hình ảnh cấp mới.
5. Chọn bán → hoàn tiền theo cấu hình, xóa công trình và giải phóng ô để xây lại. Nếu giao dịch lỗi, xử lý hoàn tiền theo luồng tương ứng.

**Điểm chính:** điều kiện không đạt phải có lý do hiển thị cho người chơi. Giá, cấp mở khóa và công trình lấy từ bảng cấu hình để dễ chỉnh. Mã tiêu biểu: `BuildingController`, `BuildingUpgradeController`, `BuildingPanel`, `BuildingCatalogData`.

**Dùng gì:** nút UI để chọn thao tác; `BuildingCatalogData` dạng ScriptableObject lưu cấu hình; lớp `UpgradeTarget` để các loại công trình cùng nhận lệnh nâng cấp.

| Phần trong luồng | Thư mục | Code cần mở |
| --- | --- | --- |
| Mở bảng, chọn công trình | `Scripts/Features/Building` | [BuildingSlot.cs](../Assets/_Game/Scripts/Features/Building/BuildingSlot.cs), [BuildingPanel.cs](../Assets/_Game/Scripts/Features/Building/BuildingPanel.cs). |
| Kiểm tra, xây và bán | `Scripts/Features/Building` | [BuildingController.cs](../Assets/_Game/Scripts/Features/Building/BuildingController.cs): `CanBuild()`, `TryBuild()`, `TrySell()`; [BuildingFootprintUtility.cs](../Assets/_Game/Scripts/Features/Building/BuildingFootprintUtility.cs) kiểm tra vật cản. |
| Nâng cấp, đổi hình thành | `Scripts/Features/Building` | [BuildingUpgradeController.cs](../Assets/_Game/Scripts/Features/Building/BuildingUpgradeController.cs), [CastleVisualController.cs](../Assets/_Game/Scripts/Features/Building/CastleVisualController.cs). |
| Nối với ô xây trên map | `Scripts/Features/Building`, `Scripts/Core` | [BuildingFeature.cs](../Assets/_Game/Scripts/Features/Building/BuildingFeature.cs) nối với [DefenseBuildingSlot.cs](../Assets/_Game/Scripts/Core/DefenseBuildingSlot.cs), nơi giữ công trình và thu tiền khi xây qua ô phòng thủ. |

**Cấu hình và UI:** `Data/Features/Building/BuildingCatalog.asset` lưu giá, prefab và cấp mở. Mẫu công trình/UI ở `Prefabs/Features/Building/`; scene làm riêng là `Scenes/Workspaces/BuildingWorkspace.unity`.

### Thắng — Máy sản xuất và quái

**Làm gì:** máy đóng chai, nâng cấp máy; các loại quái, ruồi, boss và hệ thống sinh quái theo đợt.

**Vì sao cần làm như vậy:** máy nối nguyên liệu từ bò với hàng giao bán. Quái tạo áp lực lên thành và đàn bò, khiến người chơi phải dùng thu nhập để phòng thủ thay vì chỉ sản xuất liên tục.

**Luồng máy sản xuất:** đến máy, nhấn E → kiểm tra điện và sữa thô → trừ nguyên liệu → đếm thời gian → chờ nhận sữa chai bằng E. Nếu mất điện giữa chừng thì giữ tiến độ và chờ có điện; máy chưa nhận thành phẩm sẽ không chạy thêm lượt mới.

**Luồng chiến đấu:**

1. Nhận yêu cầu bắt đầu đợt → đọc số lượng/loại quái → sinh từng nhóm theo thời gian quy định, chọn đường thuộc khu đang mở.
2. Quái mặt đất đi theo đường đến thành; ruồi tìm bò để gây bệnh.
3. Quái bị tháp hạ → báo tiền và EXP. Quái lọt đến thành → gây mất máu thành, không tính là một lần giết quái.
4. Khi đã sinh hết và không còn quái của đợt → thưởng hoàn thành, kết thúc đợt. Boss cuối bị hạ sẽ báo điều kiện thắng cho hệ thống trận.

**Điểm chính:** tái sử dụng quái đã kết thúc hoạt động để giảm tạo/xóa liên tục; đếm quái và thưởng đúng một lần. Mã tiêu biểu: `ProductionMachine`, `MachineUpgrade`, `WaveSpawner`, `GroundEnemy`, `FlyEnemy`.

**Dùng gì:** máy dùng bộ đếm thời gian và trạng thái có điện; sinh quái dùng coroutine để chờ giữa các lần sinh. Quái đi theo waypoint và được giữ lại trong danh sách để tái sử dụng.

| Phần trong luồng | Thư mục | Code cần mở |
| --- | --- | --- |
| Đóng chai, giữ tiến độ, nhận hàng | `Scripts/Features/Production` | [ProductionMachine.cs](../Assets/_Game/Scripts/Features/Production/ProductionMachine.cs): `Interact()`, `StartProcessing()`, `Update()`, `Collect()`; kiểm tra điện qua [PowerSource.cs](../Assets/_Game/Scripts/Core/PowerSource.cs). |
| Áp dụng cấp máy | `Scripts/Features/Production` | [MachineUpgrade.cs](../Assets/_Game/Scripts/Features/Production/MachineUpgrade.cs): `ApplyLevel()` truyền thông số mới cho máy. |
| Sinh quái, đếm và kết thúc đợt | `Scripts/Features/Enemies` | [WaveSpawner.cs](../Assets/_Game/Scripts/Features/Enemies/WaveSpawner.cs): `SpawnWave()`, `SpawnEnemy()`, `CheckWaveFinished()`. |
| Đi đến thành, nhận sát thương, gây bệnh | `Scripts/Features/Enemies` | [EnemyController.cs](../Assets/_Game/Scripts/Features/Enemies/EnemyController.cs), [GroundEnemy.cs](../Assets/_Game/Scripts/Features/Enemies/GroundEnemy.cs), [FlyEnemy.cs](../Assets/_Game/Scripts/Features/Enemies/FlyEnemy.cs). |

**Đối tượng và dữ liệu:** máy ở `Prefabs/Features/Production/ProductionMachine.prefab`; module quái ở `Prefabs/Modules/Enemies/`. `Data/Defense/Enemy0.asset` đến `Enemy4.asset` lưu thông số địch; `Wave01.asset` đến `Wave10.asset` lưu từng đợt. Scene thử: `Scenes/Tests/ProductionTest.unity`, `Scenes/Workspaces/EnemiesWorkspace.unity`.

### An — Đơn hàng và tháp phòng thủ

**Làm gì:** đơn hàng, giao hàng bằng drone, cửa hàng thức ăn; tháp, đạn, bảng mua/nâng cấp/bán và UI chiến đấu.

**Vì sao cần làm như vậy:** đơn hàng biến sữa chai thành tiền để người chơi tiếp tục chăm bò và xây tháp. Các loại tháp có vai trò khác nhau để người chơi lựa chọn cách bảo vệ thành.

**Luồng đơn hàng:** tạo yêu cầu số chai và tiền thưởng → người chơi chọn giao → kiểm tra đủ hàng và không đang giao → trừ sữa, cộng thưởng và chạy hiệu ứng drone → kết thúc giao, cập nhật số đơn và tạo đơn tiếp theo. Cửa hàng thức ăn dùng Credits để bổ sung thức ăn cho vòng nuôi bò.

**Luồng tháp:**

1. Chọn loại tháp ở HUD → bấm ô đã mở → kiểm tra tiền/loại ô/cấp thành → xây tháp.
2. Tháp tìm quái trong tầm → bắn đạn → đạn đến mục tiêu mới gây sát thương. Cannon có sát thương vùng, Ice làm chậm; Arrow và Sniper dùng thông số bắn riêng.
3. Bấm tháp đã xây → mở thông tin, xem tầm bắn và chọn nâng cấp hoặc bán. Sau tích hợp, thao tác này nối với phần xây dựng của Tuấn để dùng chung giao dịch.
4. UI chiến đấu hiển thị lên cấp, tạm dừng và thắng/thua; chơi lại tải trận mới.

**Điểm chính:** khóa giao hàng trong lúc drone hoạt động để tránh gửi trùng; tái sử dụng đạn để giảm tạo/xóa liên tục. Thông số tháp và hình ảnh phải khớp cấp I/II/III. Mã tiêu biểu: `OrderController`, `DroneDeliveryEffect`, `TowerController`, `Projectile`, `DefenseShopUI`.

**Dùng gì:** đơn hàng dùng dữ liệu yêu cầu và sự kiện cập nhật UI; drone dùng coroutine cho chuyển động theo thời gian. Tháp dùng `SpriteRenderer`, `Animator`, cấu hình riêng và bộ tái sử dụng đạn.

| Phần trong luồng | Thư mục | Code cần mở |
| --- | --- | --- |
| Tạo đơn, giao hàng, nhận thưởng | `Scripts/Features/Orders` | [OrderController.cs](../Assets/_Game/Scripts/Features/Orders/OrderController.cs): `GenerateNextOrder()`, `TryDeliverOrder()`, `FinishDelivery()`; [OrderData.cs](../Assets/_Game/Scripts/Features/Orders/OrderData.cs) giữ yêu cầu đơn. |
| Drone và mua thức ăn | `Scripts/Features/Orders` | [DroneDeliveryEffect.cs](../Assets/_Game/Scripts/Features/Orders/DroneDeliveryEffect.cs), [FeedShop.cs](../Assets/_Game/Scripts/Features/Orders/FeedShop.cs). |
| Chọn tháp, bấm ô xây | `Scripts/UI`, `Scripts/Features/Towers` | [DefenseShopUI.cs](../Assets/_Game/Scripts/UI/DefenseShopUI.cs): `SelectTower()`, `HandleSlotClick()`; [TowersFeature.cs](../Assets/_Game/Scripts/Features/Towers/TowersFeature.cs) nhận click và nối tháp với trận. |
| Tìm quái, bắn, gây sát thương | `Scripts/Features/Towers` | [TowerController.cs](../Assets/_Game/Scripts/Features/Towers/TowerController.cs): `FindBestTarget()`, `Shoot()`; [Projectile.cs](../Assets/_Game/Scripts/Features/Towers/Projectile.cs) xử lý trúng đích; [ProjectilePool.cs](../Assets/_Game/Scripts/Features/Towers/ProjectilePool.cs) tái sử dụng đạn. |
| Nâng/bán tháp, tạm dừng, chơi lại | `Scripts/UI` | [TowerActionMenuUI.cs](../Assets/_Game/Scripts/UI/TowerActionMenuUI.cs), [BattleUI.cs](../Assets/_Game/Scripts/UI/BattleUI.cs). |

**Đối tượng và hình ảnh:** đơn hàng/shop thức ăn ở `Prefabs/Features/Orders/`; tháp ở `Prefabs/Features/Tower/`. `Data/Defense/Tower0.asset` đến `Tower3.asset` lưu thông số; ảnh ở `Art/Features/Towers/`, hoạt ảnh ba cấp ở `Animation/Tower/Levels/`. Scene thử: `Scenes/Workspaces/TowersWorkspace.unity`.

### Luồng chung giữa các phần

**Mua thức ăn → chăm bò → lấy sữa thô → đóng chai → giao hàng/ bán sữa → nhận tiền → xây và nâng tháp → diệt quái → nhận tiền, EXP → mở khu vực → hạ boss cuối.**

Phần nông trại tạo thu nhập, phần phòng thủ bảo vệ thành; kho và hệ thống trận nối các công việc của cả nhóm lại với nhau.

## 2. Những phần đã hoàn thiện trong Main

Các mục dưới đây là phần ghép và sửa chung, có hỗ trợ Codex; không tính lại toàn bộ vào công việc gốc của một thành viên.

- **Ghép hệ thống:** kinh tế, xây dựng, quái và tháp cùng dùng một kho; các thao tác mua, xây, nâng cấp và bán được đồng bộ.
- **Chuồng bò:** giữ một chuồng gọn, có rào và lối vào. Bò đi trong sân chuồng; mua thêm bò theo sức chứa cấp I/II/III là **2/3/4 con**.
- **Phòng thủ:** có **22 ô xây tháp**, bố trí dọc đường quái và gần thành. Bốn loại Arrow, Cannon, Ice, Sniper đều có ba cấp I/II/III.
- **Bản đồ:** sửa khung ảnh cây/bụi/hoa bị cắt; thêm cụm cây, vườn, ao và dãy cửa hàng. Đưa máy sản xuất về khu xưởng, giữ đường đi và ô tháp thông thoáng.
- **HUD:** thu gọn thanh chọn tháp và nhãn khóa cấp để không che hướng dẫn tương tác.
- **Vòng chơi:** bắt đầu với **300 Credits, 10 Feed**, có **10 đợt quái**. Hạ boss cuối để thắng; thành hết máu thì thua; nút chơi lại khởi tạo trận mới.

### Code của phần hoàn thiện chung

Các file dưới đây ở **`Assets/_Game/Editor/`**, dùng trong Unity Editor để dựng hoặc kiểm tra Main.

| Phần cần chỉnh | Code | Công dụng |
| --- | --- | --- |
| Ghép tính năng | [PolishCowAllFeaturesSetup.cs](../Assets/_Game/Editor/PolishCowAllFeaturesSetup.cs) | Nối module, kho, UI và cấu hình vào Main. |
| Ô tháp và chuồng | [PolishCowFixedBuildLayout.cs](../Assets/_Game/Editor/PolishCowFixedBuildLayout.cs), [PolishCowBarnEnclosure.cs](../Assets/_Game/Editor/PolishCowBarnEnclosure.cs) | Căn ô xây, giữ một chuồng bò và dựng rào. |
| Cây, đường, cảnh quan | [PolishCowMapArtPass.cs](../Assets/_Game/Editor/PolishCowMapArtPass.cs) | Chọn đúng khung ảnh, dựng cây, vườn, ao, chợ. Ảnh nguồn ở `Art/SproutLands/`; sprite dùng ở `Data/Defense/Sprites/`. |
| HUD và tháp ba cấp | [PolishCowUIPolish.cs](../Assets/_Game/Editor/PolishCowUIPolish.cs), [PolishCowTowerLevelSetup.cs](../Assets/_Game/Editor/PolishCowTowerLevelSetup.cs) | Căn HUD; chuẩn bị hình ảnh và hoạt ảnh I/II/III. |
| Test đến chiến thắng | [PolishCowVictoryPlaythrough.cs](../Assets/_Game/Editor/PolishCowVictoryPlaythrough.cs) | Tự xây/nâng tháp, chạy 10 đợt, kiểm tra thắng và chơi lại. |

### Các lỗi chính đã xử lý

- Nhân vật đứng gần ô xây bị tính nhầm là vật cản.
- Mua bò chọn nhầm bò riêng lẻ thành chuồng, dẫn đến vượt sức chứa.
- Bán công trình chưa giải phóng ô xây; trạng thái nâng cấp tháp chưa đồng bộ.
- Cấp phát đạn khi hết đạn dự phòng và xử lý sát thương vùng chưa đúng.
- Cây/trang trí lấy sai phần ảnh; thanh chọn tháp chồng lên HUD tương tác.

## 3. Kết quả kiểm tra gần nhất

| Nội dung | Kết quả |
| --- | --- |
| Chơi từ 300 Credits, tự xây/nâng tháp bằng tiền kiếm được | **Qua đủ 10 đợt**, thành còn **320/320 máu**; không cấp thêm tiền, EXP hay sát thương từ test. |
| Màn hình chiến thắng và nút chơi lại | Hiện đúng; chơi lại về đợt 0, 300 Credits và thành đầy máu. |
| Cây và vị trí xây tháp trên map mới | Cây/bụi/hoa khớp khung ảnh gốc; cả 22 ô không bị vật trang trí chặn. |
| Thanh chọn tháp và HUD tương tác | Đã xem trong Play Mode; đủ bốn nút tháp, không còn chồng lên gợi ý E. |

**Lưu ý về test:** lượt phá đảo chạy tốc độ ×8, bỏ thời gian chờ giữa các đợt và dùng seed 213 để lặp lại. Kết quả xác nhận một chiến lược chơi đã thắng, không có nghĩa mọi cách chơi đều dễ thắng. Test tích hợp có cấp sẵn tài nguyên là chế độ riêng.

## 4. Điểm cần nhớ khi bàn giao

- Chạy game ở `Assets/_Game/Scenes/Main.unity`. Dùng WASD/phím mũi tên để đi, E để tương tác; chọn tháp rồi bấm ô trống để xây.
- Hiện chỉ có **một chuồng bò**; các ô xây trống còn lại dùng cho tháp. Không dùng mô tả cũ về việc xây thêm nhiều chuồng.
- Bản đồ mở theo cấp thành. Giữ khoảng trống quanh đường quái, cửa chuồng và ô tháp khi chỉnh cảnh quan.
- Phân biệt phần tính năng gốc của thành viên với phần sửa/tích hợp chung. Kiểm tra `git status` trước khi chốt bản bàn giao.
- Chưa ghi nhận âm thanh chiến đấu là phần đã hoàn thiện.

Tài liệu chi tiết khi cần: [chạy Main](MAIN_INTEGRATION.md), [test phá đảo](VICTORY_PLAYTHROUGH.md), [bản đồ](MAP_ART_PASS.md), [ba cấp tháp](TOWER_LEVELS.md).
