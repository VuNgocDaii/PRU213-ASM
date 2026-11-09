# PolishCow – Kế hoạch phát triển: Sản xuất sữa + Phòng thủ thành

Project: PRU213-ASM (Unity 6000.6.2f1) · Nhóm 5 người · Lộ trình 4 tuần

## 1. Tầm nhìn game

Người chơi quản lý một trang trại bò sữa nằm trong một thành. Vòng chơi chính:

**Mua Feed → Bò ra RawMilk → Máy đóng chai ra BottledMilk → Bán lấy Credits → Mua tháp và thuốc → Hạ quái lấy EXP → Thành lên cấp → Mở rộng map và mở khóa cửa hàng → Đánh boss wave 10.**

- Quái xuất hiện theo từng đợt (wave) có **đồng hồ đếm ngược** hiển thị đến đợt kế tiếp.
- Hạ quái cho **EXP**. Hạ sạch wave 1–4 đủ EXP để lên **thành cấp 2**; hạ sạch wave 5–9 đủ EXP để lên **cấp 3 (tối đa)**.
- Mỗi cấp thành mở rộng diện tích (mở thêm khu đất) và mở khóa thêm vật phẩm phòng thủ trong cửa hàng. Cấp 3 mở hoàn toàn cửa hàng để đánh **boss ở wave 10**.
- Quái tấn công thành làm giảm máu thành. Máu về 0 thì **thành nổ và thua**.
- **Ruồi** là một loại quái đặc biệt: bay tới bò, làm bò bệnh và hỏng sữa theo 3 mức độ (xem mục 3B).
- Thắng khi hạ được boss ở wave 10.

## 2. Hiện trạng project (đã đọc code)

| Đã có, dùng lại | Chưa có, cần làm |
| --- | --- |
| Player (WASD) và tương tác phím E | Quái, wave, đồng hồ đếm ngược |
| Kho `InventoryManager` (Credits, Feed, RawMilk, BottledMilk), sự kiện `OnChanged`, HUD | Tháp, đạn, nhắm mục tiêu |
| Bò: 10 giây ra 1 RawMilk, 3 cấp (`CowUpgrade`) | Máu thành, điều kiện thắng/thua |
| Máy đóng chai: 2 RawMilk → 1 chai, 5 giây, 3 cấp (`MachineUpgrade`), có điện/mất điện | Xây dựng thật (mới có `BuildingConnectionSample`) |
| `FeedShop`: 10 Credits mua 5 Feed | Mở rộng map theo cấp thành |
| `OrderController` + drone giao hàng (đang giới hạn 5 đơn) | Lưu/tải, menu chính, tạm dừng, âm thanh |
| `UpgradeTarget.ApplyLevel(level)`, `FeatureReferences`, `RoamingArea`, `PowerSource` | Bán sữa liên tục (không giới hạn đơn) |
| 4 scene thử độc lập, menu Editor (Prepare/Audit/Verify) |  |

## 3. Quyết định thiết kế đã chốt

1. **Mở rộng map theo khu đất (MapZone):** map chia thành các khu đã vẽ sẵn, bị khóa/che; thành lên cấp thì mở thêm khu (cấp 1: 1 khu, cấp 2: 3 khu, cấp 3: 5 khu). Không sinh map động.
2. **Thành lên cấp bằng EXP, không bằng Credits.** Đạt ngưỡng EXP thì tự lên cấp; cấp tối đa là 3. Credits chỉ dùng cho chăn nuôi, tháp và thuốc.
3. **Thành nổ khi hết máu:** máu thành về 0 thì phát `OnCastleDestroyed`, chạy hiệu ứng nổ rồi hiện màn thua.
4. **Quái đi theo waypoint** (đường định sẵn) ở giai đoạn đầu; chỉ cân nhắc A\* nếu còn thời gian. **Ruồi là ngoại lệ:** bay thẳng tới bò gần nhất.
5. **Giữ quy ước hiện tại:** các tính năng trao đổi tài nguyên qua `InventoryManager`; không dùng singleton hay `Find` để tìm kho; đối tượng tạo lúc Play phải gọi `Initialize(inventory)` ngay sau `Instantiate`.
6. **Dữ liệu cân bằng** (tháp, quái, wave, EXP, cấp thành, bệnh) để trong **ScriptableObject**, không hard-code, để chỉnh số không cần sửa code.
7. **Bán sữa vô hạn:** `OrderController` đổi từ demo 5 đơn sang bán liên tục; đơn hàng chỉ còn là thưởng thêm.

