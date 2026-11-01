# PolishCow 

Hướng dẫn kỹ thuật base dùng chung, API và kế hoạch hai giai đoạn. Cấu trúc: [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md). Quy trình nhánh: [GIT_WORKFLOW.md](GIT_WORKFLOW.md).

## 1. Chuẩn bị

Đã đọc code, Main, bốn scene thử và prefab hiện có. Các scene đều có một kho và một InventoryHUD trỏ kho của scene; Player Caveman, collider, di chuyển, hướng nhìn, tương tác E và các mẫu đã có. Tiếp tục dùng CowSample, ProductionSample, BuildingSample, OrderSample, không dựng lại Main/map/Player.

Code hiện có FeatureReferences/Initialize, UpgradeTarget/ApplyLevel, RoamingArea và PowerSource. Đã kiểm tra trên đĩa có DummyCowBarn/DummyMachine cùng .meta, vùng đi/điện trong scene thử và tài nguyên thử đúng bảng bên dưới. Log Editor ghi Audit đạt 5/5 scene; kết quả này không thay cho Verify/Play.

1. Thoát Play, đợi biên dịch; tự lưu các scene thử/prefab đang sửa dở.
2. Chạy **Tools → PolishCow → Prepare Parallel Feature Base**. Menu gọi lại Prepare Independent Feature Tests: bổ sung điểm nối, tạo DummyBuilding/DummyCowBarn/DummyMachine còn thiếu, thêm vùng đi/nguồn điện thử, đặt tài nguyên và lưu scene. Hai menu chuẩn bị dùng chung logic, chỉ cần chạy một. Nếu scene thử hoặc Prefab Stage đang dirty, menu dừng để tránh lưu chồng phần đang sửa.
3. Chạy **Audit Team Workspace**, **Verify Independent Feature Contracts** và **Verify Parallel Feature Base**; sau đó Play từng scene. Verify chỉ kiểm tra Edit Mode.
4. Nếu thiếu scene, chạy **Prepare Team Workspace** trước. Scene/prefab cũ không bị ghi đè; lỗi Audit vẫn cần sửa, không coi việc bỏ qua là đã hoàn thiện.

Không đổi Main, map, Player, HUD, tài nguyên prefab TeamServices hoặc logic thành viên. Không Apply dữ liệu scene thử ngược prefab chung. Các menu chạy thủ công qua Unity Editor API, không auto-run khi reload, ExecuteAlways, singleton, reflection hoặc package mới; không ghi YAML/.meta bằng tay.

## 2. Phân công

Đường dẫn tính từ Assets/_Game/.

| Người | Nhánh Git gợi ý | Scene | Code / Prefab | Đầu vào → đầu ra |
| --- | --- | --- | --- | --- |
| Base/tích hợp | base/integration | Scenes/Main.unity | Scripts/Core, code base, Prefabs/Shared | Điểm nối chung, ghép prefab và kiểm thử Main |
| Cows | feature/cows | Scenes/Tests/CowTest.unity | Scripts/Features/Cows, Prefabs/Features/Cows | Feed → RawMilk |
| Production | feature/production | Scenes/Tests/ProductionTest.unity | Scripts/Features/Production, Prefabs/Features/Production | RawMilk → BottledMilk |
| Building | feature/building-upgrades | Scenes/Tests/BuildingTest.unity | Scripts/Features/Building, Prefabs/Features/Building | Trừ Credits, tạo/nâng cấp công trình |
| Orders | feature/orders | Scenes/Tests/OrderTest.unity | Scripts/Features/Orders, Prefabs/Features/Orders | BottledMilk → Credits; mua thức ăn: Credits → Feed |

Chỉ trao đổi tài nguyên qua InventoryManager. Máy không tham chiếu script bò, đơn hàng không tham chiếu máy, Building không sửa biến riêng của Cow/Machine. Người Orders triển khai điểm mua thức ăn theo quy tắc Credits → Feed.

