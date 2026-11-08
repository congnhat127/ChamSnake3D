# Rồng Rắn Chăm Chăm 3D - Project Constitution & Engineering Principles

---

## I. Evidence-Based & Anti-Arbitrary Rationale (CẤM TỰ BỊA SỐ MA)
1. **Tuyệt đối KHÔNG tự bịa ra thông số cứng (No Magic Numbers):**
   - Mọi kích thước, cự ly và phạm vi va chạm phải có cơ sở đối chiếu rõ ràng:
     - **Sải tay cản:** Phải đo từ khung xương (Rig Bones: `Hand_L` đến `Hand_R`) hoặc bounding box của cánh tay.
     - **Cự ly đàn con:** Phải đo từ bán kính thực của Collider (`SphereCollider.radius` / `Collider.bounds.extents`) cộng với khoảng đệm an toàn, hoặc dùng Physics Joint.
     - **Phạm vi vồ bắt:** Phải khớp với sải vung tay của animation tấn công.
2. **Data-Driven Configuration:**
   - Tất cả thông số cân bằng game (tốc độ, thời gian round, máu, cự ly) bắt buộc phải tập trung trong `ScriptableObject` (`GameBalanceConfig.asset`), không hardcode trong mã C#.
   - Phải có visualize bằng `OnDrawGizmosSelected()` để Game Designer mắt thấy tai nghe trực tiếp trên Unity Scene View.

---

## II. Leverage Industry Standards & Proven Ecosystem (ĐỨNG TRÊN VAI NGƯỜI KHỔNG LỒ)
1. **Tuyệt đối KHÔNG "chế bánh xe méo":**
   - Khi Unity đã có giải pháp chính thức chuẩn công nghiệp, bắt buộc phải tận dụng thay vì tự viết thuật toán nghiệp dư chắp vá:
     - **AI Pathfinding:** Dùng **Unity NavMesh** (`NavMeshAgent` + `NavMeshObstacle` Carve) thay vì tự code vector sin/cos lách sườn.
     - **Camera:** Dùng **Cinemachine 3.x** (`CinemachineCamera` + `CinemachineTargetGroup` + Damping) thay vì tự viết script lerp vị trí.
     - **Mobile Touch Input:** Dùng **Unity New Input System** (`com.unity.inputsystem` với `OnScreenStick` và `OnScreenButton`) thay vì tự parse pixel chuột/touch.
     - **Tweening & Polish:** Dùng thư viện Tweening chuyên nghiệp (PrimeTween / DOTween) thay vì coroutine cộng dồn `Time.deltaTime`.
     - **Physics & Chain:** Dùng `ConfigurableJoint` / `SpringJoint` hoặc Animation Rigging cho chuyển động liên kết.

---

## III. Prefab-First & Modular Asset Architecture (CẤM TỰ ĐẺ HÌNH HỌC THÔ BẰNG CODE)
1. **Tất cả thực thể game (Mẹ, Con, Thợ Săn, Sân đấu, Item) PHẢI là Prefab hoàn chỉnh** được lưu trữ trong `Assets/_Game/Prefabs/`.
2. **Tuyệt đối CẤM** dùng script C# để tự sinh hình học thô (`GameObject.CreatePrimitive` Sphere, Cube, Cylinder), tự vẽ nón lá hay ghép mesh thủ công.
3. Phụ kiện (Nón lá, vương miện, nơ) phải là Model 3D (`.fbx`) và được gắn vào các điểm neo **Sockets** (`Socket_Head`, `Socket_Hand_R`) của khung xương nhân vật trong Prefab Mode.

---

## IV. Extensible & Decoupled Architecture (TIỀM NĂNG MỞ RỘNG CAO)
1. **Nguyên tắc Đơn Nhiệm (Single Responsibility Principle - SRP):**
   - Lớp Visual chỉ lo Model & Animator.
   - Lớp Motor chỉ lo nhận vector vận tốc và tương tác vật lý.
   - Lớp Brain (Controller / AI) chỉ lo ra quyết định.
   - Lớp GameManager chỉ lo State ván đấu.
2. **Event-Driven Decoupling:**
   - Các hệ thống giao tiếp qua C# `event` / `Action` (Observer Pattern).
   - Khi Con Út bị bắt: Mẹ chỉ phát `OnConUtCaptured`. GameManager, Audio, Camera, UI tự lắng nghe và xử lý độc lập, không tham chiếu chéo (No Spaghetti Dependencies).

---

## V. One Mechanic at a Time & Isolation Testing
1. Phát triển theo từng tầng từ dưới lên (Bottom-Up Core-to-Meta):
   1. Động học Mẹ (Run 360° & Lock-Facing Strafe).
   2. Chuỗi Đàn Con uốn lượn đàn hồi (Spring-Damper Chain).
   3. Thợ Săn NavMesh đuổi theo Con Út & Vung tay chém.
   4. Đấu trường Arena 32m & Camera Cinemachine.
   5. Luật trận đấu 45s & Minigame Chăm Chăm Chăm.
   6. UI Mobile HUD & Đánh bóng trải nghiệm (Juice).
2. Mỗi cơ chế phải được kiểm thử độc lập trong Sandbox/Playground trước khi tích hợp vào Scene Gameplay chính.

---

## VI. Verification Before Handoff (KIỂM CHỨNG TRƯỚC KHI BÀN GIAO)
1. AI Agent tuyệt đối KHÔNG được bảo người dùng "bấm test thử" khi bản thân chưa kiểm chứng:
   - Console Unity phải sạch 100% (0 compiler errors, 0 runtime exceptions).
   - Phải kiểm tra log thực tế trong `Logs/Editor.log`.
   - Phải kiểm tra trạng thái thực tế của các component qua MCP Bridge (`http://127.0.0.1:8090/`).

---

## VII. Performance & Clean Unity 6 Standards
1. Toàn bộ gameplay script nằm gọn trong namespace `RongRanChamCham` và assembly `RongRanChamCham.Runtime.asmdef`.
2. **Zero GC Allocation trong vòng lặp:** Không dùng LINQ, không `new` mảng/list trong `Update()` hoặc `FixedUpdate()`.
3. Tối ưu hóa cho thiết bị di động mục tiêu: Duy trì ổn định 60 FPS.

---

**Version**: 2.0.0 | **Ratified**: 2026-11-08 | **Project**: RongRanChamCham3D (Unity 6.6 LTS)