## 3A. Tiến trình EXP và cấp thành

Thành có 3 cấp, lên cấp tự động khi đủ EXP; ngưỡng bằng 90% EXP lý thuyết của các wave tương ứng (để vài con lọt lưới không làm kẹt tiến trình).

| Cấp thành | Điều kiện | Máu tối đa | Khu đất mở | Ô tháp / bò / máy | Cửa hàng mở khóa |
| --- | --- | --- | --- | --- | --- |
| 1 | Bắt đầu | 120 | 1 | 4 / 3 / 1 | Cung, Feed, thuốc; tháp tối đa cấp 1 |
| 2 | Đủ \~495 EXP (hạ sạch wave 1–4) | 200 | 3 | 8 / 5 / 2 | Thêm Pháo, Băng; tháp nâng tối đa cấp 2 |
| 3 (tối đa) | Đủ \~3.209 EXP (hạ sạch wave 1–9) | 320 | 5 | 12 / 8 / 4 | Mở hoàn toàn: thêm Súng bắn tỉa, tháp cấp 3, mọi vật phẩm phòng thủ |

**Điểm cần lưu ý:** thành chỉ lên cấp 3 sau khi hạ sạch wave 9, nên người chơi chỉ còn một đợt đếm ngược (đề xuất 180 giây) để mua đồ mới mở trước boss. Nếu chơi thử thấy quá gấp, hạ ngưỡng cấp 3 xuống để đạt ngay sau wave 8. Quái lọt lưới không cho EXP; nếu hạ chỉ 85% quái, thành lên cấp 2 trễ 1 wave và không kịp cấp 3 trước boss.

## 3B. Ruồi và bệnh của bò (3 mức độ)

Giáo viên gợi ý ruồi gây bệnh cho bò và làm hỏng sữa: bệnh nhẹ thì chậm hơn, bệnh nặng thì sữa hỏng, không đảm bảo chất lượng. Đề xuất 3 mức bệnh (mức Vừa do người soạn thêm, cần nhóm và giáo viên xác nhận):

| Mức bệnh | Biểu hiện | Thời gian ra sữa | Sữa dùng được |
| --- | --- | --- | --- |
| Khỏe | Bình thường | ×1 | 100% |
| Nhẹ | Ra sữa chậm hơn | ×1,5 | 100% |
| Vừa (đề xuất) | Chậm gấp đôi, một nửa sữa kém chất lượng, không đóng chai được | ×2 | 50% |
| Nặng | Sữa hỏng hoàn toàn, không đảm bảo chất lượng | ×1 | 0% |

- **Ruồi** bay tới bò gần nhất (không đánh thành). Chạm bò thì bò tăng 1 mức bệnh, ruồi biến mất. Hết bò thì ruồi bay tới thành.
- Bệnh không chữa sẽ leo thang sau khoảng 30–45 giây (giá trị đề xuất, cần chỉnh khi chơi thử).
- **Thuốc** (đề xuất 20 Credits) mua ở cửa hàng, nhấn E vào bò bệnh để chữa về Khỏe.
- Sữa hỏng là vật phẩm mới `SpoiledMilk`: máy không nhận, không bán được, người chơi phải bỏ đi.
- Tháp có thể bắn ruồi trước khi tới bò; đây là lý do phải đặt tháp cả quanh chuồng, không chỉ ở cổng thành.

## 4. Giao diện chung (Người 1 viết trong tuần 1, mọi người dùng)

| Thành phần | Mô tả |
| --- | --- |
| `IDamageable` | Có `TakeDamage(int amount)`; thành và quái cùng dùng |
| `GameState` | Enum: Preparing, Fighting, Won, Lost, Paused |
| `GameManager` | Giữ trạng thái game và phát các sự kiện bên dưới |
| `WaveTimer` | Đồng hồ đếm ngược đến wave kế tiếp |
| `CastleHealth` | Máu thành, `IDamageable`, phát `OnCastleDamaged`, `OnCastleDestroyed` (thành nổ) |
| `CastleProgress` (mới) | Giữ EXP và cấp thành (tối đa 3); phát `OnExpChanged(int)`, `OnCastleLevelUp(int level)` |
| Sự kiện wave/game | `OnWaveStarted(int wave)`, `OnWaveEnded(int wave)`, `OnEnemyKilled(int exp, int credits)`, `OnGameOver(bool won)` |
| Sự kiện bệnh bò | `OnCowInfected(severity)`, `OnCowCured` |
| Vật phẩm mới | `ItemType.Medicine` và `ItemType.SpoiledMilk` (Người 1 thêm vào kho, phối hợp Người 2) |
| Tiền | Mọi giao dịch qua `InventoryManager.TryRemove(ItemType.Credits, n)` và `Add` |

