# Phần 2 – Máy sản xuất (đóng chai sữa)

## Script

| Script | Việc làm |
|---|---|
| `ProductionMachine` (kế thừa `Interactable`) | Nhấn E: đủ RawMilk thì trừ và bắt đầu. Có điện thì đếm giờ, mất điện thì dừng (giữ tiến độ). Xong thì nhấn E nhận BottledMilk. Hiện chữ trạng thái + thanh tiến độ. |
| `MachineUpgrade` (kế thừa `UpgradeTarget`) | Building gọi `ApplyLevel(level)`. Cấp 1 ×1, cấp 2 thời gian ×0.8, cấp 3 thời gian ×0.6 và sản lượng ×2. |

Luồng:
```
Chờ  --E, có điện, TryRemove(RawMilk) thành công-->  Đang chạy
Đang chạy  --có điện: giảm thời gian / mất điện: giữ nguyên-->  Xong
Xong  --E: Add(BottledMilk)-->  Chờ
```

Đáp ứng yêu cầu:
- Nhấn E dùng máy, trừ RawMilk, cộng BottledMilk khi xong.
- Hiện tiến trình (thanh) và thời gian còn lại (chữ).
- Không chạy chồng lượt: đang chạy hoặc đã xong mà chưa nhận thì E không bắt đầu lượt mới.
- Điện đơn giản: đọc `PowerSource.HasPower` mỗi frame; mất điện thì dừng, không trừ thêm/hoàn RawMilk.
- Cấu hình trong Inspector: `Raw Milk Cost`, `Bottled Milk Amount`, `Process Time`.
- Thời gian và sản lượng **chốt lúc bắt đầu lượt**, nâng cấp giữa chừng chỉ áp dụng lượt sau.

## Prefab `ProductionMachine` (đã lắp sẵn)

`Assets/_Game/Prefabs/Features/Production/ProductionMachine.prefab`, nhân bản từ `ProductionSample`:

```
ProductionMachine   SpriteRenderer, WorldDepthSorting, BoxCollider2D,
                    FeatureReferences, PowerSource, ProductionMachine, MachineUpgrade
├─ InteractionPoint
├─ StatusText       TextMeshPro, Order In Layer 3000 (chữ trạng thái)
└─ ProgressBar      (0, 0.8)
   ├─ Background    Square xám, scale (1.5, 0.15)
   └─ FillPivot     (-0.75, 0) ← ô Progress Fill, script đổi scale X 0→1
      └─ Fill       Square xanh, (0.75, 0)
```

Các ô của `ProductionMachine` đã trỏ vào FeatureReferences, PowerSource, StatusText, FillPivot của chính prefab. `MachineUpgrade.Machine` trỏ vào `ProductionMachine`.

## ProductionTest (đã cập nhật)

- Instance `ProductionMachine` nằm trong `FeatureWork`, đúng chỗ cũ của mẫu (1, -3.5).
- `ProductionSample` cũ được **tắt** (không xoá, để đối chiếu).
- Mảng **Features** của `FeatureReferenceInitializer` đã thay mẫu bằng `ProductionMachine`.

Khi ghép Main, người phụ trách base chỉ cần kéo prefab vào và thêm FeatureReferences của nó vào mảng Features của Main.

## Thử (checklist)

| Thử | Kết quả đúng |
|---|---|
| Đứng gần máy | Hiện "Nhấn E: Dùng máy đóng chai" |
| E khi đủ RawMilk (scene thử có 50) | RawMilk −2, chữ "Đang đóng chai... 5.0s", thanh chạy |
| E liên tục khi đang chạy | Chỉ hiện "Máy đang chạy...", RawMilk không bị trừ thêm |
| Chuột phải `PowerSource` → **Power Off** khi đang chạy | Chữ đỏ "MẤT ĐIỆN - tạm dừng", thời gian đứng yên |
| **Power On** | Chạy tiếp từ chỗ cũ, không trừ RawMilk lần nữa |
| Hết giờ | Chữ xanh "Xong! Nhấn E nhận 1 chai" |
| E | BottledMilk +1 (HUD cập nhật), về trạng thái chờ |
| E khi RawMilk < 2 | "Thiếu RawMilk (cần 2)", không có gì bị trừ |
| E khi mất điện lúc máy đang chờ | "Mất điện, chưa chạy được", không trừ RawMilk |
| Chuột phải `MachineUpgrade` → **Test: Level 3** khi đang chạy | Lượt hiện tại giữ nguyên; lượt sau 3 giây, ra 2 chai |

## Commit
```powershell
git add -- Assets/_Game/Scripts/Features/Production Assets/_Game/Prefabs/Features/Production Assets/_Game/Scenes/Tests/ProductionTest.unity
git commit -m "feat(production): add bottling machine with power and upgrades"
git push -u origin feature/production
```
