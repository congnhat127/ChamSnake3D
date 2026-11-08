# Technical Implementation Plan: Core Gameplay Mechanics (Mobile-First)

**Feature ID**: `001-core-gameplay`  
**Spec Reference**: [spec.md](./spec.md)  

---

## 1. Kiến trúc Hệ thống & Luồng Điều khiển (Architecture & Input Flow)

```
[Màn hình cảm ứng Mobile]
  ├── [Nửa trái: Virtual Joystick] ──> Vector2 moveDir ──┐
  └── [Nửa phải: Lock-Facing Button] ─> bool isFacingLocked ──┼──> [MotherController]
                                                              │          │ (Transform)
[Bàn phím PC (Fallback)]: [WASD + Space] ─────────────────────┘          ▼
                                                                  [SnakeChainIK]
                                                                         │
                                                       ┌─────────────────┼─────────────────┐
                                                       ▼                 ▼                 ▼
                                                   [Child 1]         [Child 2]       [Con Út (3)]
                                                                                           ▲
                                                                                           │ (Săn bắt)
                                                                                     [HunterAI]
                                                                                     (Speed = 6.5 m/s)
                                                                                     - Flank around Mother
                                                                                     - Grab Action (Vồ/Chém)
```

### Chi tiết các thành phần:

1. **`TouchInputManager.cs` (`Assets/_Game/Scripts/UI/TouchInputManager.cs`)**:
   - Quản lý Joystick ảo ở góc trái dưới và Nút giữ Khóa Hướng ở góc phải dưới.
   - Tự động nhận diện thiết bị: Trên điện thoại dùng cảm ứng, trên Unity Editor / PC nhận `WASD` và phím `SPACE`.
   - Cung cấp dữ liệu: `Vector2 MoveDirection` và `bool IsFacingLocked`.

2. **`MotherController.cs` (`Assets/_Game/Scripts/Entities/MotherController.cs`)**:
   - Tốc độ: `6.5 m/s`.
   - Logic Khóa Hướng:
     - Khi `IsFacingLocked == false`: Xoay mặt mượt mà theo `MoveDirection` (Chạy tự do 360°).
     - Khi `IsFacingLocked == true`: **Cố định nguyên góc quay mặt hiện tại**. Dù người chơi kéo Joystick lùi về sau, nhân vật chỉ di chuyển lùi, mặt không đổi hướng, đảm bảo đàn con luôn nằm sau lưng Mẹ.

3. **`SnakeChainIK.cs` (`Assets/_Game/Scripts/Systems/SnakeChainIK.cs`)**:
   - Quản lý đúng 3 node con: Con 1 ➔ Con 2 ➔ Con Út.
   - Thuật toán kéo trực tiếp kết hợp quán tính đàn hồi:
     - `linkSpacing = 1.30m`.
     - Tốc độ bám theo: `18.0 m/s`.
     - Lực văng ly tâm tiếp tuyến khi Mẹ quay góc gắt.

4. **`HunterAI.cs` (`Assets/_Game/Scripts/AI/HunterAI.cs`)**:
   - Tốc độ di chuyển: Cố định `6.5 m/s` (bằng đúng Mẹ).
   - Sensor lách sườn: Tính vector tới Con Út, nếu Mẹ nằm trong phạm vi chắn (< 2.5m), rẽ vòng sang sườn Mẹ.
   - **Cơ chế Vồ/Chém (`PerformGrab`)**:
     - Khoảng cách kích hoạt: `< 1.6m` tới Con Út.
     - Thời gian vung tay (Wind-up): `0.25s`.
     - Hitbox kiểm tra: SphereCast bán kính `0.8m` phía trước mặt Thợ Săn.
     - Nếu trúng Con Út ➔ Gọi sự kiện `OnCaughtConUt`.
     - Nếu trượt ➔ Bị trễ nhịp (cooldown) `0.5s` không thể vồ lại ngay.

---

## 2. Thiết kế Prefab Chuẩn (Prefab Specifications)

1. **`Mother.prefab`** (`Assets/_Game/Prefabs/Characters/Mother.prefab`):
   - Root: `CharacterController`, `MotherController`.
   - Visual: Capsule xanh lá + Sải tay ngang (Cylinder span 2.8m).
   - Vòng hào quang hiển thị khi kích hoạt Khóa Hướng.
2. **`Child.prefab`** (`Assets/_Game/Prefabs/Characters/Child.prefab`):
   - Root: `SphereCollider` (isTrigger), `ChildFollower`.
   - Visual: Khối cầu toon (Vàng cho Con 1, 2; Hồng pastel cho Con Út).
   - Nơ/vương miện trên đầu Con Út.
3. **`Hunter.prefab`** (`Assets/_Game/Prefabs/Characters/Hunter.prefab`):
   - Root: `CharacterController`, `HunterAI`.
   - Visual: Capsule tím + Nón lá truyền thống.
   - Cánh tay có animation hoặc tweening vung chém khi kích hoạt đòn vồ.
4. **`UI_TouchControls.prefab`** (`Assets/_Game/Prefabs/UI/UI_TouchControls.prefab`):
   - Canvas chuẩn di động (CanvasScaler: Scale With Screen Size, 1920x1080).
   - Cụm Joystick ảo bên trái + Nút tròn to "KHÓA HƯỚNG" bên phải.

---

## 3. Tiêu chuẩn Kiểm thử Độc lập (Playground Verification)
- Tạo Scene thử nghiệm sạch: `Assets/_Game/Scenes/Playground_CorePhysics.unity`.
- Kiểm tra 3 điều kiện:
  1. Giữ nút Khóa Hướng và đi lùi ➔ Mẹ đi lùi, mặt không quay lại, đàn con không bị chìa ra trước.
  2. Thợ Săn chạy cùng tốc độ, lượn vòng né Mẹ.
  3. Thợ Săn chỉ bắt được Út khi vung tay chém trúng.
  4. 0 lỗi Console (`Editor.log` hoàn toàn sạch).
