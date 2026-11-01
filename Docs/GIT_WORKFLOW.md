# PolishCow — quy trình Git cho team

Mỗi người làm trên nhánh riêng, mở PR vào `main`; người phụ trách base review và merge. Xem [phân công và hợp đồng kỹ thuật](TEAM_GUIDE.md) trước khi sửa code chung.

## 1. Chọn đúng nhánh

| Phần việc | Nhánh gợi ý | Scene được sửa |
| --- | --- | --- |
| Base/tích hợp | `base/integration` | Main |
| Cows | `feature/cows` | CowTest |
| Production | `feature/production` | ProductionTest |
| Building | `feature/building-upgrades` | BuildingTest |
| Orders/mua Feed | `feature/orders` | OrderTest |

Các lệnh dưới chạy trong **Terminal của VS Code hoặc PowerShell tại thư mục gốc project** — nơi có Assets, Packages, ProjectSettings. Ví dụ dùng `feature/cows`; thay bằng nhánh của mình.

Repo dùng `origin` và `main`. Workspace còn metadata Plastic SCM; các bước này dành cho Git, không phải thao tác chuyển hệ quản lý phiên bản.

## 2. Lần đầu nhận project

Nếu chưa có repo, clone rồi mở thư mục:

```powershell
git clone https://github.com/VuNgocDaii/PRU213-ASM.git
cd PRU213-ASM
```

Nếu đã có repo, mở terminal tại thư mục hiện tại, không clone lồng vào project.

Kiểm tra `git status`; chỉ chuyển nhánh khi đã lưu/commit công việc và working tree sạch:

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/cows
```

`switch -c` chỉ dùng để tạo nhánh lần đầu. Nếu nhánh đã có trên máy, dùng `git switch feature/cows`. Nếu chỉ có trên GitHub:

```powershell
git fetch origin
git switch --track origin/feature/cows
```

Mở project bằng Unity 6000.6.2f1, đợi import/biên dịch, mở scene thử của mình. Base chuẩn bị scene và prefab dùng chung một lần; thành viên không chạy setup rồi commit lại cả bốn scene.

## 3. Lưu một phần việc và push

1. Play thử tính năng, kiểm tra Console, tương tác, kho và HUD.
2. Thoát Play, lưu scene/prefab, chờ Unity tạo đủ `.meta`.
3. Xem thay đổi:

```powershell
git status
git diff
```

4. Stage đúng script/prefab/scene thuộc phần mình cùng `.meta`. Có thể chọn từng file trong giao diện Source Control của VS Code. Ví dụ chỉ stage code và prefab Cows:

```powershell
git add -- Assets/_Game/Scripts/Features/Cows Assets/_Game/Prefabs/Features/Cows
```

Nếu tạo thư mục mới, stage thêm file `.meta` bên cạnh thư mục đó. Nếu sửa scene, stage scene cùng `.meta` tương ứng. Kiểm tra để không bỏ sót hoặc gom thay đổi của người khác; tránh `git add .` khi còn thay đổi ngoài phạm vi.

5. Xem nội dung đã stage rồi commit/push:

```powershell
git diff --cached
git commit -m "feat(cows): add feeding interaction"
git push -u origin feature/cows
```

Các lần sau dùng `git push`. Mỗi commit hoàn thành một việc rõ ràng.

## 4. Lấy cập nhật từ main

Lưu và commit công việc; bảo đảm working tree sạch. Đóng Unity trước khi merge thay đổi scene/prefab.

```powershell
git fetch origin
git switch feature/cows
git merge origin/main
```

Mở Unity, đợi import/biên dịch và Play lại scene. Khi kiểm tra xong, `git push` nếu nhánh đã có trên GitHub. Không force-push để ghi đè lịch sử người khác.

Nếu `pull --ff-only` hoặc merge báo lịch sử lệch/conflict, kiểm tra nguyên nhân trước khi tiếp tục; không dùng reset để bỏ thay đổi chưa xem xét.

## 5. Tạo PR để bàn giao

Mở Pull Request từ nhánh tính năng vào `main`. Dùng mẫu sau trong mô tả:

```text
Đã làm:
- Hành vi/tính năng đã hoàn thành.

Cách thử:
- Scene, vị trí đối tượng, thao tác và kết quả mong đợi.

Khi ghép Main:
- Prefab cần dùng, các ô Inspector/OnClick cần gán.

Đã kiểm tra:
- Build / Audit / Verify Edit Mode / Play: ghi rõ phần thực sự đã chạy.

Còn thiếu:
- Phần chưa xong hoặc cần người khác phối hợp.
```

Trước khi gửi:

- [ ] Chỉ có file thuộc phần việc và `.meta` tương ứng.
- [ ] Prefab không chứa thêm Player/kho/HUD/camera/EventSystem hoặc tham chiếu scene thử.
- [ ] Prefab chuồng/máy có FeatureReferences và một lớp kế thừa UpgradeTarget ở gốc; bỏ UpgradeTargetSample khi có lớp thật.
- [ ] Chuồng đóng gói vùng đi + bò; máy đóng gói PowerSource.
- [ ] Thực hiện checklist kỹ thuật trong TEAM_GUIDE; không ghi Play đạt khi mới chạy build/Verify.

Người phụ trách base ghép prefab thật vào Main, gán tham chiếu scene và Initialize. Building có thể tiếp tục thử độc lập với prefab giả trước khi tích hợp.

## 6. Khi có conflict

| Loại file | Cách xử lý |
| --- | --- |
| Code | Đọc cả hai thay đổi, trao đổi người liên quan rồi sửa; không chọn cả một phía khi chưa hiểu |
| Scene/prefab/.meta | Phối hợp base để giữ bản thống nhất, dựng lại phần cần thiết bằng Unity Editor; không viết YAML/GUID bằng tay |

Nếu cần hủy lần merge đang có conflict:

```powershell
git merge --abort
```

Sau khi giải quyết, xem `git status`, stage file đã sửa, hoàn tất merge commit, rồi kiểm tra Unity compile, Audit và Play trước khi push.

## 7. File nào cần commit?

| Commit | Không commit |
| --- | --- |
| Assets và `.meta` của asset/script/prefab/scene/thư mục | Library, Temp, obj, Logs, UserSettings, .vs |
| Packages, ProjectSettings | Bản build, file IDE sinh tự động |
| Docs, README, Tools, cấu hình nhóm cần dùng | Mật khẩu, token, cấu hình cá nhân |

Di chuyển/đổi tên asset trong Unity để giữ GUID. Không xóa hoặc sinh lại `.meta` tùy tiện. `.gitignore` quản lý file bỏ qua của Git; `ignore.conf` thuộc Plastic không thay thế nó.

Không Apply tài nguyên giả về prefab kho chung, không ghi đè Main bằng scene thử. Muốn đổi API/base, thống nhất với người phụ trách trước.

## 8. Script Publish-PolishCow dùng khi nào?

`Tools/Publish-PolishCow.ps1` dành cho bước publish base trên `main` của người phụ trách. Script stage nhiều thư mục, tạo commit nếu có thay đổi và push thẳng `main`; workflow tính năng hằng ngày dùng nhánh riêng và PR như trên.

Tra đường dẫn code/asset tại [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md).
