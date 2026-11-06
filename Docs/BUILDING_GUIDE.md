# Luồng 3 — hướng dẫn chạy và bàn giao

## Chạy tính năng

Mở bằng Unity **6000.6.2f1**, mở `Assets/_Game/Scenes/Tests/BuildingTest.unity`, nhấn Play.
Scene có 1000 Credits, ba biển tương tác ở (-6, -4), (0, -4), (6, -4).
Đi đến biển, nhấn **E**, rồi bấm **Xây** hoặc **Nâng cấp**. E không trừ tiền.
Đi xa quá 1.5 đơn vị, nhấn Esc hoặc Đóng để đóng panel. Người chơi vẫn di chuyển được khi panel mở.

| Slot | Giá xây | Lên cấp 2 | Lên cấp 3 | Prefab |
| --- | ---: | ---: | ---: | --- |
| Chuồng bò 1 | 150 | 150 | 300 | Cows/CowBarn |
| Chuồng bò 2 | 250 | 150 | 300 | Cows/CowBarn |
| Máy chế biến | 300 | 200 | 400 | Production/ProductionMachine |

Tất cả giá và prefab nằm trong Inspector. Cấp tối đa bằng số giá nâng cấp + 1, không vượt hợp đồng cấp 1–3.
1000 Credits đủ thử riêng từng nhánh; tổng xây và nâng cả ba đến cấp 3 là 2200 Credits.
Bài smoke test chỉ bổ sung tài nguyên trong Play, không lưu số tiền thử về asset.

## Các thành phần

- `BuildingController`: kho qua FeatureReferences, cấu hình xây, instance, cấp, khóa dùng chung và hoàn tiền.
- `BuildingUpgradeController`: bảng giá, kiểm tra cấp tiếp theo, gọi UpgradeTarget.ApplyLevel sau thanh toán.
- `BuildingSlot`: một Interactable ở biển; giữ player/panel, nhận E và mở giao diện.
- `BuildingPanel`: Canvas riêng, một panel chung cho ba slot; nghe OnChanged và Changed; nút là nơi xác nhận giao dịch.

Các prefab bàn giao nằm trong `Assets/_Game/Prefabs/Features/Building/`: `BuildingSlot1`, `BuildingSlot2`, `BuildingSlot3`, `BuildingCanvas`.
Prefab slot không chứa Player, Inventory, HUD, Camera hoặc EventSystem. Tham chiếu player/panel chỉ gán ở scene.
Prefab Canvas không có EventSystem riêng, dùng EventSystem sẵn có của scene.
`BuildingConnectionSample` và prefab dummy được giữ để đối chiếu; mẫu cũ trong BuildingTest bị tắt.

## Gán Inspector khi tích hợp

1. Đặt ba prefab slot và một BuildingCanvas. Đặt biển ngoài RoamingArea, đủ xa bò và máy để E chọn đúng mục tiêu. SpawnPoint là điểm sinh công trình; instance sinh ở root scene, không làm con slot.
2. Gán Player và BuildingPanel cho từng BuildingSlot. Các tham chiếu controller, upgrade, FeatureReferences và SpawnPoint đã nằm trong prefab.
3. Thêm FeatureReferences của từng slot vào Features của FeatureReferenceInitializer và dùng đúng kho scene.
4. Nếu có công trình đặt sẵn, gán Existing Building và Existing Level trong BuildingController; công trình phải ở ngoài cây slot. Thêm FeatureReferences của công trình đó vào cùng Features. Start nhận công trình sau các Awake của initializer, không mua lại hoặc trừ tiền.
5. Prefab công trình có FeatureReferences và đúng một UpgradeTarget ở gốc; các controller con trỏ về FeatureReferences đó. Hỗ trợ ApplyLevel tuyệt đối từ 1–3, không tự thu tiền.
6. Button OnClick không cần gán thủ công: BuildingPanel gắn và tháo listener khi chạy. Không thêm listener thanh toán thứ hai.

Các menu ở **Tools → PolishCow → Building**:

- **Set Up BuildingTest**: dựng scene thử nếu chưa có BuildingFeature; không ghi đè cấu hình đã dựng. Yêu cầu lưu scene đang mở trước khi chạy. Chỉ tác động BuildingTest và prefab Building.
- **Run Play Mode Smoke Test**: chạy giao dịch thật trong Play Mode và ghi `Logs/BuildingSmokeTest.txt`; tự thoát Play sau kiểm thử. Log PASS/FAIL là nguồn xác nhận, không suy từ việc có menu.

Batch: gọi `PolishCowBuildingSmokeTest.RunBatch` bằng `-executeMethod`, cùng `-batchmode -nographics -projectPath ... -logFile ...`. Không thêm `-quit`: bài thử tự thoát Editor sau khi trở về Edit Mode.

## Kiểm tra thủ công bổ sung

Thử WASD → E → click thực tế, độ đọc được của chữ tiếng Việt, độ nổi bật của biển, các độ phân giải màn hình và di chuyển quanh bò. Thử thoát/vào Play lần nữa: tiền trở về 1000, slot trống. Các kiểm tra hình ảnh và thao tác phím/chuột thực tế khác với smoke test gọi phương thức trong Play.

## Khi ghép Main

Chưa ghép Main: tài liệu yêu cầu nhóm chốt vị trí ba slot, công trình đặt sẵn hoặc vốn ban đầu, và phối hợp PopupLayer với Orders. Giá giữ đúng bản chốt, chưa cân bằng lại theo demo năm đơn. Không thay đổi Core, kho, HUD, Player hoặc prefab của luồng khác.

ApplyLevel trả void nên Building không thể phát hiện một target âm thầm từ chối cấp. Cần giữ hợp đồng prefab cấp 1–3; khi thay prefab, chạy lại smoke test. Nếu target ném exception, giao dịch nâng cấp hoàn tiền và thử khôi phục cấp tuyệt đối trước đó.

## Kết quả kiểm chứng

Kết quả thực chạy được ghi trong BUILDING_TASKS.md khi hoàn tất, gồm giới hạn kiểm chứng. Không coi biên dịch hoặc kiểm tra Edit Mode là Play đạt.