| Scene | Dữ liệu menu đặt tuyệt đối | Cách thử |
| --- | --- | --- |
| CowTest | Feed=20 | CowSample; TestRoamingArea để người 2 gán vào script di chuyển |
| ProductionTest | RawMilk=50 | ProductionSample và TestPowerSource; không cần bò |
| BuildingTest | Credits=1000 | BuildingSample và DummyBuilding; có thể chọn DummyCowBarn/DummyMachine trong Inspector |
| OrderTest | BottledMilk=100, Credits=100 | OrderSample, panel/nút/E, đọc BottledMilk; không cần máy |

Chỉ các ô trong bảng được reset; các ô khác giữ nguyên. Chạy lại vẫn 20/50/1000/100, không cộng thêm. Đây là reset dữ liệu thử có chủ đích: số thành viên đã sửa ở các ô đó sẽ về số trong bảng; vị trí, script, callback và dữ liệu tính năng khác giữ nguyên. Main và prefab kho không nhận các số này.

Không sửa trực tiếp chữ HUD. Khi Play, InventoryHUD.OnEnable đọc kho và hiển thị số đầu. Scene thử là bản chụp độc lập, không tự cập nhật theo Main. Chỉ Play một scene mỗi lần, làm việc dưới FeatureWork, không kéo thêm kho/HUD/Player thứ hai.

## 3. Kho và HUD

API thực tế giữ nguyên:

```csharp
int amount = inventory.GetAmount(ItemType.Feed);
inventory.Add(ItemType.RawMilk, 1);
bool removed = inventory.TryRemove(ItemType.Feed, 1);
```

Đây là ví dụ API, không tự chạy trong mẫu. GetAmount trả int; Add trả void, từ chối số <=0/tràn int; TryRemove trả bool, từ chối số <=0/không đủ. Chỉ trao kết quả khi TryRemove thành công. ItemType có Credits, Feed, RawMilk, BottledMilk. Không có InventoryManager.Instance hoặc SetAmount.

Kho phát OnChanged sau Add/TryRemove thành công. InventoryHUD đã đăng ký/bỏ đăng ký sự kiện; tính năng không sửa Text/icon và không tự phát OnChanged. Sửa số trong Inspector lúc Play không gọi API nên không tự phát sự kiện.

TeamServices.prefab chứa GameSystems/InventoryManager và Canvas/InventoryHUD, giữ tham chiếu HUD → kho nội bộ. Scene thử đã có cả hai, không kéo thêm prefab này. Player.Interaction Hint là tham chiếu ngoài prefab Player, cần gán chữ Canvas/HUD/InteractionHint tại scene.

## 4. FeatureReferences và Initialize

API gán kho hiện có:

- Core/FeatureReferences.cs: `public InventoryManager Inventory { get; private set; }`, `public void Initialize(InventoryManager inventory)`.
- Core/FeatureReferenceInitializer.cs: SerializeField InventoryManager và mảng FeatureReferences; Awake Initialize mỗi phần tử khác null. Không Find hoặc singleton.

Initialize từ chối null, giữ tham chiếu hợp lệ cũ nếu có. Gọi lại cùng kho chỉ gán tham chiếu, không sinh tài nguyên/gameplay.

Prefab gốc của mỗi tính năng có đúng một FeatureReferences. Script tính năng khai báo `[SerializeField] private FeatureReferences featureReferences;` và kéo component trên gốc vào ô này trong Prefab Mode. Không lưu tham chiếu kho scene vào prefab.

Với prefab đặt sẵn: trong scene, kéo **TeamServices/GameSystems** vào Inventory của FeatureReferenceInitializer, thêm FeatureReferences của các instance vào mảng Features. Menu làm việc này cho mẫu; chỉ thêm phần tử còn thiếu, không xóa danh sách cũ.

Không xử lý gameplay cần kho trong Awake/OnEnable của tính năng. Awake chỉ lấy component nội bộ; Interact/Update/phương thức sản xuất kiểm tra đã Initialize. Không dựa vào thứ tự Awake. Tương tác E trong Update chạy sau các Awake của scene.

Với prefab tạo khi Play, người tạo gọi Initialize ngay sau Instantiate:

