# PolishCow — hướng dẫn làm việc cho team

Đọc file này để dùng base chung. Hướng dẫn triển khai cả bốn tính năng: [CODING_GUIDE.md](CODING_GUIDE.md). Tra đường dẫn tại [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md); tạo nhánh và bàn giao theo [GIT_WORKFLOW.md](GIT_WORKFLOW.md).

## 1. Bắt đầu trong 5 bước

1. Mở project bằng Unity **6000.6.2f1**, đợi import và biên dịch xong.
2. Chuyển sang nhánh của mình, mở scene thử trong `Assets/_Game/Scenes/Tests/` phù hợp với tính năng đang làm.
3. Làm trong `FeatureWork` và thư mục tính năng của mình. Dùng Player, kho và HUD có sẵn.
4. Gán `FeatureReferences`, dùng API kho và điểm nối chung ở mục 4–7.
5. Play thử, hoàn thành checklist ở mục 9 rồi tạo PR.

**Base hiện có:** Player, tương tác E, kho/HUD, scene và prefab mẫu, Initialize, nâng cấp, vùng đi và nguồn điện. Các mẫu chủ yếu kiểm tra điểm nối; thành viên cần triển khai gameplay thật.

## 2. Quy ước dùng code chung

Mọi tính năng trao đổi tài nguyên qua `InventoryManager`. Máy không gọi script bò; đơn hàng không gọi máy; Building không sửa biến riêng của bò/máy.

**Cần trao đổi người phụ trách base trước khi sửa:** Main; kho/HUD; Player và hệ tương tác; Core; WorldDepthSorting; công cụ Editor; prefab Shared/World; Art/import settings; ProjectSettings/Packages và tài liệu base. Thành viên sửa code, prefab và scene tính năng của mình. Building được thay `BuildingConnectionSample` bằng logic thật nhưng giữ hợp đồng Initialize/ApplyLevel và prefab giả để đối chiếu.

## 3. Chuẩn bị scene thử — người phụ trách base

Base chuẩn bị và commit scene/prefab cùng `.meta` một lần, rồi team lấy cùng phiên bản. Thành viên không cần chạy lại setup hằng ngày vì menu có thể sửa cả bốn scene.

1. Thoát Play, lưu scene/prefab đang sửa và đợi biên dịch. Menu dừng nếu scene thử hoặc Prefab Stage còn thay đổi chưa lưu.
2. Nếu thiếu scene, chạy **Tools → PolishCow → Prepare Team Workspace**. Menu tạo phần còn thiếu, không ghi đè scene/prefab cũ.
3. Chạy **Prepare Parallel Feature Base**. Menu dùng chung logic với **Prepare Independent Feature Tests**; chỉ cần chạy một trong hai.
4. Chạy **Audit Team Workspace**, **Verify Independent Feature Contracts**, **Verify Parallel Feature Base**.
5. Play riêng từng scene và kiểm tra theo mục 9. Audit/Verify chỉ kiểm tra Edit Mode.

| Scene | Ô tài nguyên được đặt lại | Đối tượng hỗ trợ |
| --- | --- | --- |
| CowTest | Feed = 20 | CowSample, TestRoamingArea |
| ProductionTest | RawMilk = 50 | ProductionSample, TestPowerSource |
| BuildingTest | Credits = 1000 | BuildingSample, DummyBuilding/DummyCowBarn/DummyMachine |
| OrderTest | BottledMilk = 100, Credits = 100 | OrderSample, panel/nút/E |

**Prepare đặt số tuyệt đối, không cộng thêm.** Các ô khác giữ nguyên; Main và prefab kho không nhận số thử. Không chạy lại khi cần giữ trạng thái gameplay đang thử, không Apply dữ liệu này về prefab chung.

Scene thử là bản độc lập, không tự cập nhật theo Main. Mỗi scene chỉ có một Player, một kho, một HUD và một EventSystem; OrderCanvas là UI đơn hàng riêng. Không kéo thêm TeamServices/Player. Setup không tự gán vùng đi/nguồn điện vào script thành viên; cần gán qua Inspector.