**Quy tắc phối hợp:** `Main`, Core, kho/HUD, Player, Prefab Shared, Editor chỉ do Người 1 sửa (theo `TEAM_GUIDE`). Mỗi người làm trong thư mục tính năng và scene thử riêng; muốn thay đổi file của người khác thì tạo issue hoặc nhắn người đó.

## 5. Phân công chi tiết

### Người 1 – Core & tích hợp (trưởng nhóm)

**Phụ trách:** `Scripts/Core/`, `Main.unity`, `Editor/`, Git/PR.

**Nhiệm vụ:**

1. Viết `IDamageable`, `GameState`, `GameManager`, `WaveTimer`, `CastleHealth` và các sự kiện ở mục 4 (**hạn: cuối tuần 1**, đây là việc chặn các bạn khác).
2. Viết `CastleProgress`: nhận EXP qua `OnEnemyKilled`, lên cấp 2 và 3 khi đạt ngưỡng (đọc từ ScriptableObject), phát `OnCastleLevelUp(level)`; không vượt cấp 3.
3. Logic thắng/thua: máu thành bằng 0 thì phát `OnCastleDestroyed`, chờ hiệu ứng nổ rồi thua; hạ boss wave 10 thì thắng; khóa/mở điều khiển khi kết thúc.
4. Thêm `ItemType.Medicine` và `ItemType.SpoiledMilk` vào kho và HUD kho (phối hợp Người 2).
5. Tạo scene thử `CoreTest` có đồng hồ, máu thành giả, nút cộng EXP và nút gây sát thương để thử.
6. Hệ thống lưu/tải (JSON): tài nguyên, EXP, cấp thành, công trình/tháp đã đặt, wave hiện tại, máu thành, mức bệnh của từng bò.
7. Tích hợp từng module vào `Main` mỗi tuần; quản lý nhánh `develop`, review PR, xử lý xung đột scene/prefab; cập nhật `TEAM_GUIDE` và `PROJECT_STRUCTURE`.
8. Tuần 4: build bản demo và kiểm thử Play toàn bộ vòng chơi.

**Tiêu chí hoàn thành:**

- [ ] Đồng hồ đếm ngược chạy đúng, hết giờ phát `OnWaveStarted`.
- [ ] Đủ EXP thì lên đúng cấp 2 và cấp 3, không vượt cấp 3.
- [ ] Máu thành về 0 thì thành nổ rồi hiện thua; hạ boss wave 10 thì hiện thắng.
- [ ] Lưu rồi tải lại giữ đúng tài nguyên, EXP, cấp thành, wave, máu thành, bệnh của bò.
- [ ] `Main` chạy trọn vòng chơi, không lỗi trong console.

### Người 2 – Sản xuất & kinh tế

**Phụ trách:** `Features/Cows/`, `Features/Production/`, `Features/Orders/`, bảng cân bằng kinh tế.

**Nhiệm vụ:**

1. Đổi `OrderController` sang **bán liên tục**: bỏ giới hạn `targetOrderCount` và `OnAllOrdersCompleted`; thêm điểm bán sữa giá cố định mỗi chai; đơn hàng chỉ còn là thưởng thêm (ví dụ giá ×1,5).
2. **Hệ thống bệnh `CowHealth`**: 4 trạng thái Khỏe / Nhẹ / Vừa / Nặng; hàm `Infect()` (do ruồi của Người 4 gọi) tăng 1 mức; mức Nhẹ ra sữa chậm ×1,5, mức Vừa chậm ×2 và một nửa sữa kém chất lượng, mức Nặng chỉ ra `SpoiledMilk`.
3. **Sữa hỏng:** `SpoiledMilk` không vào máy, không bán được; có cách bỏ đi; hiện biểu tượng bệnh và cảnh báo chất lượng trên bò.
4. **Thuốc:** bán ở cửa hàng (đề xuất 20 Credits); nhấn E vào bò bệnh để chữa về Khỏe; bệnh không chữa leo thang sau khoảng 30–45 giây.
5. Mua thêm bò và máy; bò và máy có 3 cấp qua `ApplyLevel` (đã có, kiểm tra lại với giá mới).
6. Chỉnh giá `FeedShop`, thời gian sản xuất, sản lượng theo bảng cân bằng (file Excel kèm theo); giới hạn kho và cảnh báo kho đầy nếu cần.
7. (Tùy chọn) Sản phẩm phụ: phô mai/bơ, giá cao hơn nhưng cần thời gian chế biến.
8. Cập nhật scene thử `CowTest`, `ProductionTest`, `OrderTest`; thêm nút gây bệnh nhẹ/vừa/nặng để thử.