```csharp
GameObject instance = Instantiate(buildingPrefab, spawnPoint.position, spawnPoint.rotation);
FeatureReferences references = instance.GetComponent<FeatureReferences>();
if (references == null)
{
    Debug.LogError("Prefab thiếu FeatureReferences ở gốc.");
    return;
}
references.Initialize(inventory);
```

Kiểm tra buildingPrefab, spawnPoint, inventory khác null trước đoạn trên. Người tạo lấy inventory từ FeatureReferences của mình hoặc tham chiếu Inspector của scene. Không cần thêm instance runtime vào mảng Initializer vì người tạo đã Initialize trực tiếp.

InventoryInteractionSample được chỉnh tiếp, từ chối chạy nếu chưa Initialize. Ô inventory cũ được giữ để không mất dữ liệu scene/prefab và tương thích Audit; mẫu không đọc kho gameplay từ ô cũ. Code mới dùng FeatureReferences.

Menu bổ sung điểm nối cho bốn prefab mẫu và Shared/InteractableSample, giữ hình/collider/callback. Đây là chỉnh trong Prefab Mode, không Apply tài nguyên scene vào asset. Player.prefab, TeamServices.prefab và các prefab World giữ nguyên.

## 5. Tương tác chung

Ví dụ code của người làm Cows, lưu tại Scripts/Features/Cows/CowInteraction.cs:

```csharp
using UnityEngine;
public class CowInteraction : Interactable
{
    [SerializeField] private FeatureReferences featureReferences;
    public override void Interact()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning("Bò chưa Initialize.", this);
            return;
        }
        InventoryManager inventory = featureReferences.Inventory;
        int feed = inventory.GetAmount(ItemType.Feed);
        Debug.Log("Thức ăn để thử: " + feed, this);
    }
}
```

Tháo InventoryInteractionSample khi thay bằng tương tác thật; không gắn hai Interactable trên cùng đối tượng. Sửa Interaction Text trong Inspector. PlayerInteraction đã đọc E, chọn mục tiêu và hiện gợi ý; tính năng không viết thêm hệ thống đọc E.

Collider2D cần bật, nằm cùng đối tượng hoặc con của Interactable. LayerMask Player chứa layer collider; trigger cần Physics2D Queries Hit Triggers bật. Gốc Interactable phải gần chỗ Player tiếp cận được, bán kính hiện 1.5. InteractionPoint chỉ đánh dấu hiệu ứng/biểu tượng.

DeliverButton gọi InventoryInteractionSample.Interact trong cùng OrderSample, cũng chặn khi chưa Initialize. Nếu thay component đơn hàng, gán lại OnClick. Hiện chỉ ghi Console, không trừ sữa.

## 6. Nâng cấp chung

UpgradeTarget là MonoBehaviour với `public virtual void ApplyLevel(int level)`, cấp hợp lệ từ 1. CowUpgrade/MachineUpgrade sau này kế thừa nó; override cũng phải từ chối level <1.

Building quản lý giá, thanh toán và cấp hiện tại. Chỉ gọi ApplyLevel sau TryRemove(Credits, price) thành công. Giữ tham chiếu kiểu UpgradeTarget qua Inspector, không cần biết script bò/máy cụ thể và không sửa biến nội bộ của chúng.

Cows/Production tự áp dụng chỉ số theo cấp: tính từ giá trị gốc, không cộng/nhân tiếp giá trị hiện tại. ApplyLevel là đặt cấp tuyệt đối; gọi cấp 2 hai lần vẫn là chỉ số cấp 2, không dùng currentLevel++.

UpgradeTargetSample chỉ lưu CurrentLevel và Debug.Log, mặc định 1; đặt 2 hai lần vẫn 2, 0/âm bị từ chối, không có logic bò/máy.