## 4. Lấy kho qua FeatureReferences

Mỗi prefab tính năng có đúng một `FeatureReferences` ở gốc. Script tính năng khai báo và gán component này trong Prefab Mode:

```csharp
[SerializeField] private FeatureReferences featureReferences;
```

| Đối tượng | Cách gán kho |
| --- | --- |
| Đặt sẵn trong scene | Gán kho scene vào Inventory của `FeatureReferenceInitializer`; thêm FeatureReferences của instance vào mảng Features |
| Tạo khi Play | Người tạo gọi `Initialize(inventory)` ngay sau `Instantiate` |

Ví dụ tạo prefab, sau khi kiểm tra `buildingPrefab`, `spawnPoint`, `inventory` khác null và prefab có FeatureReferences ở gốc:

```csharp
GameObject instance = Instantiate(
    buildingPrefab, spawnPoint.position, spawnPoint.rotation);
instance.GetComponent<FeatureReferences>().Initialize(inventory);
```

Instance runtime không cần thêm vào mảng Initializer. Không lưu kho của scene vào prefab, không dùng Find hoặc singleton để tìm kho.

`Initialize(null)` bị từ chối và giữ tham chiếu hợp lệ cũ. Gọi lại cùng kho không sinh tài nguyên/gameplay. `FeatureReferences.Inventory` null trong Edit Mode là bình thường.

**Awake/OnEnable của tính năng không chạy gameplay cần kho.** Awake chỉ lấy component nội bộ; Interact/Update/phương thức sản xuất phải kiểm tra đã Initialize. Không dựa vào thứ tự Awake.

### API kho và HUD

```csharp
if (featureReferences == null || featureReferences.Inventory == null)
    return;

InventoryManager inventory = featureReferences.Inventory;
int feed = inventory.GetAmount(ItemType.Feed);
if (inventory.TryRemove(ItemType.Feed, 1))
{
    inventory.Add(ItemType.RawMilk, 1); // Ví dụ trao đổi tài nguyên.
}
```

- `ItemType`: Credits, Feed, RawMilk, BottledMilk.
- `GetAmount` trả `int`; `Add` trả `void`, từ chối số ≤ 0 hoặc tràn int.
- `TryRemove` trả `bool`, từ chối số ≤ 0 hoặc không đủ. Chỉ trao kết quả khi trả về true.
- Không có `InventoryManager.Instance` hoặc `SetAmount`.
- Kho phát `OnChanged` sau thay đổi thành công; HUD tự cập nhật và đọc số đầu trong OnEnable. Không sửa chữ/icon HUD hoặc tự phát sự kiện.
- Đổi số trong Inspector lúc Play không gọi API nên không phát `OnChanged`.

## 5. Tương tác E và nút UI

Kế thừa `Interactable`, override `Interact()` và kiểm tra kho như mục 4. PlayerInteraction đã chọn mục tiêu, đọc E và hiện gợi ý; tính năng không viết thêm hệ thống đọc E.

Khi thay tương tác mẫu, tháo `InventoryInteractionSample`; không gắn hai Interactable trên cùng đối tượng. Code mới dùng FeatureReferences để lấy kho.

Để E hoạt động:

- Gán Interaction Text; bật Collider2D trên đối tượng hoặc con của Interactable.
- LayerMask của Player phải chứa layer collider. Với trigger, bật Physics2D Queries Hit Triggers.
- Gốc Interactable phải ở gần vị trí Player tiếp cận được; bán kính hiện tại là 1.5. InteractionPoint chỉ đánh dấu hiệu ứng/biểu tượng.
- Gán `PlayerInteraction.Interaction Hint` vào `Canvas/HUD/InteractionHint` của scene.

Khi thay script đơn hàng, gán lại OnClick của DeliverButton vào phương thức giao hàng mới.

## 6. Xây dựng và nâng cấp