**Tiêu chí hoàn thành:**

- [ ] Người chơi bán sữa vô hạn lần mà không bị chặn.
- [ ] Bệnh nhẹ ra sữa chậm ×1,5; bệnh vừa chậm ×2 và nửa sữa kém; bệnh nặng chỉ ra `SpoiledMilk`.
- [ ] Thuốc đưa bò về Khỏe; không chữa thì bệnh leo thang đúng thời gian.
- [ ] Thu nhập thực tế sát bảng cân bằng (sai số ≤ 20%).
- [ ] Nâng cấp bò/máy qua `ApplyLevel` áp dụng đúng, gọi lặp không cộng dồn; không còn log lỗi khi chưa `Initialize`.

### Người 3 – Xây dựng & mở rộng thành/map

**Phụ trách:** `Features/Building/`, Tilemap, camera, `BuildingTest`.

**Nhiệm vụ:**

1. Thay `BuildingConnectionSample` bằng logic thật, giữ hợp đồng `Initialize`/`ApplyLevel`: menu xây chuồng bò và máy đóng chai trên ô trống; kiểm tra tiền và trừ Credits qua kho.
2. Viết `MapZone` và `CastleLevelView`: nghe `OnCastleLevelUp(level)` để mở khu đất (cấp 1: 1 khu, cấp 2: 3 khu, cấp 3: 5 khu), kèm hiệu ứng mở khu. **Thành không nâng bằng Credits** mà nâng bằng EXP do Người 1 quản lý.
3. Hình ảnh thành theo từng cấp 1–3 và **hiệu ứng thành nổ** khi nhận `OnCastleDestroyed` (phối hợp Người 5 về âm thanh/VFX).
4. Hệ thống ô đặt (placement slots) theo cấp thành: tháp 4/8/12, bò 3/5/8, máy 1/2/4 (số liệu đề xuất trong sheet Thanh).
5. Điều chỉnh camera theo vùng đang mở; cập nhật `RoamingArea` của bò khi mở khu mới.
6. Vẽ/ghép Tilemap cho các khu đất từ tileset hiện có; có điểm spawn quái ở rìa khu theo từng cấp (phối hợp Người 4).

**Tiêu chí hoàn thành:**

- [ ] Lên cấp thành thì mở đúng khu đất và camera mở rộng theo.
- [ ] Máu thành về 0 thì thành nổ với hiệu ứng rồi chuyển sang thua.
- [ ] Không xây được khi thiếu tiền hoặc ô đã bị chiếm.
- [ ] Công trình đã xây còn nguyên sau khi lưu và tải lại (phối hợp Người 1).
- [ ] Gọi `ApplyLevel` lặp lại không tạo trùng khu hay công trình.

### Người 4 – Quái & wave

**Phụ trách:** `Features/Enemies/` (mới), scene thử `EnemyTest`.

**Nhiệm vụ:**

1. Khung quái `EnemyController` (máu, tốc độ, sát thương lên thành, EXP và Credits khi chết), kế thừa/nhận `IDamageable`; khi chết phát `OnEnemyKilled(exp, credits)` để Core cộng EXP và tiền.
2. Di chuyển theo waypoint từ điểm spawn đến thành; chạm thành thì gọi `TakeDamage` của `CastleHealth` rồi biến mất (quái lọt lưới không cho EXP).
3. 5 loại quái: Thường, Nhanh, Trâu bò, **Ruồi**, Boss.
4. **Ruồi:** bay thẳng tới bò gần nhất (ưu tiên bò khỏe), chạm thì gọi `CowHealth.Infect()` của Người 2 rồi biến mất; hết bò thì bay tới thành.
5. `WaveSpawner` nghe `OnWaveStarted`, đọc cấu hình wave từ ScriptableObject (loại quái, số lượng, khoảng cách giữa các con, EXP mỗi loại), phát `OnWaveEnded` khi hết quái; wave 10 có boss.
6. Object pooling cho quái; thanh máu trên đầu quái.
7. Điểm spawn tăng theo khu đã mở (phối hợp Người 3).