Sau khi chạy menu chuẩn bị, BuildingSample cũ được thêm BuildingConnectionSample và BuildSpawnPoint nếu thiếu; điểm đã chỉnh không bị đặt lại. Nếu thành viên đã thay tương tác mẫu, menu bỏ qua phần nối mẫu và báo để gán thủ công. DummyBuilding dùng hình FarmHouse; DummyCowBarn có vùng đi con, DummyMachine có nguồn điện. Các prefab giả ở Prefabs/Features/Building có FeatureReferences/UpgradeTargetSample ở gốc; không ghi đè asset đã tồn tại.

BuildingConnectionSample dùng UpgradeTarget chung, không phụ thuộc lớp bò/máy. Đổi Building Prefab trong Inspector để thử prefab giả hoặc prefab thật. Khi đổi prefab thử, thoát rồi vào Play lại để tạo instance mới. Prefab thật có đúng một lớp kế thừa UpgradeTarget ở gốc; bỏ UpgradeTargetSample khi đã có lớp thật.

Trong Play, E tại BuildingSample: kiểm tra Initialize → Instantiate DummyBuilding một lần → Initialize kho BuildingTest ngay sau tạo → ApplyLevel(testLevel=2) hai lần → assert vẫn cấp 2 và Credits giữ nguyên. E lại dùng công trình đang có, không tạo thêm. Có thể chạy context menu **Test Instantiate and Upgrade (Play Mode)** trên BuildingConnectionSample.

Đây là phép thử điểm nối, không gọi Add/TryRemove hoặc thanh toán tự động. Nâng cấp thật vẫn phải trả tiền thành công trước ApplyLevel.

Quy ước nâng cấp đề xuất, tính từ cấu hình cấp 1:

| Cấp | Thời gian bò/máy | Sản lượng | Feed/nguyên liệu |
| --- | --- | --- | --- |
| 1 | x1 | x1 | Giữ nguyên |
| 2 | x0.8 | x1 | Giữ nguyên |
| 3 | x0.6 | x2 | Giữ nguyên |

Base không tự áp dụng hệ số vào bò/máy; người 2/3 triển khai và người 4 cấu hình giá/số cấp tương ứng. Lượt đang chạy giữ thời gian/nguyên liệu/sản lượng đã chốt, cấp mới áp dụng từ lượt tiếp theo. Sữa sẵn không đổi lượng khi nâng cấp. Override tự từ chối cấp ngoài bảng hỗ trợ.

### Vùng đi và nguồn điện

RoamingArea có WorldBounds, GetRandomPoint(padding), ClampPosition(position, padding), Contains(position, padding). Vùng chữ nhật theo trục world, dùng đơn vị world, không theo xoay/scale Transform. Padding tính cả kích thước và độ lệch collider bò. Gán vùng qua Inspector, kiểm tra vùng đủ chứa collider và giới hạn cả bước đi; component không tự di chuyển hoặc tránh vật cản.

Người 2 bàn giao prefab chuồng chứa bò và vùng đi đứng yên; không đặt vùng làm con của bò. Vùng trong CowTest là dữ liệu thử, cần chỉnh theo đất trống. Người 4 không cần truyền biên khi tạo prefab chuồng hoàn chỉnh.

Setup chỉ thêm TestRoamingArea/TestPowerSource khi FeatureWork chưa có component tương ứng, không tự gán vào script thành viên. Tâm vùng thử đặt tại CowSample nếu có. Ví dụ sử dụng trong script di chuyển:

```csharp
[SerializeField] private RoamingArea roamingArea;
Vector2 padding = new Vector2(0.35f, 0.4f); // Thay theo collider thực.
Vector2 destination = roamingArea.GetRandomPoint(padding);
Vector2 safePosition = roamingArea.ClampPosition(nextPosition, padding);
bool inside = roamingArea.Contains(safePosition, padding);
```

Padding gồm nửa kích thước collider world và độ lệch tâm so với gốc bò. Collider lớn hơn vùng thì Contains luôn false; Clamp/GetRandomPoint trả tâm trên trục thiếu chỗ. Kiểm tra cấu hình trước khi di chuyển.

