# PolishCow

Project Unity 2D mô phỏng trang trại bò sữa, phục vụ bài tập PRU213. Vòng chơi mục tiêu: mua Feed → chăm bò và thu RawMilk → sản xuất BottledMilk → giao đơn nhận Credits → xây dựng và nâng cấp.

Repository: [VuNgocDaii/PRU213-ASM](https://github.com/VuNgocDaii/PRU213-ASM). Giữ nguyên tên thư mục project và repository.

## Môi trường

- Unity **6000.6.2f1**, revision `770e33f6875c`, theo `ProjectSettings/ProjectVersion.txt`.
- Unity Hub và Unity Editor đúng phiên bản; cài module build tương ứng nếu cần xuất bản.
- Unity khôi phục package từ `Packages/manifest.json` và `Packages/packages-lock.json` khi mở project. Cần kết nối mạng cho lần import đầu.
- Git để làm việc nhóm. Hiện chưa cần Git LFS: không có file dự kiến commit đạt 50 MiB; không đưa toàn bộ ảnh hoặc `.meta` vào LFS.

## Mở và chạy

1. Clone repository hoặc lấy cập nhật từ nhánh `main`.
2. Trong Unity Hub, chọn **Add project from disk**, trỏ tới thư mục chứa `Assets`, `Packages`, `ProjectSettings`.
3. Mở bằng Unity 6000.6.2f1, chờ import và biên dịch xong.
4. Mở `Assets/_Game/Scenes/Main.unity`, nhấn **Play**. Di chuyển bằng WASD/phím mũi tên, đến gần đối tượng rồi nhấn **E**.
5. Thử từng phần trong `Assets/_Game/Scenes/Tests/`: `CowTest`, `ProductionTest`, `BuildingTest`, `OrderTest`. Mỗi lần chỉ Play một scene thử.

Build Settings đã đặt `Assets/_Game/Scenes/Main.unity` làm scene khởi động. Cần kiểm thử bản build trước khi bàn giao demo.

## Base hiện tại

Đã có Player, kho/HUD, tương tác E, API Initialize/ApplyLevel, vùng đi RoamingArea và nguồn điện PowerSource. Main tích hợp bò ở khu chuồng bên trái, máy đóng chai ở Production, FeedShop tại (4, -1), và trạm giao hàng/drone tại (7, -4). Các tính năng cùng dùng kho của GameSystems qua FeatureReferenceInitializer. Main bắt đầu với 100 Credits và 10 Feed; RawMilk/BottledMilk bằng 0. Mua/xây dựng công trình và giao diện nâng cấp chưa hoàn chỉnh.

Thử trong Main: đến FeedShop, nhấn E mua 5 Feed với 10 Credits. Đến gần bò, E dùng 1 Feed; chờ 10 giây rồi E thu 1 RawMilk. Lặp lại để có 2 RawMilk, đến máy ở Production, E bắt đầu đóng chai; chờ 5 giây rồi E nhận 1 BottledMilk. Khi đủ số chai trong bảng đơn hàng, đến trạm nhấn E hoặc bấm nút giao hàng; drone hoàn tất chuyến giao và chuyển đơn tiếp theo. Hoàn thành 5 đơn để kết thúc demo.

Chạy kiểm tra tự động trong Unity: mở Main ở Edit Mode, chọn **Tools → PolishCow → Run Main Gameplay Smoke Test**. Bài thử chạy gameplay tăng tốc, kiểm tra kho/HUD, mất điện, giao hàng và màn hoàn thành; tài nguyên bổ sung để thử 5 đơn chỉ tồn tại trong Play Mode. Kết quả ghi ở `Logs/MainGameplaySmokeTest.txt`, ảnh đầu/cuối ở `output/main-gameplay-start.png` và `output/main-gameplay-complete.png`. Menu **Integrate Orders Into Main** dùng để gắn lại prefab và tham chiếu khi cần; hãy lưu scene trước khi chạy menu tích hợp.

Code runtime/Editor đã build bằng MSBuild; log Unity ghi Audit 5/5 scene và `PASS Parallel Feature Base` sau khi sửa lỗi sai số float ở biên vùng đi. Đây là kiểm tra Edit Mode, chưa xác nhận Play toàn bộ vòng chơi.

Để chuẩn bị lại scene thử, lưu công việc và thoát Play rồi chạy **Tools → PolishCow → Prepare Parallel Feature Base**, sau đó **Audit Team Workspace**, **Verify Independent Feature Contracts**, **Verify Parallel Feature Base**. Menu Prepare đặt lại tài nguyên thử theo tài liệu; không chạy khi cần giữ số thử đã chỉnh. Main và prefab kho không nhận tài nguyên thử.

## Tài liệu nhóm

- [Cấu trúc project và asset](Docs/PROJECT_STRUCTURE.md)
- [API, phân công, hai giai đoạn làm việc và checklist](Docs/TEAM_GUIDE.md)
- [Quy trình Git và bàn giao](Docs/GIT_WORKFLOW.md)

Commit `Assets` cùng `.meta`, `Packages`, `ProjectSettings`, `Docs`, README và cấu hình nhóm cần dùng. `.gitignore` loại cache Unity, build, log, cấu hình cá nhân và file IDE sinh tự động; Unity sẽ tạo lại dữ liệu cần thiết khi mở project.
