# Implementation Tasks: Stylized Stickman Party Character Rigs (Vietnamese Folk Theme)

**Feature ID**: `001-character-visuals`  
**Ref Spec**: [spec.md](./spec.md) | **Ref Plan**: [plan.md](./plan.md)  
**Standard**: Speckit & Project Constitution v2.0  

---

## Task Checklist

### Phase 1: Dọn dẹp & Khử rác kiến trúc cũ (Cleanup)
- [x] **Task 1.1**: Vô hiệu hóa script `CharacterAssetPipeline.cs` tự động nạp Knight/Rogue/Mage.
- [x] **Task 1.2**: Di dời các file model phương Tây (`Knight.fbx`, `RogueHooded.fbx`, `Mage.fbx`, `Barbarian.fbx`) sang `_Archive_Fantasy/`. Thư mục `Assets/_Game/Models/Characters/` hoàn toàn sạch sẽ.
- [x] **Task 1.3**: Đặt lại cờ `.specify/feature.json` trỏ về `specs/001-character-visuals`.

### Phase 2: Kiến trúc Prefab & Hệ thống Sockets chuẩn
- [ ] **Task 2.1**: Thiết lập Prefab `Mother.prefab` chuẩn Chibi:
  - Base Pivot: Chân tiếp xúc đất phẳng tại $Y = 0.0m$.
  - Khung xương Humanoid chuẩn với Animator Controller (`Idle`, `Walk`, `Run`, `Block`).
  - Sockets gắn xương: `Socket_Head` (vị trí Nón Lá), `Socket_Hand_L` & `Socket_Hand_R` (vị trí Sải tay cản).
  - Component: `CharacterController` (height 1.7m, radius 0.45m), `MotherController`.
- [ ] **Task 2.2**: Thiết lập Prefab `Child.prefab` chuẩn Chibi 1:1:
  - Base Pivot: $Y = 0.0m$.
  - Sockets: `Socket_Head`, `Socket_Crown` (trên đỉnh đầu cho Con Út).
  - Material Variants: `Mat_Child` (Vàng/Cam ấm cho Con 1 & 2), `Mat_ConUt` (Hồng pastel cho Út).
  - Component: `CharacterController`, `ChildFollower`.
- [ ] **Task 2.3**: Thiết lập Prefab `Hunter.prefab` chuẩn Thầy Thuốc:
  - Base Pivot: $Y = 0.0m$.
  - Sockets: `Socket_Head` (Nón lá), `Socket_Hand_R` (Vung chém vồ bắt kèm Grab Hitbox).
  - Material: `Mat_Hunter` (Áo dài tím truyền thống).
  - Component: `CharacterController`, `NavMeshAgent`, `HunterAI`.

### Phase 3: Phụ kiện Văn hóa 3D (Sockets-based)
- [ ] **Task 3.1**: Chuẩn hóa Prefab `NonLa.prefab` (Nón lá Việt Nam chóp nón mềm mại, texture đan tre) snap vào `Socket_Head`.
- [ ] **Task 3.2**: Chuẩn hóa Prefab `Crown_ConUt.prefab` (Vương miện vàng nhỏ 3 chóp) snap vào `Socket_Crown` của Con Út.
- [ ] **Task 3.3**: Chuẩn hóa Visual sải tay bảo vệ (2.8m đo từ xương tay `Hand_L` đến `Hand_R`) snap vào xương cánh tay Mẹ.

### Phase 4: Sân Đấu & Trưng Bày (Showcase Verification)
- [ ] **Task 4.1**: Thiết lập `Arena.prefab` hoàn chỉnh: Đường kính 32m, sàn cỏ URP Lit, rào chắn va chạm kín 3.5m.
- [ ] **Task 4.2**: Cập nhật Scene `Character_Showcase.unity`:
  - Trưng bày Mẹ, Thầy Thuốc, 3 Con trên sân khấu ánh sáng đầy đủ.
  - Kiểm tra tiếp đất $Y = 0$, không lơ lửng, không xuyên sàn.
- [ ] **Task 4.3 (Verification Before Handoff)**:
  - `unity_get_console_logs`: 0 Errors, 0 Exceptions.
  - Chụp ảnh kiểm chứng hoặc xác thực qua MCP.
