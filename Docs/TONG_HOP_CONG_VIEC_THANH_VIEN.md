# Tổng hợp công việc các thành viên — PolishCow

Cập nhật: 12/11/2026. Nội dung dựa trên lịch sử Git và project hiện tại. Phần hoàn thiện Main được ghi riêng để không nhầm với đóng góp gốc của từng người.

## 1. Công việc từng thành viên

| Thành viên | Phần phụ trách | Công việc chính |
| --- | --- | --- |
| **Đại** | Nền project và tích hợp | Tổ chức project, điều khiển nhân vật, tương tác bằng E, kho và HUD chung. Làm hệ thống máu/cấp thành, trạng thái trận, bản đồ và các scene làm việc riêng. Ghép các tính năng vào Main. |
| **Hoàng** | Bò sữa và kinh tế | Bò di chuyển trong chuồng, cho ăn, tạo và thu hoạch sữa, nâng cấp chuồng. Làm bệnh của bò, thuốc chữa, mua bò và bán sữa. |
| **Tuấn** | Xây dựng và nâng cấp | Xây, nâng cấp, bán công trình; kiểm tra tiền, cấp mở khóa và vật cản. Làm bảng chọn công trình, hình thành theo cấp và kết nối nâng cấp tháp. |
| **Thắng** | Máy sản xuất và quái | Máy chuyển sữa thô thành sữa chai, nâng cấp và tạm dừng khi mất điện. Làm quái thường, nhanh, trâu, ruồi, boss; sinh quái theo đợt và thưởng khi diệt quái. |
| **An** | Đơn hàng và tháp phòng thủ | Làm đơn hàng, giao hàng bằng drone và cửa hàng thức ăn. Làm bốn loại tháp, đạn, bảng mua/nâng cấp/bán tháp, tạm dừng và màn hình thắng/thua. |

## 2. Những phần đã hoàn thiện trong Main

Các mục dưới đây là phần ghép và sửa chung, có hỗ trợ Codex; không tính lại toàn bộ vào công việc gốc của một thành viên.

- **Ghép hệ thống:** kinh tế, xây dựng, quái và tháp cùng dùng một kho; các thao tác mua, xây, nâng cấp và bán được đồng bộ.
- **Chuồng bò:** giữ một chuồng gọn, có rào và lối vào. Bò đi trong sân chuồng; mua thêm bò theo sức chứa cấp I/II/III là **2/3/4 con**.
- **Phòng thủ:** có **22 ô xây tháp**, bố trí dọc đường quái và gần thành. Bốn loại Arrow, Cannon, Ice, Sniper đều có ba cấp I/II/III.
- **Bản đồ:** sửa khung ảnh cây/bụi/hoa bị cắt; thêm cụm cây, vườn, ao và dãy cửa hàng. Đưa máy sản xuất về khu xưởng, giữ đường đi và ô tháp thông thoáng.
- **HUD:** thu gọn thanh chọn tháp và nhãn khóa cấp để không che hướng dẫn tương tác.
- **Vòng chơi:** bắt đầu với **300 Credits, 10 Feed**, có **10 đợt quái**. Hạ boss cuối để thắng; thành hết máu thì thua; nút chơi lại khởi tạo trận mới.

### Các lỗi chính đã xử lý

- Nhân vật đứng gần ô xây bị tính nhầm là vật cản.
- Mua bò chọn nhầm bò riêng lẻ thành chuồng, dẫn đến vượt sức chứa.
- Bán công trình chưa giải phóng ô xây; trạng thái nâng cấp tháp chưa đồng bộ.
- Cấp phát đạn khi hết đạn dự phòng và xử lý sát thương vùng chưa đúng.
- Cây/trang trí lấy sai phần ảnh; thanh chọn tháp chồng lên HUD tương tác.

## 3. Kết quả kiểm tra gần nhất

| Nội dung | Kết quả |
| --- | --- |
| Chơi từ 300 Credits, tự xây/nâng tháp bằng tiền kiếm được | **Qua đủ 10 đợt**, thành còn **320/320 máu**; không cấp thêm tiền, EXP hay sát thương từ test. |
| Màn hình chiến thắng và nút chơi lại | Hiện đúng; chơi lại về đợt 0, 300 Credits và thành đầy máu. |
| Cây và vị trí xây tháp trên map mới | Cây/bụi/hoa khớp khung ảnh gốc; cả 22 ô không bị vật trang trí chặn. |
| Thanh chọn tháp và HUD tương tác | Đã xem trong Play Mode; đủ bốn nút tháp, không còn chồng lên gợi ý E. |

**Lưu ý về test:** lượt phá đảo chạy tốc độ ×8, bỏ thời gian chờ giữa các đợt và dùng seed 213 để lặp lại. Kết quả xác nhận một chiến lược chơi đã thắng, không có nghĩa mọi cách chơi đều dễ thắng. Test tích hợp có cấp sẵn tài nguyên là chế độ riêng.

## 4. Điểm cần nhớ khi bàn giao

- Chạy game ở `Assets/_Game/Scenes/Main.unity`. Dùng WASD/phím mũi tên để đi, E để tương tác; chọn tháp rồi bấm ô trống để xây.
- Hiện chỉ có **một chuồng bò**; các ô xây trống còn lại dùng cho tháp. Không dùng mô tả cũ về việc xây thêm nhiều chuồng.
- Bản đồ mở theo cấp thành. Giữ khoảng trống quanh đường quái, cửa chuồng và ô tháp khi chỉnh cảnh quan.
- Phân biệt phần tính năng gốc của thành viên với phần sửa/tích hợp chung. Kiểm tra `git status` trước khi chốt bản bàn giao.
- Chưa ghi nhận âm thanh chiến đấu là phần đã hoàn thiện.

Tài liệu chi tiết khi cần: [chạy Main](MAIN_INTEGRATION.md), [test phá đảo](VICTORY_PLAYTHROUGH.md), [bản đồ](MAP_ART_PASS.md), [ba cấp tháp](TOWER_LEVELS.md).
