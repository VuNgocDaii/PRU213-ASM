# PolishCow — bản đồ project

Dùng file này để tìm scene, script và asset. Bắt đầu triển khai theo [TEAM_GUIDE.md](TEAM_GUIDE.md); commit và bàn giao theo [GIT_WORKFLOW.md](GIT_WORKFLOW.md).

## 1. Mở đâu để làm việc?

Project dùng Unity **6000.6.2f1**. Trong Unity Hub, mở thư mục chứa `Assets`, `Packages`, `ProjectSettings`.

| Bạn làm phần | Scene trong `Assets/_Game/Scenes/` | Thư mục tính năng |
| --- | --- | --- |
| Tích hợp | `Main.unity` | Code base và Shared |
| Bò | `Tests/CowTest.unity` | `Cows` |
| Máy chế biến | `Tests/ProductionTest.unity` | `Production` |
| Xây dựng/nâng cấp | `Tests/BuildingTest.unity` | `Building` |
| Đơn hàng/mua Feed | `Tests/OrderTest.unity` | `Orders` |

Mỗi tính năng có thư mục riêng dưới `Scripts/Features/` và `Prefabs/Features/`. Thành viên làm trong scene của mình, dưới FeatureWork; base phụ trách Main.

## 2. Thư mục chính

Các đường dẫn dưới đây tính từ `Assets/_Game/`.

| Đường dẫn | Nội dung |
| --- | --- |
| `Scripts/` | Logic runtime: Core, kho, Player, UI, tương tác, tính năng |
| `Editor/` | Menu setup, Audit, Verify; không phải gameplay |
| `Scenes/Main.unity` | Scene game chung |
| `Scenes/Tests/` | Bốn scene thử độc lập |
| `Prefabs/Shared/` | Player, TeamServices, InteractableSample |
| `Prefabs/Features/` | Prefab mẫu và prefab tính năng |
| `Prefabs/World/Buildings/` | Nhà, kho, bàn sản xuất, công trình mẫu |
| `Prefabs/World/Environment/` | Hàng rào, cây, đá, cỏ, đồ vật |
| `Art/Actor/` | Sprite Caveman và động vật |
| `Art/Backgrounds/` | Tileset nhà, môi trường, nền và đồ vật |
| `Art/UI/` | Hình ảnh và font giao diện |
| `Data/Prototype/` | Tile cỏ/đất, hình vuông tạm của map mẫu |
| `Data/WorldSprites/` | Sprite công trình/môi trường tham chiếu ảnh nguồn |
| `Audio/` | Âm thanh |

Ngoài `Assets`: `Packages/` lưu cấu hình package; `ProjectSettings/` lưu cấu hình Unity; `Docs/` lưu tài liệu nhóm; `Tools/` lưu script hỗ trợ.

Ảnh PNG là ảnh nguồn; sprite là phần ảnh đã cắt; prefab đóng gói đối tượng với component/collider. Có prefab trong Project chưa có nghĩa đã đặt nó vào Main. Máy chế biến sữa và bãi đáp chuyên dụng chưa có sẵn; có thể dùng hình tạm khi triển khai.

## 3. Script cần biết

Đường dẫn tính từ `Assets/_Game/Scripts/`, tên file có đuôi `.cs`.

| Script | Dùng khi |
| --- | --- |
| `Core/FeatureReferences` | Lấy kho sau Initialize |
| `Core/FeatureReferenceInitializer` | Gán kho cho các instance đặt sẵn qua Inspector trong Awake |
| `Inventory/ItemType` | Chọn Credits, Feed, RawMilk, BottledMilk |
| `Inventory/InventoryManager` | Đọc/thêm/trừ tài nguyên qua GetAmount/Add/TryRemove |
| `UI/InventoryHUD` | Hiển thị kho, nghe OnChanged |
| `Interaction/Interactable` | Kế thừa để triển khai Interact/GetInteractionText |
| `Interaction/PlayerInteraction` | Chọn mục tiêu, nhận E, hiện/ẩn gợi ý |
| `Interaction/InventoryInteractionSample` | Tham khảo tương tác mẫu; chưa có gameplay thật |
| `Core/UpgradeTarget` | Kế thừa để nhận ApplyLevel |
| `Core/UpgradeTargetSample` | Thử cấp tuyệt đối, chỉ lưu cấp và ghi log |
| `Core/RoamingArea` | Lấy điểm/giới hạn vị trí bò theo vùng và padding collider |
| `Core/PowerSource` | Đọc trạng thái điện của máy |
| `Features/Building/BuildingConnectionSample` | Thử Instantiate → Initialize → ApplyLevel lặp lại |
| `Features/Player/PlayerMovement` | Di chuyển WASD/mũi tên, Rigidbody2D, sprite bốn hướng |
| `World/WorldDepthSorting` | Thứ tự vẽ theo vị trí chân |
| `Inventory/InventoryTester` | Test kho cũ, giả định Credits=100 và Feed=0 |

Không bật InventoryTester trong scene đã đặt tài nguyên thử. Quyền sửa API/base và ví dụ dùng các script nằm trong TEAM_GUIDE.

## 4. Các điểm nối khi chạy

```text
Scene bắt đầu
  → FeatureReferenceInitializer.Awake
  → FeatureReferences.Initialize(kho của scene)
  → Player đến gần và nhấn E
  → PlayerInteraction gọi Interactable.Interact()
  → Tính năng kiểm tra kho rồi dùng InventoryManager
  → Kho đổi thành công → OnChanged → HUD cập nhật
```

Với đối tượng sinh khi Play, người tạo gọi Initialize ngay sau Instantiate. Prefab tính năng dùng tham chiếu nội bộ; kho scene được truyền từ bên ngoài.

Chuỗi tài nguyên mục tiêu:

```text
Credits → Feed → RawMilk → BottledMilk → Credits
   └→ xây dựng / nâng cấp
```

Cows bàn giao chuồng + vùng đi đứng yên + bò; Production bàn giao máy + nguồn điện. Building dùng UpgradeTarget và prefab được gán qua Inspector. Các tính năng trao đổi tài nguyên qua kho.

## 5. Menu hỗ trợ

Tất cả nằm trong **Tools → PolishCow**. Base chạy setup chung và commit kết quả; thành viên lấy cập nhật.

| Menu | Công dụng |
| --- | --- |
| Prepare Team Workspace | Tạo scene/prefab còn thiếu, không ghi đè bản cũ |
| Prepare Parallel Feature Base | Bổ sung điểm nối, prefab giả, vùng đi/điện, đặt lại tài nguyên thử và lưu scene |
| Prepare Independent Feature Tests | Dùng chung logic chuẩn bị với menu trên; không cần chạy cả hai |
| Audit Team Workspace | Kiểm tra component/tham chiếu trong Edit Mode |
| Verify Independent Feature Contracts | Thử Initialize và nâng cấp lặp lại trong scene tạm |
| Verify Parallel Feature Base | Thử prefab giả chuồng/máy, vùng đi và điện trong Edit Mode |
| Repair Current Test Scene References | Bổ sung/sửa tham chiếu base của scene thử đang mở rồi lưu |

**Prepare đặt lại một số ô tài nguyên thử.** Xem số cụ thể, thứ tự chạy và checklist tại [TEAM_GUIDE.md](TEAM_GUIDE.md). Audit/Verify không thay cho Play thử.
