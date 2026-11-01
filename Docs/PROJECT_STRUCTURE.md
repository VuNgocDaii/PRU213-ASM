# PolishCow — cấu trúc code và asset

## 1. Các thư mục

Phần game chính nằm trong `Assets/_Game/`:

| Thư mục | Nội dung |
| --- | --- |
| `Scripts` | Logic runtime: Player, kho, UI, tương tác, Core và tính năng |
| `Editor` | Menu setup scene/prefab, Audit và kiểm tra điểm nối; không phải gameplay |
| `Scenes/Main.unity` | Scene game chung, người phụ trách base quản lý |
| `Scenes/Tests` | Bốn scene thử độc lập cho thành viên |
| `Prefabs/Shared` | Player, TeamServices chứa kho/UI, InteractableSample |
| `Prefabs/Features` | Mẫu và prefab riêng của Cows, Production, Building, Orders |
| `Prefabs/World/Buildings` | Nhà, kho, bàn sản xuất và các công trình mẫu |
| `Prefabs/World/Environment` | Hàng rào, cây, đá, cỏ, thùng, hòm, biển, xe kéo |
| `Art/Actor` | Sprite nhân vật Caveman và động vật như bò |
| `Art/Backgrounds` | Tileset nhà, cây/đá/cỏ, nền đất/cỏ và đồ vật |
| `Art/UI` | Hình ảnh giao diện, font phục vụ HUD |
| `Data/Prototype` | Tile cỏ/đất và hình vuông tạm của map mẫu |
| `Data/WorldSprites` | Sprite riêng dùng cho công trình/môi trường, tham chiếu ảnh nguồn |
| `Audio` | Thư mục dành cho âm thanh |

Ảnh nguồn là PNG trong Art; sprite là phần ảnh đã cắt; prefab là đối tượng có sprite/component/collider để tái sử dụng. Có prefab trong Project không đồng nghĩa nó đã được đặt trong Main. Chưa có máy chế biến sữa hoặc bãi đáp chuyên dụng; mẫu sản xuất có thể dùng hình tạm.

Các thư mục Cows/Production/Building/Orders nằm dưới cả `Scripts/Features` và `Prefabs/Features`:

| Tính năng | Scene | Thư mục tính năng | Tài nguyên thử | Luồng tài nguyên |
| --- | --- | --- | --- | --- |
| Bò | CowTest | Cows | 20 Feed | Feed → RawMilk |
| Máy | ProductionTest | Production | 50 RawMilk | RawMilk → BottledMilk |
| Xây dựng/nâng cấp | BuildingTest | Building | 1.000 Credits | Trừ Credits |
| Đơn hàng + mua thức ăn | OrderTest | Orders | 100 BottledMilk, 100 Credits | BottledMilk → Credits; Credits → Feed |

Máy không gọi script bò, đơn hàng không gọi máy. Tất cả trao đổi tài nguyên qua kho. Người Orders phụ trách điểm mua Feed. Các số thử trong bảng chỉ được đặt sau khi chạy menu chuẩn bị thành công.

## 2. Các script chính

Đường dẫn dưới đây tính từ `Assets/_Game/Scripts/`.

| Script | Vai trò |
| --- | --- |
| `Features/Player/PlayerMovement` | WASD/mũi tên, Rigidbody2D, đổi sprite bốn hướng |
| `Inventory/ItemType` | Credits, Feed, RawMilk, BottledMilk |
| `Inventory/InventoryManager` | `GetAmount`, `Add`, `TryRemove`; phát `OnChanged` khi đổi kho thành công |
| `UI/InventoryHUD` | Nghe `OnChanged`, cập nhật số lượng; tính năng không sửa chữ HUD |
| `Interaction/Interactable` | Lớp cha có `Interact()` và `GetInteractionText()` |
| `Interaction/PlayerInteraction` | Chọn mục tiêu gần nhất, đọc E một lần, hiện/ẩn gợi ý |
| `Core/FeatureReferences` | Giữ kho runtime sau `Initialize(inventory)` |
| `Core/FeatureReferenceInitializer` | Trong Awake, Initialize các phần tử gán qua Inspector |
| `Core/UpgradeTarget` | Điểm nối `ApplyLevel(int level)`, cấp hợp lệ từ 1 |
| `Core/UpgradeTargetSample` | Chỉ lưu cấp tuyệt đối và ghi Console |
| `Core/RoamingArea` | Vùng đi chữ nhật theo trục world; lấy điểm ngẫu nhiên, giới hạn/kiểm tra vị trí theo padding collider |
| `Core/PowerSource` | HasPower, SetPower(bool), OnPowerChanged; nguồn điện đơn giản cho máy |
| `Interaction/InventoryInteractionSample` | Mẫu E/nút: kiểm tra Initialize, đọc kho và ghi Console |
| `Features/Building/BuildingConnectionSample` | Thử tạo prefab, Initialize, đặt cùng cấp hai lần; không trừ tiền |
| `World/WorldDepthSorting` | Vẽ trước/sau theo vị trí chân |
| `Inventory/InventoryTester` | Test kho cũ, giả định Credits=100 và Feed=0; không bật khi dùng tài nguyên giả |

## 3. Flow khi chạy game

```text
Scene bắt đầu
  → FeatureReferenceInitializer.Awake
  → FeatureReferences.Initialize(kho của scene)
  → Player đến gần đối tượng và nhấn E
  → PlayerInteraction gọi Interactable.Interact()
  → Tính năng kiểm tra đã Initialize
  → Tính năng dùng InventoryManager
  → Kho phát OnChanged → HUD cập nhật
```

