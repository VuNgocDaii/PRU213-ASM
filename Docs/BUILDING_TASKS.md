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
