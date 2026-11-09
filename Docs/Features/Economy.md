# Economy — bàn giao

Người 1, nhánh `feature/economy`, scene `Assets/_Game/Scenes/Workspaces/EconomyWorkspace.unity`.

Sở hữu Features/Economy, Cows, Production, Orders và Prefabs/Modules/Economy/EconomyFeature.prefab. Làm CowHealth triển khai ICowDiseaseTarget; thuốc/sữa hỏng; bán sữa/đơn thưởng; shop Feed và cân bằng kinh tế. UI đặt trong module riêng.

Building sở hữu mua/nâng công trình; Economy cung cấp prefab bò/máy và ApplyLevel. Khi chuyển bò/máy/shop vào EconomyFeature, ghi rõ để tích hợp gỡ instance cũ trong Main.

## Kết quả

- Đã làm: gameplay nông trại cũ và composition root rỗng.
- Prefab/API cung cấp: cập nhật khi làm.
- Bước Play thử, kết quả và phần chưa xong: cập nhật khi làm.