- Prefab gốc có `FeatureReferences`; script tính năng kéo tham chiếu này qua Inspector.
- Đối tượng đặt sẵn: thêm vào mảng của `FeatureReferenceInitializer`, gán kho cùng scene.
- Đối tượng sinh khi Play: người tạo gọi `Initialize(inventory)` ngay sau `Instantiate`.
- Không xử lý gameplay cần kho trong Awake/OnEnable của tính năng; chưa Initialize thì chưa hoạt động.
- Không viết thêm code đọc E. Kế thừa `Interactable` và override `Interact()`.

API kho:

```csharp
InventoryManager inventory = featureReferences.Inventory; // Kiểm tra khác null trước.
int feed = inventory.GetAmount(ItemType.Feed);
bool removed = inventory.TryRemove(ItemType.Feed, 1);
// Chỉ xử lý tiếp nếu removed == true.
```

Nâng cấp: Building quản lý tiền/cấp và chỉ gọi `ApplyLevel` sau thanh toán thành công. CowUpgrade/MachineUpgrade kế thừa `UpgradeTarget`, tự áp dụng chỉ số từ giá trị gốc. Đặt cấp 2 hai lần vẫn là cấp 2, không cộng thêm chỉ số.

Prefab công trình thật có FeatureReferences và đúng một lớp kế thừa UpgradeTarget ở gốc. Cows đóng gói chuồng + vùng đi đứng yên + bò; Production đóng gói máy + PowerSource. Building không cần biết lớp nội bộ. RoamingArea không tự xử lý di chuyển/vật cản; PowerSource không tự quản lý tiến trình máy.

## 4. Menu cần dùng

Tất cả nằm trong **Tools → PolishCow**:

1. **Prepare Team Workspace**: tạo scene/prefab còn thiếu; không ghi đè scene cũ.
2. **Prepare Parallel Feature Base**: gọi lại Prepare Independent Feature Tests để bổ sung điểm nối, tạo prefab giả còn thiếu, thêm vùng đi/điện thử, đặt số thử tuyệt đối và lưu scene. Hai menu chuẩn bị dùng chung logic, chỉ cần chạy một.
3. **Audit Team Workspace**: kiểm tra component/tham chiếu; chưa phải kiểm tra Play Mode.
4. **Verify Independent Feature Contracts**: thử Initialize/cấp lặp lại trong scene tạm Edit Mode.
5. **Repair Current Test Scene References**: sửa bổ sung base an toàn cho scene thử đang mở; kiểm tra rồi tự lưu.
6. **Verify Parallel Feature Base**: kiểm tra hợp đồng hai prefab giả chuồng/máy, vùng đi và điện trong scene tạm Edit Mode.

Menu chuẩn bị độc lập không thay Main hoặc Apply tài nguyên về prefab kho. Với BuildingTest, Play rồi nhấn E tại mẫu để thử tạo DummyBuilding và đặt cấp 2 hai lần; nhấn lại không tạo thêm nhà.

Editor/PolishCowParallelBaseSetup.cs tạo DummyCowBarn (FeatureReferences, UpgradeTargetSample, RoamingArea con) và DummyMachine (FeatureReferences, UpgradeTargetSample, PowerSource) trong Prefabs/Features/Building khi chưa có. Không ghi đè prefab tồn tại. Người 4 chọn prefab giả trong BuildingConnectionSample để làm độc lập, sau đó thay prefab thật qua Inspector.

Setup thêm TestRoamingArea vào CowTest, TestPowerSource vào ProductionTest khi FeatureWork chưa có component tương ứng; script thành viên tự gán qua Inspector. Đã kiểm tra prefab giả/.meta, vùng đi/điện và tài nguyên thử có trên đĩa; log Editor ghi Audit 5/5. Code đã build bằng MSBuild. Lỗi sai số float tại biên RoamingArea trong Verify đã được sửa; cần chạy lại Verify và Play để xác nhận. Chi tiết trạng thái trong TEAM_GUIDE.md.

## 5. Lưu ý khi làm

- Mỗi scene có một Player, một kho, một HUD và một EventSystem; HUD và tính năng dùng cùng kho.
- Làm trong scene và thư mục tính năng của mình, không sửa scene của người khác.
- Prefab tính năng không đóng kèm Player/kho/HUD. Các tham chiếu ngoài prefab phải gán tại scene hoặc qua Initialize.
- Chưa Initialize thì không chạy gameplay cần kho; không tự tìm kho toàn scene.
- Dùng Interactable để nhận E; thay component mẫu khi thêm tương tác thật, không gắn hai component tương tác lên cùng đối tượng.
- Chỉ đổi tài nguyên qua API kho, không sửa chữ HUD hoặc gọi trực tiếp script tính năng khác.
- Menu chuẩn bị độc lập đặt lại các ô tài nguyên thử; tránh chạy khi đang kiểm tra trạng thái gameplay cần giữ.
- Play thử thường xuyên: di chuyển, va chạm, gợi ý E, nút và HUD. Build/Audit không thay cho Play thử.
- Không tự sửa InventoryManager, ItemType, PlayerInteraction, Interactable, các điểm nối Core, Main hoặc prefab base. Cần đổi API thì trao đổi người phụ trách base.
- Các mẫu hiện chỉ kiểm tra điểm nối; từng người triển khai gameplay trong phần tính năng được phân công.

Chi tiết kỹ thuật, kế hoạch hai giai đoạn và API vùng đi/điện: [TEAM_GUIDE.md](TEAM_GUIDE.md). Quy trình Git: [GIT_WORKFLOW.md](GIT_WORKFLOW.md).
