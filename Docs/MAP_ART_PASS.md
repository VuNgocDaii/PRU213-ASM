# Map Main: lượt chỉnh cảnh quan

Dựng lại bằng menu `Tools > PolishCow > Map > Rebuild Landscape and Repair Atlas Sprites` trong Edit Mode, scene Main. Bản sao trước khi làm cảnh quan ở `output/backups/Main-before-landscape.unity`.

Đã sửa asset Tree/Shrub/Flowers và chọn lại sprite cỏ, hoa, cây trồng theo đúng khung của atlas nguồn. Không cắt đều 16x16 cho vật thể nhiều ô. Ví dụ cây tròn dùng rect (19,47,26,33), bụi dùng (0,15,33,18), hoa dùng (113,33,13,11), đơn vị pixel tính từ góc dưới trái.

Bố cục:
- Rừng viền và các cụm cây chừa đường, tán cây không che ô tháp.
- Một chuồng bò gọn, lối vào nối đường chính.
- Xưởng ở phía đông, máy sản xuất tại (11,4).
- Cửa hàng thức ăn, bò, thuốc, sữa và điểm giao hàng gom vào dãy chợ phía nam; drone chỉ hiện khi giao hàng.
- Vườn táo phía tây, các luống rau phía nam, ao có mép cong và collider riêng ngoài tuyến chiến đấu.
- Giữ nguyên 22 vị trí tháp và đường quái đã kiểm thử.

Cảnh quan nằm dưới `ExpandedFarm/<zone>/Content/MapArt`, mở theo cấp thành như trước. Lệnh dựng lại thay đúng nhóm trang trí sinh tự động và giữ các hệ thống chơi.

Xác minh: 22 ô tháp không đè collider; Tree/Shrub/Flowers khớp khung atlas. Test kinh tế thường với seed 213, 300 Credits, không cấp tiền/EXP hoặc sát thương trực tiếp đã PASS đủ 10 wave: 2030 phát bắn, 302 mạng, thành 320/320; bảng chiến thắng và chơi lại PASS. Test chạy x8 và bỏ thời gian chuẩn bị.

Nhật ký: `Logs/MapArtPass.txt`, `Logs/VictoryPlaythrough-landscape.txt`.
Ảnh: `output/map-landscape-overview.png`, `output/map-landscape-central.png`.
