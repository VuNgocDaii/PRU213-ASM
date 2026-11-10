# Economy — Bàn giao tính năng

**Người phụ trách:** Người 1  
**Nhánh:** `feature/economy`  
**Scene làm việc chính:** Assets/_Game/Scenes/Workspaces/EconomyWorkspace.unity  
**Prefab module bàn giao:** Assets/_Game/Prefabs/Modules/Economy/EconomyFeature.prefab

---

## 1. Thành phần sở hữu và đã hoàn thiện

### Bò và Bệnh bò (Features/Cows/)
- **CowHealth.cs**:
  - Triển khai interface ICowDiseaseTarget (TargetTransform, IsHealthy, CanBeInfected, Infect()). Ruồi từ module Enemies có thể nhắm vào bò và gọi Infect() độc lập.
  - Quản lý 4 cấp độ bệnh: Healthy, Mild, Moderate, Severe.
  - Hiệu ứng bệnh:
    - DurationMultiplier: Healthy = 1.0x, Mild = 1.5x, Moderate = 2.0x, Severe = 2.0x.
    - Sữa đầu ra: Mild ra RawMilk bình thường; Moderate dùng cơ chế fractional accumulator (0.5/lượt, đủ 1.0 mới thu 1 RawMilk); Severe ra 100% SpoiledMilk.
  - **Tự động leo thang bệnh**: Bệnh không được chữa trị sẽ tự leo thang sau 40 giây (chuẩn theo tài liệu thiết kế 30-45s).
  - Phương thức TryHeal(inventory): Tiêu thụ 1 Medicine để đưa bò về Healthy, reset bộ đếm leo thang và bộ tích lũy sữa.
  - Tự động đăng ký và hủy đăng ký với DefenseSceneContext (RegisterCow/UnregisterCow).
  - Hỗ trợ đầy đủ ContextMenu để test nhanh các cấp độ bệnh trực tiếp trên Inspector.
- **CowController.cs**:
  - Tự động đảm bảo bò có CowHealth ngay khi Awake().
  - Tích hợp CowHealth: Tính thời gian chu kỳ theo DurationMultiplier.
  - Phương thức AddMilkByDisease: Xử lý phân phối RawMilk / SpoiledMilk theo mức bệnh hiện tại của bò.
- **CowInteraction.cs**:
  - Kế thừa Interactable, cập nhật gợi ý động:
    - Nhấn **E**: Cho bò ăn (TryFeed) hoặc thu hoạch sữa (TryCollectMilk).
    - Nhấn **F**: Chữa bệnh chủ động (TryHealCow) tiêu tốn 1 Medicine, không ép người chơi phải chữa bệnh mới được cho ăn.
- **CowUpgrade.cs & CowMovement.cs**: Hoàn thiện nâng cấp 3 cấp và di chuyển trong RoamingArea.

### Module Economy (Features/Economy/)
- **EconomyFeature.cs**:
  - Kế thừa DefenseModule.
  - OnConnected(): Tự động tìm tất cả CowHealth trong hierarchy hoặc scene và đăng ký vào Context.RegisterCow().
  - OnDisconnected(): Hủy đăng ký toàn bộ bò qua Context.UnregisterCow().
  - Cung cấp ContextMenu: Test/Infect All Cows, Test/Heal All Cows, Test/Add Test Resources.
- **MedicineShop.cs**:
  - Điểm tương tác mua thuốc (E): Mua 1 Medicine với 20 Credits (cấu hình linh hoạt qua Inspector).
- **MilkSaleShop.cs**:
  - Điểm tương tác bán sữa trực tiếp (E): Bán BottledMilk (+25 Credits) hoặc RawMilk (+10 Credits) mà không cần phụ thuộc vào đơn hàng ngẫu nhiên.
  - Hỗ trợ đổ bỏ SpoiledMilk để dọn dẹp kho rác khi không có sữa tốt để bán.
- **Visuals/CowHealthUI.cs**:
  - Hiển thị nhãn trạng thái bệnh nổi trên đầu bò qua TextMeshPro (vàng cho Mild, cam cho Moderate, đỏ cho Severe, ẩn khi Healthy).

### Đơn hàng và Máy chế biến (Features/Orders/, Features/Production/)
- **OrderController.cs**:
  - Cập nhật mặc định limitDemoOrders = false để hỗ trợ vòng chơi liên tục không bị khóa sau 5 đơn.
- **OrderUI.cs**:
  - Cập nhật text hiển thị tiến độ và thông báo linh hoạt theo `targetOrderCount`.
