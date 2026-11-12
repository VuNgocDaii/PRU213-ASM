# Tháp I → II → III: tầm bắn, sát thương và animation

Tầm bắn đo bằng **đơn vị thế giới Unity**, theo bán kính từ tâm slot đến tâm enemy. Cùng một giá trị được dùng để chọn mục tiêu, loại mục tiêu ra khỏi tầm, vẽ vòng tầm bắn và hiển thị trên menu.

## Thông số hiện tại

| Tháp | Sát thương I / II / III | Tầm bắn I / II / III | Lần bắn/giây | Hiệu ứng |
| --- | --- | --- | ---: | --- |
| Arrow | 8 / 12 / 18 | 7,5 / 9 / 10,5 | 1,2 | Sát thương đơn |
| Cannon | 25 / 38 / 55 | 7 / 8,5 / 10 | 0,5 | AOE bán kính 1,5 / 1,8 / 2,2 |
| Ice | 5 / 8 / 12 | 7 / 8 / 9 | 1 | Tốc độ enemy còn 70% / 60% / 50% trong 2 / 2,5 / 3 giây |
| Sniper | 40 / 60 / 90 | 13 / 15 / 17 | 0,4 | Sát thương đơn, tầm xa |

Sát thương và nhịp bắn giữ theo dữ liệu tháp đã có; bổ sung tầm bắn riêng cho từng cấp và đảm bảo runtime sử dụng đúng cấp. Cả bốn loại hiện được cấu hình bắn mục tiêu bay. AOE chỉ tác động enemy hợp lệ, không gây sát thương lên bò/thành và không tính một enemy nhiều lần vì có nhiều collider.

## Cơ sở điều chỉnh tầm bắn

- Khoảng cách gần nhất từ bốn slot tháp ban đầu tới đường quái: **3,5; 5,5; 6,5; 3,5**. Tầm Arrow cũ là 4, nên hai slot không phủ được đường quái.
- Các slot bên hông vùng mở tiếp theo cách đường quái khoảng **3,5–4,5**.
- Bốn slot ngoài ở y = ±18 cách đường quái khoảng **11,5–12,5**; đây là vị trí phù hợp cho Sniper với tầm cơ bản 13.
- Các tháp thông thường được tăng tầm để hoạt động tại khu đầu và khu bên hông. Sniper giữ vai trò phủ xa từ các slot ngoài; tăng tầm qua II/III mở rộng đoạn đường được bao phủ.

Đây là cấu hình dựa trên hình học map và vai trò từng tháp; vẫn cần chơi thử để đánh giá độ khó/cân bằng cả mười wave.

## Nâng cấp và hiển thị

- Thanh toán và kiểm tra giới hạn cấp qua `BuildingUpgradeController`; tháp không nâng quá cấp thành.
- Nâng cấp cập nhật `CurrentLevel`, `CurrentDamage`, `CurrentRange`, hình thân tháp và bộ animation vũ khí tương ứng.
- Thân tháp dùng đúng sprite có nhãn I, II, III; mỗi cấp có bộ vũ khí riêng.
- Chuẩn hóa tỷ lệ vũ khí theo pixels-per-unit của thân tháp, tránh hiện tượng nâng lên II/III nhưng vũ khí nhỏ đi do import settings khác nhau giữa các sheet.
- `ApplyLevel(level)` đặt cấp tuyệt đối; gọi lặp không cộng dồn thông số. `Start()` giữ cấp đã gán trước đó.
- Menu tháp hiển thị tên/cấp La Mã, sát thương và tầm bắn hiện tại → cấp tiếp theo. Cấp III hiển thị thông số tối đa.
- Khi mở menu, vòng xanh biểu diễn tầm hiện tại; nâng cấp cập nhật vòng ngay. Đóng menu ẩn vòng.

## Animation

- Mỗi loại tháp có controller riêng ở `Assets/_Game/Animation/Tower/Levels/`.
- Controller có sáu state: `Idle1`, `Fire1`, `Idle2`, `Fire2`, `Idle3`, `Fire3`.
- Mặc định đứng yên. Chỉ một lần bắn thực sự mới kích hoạt `Fire` của cấp hiện tại; clip kết thúc chuyển về `Idle` cùng cấp.
- Không có enemy hợp lệ trong tầm thì dừng bắn và về hình idle. Vũ khí xoay về mục tiêu khi nhắm.
- Pause dừng tiến trình animation và bắn. Thắng/thua ngừng xử lý bắn.
- Các clip lấy frame từ asset An_Tower hiện có; không tạo hình mới. Thân Sniper dùng bộ Tower 07 cùng bộ vũ khí, thay cho thân Tower 06 bị ghép lệch trước đó.
- `Tower 07.png` trước đây nhập thành một sprite chứa cả ba thân; importer được tách thành ba sprite để dùng đúng từng cấp. Các sheet còn lại giữ nguyên importer.

## Thiết lập và kiểm tra

- **Tools > PolishCow > Towers > Configure Three Levels and Attack Animations** cập nhật dữ liệu, prefab và clip/controller đã có, không sinh bản sao tên khác sau mỗi lần chạy.
- **Tools > PolishCow > Integrate All Features Into Main** cũng gọi bước cấu hình tháp, sau đó nối UI trong Main.
- **Tools > PolishCow > Towers > Run Tower Level Smoke Test** kiểm tra bốn loại × ba cấp trong Play Mode: nâng cấp qua Building, hình riêng, thông số tăng, ngoài tầm không bắn, vào tầm có animation và sát thương đúng, pause, mất mục tiêu, menu và vòng tầm.
- Báo cáo cục bộ: `Logs/TowerLevelsSmokeTest.txt`. Dummy/tài nguyên của test chỉ tồn tại trong Play Mode, không lưu vào scene Main.

Kết quả lượt kiểm tra: toàn bộ 12 tổ hợp loại/cấp đạt; test chiến đấu Main qua mười wave, thắng/thua và restart cũng đạt. Console ở trạng thái kết thúc kiểm tra không có lỗi biên dịch hoặc runtime hiện tại.

Ảnh đối chiếu bộ hình và thông số của 12 tổ hợp loại/cấp: [tower-levels-I-II-III.png](../output/tower-levels-I-II-III.png).
