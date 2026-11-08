# Implementation Tasks: Stylized Stickman Party Character Rigs & Arena

**Feature ID**: `001-character-visuals`  
**Ref Spec**: [spec.md](./spec.md) | **Ref Plan**: [plan.md](./plan.md)

---

## Task Checklist

- [x] **Task 1.1**: Chuẩn hóa bộ Material URP Lit (Màu da, mắt, má hồng, nón lá, vương miện, cỏ sân đấu).
  - *Verify:* Đã tạo và kiểm tra 12 file `.mat` trong `Assets/_Game/Materials/`.
- [x] **Task 1.2**: Đóng gói Prefab `Mother.prefab` chuẩn Stickman Party (thân bo tròn, đầu chibi, mắt to, má hồng, nón lá, sải tay 2.8m, chân tiếp đất, `CharacterController`).
  - *Verify:* Prefab lưu hợp lệ tại `Assets/_Game/Prefabs/Characters/Mother.prefab` (46.7 KB).
- [x] **Task 1.3**: Đóng gói Prefab `Child.prefab` chuẩn Chibi (đầu to thân nhỏ, má hồng, mắt hoạt hình, vương miện cho Con Út).
  - *Verify:* Prefab lưu hợp lệ tại `Assets/_Game/Prefabs/Characters/Child.prefab` (41.2 KB).
- [x] **Task 1.4**: Đóng gói Prefab `Hunter.prefab` (áo dài tím, nón lá chóp nhọn, mắt tinh quái, tay vồ vươn ra trước).
  - *Verify:* Prefab lưu hợp lệ tại `Assets/_Game/Prefabs/Characters/Hunter.prefab` (36.1 KB).
- [x] **Task 1.5**: Đóng gói Prefab `Arena.prefab` (sàn phẳng `MeshCollider` Y=0, hàng rào 24 đốt có barrier cao 3.5m).
  - *Verify:* Loại bỏ `CapsuleCollider`, dùng `MeshCollider` chuẩn; Prefab lưu tại `Assets/_Game/Prefabs/Environment/Arena.prefab` (198.7 KB).
- [x] **Task 1.6**: Tạo Scene `Assets/_Game/Scenes/Character_Showcase.unity` và đặt các nhân vật vào sân khấu.
  - *Verify:* Scene đã được lưu và nạp trên Unity Editor.
- [x] **Task 1.7 (Self-Verification)**:
  - *Console Check:* `unity_get_console_logs` trả về **0 Errors, 0 Warnings**.
  - *Telemetry Check:* Mẹ tại `(0, 0, 0)` tiếp đất phẳng, Thầy Thuốc tại `(0, 0, 7.5)`, 3 Con sau lưng Mẹ `(Z = -2.28, -3.58, -4.88)`.