- **ProductionMachine.cs & MachineUpgrade.cs**:
  - Đóng chai RawMilk thành BottledMilk, hỗ trợ mất điện giữ tiến trình và nâng cấp 3 cấp.
- **FeedShop.cs**:
  - Mua 5 Feed giá 10 Credits.

---

## 2. Hợp đồng API liên module

| Module gọi | Hợp đồng / Điểm nối | Bên triển khai (Economy) |
|---|---|---|
| **Enemies (Ruồi)** | ICowDiseaseTarget.Infect() | CowHealth.Infect() |
| **Enemies (Ruồi)** | Context.Cows | EconomyFeature đăng ký qua Context.RegisterCow |
| **Building** | UpgradeTarget.ApplyLevel(level) | CowUpgrade và MachineUpgrade |
| **Core / UI** | InventoryManager | Dùng Medicine, SpoiledMilk, RawMilk, BottledMilk, Feed, Credits |

---

## 3. Quy trình kiểm thử độc lập (Checklist)

1. **Kiểm tra biên dịch:**
   - Chạy `dotnet build Assembly-CSharp.csproj` thành công 0 lỗi, 0 warning.
2. **Kiểm tra trong `EconomyWorkspace.unity`:**
   - Mở scene `Assets/_Game/Scenes/Workspaces/EconomyWorkspace.unity`.
   - Bấm Play: `EconomyFeature` kết nối thành công với `DefenseSceneContext`, log xác nhận đã đăng ký các con bò vào Context.
   - Thử nghiệm cho bò ăn và thu hoạch RawMilk bình thường.
   - Gọi `cowHealth.Infect()` hoặc dùng Context Menu Inspector:
     - Lần 1 (Mild): Thời gian nuôi tăng lên 15s.
     - Lần 2 (Moderate): Thời gian 20s, lượt 1 không có sữa, lượt 2 được 1 RawMilk.
     - Lần 3 (Severe): Thu hoạch nhận 1 SpoiledMilk.
     - Chờ 40s không chữa: Bệnh tự động nhảy sang cấp tiếp theo.
   - Đến `MedicineShop`, nhấn E mua thuốc (-20 Credits, +1 Medicine).
   - Đến gần bò đang bệnh: Nhấn F để chữa khỏi bệnh về Healthy, hoặc nhấn E để tiếp tục cho ăn/thu hoạch.
   - Đến `MilkSaleShop`: Nhấn E bán sữa lấy Credits hoặc dọn kho SpoiledMilk.
3. **Kiểm tra phạm vi file:**
   - `Tools/Test-FeatureScope.ps1 -Role economy` hợp lệ với các thư mục thuộc quyền sở hữu của Economy.

---

## 4. Hệ thống mua bò (CowPurchaseShop)

### Thiết kế: 1 chuồng nhiều bò

Mỗi `CowBarn` có thể chứa nhiều con bò độc lập (`IndividualCow`). Sức chứa tối đa tăng theo cấp `CowUpgrade`:

| Cấp CowUpgrade | Số bò tối đa |
|---|---|
| Cấp 1 | 2 con |
| Cấp 2 | 3 con |
| Cấp 3 | 4 con |

### Luồng hoạt động

1. Người chơi đến `CowShop`, nhấn **E** để mua bò (giá: **100 Credits** / con).
2. Hệ thống tìm `CowBarn` còn chỗ trống trong scene.
3. Nếu còn chỗ và đủ tiền → trừ Credits, Instantiate `IndividualCow.prefab` vào cây con của Barn.
4. Mỗi bò mới được khởi tạo với `CowController.Initialize(featureRef, upgrade)` để chia sẻ tham chiếu.

### Files liên quan

| File | Mô tả |
|---|---|
| `Scripts/Features/Economy/CowPurchaseShop.cs` | Script quầy bán bò (Interactable, nhấn E) |
| `Prefabs/Features/Cows/IndividualCow.prefab` | Prefab con bò độc lập (tạo bởi Editor menu) |
| `Prefabs/Modules/Economy/CowShop.prefab` | Prefab quầy shop mua bò |

### Cách thêm vào scene

1. Mở Unity Editor → `Tools/PolishCow/Economy/Create IndividualCow and CowShop Prefabs`
2. Sau đó chạy `Tools/PolishCow/Economy/Add MilkShop & MedicineShop to Scene` (đã bao gồm CowShop)

### Kiểm tra

Chạy `Tools/PolishCow/Economy/Run Cow Purchase Logic Test` trong Unity Editor.