PowerSource có HasPower, SetPower(bool), OnPowerChanged. Người 3 gán nguồn điện qua Inspector, chỉ giảm thời gian xử lý khi HasPower=true; mất điện giữ tiến trình/nguyên liệu. Context menu Power Off/Power On dùng thử trong Play. Checkbox Inspector không phát sự kiện; đọc HasPower mỗi frame để hỗ trợ cách thử này. Máy thật đóng gói nguồn điện nội bộ, không cần chờ hệ thống điện khác.

HasPower mặc định true. SetPower chỉ phát sự kiện khi đổi giá trị; đăng ký OnEnable/bỏ đăng ký OnDisable nếu nghe sự kiện. Không trừ lại nguyên liệu khi có điện trở lại. Ví dụ trong xử lý sản xuất:

```csharp
[SerializeField] private PowerSource powerSource;
// Thiếu điện thì không giảm remainingTime.
if (powerSource == null || !powerSource.HasPower) return;
```

## 7. Bàn giao và tích hợp Main

Bàn giao script/prefab/scene trong thư mục riêng kèm bước thử; prefab tính năng không đóng thêm Player, kho, HUD, camera hoặc EventSystem. Commit cả .meta script/prefab/scene/asset/thư mục; không tạo .meta bằng tay, không commit Library/Temp/obj/Logs/.vs.

Người phụ trách base kéo prefab vào Main và kiểm tra:

- Gốc có FeatureReferences, các script trỏ đúng component nội bộ.
- Công trình bò/máy có một lớp kế thừa UpgradeTarget ở gốc. Chuồng chứa vùng đi và bò, máy chứa PowerSource; không tham chiếu sang scene thử.
- Instance đặt sẵn có trong mảng Features; Inventory của Initializer trỏ kho Main. Menu độc lập không tự sửa Main; người phụ trách base gán bộ nối khi ghép.
- Instance runtime được Initialize ngay sau Instantiate bằng cùng kho.
- PlayerInteraction.Interaction Hint trỏ chữ HUD Main; không thêm Player/TeamServices thứ hai.
- Transform đích xây/giao hàng, UpgradeTarget và layer collider gán đúng instance Main.
- OnClick gán lại nếu đổi script mẫu; tham chiếu nội bộ prefab giữ được, tham chiếu ngoài prefab cần gán trong scene.

Không Apply All từ scene thử, không chuyển tài nguyên giả về prefab base/Main, không ghi đè Main bằng scene thử. Main giữ tài nguyên đầu game.

## 8. Checklist kiểm tra

1. Chạy menu chuẩn bị. Mỗi scene có một Player/kho/Canvas HUD/InventoryHUD/EventSystem. OrderCanvas là UI đơn hàng, không phải HUD thứ hai.
2. HUD và Initializer cùng kho; mảng Features chứa các root đặt sẵn; ô FeatureReferences của mẫu đúng root.
3. Play riêng từng scene: di chuyển/đổi hướng/va chạm; gần mẫu hiện gợi ý, E ghi log, xa thì ẩn; giữ E không gọi liên tục.
4. HUD/Console: CowTest Feed=20; ProductionTest RawMilk=50; BuildingTest Credits=1000; OrderTest BottledMilk=100, Credits=100. Mẫu không tiêu thụ tài nguyên.
5. BuildingTest: E tạo công trình, log kho đúng/cấp 2 hai lần vẫn 2/không trừ tiền; không có Assert lỗi. E lại không thêm nhà.
6. Thoát Play, chạy setup lại: số thử không cộng dồn, số component/phần tử mảng/BuildSpawnPoint không tăng, vị trí thủ công giữ nguyên. Đóng/mở scene để kiểm tra đã lưu.
7. Verify Independent Feature Contracts dùng scene tạm không lưu: instantiate prefab giả qua Editor API, Initialize, đặt cấp 2 hai lần, kiểm tra Credits giữ nguyên, đóng scene tạm. Đây là Verify Edit Mode, chưa xác nhận Awake/Update/Play.
8. Kiểm tra OnChanged bằng Add/TryRemove trong tính năng. InventoryTester cũ giả định Credits=100, Feed=0, nên **không bật trong scene đã gieo tài nguyên giả**; Assert cũ sẽ sai. Không sửa kho để né giả định test này.
9. Chạy Verify Parallel Feature Base: hợp đồng hai prefab giả, cấp lặp lại, biên vùng đi và sự kiện điện. Sau khi có gameplay thật, Play kiểm tra bò không vượt biên, máy dừng/tiếp tục khi mất/có điện.

