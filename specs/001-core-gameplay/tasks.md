# Implementation Tasks: Core Gameplay Mechanics (Mobile-First)

**Feature ID**: `001-core-gameplay`  
**Ref Spec**: [spec.md](./spec.md) | **Ref Plan**: [plan.md](./plan.md)  

---

## Phase 1: Chuẩn hóa Bộ Mã Nguồn Cốt Lõi (Core Scripts)
- [x] **Task 1.1**: Viết `TouchInputManager.cs` hỗ trợ Joystick ảo bên trái + Nút giữ Khóa Hướng bên phải (hỗ trợ kèm WASD + Space trên PC/Editor).
  - *Verify:* Input ghi nhận chính xác `MoveDirection` và `IsFacingLocked`.
- [x] **Task 1.2**: Nâng cấp `MotherController.cs` theo cơ chế mới: Tốc độ 6.5 m/s, khi `IsFacingLocked == true` thì giữ nguyên góc mặt, cho phép bước lùi và bước ngang mà không quay đầu.
  - *Verify:* Thử nghiệm bước lùi, góc nhìn Mẹ giữ nguyên 100%.
- [x] **Task 1.3**: Tối ưu `SnakeChainIK.cs`: Cố định đúng 3 con, khoảng cách 1.3m, bảo đảm đàn con luôn nằm sau lưng Mẹ khi Mẹ bước lùi hoặc rẽ ngang.
  - *Verify:* Đàn con bám sát theo đuôi, không bị giật hay đơ.
- [x] **Task 1.4**: Nâng cấp `HunterAI.cs`: Tốc độ 6.5 m/s (bằng Mẹ), cơ chế lách sườn quanh Mẹ, và hành động Vồ/Chém (`PerformGrab`) trong phạm vi 1.6m có cooldown.
  - *Verify:* Thợ Săn không thể bắt Út nếu chưa tung đòn vồ trúng.

---

## Phase 2: Đóng gói Prefab Hoàn chỉnh (Prefab Packaging)
- [x] **Task 2.1**: Thiết lập `PrefabGenerator.cs` tự động tạo `Assets/_Game/Prefabs/Characters/Mother.prefab` (Visual xanh lá, sải tay 2.8m, `CharacterController`, `MotherController`).
- [x] **Task 2.2**: Thiết lập `PrefabGenerator.cs` tự động tạo `Assets/_Game/Prefabs/Characters/Child.prefab` (Visual vàng/hồng, `SphereCollider`, `ChildFollower`).
- [x] **Task 2.3**: Thiết lập `PrefabGenerator.cs` tự động tạo `Assets/_Game/Prefabs/Characters/Hunter.prefab` (Visual tím nón lá, `CharacterController`, `HunterAI`, đòn vồ).
- [x] **Task 2.4**: Tích hợp OnGUI / Canvas điều khiển cảm ứng trực quan trong `TouchInputManager.cs`.

---

## Phase 3: Dựng Scene Sandbox Thử nghiệm (Playground Scene)
- [x] **Task 3.1**: Dọn dẹp sạch sẽ các script tự sinh cũ (`GameplaySceneBuilder.cs`, `AutoRebuilder.cs`).
- [x] **Task 3.2**: Thiết lập `SetupCorePlayground.cs` với Menu `Tools -> Rong Ran -> Setup Core Playground (Spec 001-core)` và auto-trigger nạp Scene `Playground_CorePhysics.unity` gồm Sân đấu tròn 32m Arena, Ánh sáng URP ấm, Camera Isometric bám đuổi mượt, Đầu Đàn, 3 Trẻ rồng rắn và Thầy Thuốc AI.
- [x] **Task 3.3**: Thiết lập file cấu hình thông số cân bằng tập trung `GameBalanceConfig.asset` tại `Assets/_Game/Settings/`.

---

## Phase 4: Kiểm thử Độc lập & Tự Nghiệm Thu (Self-Verification)
- [ ] **Task 4.1 (Verify Movement & Lock-Facing):** Kiểm tra Đầu Đàn di chuyển 360°, giữ phím `Space` hoặc chạm nút ảo trên màn hình để đi lùi/strafe mà không quay lưng lại Thầy Thuốc.
- [ ] **Task 4.2 (Verify Chain 3 Children):** Kiểm tra 3 trẻ nối đuôi nhau bám sát lưng Đầu Đàn, uốn lượn mềm mại, Con Út ở cuối hàng đội vương miện.
- [ ] **Task 4.3 (Verify Hunter Flank & Grab):** Kiểm tra Thầy Thuốc chạy cùng tốc độ (6.5 m/s), lách sườn quanh Đầu Đàn và chỉ vồ bắt khi áp sát < 1.6m.
- [ ] **Task 4.4 (Zero Console Errors):** Kiểm tra log Unity, bảo đảm 0 lỗi compiler, 0 lỗi runtime.

---

## Phase 5: Bàn giao Kết quả cho Người dùng
- [ ] **Task 5.1**: Gửi báo cáo nghiệm thu chi tiết kèm số liệu đo đạc để người dùng vào trải nghiệm.