**Tiêu chí hoàn thành:**

- [ ] Đủ 10 wave chạy theo bảng cân bằng, wave 10 có boss.
- [ ] Quái chạm thành trừ đúng máu; không quái nào bị kẹt hoặc đi lạc.
- [ ] Hạ quái cộng đúng EXP và Credits; hạ sạch wave 1–4 đủ EXP lên cấp 2.
- [ ] Ruồi chạm bò thì bò tăng đúng 1 mức bệnh.
- [ ] 50 quái cùng lúc vẫn chạy mượt (nhờ pooling).

### Người 5 – Tháp & UI chiến đấu

**Phụ trách:** `Features/Towers/` (mới), `Scripts/UI/`, `Audio/`, scene thử `TowerTest`.

**Nhiệm vụ:**

1. Khung tháp `TowerController` (tầm bắn, tốc độ bắn, sát thương, chọn mục tiêu gần nhất/gần thành nhất), kế thừa `UpgradeTarget` để nhận `ApplyLevel`; bắn được cả ruồi (bay).
2. 4 loại tháp: Cung, Pháo (diện rộng), Băng (làm chậm), Súng bắn tỉa; mỗi loại 3 cấp.
3. **Cửa hàng phòng thủ mở khóa theo cấp thành:** nghe `OnCastleLevelUp`; cấp 1 chỉ có Cung; cấp 2 thêm Pháo và Băng; cấp 3 mở hoàn toàn (Súng bắn tỉa, tháp cấp 3, mọi vật phẩm phòng thủ). Mục chưa mở hiện khóa kèm điều kiện. Tháp chỉ nâng tới cấp tương ứng cấp thành.
4. Đạn `Projectile`, pooling đạn, hiệu ứng trúng đòn; menu đặt tháp lên ô đặt của Người 3 (trừ Credits qua kho), menu nâng cấp/bán tháp.
5. HUD chiến đấu: máu thành, đếm ngược, số wave, **thanh EXP và cấp thành**, thông báo "Thành lên cấp!", màn thắng/thua, menu chính, tạm dừng. Làm trong canvas/prefab riêng để Người 1 gắn vào `Main`.
6. Âm thanh và hiệu ứng: bắn, nổ (có tiếng nổ thành), nhận tiền, quái chết, nhạc nền.
7. Hướng dẫn nhanh cho 2–3 phút đầu.

**Tiêu chí hoàn thành:**

- [ ] 4 loại tháp bắn đúng mục tiêu (kể cả ruồi), nâng được 3 cấp theo cấp thành.
- [ ] Cửa hàng khóa/mở đúng theo cấp thành; cấp 3 mở hoàn toàn.
- [ ] Đặt tháp bị chặn khi thiếu tiền hoặc ô bị chiếm.
- [ ] HUD cập nhật đúng theo sự kiện (kể cả EXP và cấp thành), không gọi `Find` mỗi frame.
- [ ] Có âm thanh cho ít nhất 5 sự kiện chính.

## 6. Lộ trình 4 tuần

| Tuần | Mục tiêu chung | Cột mốc |
| --- | --- | --- |
| **1** | Chốt thiết kế, bảng cân bằng, giao diện chung. Mỗi người dựng bản tối thiểu trong scene thử riêng. | Người 1 giao `IDamageable`, `GameManager`, `WaveTimer`, `CastleHealth`, `CastleProgress`. Mỗi người có 1 bản chạy được: 1 quái, 1 tháp, 1 khu đất, bán sữa liên tục, bò có trạng thái bệnh. |
| **2** | Ghép vào `Main`, chạy trọn một vòng chơi. | Kiếm tiền → đặt tháp → đợi wave → hạ quái lấy EXP → quái đánh thành → thắng hoặc thua (thành nổ). |
| **3** | Thêm nội dung: nhiều loại quái/tháp, **ruồi và 3 mức bệnh, thuốc**, lên cấp 2 và 3 mở khu đất và cửa hàng, boss, lưu/tải. | Chơi được 10 wave đầy đủ; thành lên cấp 2 sau wave 4 và cấp 3 sau wave 9. |
| **4** | Cân bằng, âm thanh, hiệu ứng, menu, sửa lỗi, build, quay demo. | Bản build kiểm thử hoàn chỉnh. |