FeatureReferences.Inventory null trong Edit Mode là bình thường, chỉ có giá trị sau Initialize. Audit Edit Mode kiểm tra danh sách nối; nó kiểm tra ô InventoryManager serialized trong MonoBehaviour, không suy đoán tham chiếu gán động hoặc ScriptableObject. Thiếu/trùng base phải sửa Inspector, không tự xóa/chọn bản đầu tiên.

## 9. Quyền quản lý base

Thành viên không tự sửa: Main; InventoryManager, ItemType, InventoryTester, InventoryHUD; PlayerMovement, PlayerInteraction, Interactable, InventoryInteractionSample; UpgradeTarget, UpgradeTargetSample, FeatureReferences, FeatureReferenceInitializer, WorldDepthSorting; công cụ Editor; prefab Shared/World; Art/import settings, ProjectSettings/Packages và tài liệu base. Cần đổi API thì trao đổi người phụ trách base trước.

Mỗi người sửa code/prefab/scene tính năng của mình. Building có thể thay BuildingConnectionSample bằng logic thật, giữ Initialize/ApplyLevel, không sửa Core (gồm cả RoamingArea/PowerSource) hoặc công cụ Editor mới. Giữ prefab giả làm mẫu đối chiếu, tạo prefab thật riêng.

Base chuẩn bị scene/prefab/.meta dùng chung một lần rồi bàn giao cho nhóm, tránh từng người sửa cả bốn scene do menu reset. Hai giai đoạn đều làm song song:

| Người | Giai đoạn 1 | Giai đoạn 2 |
| --- | --- | --- |
| 2 | Di chuyển với RoamingArea, Feed → RawMilk bằng kho thử | UI, nâng cấp, chống vượt biên/kẹt, prefab chuồng |
| 3 | RawMilk → BottledMilk, khóa lượt, đọc PowerSource | UI, nâng cấp, mất/có điện, prefab máy |
| 4 | Slot, giá, thanh toán, xây bằng prefab giả | UI nâng cấp, chống xây chồng, thử UpgradeTargetSample |
| 5 | Đơn, giao hàng, thưởng Credits, mua Feed | Drone, UI, đủ 5 đơn, kiểm tra thiếu hàng/tiền |

Base nhận prefab để ghép Main; người 4 không cần đợi prefab thật để lập trình/kiểm thử riêng, chỉ thay prefab qua Inspector khi tích hợp. Kiểm thử vòng chơi chung cần đủ sản phẩm.

## 10. Giới hạn kiểm chứng lần cập nhật này

Đã build runtime và Editor bằng MSBuild thành công, có cảnh báo tham chiếu framework/assembly và API cũ của dự án. Prefab giả/.meta, vùng đi/điện và tài nguyên thử đã có trên đĩa; log Editor ghi Audit 5/5. Verify Parallel Feature Base trước đó lỗi kiểm tra vị trí sát biên sau khi dịch chuyển vùng do sai số float trong Contains. Bản sửa so sánh lại hai đầu biên vẫn chưa qua Verify trong Unity. Đã sửa Contains kiểm tra trực tiếp tọa độ float trả về từ ClampPosition và mở rộng Verify cả bốn góc cùng điểm ngoài vùng; build lại thành công, cần chạy lại Verify để xác nhận trong Unity. Chưa xác nhận Play toàn bộ base hoặc gameplay tính năng.

Build/Audit cũ 5/5 không thay cho kiểm tra Initialize runtime, setup hai lần không cộng dồn và ApplyLevel lặp lại. Các mẫu chưa triển khai chăm bò, sản xuất, mua thức ăn, mua công trình hoặc giao hàng.
