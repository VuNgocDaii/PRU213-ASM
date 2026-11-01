# PolishCow — hướng dẫn code cho 4 tính năng

Một tài liệu chung để triển khai Cows, Production, Building và Orders trên base hiện tại. Các tên lớp mới bên dưới là gợi ý; API InventoryManager, FeatureReferences, Interactable, UpgradeTarget, RoamingArea và PowerSource đã có trong project.

Các đoạn C# là mẫu điểm nối để ghép vào lớp tương ứng; khối `text` mô tả thuật toán, không phải code có thể dán chạy ngay. Giá, thời gian và sản lượng cần được cấu hình qua Inspector, không xem số minh họa là yêu cầu cố định.

## 1. Quy tắc chung trước khi viết

- Code tại `Assets/_Game/Scripts/Features/<Tên tính năng>/`; prefab tại `Assets/_Game/Prefabs/Features/<Tên tính năng>/`.
- Thử trong CowTest, ProductionTest, BuildingTest hoặc OrderTest. Không dựng lại Player, kho hoặc HUD.
- Mỗi prefab tính năng có một FeatureReferences ở gốc. Script dùng kho phải được gán component này qua Inspector.
- Kế thừa Interactable để nhận E; tháo InventoryInteractionSample khi thay bằng tương tác thật. Không đọc E lần nữa trong script tính năng.
- Chỉ trao đổi tài nguyên qua kho; không gọi trực tiếp script của tính năng khác hoặc sửa chữ HUD.
- Awake/OnEnable chỉ chuẩn bị nội bộ; gameplay cần kho phải đợi Initialize.

### Lấy kho và dùng API

Khai báo trong lớp:

```csharp
[SerializeField] private FeatureReferences featureReferences;
```

Trong phương thức gameplay:

```csharp
if (featureReferences == null || featureReferences.Inventory == null)
    return;

InventoryManager inventory = featureReferences.Inventory;
int feed = inventory.GetAmount(ItemType.Feed);
if (!inventory.TryRemove(ItemType.Feed, 1))
    return;
// Bắt đầu hành động sau khi trừ thành công.
```

`GetAmount` trả int; `TryRemove` trả bool; `Add` trả void. Add/TryRemove từ chối số ≤ 0; Add còn từ chối tràn int. Kho tự phát OnChanged để HUD cập nhật. Không có InventoryManager.Instance hoặc SetAmount.

Với giao dịch trao tài nguyên, kiểm tra đầu ra còn chỗ trước khi trừ đầu vào:

```csharp
// Đặt trong phương thức, sau khi đã có inventory.
int reward = 10; // Ví dụ; dùng cấu hình thực khi triển khai.
if (reward <= 0 || inventory.GetAmount(ItemType.Credits) > int.MaxValue - reward)
    return;
```

Đây là kiểm tra dung lượng của int, không phải cơ chế kho có sức chứa. Add không trả trạng thái, nên không được mặc định mọi lần cộng đều thành công. Với lượt có thời gian, kiểm tra lại trước khi trao; nếu chưa cộng được, giữ sản phẩm chờ nhận.

### Nối kho trong scene và prefab runtime

- Đặt sẵn: gán kho scene vào FeatureReferenceInitializer.Inventory và thêm FeatureReferences của instance vào mảng Features.
- Sinh khi Play: kiểm tra prefab có FeatureReferences ở gốc, rồi Initialize ngay sau Instantiate bằng kho của người tạo.
- Không lưu tham chiếu kho scene trong prefab asset. Không Find hoặc dùng singleton để tìm kho.

## 2. Cows — cho ăn, chờ và thu sữa

### Tổ chức code gợi ý

| Script | Trách nhiệm |
| --- | --- |
| CowController | Trạng thái lượt nuôi, thời gian, lượng sữa chờ nhận |
| CowInteraction : Interactable | Nhận E rồi yêu cầu cho ăn/thu sữa |
| CowMovement | Chọn điểm và di chuyển trong RoamingArea |
| CowUpgrade : UpgradeTarget | Nhận cấp và cấu hình chỉ số cho lượt mới |

Prefab bàn giao: gốc chuồng chứa FeatureReferences và CowUpgrade; bên trong có vùng đi đứng yên và bò. Gán FeatureReferences ở gốc cho các script con cần kho. Vùng đi không làm con của bò đang di chuyển.

### Luồng xử lý

```text
Idle → TryRemove(Feed, feedCost) thành công → Producing
Producing → đếm hết thời gian → Ready
Ready → cộng RawMilk thành công → Idle
```

Khi E:

1. Kiểm tra đã Initialize và cấu hình chi phí/thời gian/sản lượng hợp lệ.
2. Idle: trừ Feed một lần, chốt thời gian và sản lượng của lượt rồi chuyển Producing.
3. Producing: chỉ hiện tiến độ/trạng thái, không trừ thêm Feed.
4. Ready: kiểm tra cộng được RawMilk, trao sữa rồi xóa lượng chờ nhận và về Idle.