**Building quản lý giá, thanh toán và cấp hiện tại.** Chỉ gọi `UpgradeTarget.ApplyLevel(level)` sau khi `TryRemove(Credits, price)` thành công.

Cows/Production kế thừa `UpgradeTarget`, tự tính chỉ số từ cấu hình gốc. ApplyLevel đặt cấp tuyệt đối: gọi cấp 2 hai lần vẫn cho chỉ số cấp 2. Không dùng `currentLevel++` hoặc nhân tiếp chỉ số hiện tại. Từ chối cấp < 1 và cấp ngoài bảng hỗ trợ.

Prefab thật có FeatureReferences và đúng một lớp kế thừa UpgradeTarget ở gốc; bỏ UpgradeTargetSample khi có lớp thật. Building giữ tham chiếu kiểu UpgradeTarget qua Inspector, không cần biết lớp bò/máy.

### Thử bằng prefab giả

- `DummyBuilding`: dùng hình FarmHouse.
- `DummyCowBarn`: có vùng đi con.
- `DummyMachine`: có nguồn điện.
- Cả ba nằm trong `Prefabs/Features/Building`, có FeatureReferences/UpgradeTargetSample ở gốc. Setup chỉ tạo khi thiếu.

BuildingSample được bổ sung `BuildingConnectionSample` và `BuildSpawnPoint` nếu thiếu; điểm đã chỉnh giữ nguyên. Nếu đã thay tương tác mẫu, gán điểm nối thủ công theo thông báo của menu.

Trong BuildingTest, nhấn E tại mẫu hoặc chạy context menu **Test Instantiate and Upgrade (Play Mode)**:

1. Tạo công trình một lần và Initialize bằng kho BuildingTest.
2. Gọi ApplyLevel(2) hai lần; CurrentLevel vẫn là 2, Credits không đổi.
3. Nhấn E lại dùng công trình đang có, không tạo thêm.

Đây là phép thử điểm nối, chưa phải mua/nâng cấp thật. Đổi Building Prefab trong Inspector rồi thoát/vào Play để thử instance mới.

### Bảng nâng cấp đề xuất

| Cấp | Thời gian bò/máy | Sản lượng | Feed/nguyên liệu |
| --- | --- | --- | --- |
| 1 | ×1 | ×1 | Giữ nguyên |
| 2 | ×0.8 | ×1 | Giữ nguyên |
| 3 | ×0.6 | ×2 | Giữ nguyên |

Base không tự áp dụng bảng này. Cows/Production triển khai chỉ số; Building cấu hình giá và số cấp tương ứng. Lượt đang chạy giữ thời gian/nguyên liệu/sản lượng đã chốt; cấp mới áp dụng từ lượt tiếp theo. Sữa đã sẵn không đổi lượng khi nâng cấp.

## 7. Vùng đi của bò và điện của máy

### Cows: RoamingArea

API: `WorldBounds`, `GetRandomPoint(padding)`, `ClampPosition(position, padding)`, `Contains(position, padding)`.

Vùng chữ nhật theo trục world, dùng đơn vị world, không theo xoay/scale Transform. Gán vùng qua Inspector; component không tự di chuyển hoặc tránh vật cản.

```csharp
[SerializeField] private RoamingArea roamingArea;
// Ví dụ: thay theo nửa kích thước collider world và độ lệch tâm collider.
Vector2 padding = new Vector2(0.35f, 0.4f);
// Dùng trong phương thức di chuyển, sau khi kiểm tra roamingArea khác null:
// Vector2 destination = roamingArea.GetRandomPoint(padding);
// Vector2 safePosition = roamingArea.ClampPosition(nextPosition, padding);
```

Giới hạn cả bước di chuyển, không chỉ điểm đến. Nếu collider lớn hơn vùng, Contains luôn false; Clamp/GetRandomPoint trả tâm trên trục thiếu chỗ. Kiểm tra cấu hình trước khi chạy.

Bàn giao prefab **chuồng + vùng đi đứng yên + bò**; không đặt vùng làm con của bò. TestRoamingArea chỉ là vùng thử, cần chỉnh theo đất trống.

