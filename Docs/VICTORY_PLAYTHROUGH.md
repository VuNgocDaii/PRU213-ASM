# Luồng kiểm thử phá đảo Main

Mở `Assets/_Game/Scenes/Main.unity`, lưu scene, thoát Play Mode.

## Test tích hợp đến chiến thắng

Chọn **Tools > PolishCow > Tests > Run Full Victory Playthrough**.

1. Kiểm tra trận mới có 300 Credits, wave 0, đúng một chuồng bò, 22 ô ụ súng và đếm ngược tự động.
2. Thiết lập fixture trong Play Mode: cộng 20000 Credits và 3210 EXP qua API để mở cả bản đồ và đủ vốn kiểm tra xây/nâng cấp.
3. Kiểm tra tạm dừng/tiếp tục, tự xây tháp bắn tỉa và nâng cấp hợp lệ. Kiểm tra mỗi giao dịch chỉ trừ tiền một lần.
4. Chạy lần lượt wave 1–10 qua spawner, đường đi, projectile, sát thương và thưởng thật.
5. Kiểm tra thành còn máu, đủ mười wave, trạng thái Won và bảng chiến thắng.
6. Bấm nút chơi lại: wave 0, 300 Credits, thành đầy máu, tốc độ bình thường.

Seed 213; tốc độ x8; gọi BeginNextWave để bỏ thời gian chuẩn bị. Không gọi TakeDamage từ test, không ép thắng, không hồi máu thành, không sửa dữ liệu chiến đấu. Fixture này xác minh tích hợp đến hết game, không chứng minh cân bằng kinh tế thường.

## Test kinh tế thường

Chọn **Tools > PolishCow > Tests > Run Natural Economy Playthrough**.

Dùng cùng các bước kiểm tra, giữ 300 Credits ban đầu và chỉ dùng EXP, tiền kiếm được để mua/nâng tháp tên. Không dùng fixture vốn/EXP. Đây là một chiến lược tự chơi, seed cố định; kết quả không đại diện mọi cách chơi.

Lượt đầu chiến lược này thua ở wave 5 trước khi mở thành cấp 2. Nhật ký đã lưu riêng ở `Logs/VictoryPlaythrough-natural-first-attempt.txt` để không nhầm với test tích hợp thắng.

## Kết quả và thao tác

- Nhật ký lượt gần nhất: `Logs/VictoryPlaythrough.txt`, ghi rõ chế độ, RUNNING/PASS/FAIL, từng wave, máu thành và giao dịch.
- Ảnh đầu mỗi wave: `output/playthrough-wave-01.png` đến `10.png`.
- Ảnh chiến thắng: `output/playthrough-victory.png`.
- Tối đa 600 giây thực; kết thúc/lỗi tự thoát Play Mode. Dừng thủ công ghi FAIL.
- Giao dịch và fixture chỉ tồn tại trong Play Mode, không lưu vào scene/asset. Không chạy hai test đồng thời.

Smoke test cũ: **Tools > PolishCow > Run Main Combat Smoke Test**. Test này có hỗ trợ tiền/EXP và sát thương trực tiếp để kiểm tra tích hợp nhanh; đã cập nhật theo bố cục chỉ một chuồng bò.

## Kết quả sau khi căn lại vị trí ụ súng

Bố cục mới đặt ô hai bên đường, phủ các khúc cua và chồng tầm bắn gần thành. Cả 22 ô đều cách tuyến quái gần nhất 2–4,5 đơn vị, nằm trong tầm tháp tên cấp 1 (7,5), và không đè collider công trình/rào. Vùng khóa vẫn mở theo cấp thành.

Đã chạy PASS cả hai chế độ:
- Tích hợp có fixture: đủ 10 wave, 303 mạng quái, thành 320/320; bảng thắng và chơi lại hoạt động. Nhật ký `Logs/VictoryPlaythrough-integration.txt`.
- Kinh tế thường sau chỉnh vị trí: bắt đầu 300 Credits, không cấp thêm tiền/EXP hay sát thương; đủ 10 wave, 2057 phát bắn, 303 mạng quái, thành 320/320, xây được 22 tháp bằng tiền kiếm được. Bảng thắng và chơi lại PASS. Nhật ký `Logs/VictoryPlaythrough-natural.txt`.

Lượt thua wave 5 ở trên thuộc bố cục cũ. Các lượt kiểm tra bỏ thời gian chuẩn bị và chạy x8; kết quả áp dụng cho chiến lược tự chơi, seed 213.

Gợi ý chơi tay: ưu tiên hai ô gần thành tại (-4.5, 7.5), (5.5, 6), rồi ô giữa tại (3, -4). Bổ sung các ô hai bên đường và nâng tháp khi thành lên cấp; giữ tiền xây các tuyến mới mở.