Chỉ Update/coroutine của một nơi quản lý đồng hồ. Tránh mỗi lần E lại tạo một coroutine. Nâng cấp giữa lượt không sửa thời gian hay lượng sữa đã chốt.

### Di chuyển trong RoamingArea

- Gán RoamingArea và Rigidbody2D qua Inspector; kiểm tra vùng đủ chứa collider.
- Padding gồm nửa kích thước collider theo world và độ lệch tâm so với gốc bò.
- Lấy đích bằng GetRandomPoint(padding), giới hạn từng bước bằng ClampPosition(nextPosition, padding).
- Nếu dùng Rigidbody2D, di chuyển trong FixedUpdate với MovePosition; không đồng thời ghi Transform để di chuyển.
- Khi gần đích, chọn đích mới hoặc nghỉ. RoamingArea chỉ giới hạn biên, không tự tránh vật cản.

**Thử trong CowTest:** Feed ban đầu 20 sau setup; thiếu Feed không bắt đầu; spam E không trừ nhiều lượt; thu sữa một lần; bò không vượt biên; ApplyLevel(2) lặp lại không nhân dồn chỉ số.

## 3. Production — chế biến sữa và mất điện

### Tổ chức code gợi ý

| Script | Trách nhiệm |
| --- | --- |
| MachineController | Khóa lượt, nguyên liệu, đồng hồ, sản phẩm chờ nhận |
| MachineInteraction : Interactable | Nhận E để bắt đầu/nhận thành phẩm |
| MachineUpgrade : UpgradeTarget | Đặt chỉ số theo cấp |

Prefab máy có FeatureReferences, MachineUpgrade ở gốc và PowerSource nội bộ. Máy chỉ đọc RawMilk từ kho, không giữ tham chiếu tới bò.

### Luồng xử lý

```text
Idle → trừ RawMilk một lần → Processing
Processing + có điện → giảm remainingTime
Processing + mất điện → giữ nguyên remainingTime
Hết giờ → Ready → trao BottledMilk một lần → Idle
```

Khung đếm thời gian trong Update, với các biến do MachineController quản lý:

```csharp
if (!isProcessing || powerSource == null || !powerSource.HasPower)
    return;

remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
if (remainingTime <= 0f)
{
    isProcessing = false;
    // Chuyển Ready và giữ sản phẩm chờ nhận.
}
```

Chốt thời gian và sản lượng khi bắt đầu lượt. Mất điện không hoàn nguyên/trừ lại nguyên liệu, không tạo lượt mới. Có thể dùng context menu Power Off/Power On khi Play; nếu thử bằng checkbox Inspector, đọc HasPower mỗi frame vì checkbox không phát sự kiện.

**Thử trong ProductionTest:** RawMilk ban đầu 50 sau setup; không đủ nguyên liệu không chạy; nhấn E liên tục chỉ mở một lượt; mất/có điện tiếp tục đúng tiến độ; nhận BottledMilk một lần; nâng cấp chỉ ảnh hưởng lượt sau.

## 4. Building — mua công trình và nâng cấp

### Tổ chức code gợi ý

| Script | Trách nhiệm |
| --- | --- |
| BuildingSlot : Interactable | Giữ vị trí xây và công trình hiện có |
| BuildingController | Kiểm tra cấu hình, thanh toán, Instantiate và Initialize |
| BuildingUpgradeController | Giữ cấp hiện tại, bảng giá và UpgradeTarget |

Thử trước bằng DummyBuilding, DummyCowBarn hoặc DummyMachine trong Prefabs/Features/Building. Khi có prefab thật, thay qua Inspector. BuildingConnectionSample hiện chỉ thử kết nối, không thực hiện mua/nâng cấp.

### Xây công trình

Thực hiện trong một phương thức, theo thứ tự:

1. Kiểm tra kho, prefab, spawnPoint và giá > 0; slot còn trống; prefab có FeatureReferences và đúng UpgradeTarget ở gốc.
2. Kiểm tra vùng đặt công trình theo thiết kế, tránh xây chồng.
3. TryRemove(Credits, buildPrice); thất bại thì dừng.
4. Instantiate tại spawnPoint; Initialize ngay bằng kho hiện tại.
5. Lưu instance vào slot, khóa lần xây tiếp theo.

Điểm nối sau khi kiểm tra và thanh toán thành công:

```csharp
GameObject instance = Instantiate(
    buildingPrefab, spawnPoint.position, spawnPoint.rotation);
instance.GetComponent<FeatureReferences>().Initialize(inventory);
// Lưu instance vào slot của bạn.
```

Nếu tạo/khởi tạo thất bại sau thanh toán, xử lý hoàn tiền và dọn instance dở trong cùng thao tác; không để slot báo đã xây. Không để bước thanh toán và tạo công trình nằm ở hai callback độc lập.

### Nâng cấp

