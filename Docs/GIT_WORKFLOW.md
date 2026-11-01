# PolishCow — workflow Git cho nhóm

## 1. Quy ước

Các lệnh dưới giả định repo có remote `origin`, nhánh chung `main`. Thay tên nếu nhóm dùng quy ước khác. Workspace hiện có metadata Plastic SCM; hướng dẫn này áp dụng khi làm trên repo Git của nhóm, không tự chuyển hệ quản lý phiên bản.

| Phần việc | Nhánh gợi ý | Scene được sửa |
| --- | --- | --- |
| Base/tích hợp | `base/integration` | Main |
| Bò | `feature/cows` | CowTest |
| Máy | `feature/production` | ProductionTest |
| Xây dựng/nâng cấp | `feature/building-upgrades` | BuildingTest |
| Đơn hàng + mua Feed | `feature/orders` | OrderTest |

Mỗi người làm trên nhánh riêng, không push trực tiếp vào main. Người phụ trách base review và merge PR. Không tự sửa scene người khác, Main, API kho/tương tác/Core hoặc prefab base.

Base đưa API/công cụ chuẩn bị lên nhánh chung một lần; nhóm lấy cùng phiên bản. Sau đó bốn người làm song song theo [TEAM_GUIDE.md](TEAM_GUIDE.md), dùng tài nguyên thử và prefab giả, không cần merge code của người khác để tiếp tục phần mình.

## 2. Bắt đầu tính năng

Clone repo nhóm nếu chưa có. Khi working tree sạch:

```bash
git switch main
git pull --ff-only origin main
git switch -c feature/cows
```

Thay nhánh Cows bằng phần việc của mình. Lệnh `switch -c` chỉ dùng lúc tạo nhánh lần đầu; nếu đã có thì dùng `git switch feature/cows`.

Mở Unity, đợi import/biên dịch, mở scene thử của mình. Code trong Scripts/Features và prefab trong Prefabs/Features tương ứng. Không chạy lại menu reset tài nguyên giữa lúc đang kiểm tra trạng thái gameplay cần giữ.

Base chạy Prepare Parallel Feature Base, Audit Team Workspace và hai menu Verify để chuẩn bị scene/asset dùng chung; commit asset/.meta do Unity tạo rồi nhóm lấy cập nhật. Không để từng người commit lại cả bốn scene do menu reset. Hai prefab giả chuồng/máy được tạo bằng Editor API, không viết YAML/.meta bằng tay. Hiện asset/.meta và tài nguyên thử đã có, log Editor ghi Audit 5/5; code đã build. Cần chạy lại Verify sau sửa lỗi biên RoamingArea và Play trước khi ghi kết quả đạt trong PR.

## 3. Mỗi lần hoàn thành một phần nhỏ

1. Play thử, kiểm tra Console, tương tác, kho và HUD.
2. Thoát Play, lưu scene/prefab. Chờ Unity tạo đủ file .meta.
3. Xem thay đổi trước khi stage:

```bash
git status
git diff
```

4. Chỉ stage file thuộc phần việc của mình cùng .meta tương ứng; có thể chọn từng file trong giao diện Git. Tránh `git add .` nếu còn thay đổi ngoài phạm vi.
5. Kiểm tra nội dung đã stage, commit và push:

```bash
git diff --cached
git commit -m "feat(cows): add feeding interaction"
git push -u origin feature/cows
```

Commit sau có thể dùng `git push`. Mỗi commit nên hoàn thành một việc rõ ràng, không gom thay đổi base hoặc asset không liên quan.

## 4. Lấy cập nhật từ nhóm

Lưu và commit công việc trước, bảo đảm working tree sạch. Đóng Unity trước khi merge thay đổi scene/prefab:

```bash
git fetch origin
git switch feature/cows
git merge origin/main
```

Mở lại Unity, đợi import rồi Play lại scene của mình. Nếu nhánh đã push, push tiếp sau khi merge và kiểm tra xong. Không force-push để ghi đè lịch sử của người khác.

## 5. PR và xử lý conflict

Mở Pull Request từ nhánh tính năng vào nhánh chung. Mô tả ngắn: tính năng làm được gì, scene/prefab nào để thử, bước thử, các ô Inspector cần gán khi ghép Main và phần chưa xong.

Prefab Cows/Production có FeatureReferences và một lớp kế thừa UpgradeTarget ở gốc; bỏ UpgradeTargetSample khi có lớp thật. Chuồng chứa vùng đi và bò, máy chứa PowerSource nội bộ. Building chỉ dùng API chung/prefab giả; lúc ghép Main, base gán prefab thật qua Inspector. Bàn giao script/prefab/scene và .meta tương ứng. Phân biệt kết quả chỉ build, Verify Edit Mode và Play; không ghi đã tạo prefab hoặc Play đạt khi chưa thực hiện.

- Conflict code: đọc cả hai thay đổi, trao đổi người liên quan và sửa; không chọn toàn bộ một phía khi chưa hiểu.
- Conflict scene/prefab/.meta: phối hợp người phụ trách base, không sửa YAML hoặc tạo GUID bằng tay. Giữ bản đã thống nhất rồi dựng lại phần cần thiết bằng Unity Editor.
- Nếu chưa biết cách giải quyết, có thể `git merge --abort` để quay lại trước lần merge, sau đó thống nhất với nhóm.
- Sau merge, kiểm tra Unity compile, Audit và Play; người phụ trách base ghép prefab vào Main và gán các tham chiếu scene/Initialize.

## 6. Lưu ý khi làm

- Commit Assets, Packages, ProjectSettings và tài liệu cần thiết; luôn đi kèm .meta của asset/script/prefab/scene/thư mục mới.
- Di chuyển/đổi tên asset trong Unity để giữ GUID; không xóa hoặc sinh lại .meta tùy tiện.
- Người phụ trách base chuẩn bị `.gitignore` cho Unity: bỏ Library, Temp, obj, Logs, UserSettings, .vs, bản build và file IDE sinh tự động. `ignore.conf` của Plastic không thay thế `.gitignore`.
- Không commit mật khẩu, token hoặc cấu hình cá nhân.
- Không Apply tài nguyên giả của scene thử vào prefab kho chung; không ghi đè Main bằng scene thử.
- Trước khi sửa file base/API dùng chung, trao đổi người phụ trách base để tránh cả nhóm lệch hợp đồng.

Cấu trúc code/asset: [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md). Hướng dẫn kỹ thuật và chia việc song song: [TEAM_GUIDE.md](TEAM_GUIDE.md).