### Production: PowerSource

API: `HasPower`, `SetPower(bool)`, `OnPowerChanged`. HasPower mặc định true; SetPower chỉ phát sự kiện khi giá trị đổi.

```csharp
// Trong phương thức xử lý sản xuất:
if (powerSource == null || !powerSource.HasPower) return;
// Chỉ giảm remainingTime khi có điện.
```

Mất điện giữ tiến trình và nguyên liệu; có điện lại tiếp tục, không trừ nguyên liệu lần nữa. Có thể thử bằng context menu **Power Off/Power On** trong Play. Checkbox Inspector không phát sự kiện; đọc HasPower mỗi frame để hỗ trợ cách thử này. Nếu nghe sự kiện, đăng ký OnEnable và bỏ đăng ký OnDisable.

Bàn giao prefab máy có PowerSource nội bộ; không cần chờ hệ thống điện khác.

## 8. Bàn giao và ghép Main

Mỗi phần bàn giao gồm script, prefab, scene, `.meta` và bước thử. Prefab tính năng không chứa thêm Player, kho, HUD, camera hoặc EventSystem.

Người phụ trách base ghép prefab vào Main và kiểm tra:

- [ ] FeatureReferences ở gốc; script trỏ đúng component nội bộ.
- [ ] Chuồng/máy có đúng một lớp kế thừa UpgradeTarget; vùng đi/điện nằm trong prefab.
- [ ] Instance đặt sẵn nằm trong mảng Features; Initializer dùng kho Main.
- [ ] Instance runtime được Initialize ngay sau Instantiate bằng cùng kho.
- [ ] Interaction Hint, Transform đích, UpgradeTarget, layer collider và OnClick trỏ đúng instance Main.
- [ ] Không còn tham chiếu sang scene thử, không thêm base thứ hai.
- [ ] Main giữ tài nguyên đầu game; không Apply All từ scene thử hoặc ghi đè Main bằng scene thử.

## 9. Checklist trước khi tạo PR

- [ ] Unity biên dịch xong, không có lỗi Console trong các bước thử.
- [ ] Play riêng scene của mình: di chuyển/đổi hướng/va chạm đúng; gần hiện gợi ý, xa ẩn; giữ E không gọi liên tục.
- [ ] HUD và tính năng cùng kho; thiếu Initialize thì không chạy gameplay cần kho.
- [ ] Luồng thành công thay đổi kho/HUD đúng; thiếu tiền/nguyên liệu không trao kết quả.
- [ ] Building: tạo đúng kho, cấp 2 lặp lại vẫn cấp 2; mẫu thử không trừ tiền hoặc tạo nhà lần hai.
- [ ] Cows: không vượt biên. Production: mất/có điện giữ đúng tiến trình và nguyên liệu.
- [ ] Prefab không phụ thuộc scene thử; đã lưu và commit đủ `.meta`.
- [ ] PR kèm bước thử, kết quả và phần chưa xong.

Base kiểm tra thêm khi thay công cụ setup: chạy Prepare hai lần, số thử không cộng dồn, component/mảng/BuildSpawnPoint không tăng, vị trí thủ công giữ nguyên; đóng/mở scene để kiểm tra đã lưu.

## 10. Gỡ lỗi nhanh

| Hiện tượng | Kiểm tra |
| --- | --- |
| Kho null khi Play | Inventory và mảng Features của Initializer; runtime đã Initialize chưa |
| Không hiện E | Collider, LayerMask, khoảng cách gốc, trigger queries, Interaction Hint |
| Nút không chạy | OnClick còn trỏ component mẫu đã tháo không |
| HUD không đổi | Tính năng có dùng API và cùng kho với HUD không |
| Assert của InventoryTester sai | Tester cũ giả định Credits=100, Feed=0; không bật trong scene dùng tài nguyên thử |
| Scene thiếu/trùng base | Sửa Inspector, nhờ base kiểm tra; không tự xóa/chọn bản đầu tiên |
