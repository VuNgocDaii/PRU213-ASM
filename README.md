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

Build Settings hiện còn trỏ tới `Assets/Scenes/SampleScene.unity`. Khi xuất bản demo, người tích hợp cần chọn `Assets/_Game/Scenes/Main.unity` làm scene khởi động và kiểm thử bản build.

## Base hiện tại

Đã có Player, kho/HUD, tương tác E, các prefab/scene mẫu, API Initialize/ApplyLevel, vùng đi RoamingArea và nguồn điện PowerSource. Prefab giả chuồng/máy giúp người xây dựng làm độc lập với người bò/máy. Các tính năng chăm bò, sản xuất, mua/nâng cấp và đơn hàng cần được thành viên triển khai; bản base chưa phải demo hoàn chỉnh.

Code runtime/Editor đã build bằng MSBuild; log Unity ghi Audit 5/5 scene và `PASS Parallel Feature Base` sau khi sửa lỗi sai số float ở biên vùng đi. Đây là kiểm tra Edit Mode, chưa xác nhận Play toàn bộ vòng chơi.

Để chuẩn bị lại scene thử, lưu công việc và thoát Play rồi chạy **Tools → PolishCow → Prepare Parallel Feature Base**, sau đó **Audit Team Workspace**, **Verify Independent Feature Contracts**, **Verify Parallel Feature Base**. Menu Prepare đặt lại tài nguyên thử theo tài liệu; không chạy khi cần giữ số thử đã chỉnh. Main và prefab kho không nhận tài nguyên thử.

## Tài liệu nhóm

- [Cấu trúc project và asset](Docs/PROJECT_STRUCTURE.md)
- [API, phân công, hai giai đoạn làm việc và checklist](Docs/TEAM_GUIDE.md)
- [Quy trình Git và bàn giao](Docs/GIT_WORKFLOW.md)

Commit `Assets` cùng `.meta`, `Packages`, `ProjectSettings`, `Docs`, README và cấu hình nhóm cần dùng. `.gitignore` loại cache Unity, build, log, cấu hình cá nhân và file IDE sinh tự động; Unity sẽ tạo lại dữ liệu cần thiết khi mở project.

## Xuất bản bản base

Script `Tools/Publish-PolishCow.ps1` dành cho người phụ trách base chạy trong PowerShell có quyền ghi Git và truy cập GitHub:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Publish-PolishCow.ps1
```

Script kiểm tra nhánh main/origin, lịch sử từ xa, file lớn và mẫu thông tin bí mật trong index trước khi commit/push; không force-push hoặc đổi remote. Nếu remote có lịch sử trong khi local chưa có commit, script dừng để xử lý lịch sử an toàn. Nếu cần xác thực, đăng nhập qua cửa sổ Git Credential Manager do Git mở; không dán token vào chat hoặc URL. `ExecutionPolicy Bypass` chỉ áp dụng tiến trình chạy lệnh, không đổi thiết lập hệ thống.
