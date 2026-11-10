# Enemies — bàn giao

Người 3, nhánh `feature/enemies`, scene `Assets/_Game/Scenes/Workspaces/EnemiesWorkspace.unity`.

Sở hữu Features/Enemies và Prefabs/Modules/Enemies/EnemiesFeature.prefab. Làm EnemyController, WaveSpawner, 5 loại quái, waypoint và pooling. Quái triển khai IEnemyTarget/ISlowable. Ruồi gọi ICowDiseaseTarget.Infect qua Context.Cows; workspace có dummy bò nên không cần đợi CowHealth.

Quái chết gọi Game.ReportEnemyKilled đúng một lần; Core cộng tiền/EXP. Không thưởng khi lọt tới thành. Spawn xong và hết quái thì WaveSpawner thưởng completionCredits một lần rồi CompleteWave. Đọc Context.Waves, chỉ chọn route active.

## Kết quả

### Script (`Scripts/Features/Enemies/`)

| Script | Việc làm |
| --- | --- |
| `EnemyController` (abstract) | Phần chung mọi quái: máu theo EnemyDefinition × healthMultiplier + thanh máu, hoạt ảnh đi/trúng đòn/chết, `IEnemyTarget`, `ISlowable`, đăng ký/huỷ `Context.Enemies` theo OnConnected/OnDisconnected, chết thì rời `Context.Enemies` ngay, gọi `ReportEnemyKilled` một lần (boss: `isFinalBoss = true`), chạy hết hoạt ảnh chết rồi mới vào pool, tới thành gây `castleDamage` và không thưởng, sự kiện `OnDespawned` cho pool. |
| `GroundEnemy` | Thường/nhanh/trâu/boss: đi lần lượt `EnemyRoute.Waypoints`, hết đường thì vào `CastleTarget`. |
| `FlyEnemy` | Ruồi: bỏ waypoint, chọn bò qua `Context.Cows` (ưu tiên bò khỏe, kiểm tra `CanBeInfected`, chọn lại nếu mục tiêu mất), chạm bò gọi `Infect()` một lần rồi biến mất. Hết bò thì bay về thành; `castleDamage` của Enemy3 đang là 0 nên không trừ máu thành. |
| `WaveSpawner` | Gắn trên `EnemiesFeature.prefab`. Nghe `OnWaveStarted`, thả từng nhóm theo `spawnInterval`, chọn ngẫu nhiên route đang active, đếm quái sống (kể cả ruồi); thả xong và hết quái thì cộng `completionCredits` một lần rồi `CompleteWave`. Pool: dùng lại quái đã tắt cùng loại. Pause/Won/Lost thì ngừng. |
| `EnemyControlKeys` | Phím điều khiển nhanh, chỉ đặt trong EnemiesWorkspace (TestSupport/EnemyControls), không đặt trong Main. |

Luật slow: slow mạnh nhất thắng, thời gian lấy cái dài hơn, hết hạn về tốc độ gốc; không nhân dồn.

### Prefab (`Prefabs/Modules/Enemies/`) và dữ liệu

| Prefab | Gán vào | Hình (từ `Docs/New/Enemy`) |
| --- | --- | --- |
| `Enemy_Normal` | Enemy0 | Mushroom Run / Hit / Die |
| `Enemy_Fast` | Enemy1 | Mushroom, nhỏ hơn, ánh xanh |
| `Enemy_Tank` | Enemy2 | Mushroom, to hơn, ánh xanh lá |
| `Fly` | Enemy3 | FlyingForestEnemies Enemy3 (No-Move Fly / Hit-NoVFX / Die) |
| `Enemy_Boss` | Enemy4 | Minotaur: hàng đi bộ, hàng trúng đòn, hàng ngã; phóng to ×2 |

Mỗi prefab có thanh máu (`HealthBar`). Khung hình đã cắt sẵn trong `Art/Features/Enemies/{Mushroom,Fly,Minotaur}`. Không sửa số liệu Enemy/Wave, chỉ gán `prefab`.

### Thử trong EnemiesWorkspace

Play, rồi: **N** bắt đầu wave tiếp, **F** thả ruồi, **H** bắn mọi quái 10 damage, **J** làm chậm 50% trong 3 giây. Góc trái hiện wave, số quái còn, bò khỏe, máu thành, Credits. Có thêm một bò mẫu `SampleCow` cạnh `CowDiseaseTargetDummy`.

Kiểm tra tự động (Edit Mode, mô phỏng vòng đời OnEnable/OnDisable/Awake):

**EnemiesWorkspace – 15/15 đạt**
- Wave 1 thả đủ 5 quái, tất cả có trong `Context.Enemies`; quái bắt đầu ở waypoint 0 của route active, tới waypoint thì đi tiếp.
- Trúng 15 damage: thanh máu còn 1/2. Chết: rời danh sách quái ngay, chạy hoạt ảnh chết rồi mới biến mất.
- Tới thành: thành −5 HP, không có thưởng. Giết quái +5 Credits/con, thưởng wave +50 đúng một lần, wave kết thúc.
- Wave 2 dùng lại quái trong pool. Ruồi chạm bò gây bệnh đúng một lần.
- Boss có HP 2000 × 1.72 = 3440 ở wave 10; hạ boss cuối thì thắng.

**Main (mở để kiểm tra, không lưu) – 19/19 đạt**
- 6 route đều đủ waypoint, điểm cuối trùng CastleTarget; quái đi hết từng route tới thành. Ở cấp thành 1 chỉ CentralEast/CentralWest active (khoảng 24 giây đi bộ với quái thường).
- Chạy liên tiếp 10 wave: mỗi wave thả đúng số quái theo dữ liệu (5, 8, 14, 21, 27, 34, 41, 47, 51, 55); hết quái thì về Preparing, wave 10 hạ boss thì Won.
- Pool: 303 lượt quái trong 10 wave chỉ dùng 57 object (đỉnh 55 quái cùng lúc).
- Thành bị phá giữa wave: chuyển Lost, spawner ngừng thả, quái đứng yên.

### Chưa làm / cần thống nhất

- Kiểm tra trên là tự động; nên Play tay một lượt trong Main sau khi người tích hợp bật timer.
- Hành vi ruồi khi hết bò đang theo kế hoạch (bay về thành, damage theo dữ liệu = 0); cần nhóm chốt.
- Main: người tích hợp bật `WaveTimer.startAutomatically` khi ghép `EnemiesFeature` (đã có `WaveSpawner`).
