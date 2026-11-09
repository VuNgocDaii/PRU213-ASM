# Enemies — bàn giao

Người 3, nhánh `feature/enemies`, scene `Assets/_Game/Scenes/Workspaces/EnemiesWorkspace.unity`.

Sở hữu Features/Enemies và Prefabs/Modules/Enemies/EnemiesFeature.prefab. Làm EnemyController, WaveSpawner, 5 loại quái, waypoint và pooling. Quái triển khai IEnemyTarget/ISlowable. Ruồi gọi ICowDiseaseTarget.Infect qua Context.Cows; workspace có dummy bò nên không cần đợi CowHealth.

Quái chết gọi Game.ReportEnemyKilled đúng một lần; Core cộng tiền/EXP. Không thưởng khi lọt tới thành. Spawn xong và hết quái thì WaveSpawner thưởng completionCredits một lần rồi CompleteWave. Đọc Context.Waves, chỉ chọn route active.

## Kết quả

- Đã làm: dữ liệu/route/dummy và composition root rỗng.
- Prefab/API cung cấp: cập nhật khi làm.
- Bước Play thử, kết quả và phần chưa xong: cập nhật khi làm.
