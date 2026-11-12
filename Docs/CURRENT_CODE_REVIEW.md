# Kiểm tra code hiện tại

- Chuồng cấp I / II / III: 2 / 3 / 4 con. `CowUpgrade.Capacity` là nguồn giới hạn theo cấp; shop dùng giá trị này khi không có override.
- Đã phát hiện shop duyệt cả `CowUpgrade` trên `IndividualCow`, coi bò là chuồng mới và cho mua vượt sức chứa. Shop chỉ chọn đối tượng có `RoamingArea` của chuồng.
- HUD Main dùng Screen Space Overlay. Nền InventoryCard, CastleHUD và thẻ tương tác được đổi sang alpha 1 để cảnh/cây không xuyên qua chữ. Script dựng HUD cũng giữ cấu hình này khi chạy lại.
- Thân và vũ khí của cả Arrow, Cannon, Ice, Sniper đều có ba tham chiếu sprite hợp lệ. Bộ controller hiện có dùng Idle/Fire riêng cho từng cấp.
- Đạn xuất phát ở đầu trên sprite vũ khí, theo hướng ngắm; vẫn hỗ trợ firePoint riêng nếu prefab đã gán. Animation bắn hoàn tất recoil khi mục tiêu rời tầm và được tăng tốc nếu clip dài hơn chu kỳ bắn.

| Tháp | Tầm I | Tầm II | Tầm III |
| --- | ---: | ---: | ---: |
| Arrow | 7.5 | 9 | 10.5 |
| Cannon | 7 | 8.5 | 10 |
| Ice | 7 | 8 | 9 |
| Sniper | 13 | 15 | 17 |

Tầm đo từ tâm slot, thống nhất giữa chọn mục tiêu, menu và vòng phạm vi. Giữ các bán kính hiện tại vì đã được cấu hình theo khoảng cách slot tới đường quái; độ dài clip được đồng bộ với nhịp bắn thay vì dùng nó làm bán kính.

Kiểm chứng lượt sửa này: build `Assembly-CSharp` và `Assembly-CSharp-Editor` thành công, có cảnh báo tham chiếu thư viện Unity; kiểm tra đủ ba tham chiếu thân/vũ khí của bốn prefab và GUID asset đều tồn tại. Runtime trước sửa xác nhận chuồng cấp 1 có thể lên 3 con và bò bị liệt kê thành chuồng. Sau khi refresh, kết nối Unity timeout nên chưa chạy lại smoke test hoặc xác nhận ảnh sau sửa. Smoke test tháp đã cập nhật để chờ recoil kết thúc trước khi kiểm tra Idle.
