# Tổng hợp kế hoạch mới và asset

**Cập nhật triển khai:** Sprout Lands đã nhập vào Assets và map Main đã đổi sang 64×48 ô, chia 5 khu. Core thành/wave, slot xây, cấu hình quái/tháp và kho mới đã có base. Xem trạng thái thực tế và hợp đồng tích hợp tại [DEFENSE_BASE](../DEFENSE_BASE.md); các mục đề xuất dưới đây ghi lại phân tích tài liệu ban đầu.

Nguồn: kế hoạch phát triển `.md` và `PolishCow_BangCanBang.xlsx` trong thư mục này. Các giá trị cân bằng là đề xuất, chưa phải kết quả chơi thử.

## 1. Hướng game

Mua Feed → nuôi bò → thu RawMilk → đóng chai → bán lấy Credits → xây/nâng tháp và mua thuốc → hạ quái lấy EXP → thành lên cấp, mở đất và cửa hàng → hạ boss wave 10.

- Thắng: hạ boss wave 10. Thua: thành hết máu, chạy hiệu ứng nổ rồi hiện màn thua.
- Thành lên cấp bằng EXP; Credits dùng cho kinh tế và phòng thủ.
- Bán sữa liên tục; đơn hàng là phần thưởng thêm, bỏ kết thúc demo sau 5 đơn.
- Map vẽ sẵn thành 5 khu, mở dần; quái mặt đất đi theo waypoint, ruồi bay tới bò.

## 2. Thông số chính

| Cấp thành | EXP tích lũy | Máu tối đa | Khu mở | Ô tháp / bò / máy | Mở khóa |
| --- | --- | --- | --- | --- | --- |
| 1 | 0 | 120 | 1 | 4 / 3 / 1 | Cung, Feed, thuốc |
| 2 | 495 | 200 | 3 | 8 / 5 / 2 | Pháo, Băng; tháp cấp 2 |
| 3 | 3209.4 trong Excel; cần chốt số nguyên | 320 | 5 | 12 / 8 / 4 | Bắn tỉa; tháp cấp 3 |

- 10 wave; thời gian chuẩn bị 90–120 giây, riêng wave 10 là 180 giây.
- 5 loại quái: thường, nhanh, trâu bò, ruồi, boss.
- 4 tháp, mỗi loại 3 cấp: Cung, Pháo, Băng, Bắn tỉa; đều cần bắn được mục tiêu bay theo kế hoạch.
- Giá cơ sở: Feed 10 Credits/5; sữa chai 25 Credits; mua bò 100; mua máy 150; thuốc 20 (các giá mới là đề xuất).
- Bệnh: Khỏe; Nhẹ (thời gian ×1.5); Vừa (×2, 50% sữa dùng được); Nặng (toàn bộ là SpoiledMilk). Thuốc chữa về Khỏe.
- Cân bằng Excel giả định hiệu suất người chơi 60%, hạ 95% quái, 25% ruồi tới bò. Kết quả bảng không thay thế chơi thử.

## 3. So với code hiện tại

| Dùng lại | Cần bổ sung hoặc thay đổi |
| --- | --- |
| Player, tương tác E, kho và HUD | Medicine, SpoiledMilk và UI tương ứng |
| Bò, máy, ApplyLevel, nguồn điện | CowHealth, thuốc, sữa hỏng, mua thêm bò/máy |
| FeedShop, đơn hàng và drone | Bán liên tục, đơn thưởng thêm |
| FeatureReferences và Initialize | Core game, EXP, máu thành, thắng/thua |
| RoamingArea và các scene thử | MapZone, xây thật, placement slots, camera |
| Công cụ Editor hiện có | Quái, wave, tháp, đạn, pooling, HUD chiến đấu |
| Main hiện tại | Tích hợp module mới, menu, pause, lưu/tải, âm thanh |

## 4. Phân công và thứ tự triển khai

1. Core/tích hợp: giao IDamageable, GameState, GameManager, WaveTimer, CastleHealth, CastleProgress và hợp đồng sự kiện trước; thêm item mới vào kho.
2. Kinh tế: bán vô hạn, bệnh bò, thuốc, cân bằng bò/máy.
3. Xây dựng/map: xây thật, 5 khu đất, slot theo cấp, hình thành, camera.
4. Quái/wave: waypoint, 5 loại quái, ruồi gọi Infect, 10 wave và pooling.
5. Tháp/UI: 4 loại tháp, shop theo cấp thành, HUD chiến đấu, menu, âm thanh.