**Họp nhóm:** 15 phút đầu mỗi tuần để chốt giao diện chung; 15 phút cuối tuần để tích hợp và demo nội bộ.

## 7. Quy trình Git

- Nhánh: `main` (ổn định), `develop` (tích hợp), nhánh riêng cho từng người: `feature/core`, `feature/economy`, `feature/building`, `feature/enemy`, `feature/tower`.
- Mỗi PR chỉ chạm file của vai trò mình; PR sửa file Core/Main/Shared phải có Người 1 duyệt.
- Luôn commit `.meta` cùng asset. Không commit `Library`, `Temp`, `Logs`.
- Không hai người cùng sửa một scene: mỗi người sửa scene thử riêng; chỉ Người 1 sửa `Main`.
- Trước khi tạo PR: Play scene thử của mình, không có lỗi đỏ trong console, chạy menu Audit/Verify.

## 8. Rủi ro và cách giảm

| Rủi ro | Cách giảm |
| --- | --- |
| Xung đột scene/prefab khi merge | Mỗi người một scene thử; chỉ Người 1 sửa `Main`; commit nhỏ và thường xuyên |
| Quái đi lạc khi mở rộng map | Waypoint khai báo theo từng khu; kiểm thử sau mỗi lần thêm khu |
| Mất cân bằng: thua quá sớm hoặc quá dễ | Dùng bảng cân bằng chung; wave 1–2 rất nhẹ; thưởng tiền khi qua wave |
| Người 1 thành điểm nghẽn | Giao giao diện chung ngay tuần 1; các bạn khác dùng bản giả (stub) trong lúc chờ |
| Thiếu thời gian | Ưu tiên phần **bắt buộc**; phần tùy chọn để cuối |
| Thành chỉ lên cấp 3 sau wave 9 nên người chơi gấp trước boss | Wave 10 có đếm ngược dài hơn (đề xuất 180 giây); nếu vẫn gấp, hạ ngưỡng EXP để đạt cấp 3 sau wave 8 |
| Bệnh của bò làm người chơi bực (sữa hỏng, mất tiền) | Thuốc rẻ; bệnh leo thang chậm; có biểu tượng cảnh báo rõ trên bò; chỉnh tỷ lệ ruồi lọt khi chơi thử |
| Thêm `ItemType` mới ảnh hưởng kho và lưu/tải | Người 1 thêm `Medicine` và `SpoiledMilk` trong tuần 1 và kiểm tra lưu/tải |
| Ruồi bay thẳng nên khó bắn nếu tháp chỉ đặt gần thành | Cho phép đặt tháp quanh chuồng; tháp phải bắn được mục tiêu bay |

## 9. Nội dung bắt buộc và tùy chọn

**Bắt buộc:** `GameManager`, máu thành và thành nổ khi hết máu, đồng hồ wave, EXP và 3 cấp thành, 5 loại quái (kể cả ruồi), 3 mức bệnh của bò kèm thuốc và sữa hỏng, 4 loại tháp 3 cấp, cửa hàng mở khóa theo cấp thành, mở rộng map theo cấp thành, bán sữa vô hạn, HUD chiến đấu, thắng/thua, menu chính, tạm dừng, lưu/tải, âm thanh cơ bản.

**Tùy chọn:** phô mai/bơ, hướng dẫn nhanh, sự kiện ngẫu nhiên, bệnh lây sang bò kế bên, quái tấn công công trình nông trại, A\* thay waypoint.

## 10. Tài liệu đi kèm

File Excel **PolishCow\_BangCanBang.xlsx** gồm: thông số kinh tế gốc (đọc từ code), thu nhập theo cấp bò/máy, 4 loại tháp và cấp thành mở khóa, 5 loại quái (có EXP và ruồi), 10 wave kèm EXP lý thuyết, 3 cấp thành với ngưỡng EXP, 3 mức bệnh, và bảng đối chiếu từng wave (tiền, ô tháp, thành, EXP, thiệt hại do bệnh). Các con số là **giá trị đề xuất ban đầu**, cần chỉnh sau khi chơi thử.
