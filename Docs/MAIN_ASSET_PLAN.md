# Asset cho Production và Delivery của Main

Đã chọn và tải miễn phí **Farm & Ranch Life**, nhập ba sheet cần thiết cùng LICENSE/README vào `Art/Features/Economy/FarmRanchLife`. Máy ép được dùng làm hình production, shipping bin làm delivery; gameplay vẫn là chế biến sữa và drone giao hàng hiện có. Solaria và các pack khác dưới đây chỉ là phương án thay thế, chưa mua/tải.

Production dùng 6 frame (idle, 4 working, ready), mất điện thì ngừng animation và đổi màu. Delivery dùng 4 frame mở nắp khi giao; drone giữ animation riêng. Đã áp dụng vào prefab, Main, EconomyWorkspace, ProductionTest và OrderTest qua menu **Tools → PolishCow → Team Base → Apply Farm Production and Delivery Art**.

## Chọn một bộ đủ cả hai khu

| Bộ | Production | Delivery | Giá/trạng thái | Lưu ý khi ghép |
| --- | --- | --- | --- | --- |
| [Solaria Farming & Crafting](https://jamiebrownhill.itch.io/solaria-farming-crafting) | Cheese press, keg, cask, mechanic/crafting stations | Shipping Bin có 2 loại; thùng chứa/đạo cụ nông trại | Bản đầy đủ 10 USD; bản miễn phí chỉ có một số nội dung | 16×16 top-down; trang tác giả công bố không dùng generative AI. Palette khác Sprout Lands, nên xem preview trước |
| [Farm & Ranch Life](https://michelclauwaertgmailcom.itch.io/farm-ranch-life-free-16x16-top-down-pixel-art-asset-pack) | 14 artisan machines, có cheese press và butter churn, trạng thái idle/working/done | Shipping bin mở nắp, market stalls, sacks, hay và milk pail | Name your own price, có tải miễn phí | 16×16 top-down; tác giả công bố có AI assistance; cần kiểm tra chất lượng frame và palette khi ghép |

**Ưu tiên cho đồ án miễn phí:** thử Farm & Ranch Life; lấy một máy làm hình trạm chế biến và shipping bin làm trạm giao. **Nếu không muốn pack có AI assistance:** Solaria bản đầy đủ bao phủ cả hai khu. Không cần mua hai bộ riêng chỉ để có máy và thùng giao hàng.

## Phương án bổ sung

- [Farmstead Artisan Workshop](https://saczland.itch.io/farmstead-artisan-workshop): mẫu miễn phí có butter churn, bốn hướng, animation làm việc; mẫu chỉ phi thương mại. Cell 128×128, khác độ chi tiết Sprout Lands; không ưu tiên làm pack chính cho map 16×16.
- [Larchrow Farm & Crops](https://larchrow.itch.io/larchrow-16x16-farm-crops): miễn phí/name your own price, có market stall, shipping bin, milk can và mailbox; hợp để bổ sung Delivery. Trang không liệt kê dây chuyền đóng chai.

## Ghép vào Main

- Production: giữ ProductionMachine/PowerSource/MachineUpgrade và điểm tương tác hiện tại; chỉ thay SpriteRenderer/Visual con. Icon đầu vào RawMilk, đầu ra BottledMilk, progress và status tách khỏi sprite máy.
- Delivery: giữ OrderController, OrderInteraction và DroneDeliveryEffect. Shipping bin/market stall là vật thể dưới đất; drone là hiệu ứng tới lấy hàng, không cần một hệ thống giao dịch khác.
- Không thay toàn bộ map để dùng một máy từ pack khác. Import Point/Nearest, không mipmap, PPU theo grid gốc; điều chỉnh scale để vật thể chiếm khoảng 2–3 ô và collider nằm ở chân.
- Đặt hình máy/thùng giao dưới Visual riêng; không gắn collider phủ toàn mái hoặc toàn drone làm Player không tới được gốc tương tác trong bán kính 1.5.
- Phân biệt trạng thái: idle / processing / no power / ready của Production; idle / delivering của Delivery. Pack không có animation thì vẫn dùng progress/status của gameplay hiện tại.
- Người tích hợp thay hình trong Main; Economy nhận bản prefab gameplay đã duyệt. Asset của module sau đó đặt Art/Features/Economy để người khác không cùng đổi importer.

Sprout Lands Basic hiện có bàn/đạo cụ và Chest trong project, có thể lắp hình tạm đồng bộ palette trong lúc chọn pack. Đây là đạo cụ thay thế, không phải máy đóng chai thật.

Khi tải pack, giữ LICENSE/README của tác giả. Với pack trả phí, mua/tải bằng tài khoản của nhóm và theo quyền sử dụng của pack; không đưa toàn bộ pack nguồn lên repository như một thư viện asset phân phối lại.