Tuần 1: hợp đồng chung và từng bản tối thiểu. Tuần 2: ghép một vòng chơi vào Main. Tuần 3: đủ nội dung và lưu/tải. Tuần 4: cân bằng, hoàn thiện và build.

## 5. Asset mới đang có

`TileMap.zip` chứa Sprout Lands Basic pack:

- Nhân vật, hành động và công cụ; bò, gà.
- Cỏ, nước, đồi, đất trồng, hàng rào, cửa, nhà gỗ.
- Cây, đồ vật, cầu gỗ, rương, icon sữa và cỏ.

`Enemy/` chứa:

- Mushroom: các sheet Idle, Run, Attack, Hit, Die, Stun; có bản với/không VFX.
- FlyingForestEnemies Enemy3: Idle, Fly, Attack, Hit, Die; có biến thể chuyển động nằm trong animation và không có chuyển động.
- Minotaur sprite sheet và hai ảnh tên pngwing chưa mô tả rõ vai trò.

Đề xuất thay hình: dùng Sprout Lands cho nền, đường, cây, hàng rào, nhà, nhân vật và bò; đánh giá sprite quái trong scene thử trước khi ghép. Khi điều khiển vị trí quái bằng code, ưu tiên thử biến thể không chứa chuyển động để tránh hình trượt khỏi collider.

Chưa thấy bộ chuyên dụng cho 4 loại tháp, thành 3 cấp, máy đóng chai, thuốc và sữa hỏng trong danh sách asset mới. Cần chọn hình phù hợp hoặc tiếp tục dùng placeholder cho các phần này. Chưa nhập hoặc thay asset trong Assets.

## 6. Những quy tắc cần chốt trước khi code

- Góc nhìn: Sprout Lands phục vụ map nhìn từ trên; cần xem thực tế các sheet quái có phù hợp khi đi nhiều hướng hay không.
- EXP cấp 3: Excel là 3209.4, tài liệu ghi xấp xỉ 3209; chọn một ngưỡng nguyên thống nhất (ví dụ làm tròn lên 3210).
- Mức bệnh Vừa vẫn là đề xuất cần xác nhận; sản lượng cơ sở 1 sữa cần quy tắc chia 50% (xác suất, luân phiên hoặc tích lũy phần lẻ).
- Thời điểm bệnh/thuốc ảnh hưởng lượt sữa đang chạy cần thống nhất với quy tắc chốt thông số khi bắt đầu chu kỳ.
- Ruồi hết bò sẽ bay tới thành theo kế hoạch, nhưng bảng quái đặt sát thương thành bằng 0; cần chốt hành vi.
- Lên cấp thành: chỉ tăng máu tối đa, hồi thêm phần chênh lệch hay hồi đầy?
- Thắng ngay khi boss chết hay chỉ khi toàn bộ wave 10 được xử lý?
- Ai sở hữu việc trừ tiền và chiếm ô tháp giữa Building và Towers; bảo đảm chỉ một luồng giao dịch.
- Kế hoạch giao Người 5 làm UI nhưng quy ước chung hạn chế sửa HUD; nên làm canvas/prefab riêng và Core tích hợp.
- Excel ghi chi phí nâng bò/máy là tích lũy; khi cấu hình giá từng bước phải lấy phần chênh lệch.
- Ngưỡng 90% EXP không đảm bảo đúng mốc nếu chỉ hạ 85% quái; cần chơi thử để tránh khóa đồ cần thiết trước boss.

## 7. Bước tiếp theo đề xuất

Làm scene thử hình ảnh riêng dùng Sprout Lands với một khu đất, nhân vật, bò và mẫu quái; kiểm tra góc nhìn, tỉ lệ, pivot, collider, sorting và animation. Chọn bộ hình xong mới thay prefab/Main. Sau đó giao hợp đồng Core và triển khai một vòng chơi tối thiểu: bán sữa → mua tháp → một wave → EXP → thắng/thua.