```text
Kiểm tra công trình + cấp tiếp theo + bảng giá
  → TryRemove(Credits, upgradePrice)
  → thành công: target.ApplyLevel(nextLevel)
  → cập nhật cấp đang quản lý và UI
```

UpgradeTarget.ApplyLevel trả void, không báo thành công. Vì vậy phải thống nhất trước bảng cấp hỗ trợ với prefab, kiểm tra cấp hợp lệ trước khi trả tiền. Building không sửa biến riêng của CowController/MachineController.

Lớp nâng cấp thật tính từ cấu hình cấp 1, không từ chỉ số hiện tại. Ví dụ phần override của lớp tính năng:

```csharp
public override void ApplyLevel(int level)
{
    if (level < 1 || level > 3) return;
    currentLevel = level;
    float factor = level == 1 ? 1f : level == 2 ? 0.8f : 0.6f;
    durationForNextCycle = baseDuration * factor;
}
```

Các biến trong ví dụ do lớp đó khai báo. Không dùng currentLevel++ trong ApplyLevel. Bỏ UpgradeTargetSample khi thay bằng lớp thật; không để hai lớp nhận nâng cấp ở gốc.

**Thử trong BuildingTest:** Credits ban đầu 1000 sau setup; thiếu tiền không xây/nâng cấp; slot chỉ tạo một công trình; instance dùng đúng kho; cấp tối đa không trừ tiền; đặt cấp 2 hai lần giữ nguyên chỉ số.

## 5. Orders — giao hàng, thưởng và mua Feed

### Tổ chức code gợi ý

| Script | Trách nhiệm |
| --- | --- |
| OrderData | Số BottledMilk cần, Credits thưởng, thông tin hiển thị |
| OrderController | Đơn hiện tại, kiểm tra và xử lý giao dịch, đổi đơn |
| OrderInteraction : Interactable | Cho phép giao bằng E |
| FeedShop : Interactable | Mua Feed bằng Credits |
| OrderUI | Đọc trạng thái đơn; gọi cùng phương thức giao hàng với E |

UI không tự trừ kho. E và DeliverButton phải gọi cùng một phương thức để không có hai luồng giao dịch khác nhau. Khi bỏ script mẫu, gán lại OnClick trong Inspector.

### Giao đơn

```text
Kiểm tra có đơn + chưa giao/không bận + số lượng/tiền thưởng > 0
  → kiểm tra Credits nhận thêm không tràn int
  → TryRemove(BottledMilk, requiredAmount)
  → thành công: Add(Credits, reward)
  → đánh dấu hoàn tất, cập nhật UI và chuyển đơn
```

Đặt cờ đang xử lý trước khi đổi kho; OnChanged gọi đồng bộ có thể kích hoạt callback khác. Giải phóng cờ khi kết thúc, kể cả nhánh thất bại. Nếu có drone/animation giao hàng, xác định rõ lúc trừ hàng và trao thưởng; giữ khóa đến khi hoàn tất để nút/E không trao thưởng lần hai.

### Mua Feed

```text
Kiểm tra price > 0, feedAmount > 0, Feed nhận thêm không tràn int
  → TryRemove(Credits, price)
  → thành công: Add(Feed, feedAmount)
```

Chưa đủ tiền thì không cộng Feed. Đơn hàng không cần máy trong scene; chỉ dùng BottledMilk từ kho.

**Thử trong OrderTest:** BottledMilk=100, Credits=100 sau setup; đủ hàng giao đúng một lần; thiếu hàng không nhận thưởng; thiếu tiền không mua Feed; E/nút không giao trùng; thử chuỗi 5 đơn nếu triển khai theo kế hoạch gameplay.

## 6. Hoàn thiện trước khi ghép Main

- [ ] Không lỗi Console; thiếu tham chiếu không làm mất tiền/tài nguyên.
- [ ] Đã thử đường thành công, thiếu tài nguyên và thao tác liên tiếp.
- [ ] HUD phản ánh đúng thay đổi kho qua API.
- [ ] Thoát/vào Play không để lại trạng thái thử trong asset chung.
- [ ] Prefab chỉ chứa phần tính năng, tham chiếu nội bộ đầy đủ; không chứa Player/kho/HUD/camera/EventSystem.
- [ ] Chuồng/máy nhận nâng cấp qua UpgradeTarget và kho qua FeatureReferences.
- [ ] Lưu scene/prefab trong Unity và commit đủ `.meta`.
- [ ] Ghi bước thử, tham chiếu Inspector cần gán và phần còn thiếu trong PR.

File này mô tả cách triển khai, chưa tạo các lớp gameplay gợi ý và chưa xác nhận Play của các tính năng. Chi tiết setup, gỡ lỗi và kiểm tra base: [TEAM_GUIDE.md](TEAM_GUIDE.md). Tra đường dẫn: [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md). Bàn giao: [GIT_WORKFLOW.md](GIT_WORKFLOW.md).
