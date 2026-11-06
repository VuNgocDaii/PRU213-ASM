# Luồng 3 — Xây dựng và nâng cấp

Nguồn yêu cầu: PolishCow_Luong3_Xay_dung_va_nang_cap.docx (05/11/2026).
Nhánh: `feature/building-upgrades`. Phạm vi: Building và BuildingTest.

## Task và commit

1. Giao dịch xây: kiểm tra cấu hình/kho, chống xây đè, khóa trước TryRemove, Initialize ngay, hoàn tiền và dọn instance lỗi.
2. Nâng cấp và công trình đặt sẵn: giá Inspector, cấp tuyệt đối, tối đa theo bảng giá, dùng chung khóa giao dịch.
3. Tương tác và UI: E mở panel, nút giao dịch, cập nhật tiền/cấp, đóng khi đi xa.
4. BuildingTest và prefab: ba slot 150/250/300; bảng nâng cấp 150/300 và 200/400; 1000 Credits; tham chiếu được lưu bằng Unity Editor.
5. Kiểm thử và bàn giao: kiểm tra các nhánh lỗi, spam, cấp tối đa, prebuilt, API thực và ghi rõ mức kiểm chứng.

## Quyết định phạm vi

- Không sửa Core, Inventory, Player, HUD, Main hoặc prefab dùng chung.
- Panel cho phép di chuyển và tự đóng khi ra ngoài phạm vi tương tác.
- Main còn cần chốt vị trí và công trình ban đầu. Giá giữ đúng tài liệu.
- CowUpgrade và MachineUpgrade hiện có cấp 1–3; Building chỉ gọi UpgradeTarget.ApplyLevel.
- Commit cục bộ từng task; push/PR không nằm trong yêu cầu hiện tại.

## Hoàn tất và kiểm chứng

- [x] Task 1 — `124b2a2`: giao dịch xây và hoàn tiền.
- [x] Task 2 — `575d2da`: nâng cấp và công trình đặt sẵn.
- [x] Task 3 — `529adc8`: tương tác E và panel.
- [x] Task 4 — `04db0a4`: ba slot, prefab thật, Canvas và BuildingTest được lưu bằng Unity Editor.
- [x] Task 5 — commit chứa tài liệu này: smoke test Play Mode và hướng dẫn bàn giao.

Đã chạy Unity **6000.6.2f1**, biên dịch runtime/Editor thành công. `PolishCowBuildingSmokeTest.RunBatch` kết thúc với mã 0; báo cáo `Logs/BuildingSmokeTest.txt` có **52 dòng PASS** (bao gồm các kiểm tra từng object và dòng tổng kết). Không có FAIL, lỗi C# hoặc exception trong lần chạy đạt. Các cảnh báo do ca thử cố ý cung cấp cấu hình sai là dự kiến.

| Ca trong tài liệu | Kiểm chứng thực tế |
| --- | --- |
| 1. Thiếu Credits | Play: xây/nâng cấp bị từ chối, tiền và cấp không đổi, nút xám |
| 2. Spam | Play: gọi E/nút lặp cùng frame, gọi lại từ OnChanged không tạo giao dịch thứ hai |
| 3. Xây đè | Play: slot có công trình không mua lại |
| 4. Cấp 1→2→3 | Play: cả chuồng và máy trừ đúng giá |
| 5. Cấp tối đa | Play: không trừ tiền, panel ghi Cấp tối đa và khóa nút |
| 6. ApplyLevel lặp | Play: UpgradeTargetSample nhận cấp 2 hai lần, giữ cấp 2 |
| 7. Kho/HUD | Play: instance dùng chung kho, HUD hiển thị đúng sau giao dịch |
| 8. Lỗi Initialize | Play: làm mất hợp đồng prefab trong callback sau thanh toán; kiểm tra hoàn tiền, slot trống, mở khóa và xây lại được |
| 9. Công trình đặt sẵn | Play: đăng ký instance đã Initialize ở cấp 2 không mất tiền, nâng lên cấp 3 được |
| 10. Điểm tương tác | Play: PlayerInteraction chọn biển slot khi đã sinh bò; chưa thử phím E thủ công lúc đi lại |
| 11. Thoát/vào Play | Play: đầu lần chạy 1000 Credits; thoát Play trở lại 1000. Chưa kiểm tra thủ công nhiều lần |
| 12. Console/prefab | Unity compile và Play không lỗi trong lần chạy đạt; instance không nằm dưới slot, từng object tối đa một Interactable; có đủ .meta |

Chưa xuất bản build standalone hoặc kiểm tra hình ảnh/phím chuột thủ công. Chưa ghép Main và chưa push. Hướng dẫn thao tác, Inspector, menu và việc cần chốt trước tích hợp nằm trong `BUILDING_GUIDE.md`.

Lần đầu import tạo ra thay đổi cache glyph trong font dùng chung; các thay đổi này đã được loại khỏi commit. Unity cũng reload assembly sau khi tạo prefab trong lần thử đầu; runner batch đã được sửa để đợi import ổn định trước khi vào Play. Chỉ lần chạy hoàn tất được tính là PASS.
