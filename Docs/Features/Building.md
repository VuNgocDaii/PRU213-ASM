# Building — bàn giao

Người 2, nhánh `feature/building`, scene `Assets/_Game/Scenes/Workspaces/BuildingWorkspace.unity`.

Sở hữu Features/Building và Prefabs/Modules/Building/BuildingFeature.prefab. DefenseBuildingSlot.cs của map mới nằm trong Core và do người tích hợp giữ. Code Building của Tuan (BuildingController, BuildingUpgradeController, BuildingPanel, BuildingSlot) được giữ nguyên; phát triển tiếp và nối nó với Context.Slots/Progress, không chạy hai giao dịch cho một lần xây.

Cung cấp controller nâng cấp dùng qua Inspector/UnityEvent để UI Towers không tham chiếu lớp cụ thể. Base giữ MapZone/camera/Core. Xác nhận cấp hỗ trợ trước khi thanh toán rồi gọi ApplyLevel.

## Kết quả

- Đã làm: slot/giao dịch base và composition root rỗng.
- Prefab/API cung cấp: cập nhật khi làm.
- Bước Play thử, kết quả và phần chưa xong: cập nhật khi làm.
