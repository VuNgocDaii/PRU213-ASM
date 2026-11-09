# Towers — bàn giao

Người 4, nhánh `feature/towers`, scene `Assets/_Game/Scenes/Workspaces/TowersWorkspace.unity`.

Sở hữu Features/Towers và Prefabs/Modules/Towers/TowersFeature.prefab. Làm tháp, đạn/pooling, 4 loại/3 cấp, slow/AOE, shop khóa theo cấp thành, UI/audio chiến đấu trong module riêng.

Chọn qua Context.Enemies/IEnemyTarget, dùng ISlowable cho băng. Workspace có dummy mặt đất/bay, không cần đợi EnemyController. Không sửa Main/shared HUD. Xây dùng TryBuild; nâng cấp gọi controller Building qua Inspector/UnityEvent; không tự trừ tiền hoặc thưởng quái chết.

## Kết quả

- Đã làm: dữ liệu/slot/dummy và composition root rỗng.
- Prefab/API cung cấp: cập nhật khi làm.
- Bước Play thử, kết quả và phần chưa xong: cập nhật khi làm.
