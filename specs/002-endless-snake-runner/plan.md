# Technical Implementation Plan: Endless Snake Runner & "Chăm Chăm Chăm" Revive

**Feature ID**: `002-endless-snake-runner`  
**Spec Reference**: [spec.md](./spec.md)  

---

## 1. Kiến trúc Hệ thống Tổng thể (System Architecture)

```
[Touch / Mouse Drag Input] 
           │ (Normalized Lateral Offset X)
           ▼
[EndlessSnakeLeaderMotor] ─── (Forward Speed + Clamped X Position)
           │
           │ (Transform History & Centrifugal Angular Velocity)
           ▼
[SnakeTailCentrifugalMotor] ─── (Pulls Child 1 -> Child 2 -> Child 3...)
           │
           │ (SphereCollider Va chạm)
           ▼
[ObstacleNode.cs] ─── (OnTriggerEnter với Snake Node)
           │
           ▼
[ChamChamChamReviveManager] ─── (Slow-motion 0.05x + Direction QTE Challenge)
     ├── THẮNG (Look Away) ──> [Revive Buff + Shield 3s] ──> Resume Running
     └── THUA (Match Dir)   ──> [EndlessRunnerGameManager.GameOver] ──> Summary UI
```

---

## 2. Chi tiết các Thành phần Kỹ thuật (Technical Components)

### 2.1. Cấu hình Cân bằng (`EndlessBalanceConfig.cs`)
- Nằm tại: `Assets/_Game/Scripts/Configurations/EndlessBalanceConfig.cs`
- Kế thừa: `ScriptableObject`
- Quản lý tất cả thông số: Tốc độ cơ bản, độ nhạy vuốt, hệ số văng ly tâm, thời gian phản xạ QTE Chăm Chăm Chăm, bán kính đường chạy.

### 2.2. Điều khiển 1 Ngón (`OneFingerRunnerInput.cs`)
- Nằm tại: `Assets/_Game/Scripts/Input/OneFingerRunnerInput.cs`
- Cơ chế:
  - Mobile: Nhận `TouchPhase.Moved` hoặc `TouchPhase.Stationary`, tính delta X kéo theo chiều ngang màn hình.
  - PC / Editor: Nhận kéo chuột trái (Mouse Drag) hoặc phím `A`/`D` / `Mũi tên`.
  - Cung cấp: `float TargetLateralX` (chuẩn hóa từ `-1.0` tới `+1.0`).

### 2.3. Motor Đầu Đàn (`EndlessSnakeLeaderMotor.cs`)
- Nằm tại: `Assets/_Game/Scripts/Entities/EndlessSnakeLeaderMotor.cs`
- Điều khiển nhân vật Đầu Đàn:
  - Vector tiến: Luôn tịnh tiến về trục `+Z` với vận tốc `currentSpeed`.
  - Vector ngang: Nội suy mượt mà trục `X` theo `TargetLateralX * RoadHalfWidth`.
  - Góc xoay người: Hơi nghiêng người (Bank/Tilt) theo hướng đánh lái để tạo cảm giác thể thao và sống động.
  - Tích hợp `ChibiProceduralAnimator` để chân bước nhịp nhàng liên tục theo vận tốc.

### 2.4. Motor Đuôi Rắn Ly Tâm (`SnakeTailCentrifugalMotor.cs`)
- Nằm tại: `Assets/_Game/Scripts/Systems/SnakeTailCentrifugalMotor.cs`
- Thuật toán bám đuôi không cấp phát bộ nhớ (Zero GC):
  - Dùng mảng vòng tròn cố định (`Fixed Circular Buffer`) lưu lịch sử vị trí đường đi của Đầu Đàn.
  - Mỗi đốt con lấy mẫu vị trí trong buffer theo khoảng cách mong muốn ($d = 1.2\text{ m}$).
  - **Lực văng ly tâm:** Khi Đầu Đàn bẻ góc gắt ($\Delta X / \Delta t$ lớn), các đốt con phía sau bị cộng thêm một vector dịch chuyển hướng ra mép cua ($V_{centrifugal} = -\text{Sign}(\text{Turn}) \times \text{Factor} \times \text{Speed}^2$), tạo nên hiệu ứng đuôi rắn vắt qua lại cực kỳ chân thực.
  - Mỗi đốt con có gắn `SphereCollider` (IsTrigger = true) để kiểm tra va chạm.

### 2.5. Hệ thống Sinh Đường Vô Tận & Object Pooling (`TrackChunkSpawner.cs`)
- Nằm tại: `Assets/_Game/Scripts/Environment/TrackChunkSpawner.cs`
- Cấu trúc Chunk:
  - Mỗi `TrackChunk` dài chuẩn $30\text{ m}$ theo trục Z.
  - Sử dụng hàng đợi tuần hoàn (Queue / Pool) gồm 5-6 chunks hoạt động đồng thời.
  - Khi Đầu Đàn vượt qua điểm cuối của Chunk cũ $\rightarrow$ Chunk cũ được tái sử dụng (Reposition) lên đầu con đường.
- **Biomes Văn Hóa:**
  - `Theme_VietnamVillage`: Mặt gạch nung Bát Tràng, bụi tre xanh hai bên, chum vại gốm làm vật cản.
  - `Theme_HoiAn`: Mặt đá rêu phong, dàn hoa giấy, cột treo đèn lồng Hội An đung đưa.
  - `Theme_KoreaPalace`: Lát đá hoa cương cung điện, tường Dancheong, đèn Cheongsachorong xanh đỏ, chum tương Onggi.

### 2.6. Quản lý Cơ Chế Cứu Mạng "Chăm Chăm Chăm" (`ChamChamChamReviveManager.cs`)
- Nằm tại: `Assets/_Game/Scripts/Systems/ChamChamChamReviveManager.cs`
- Luồng hoạt động:
  1. Khi nhận tín hiệu va chạm từ bất kỳ đốt nào:
  2. Bật `Time.timeScale = 0.05f`.
  3. Bật Canvas QTE "Chăm Chăm Chăm!":
     - Hiển thị cánh tay thần hộ mệnh chỉ hướng (Trái / Phải / Lên).
     - Đếm ngược thanh thời gian $1.2\text{ s}$ thực tế.
  4. Người chơi vuốt màn hình sang Trái / Phải / Lên.
  5. Đối chiếu kết quả:
     - Nếu hướng chọn **KHÁC** hướng Thần chỉ $\rightarrow$ **CHIẾN THẮNG**:
       - Khôi phục `Time.timeScale = 1.0f`.
       - Kích hoạt Khiên Bất Tử 3 giây, đẩy lùi chướng ngại vật va chạm, tiếp tục chạy.
     - Nếu hướng chọn **TRÙNG** hướng Thần chỉ hoặc hết giờ $\rightarrow$ **THẤT BẠI**:
       - Kích hoạt Game Over.

### 2.7. Quản lý Điểm & Trạng thái Game (`EndlessRunnerGameManager.cs`)
- Nằm tại: `Assets/_Game/Scripts/Managers/EndlessRunnerGameManager.cs`
- Quản lý vòng đời trận đấu: `Intro`, `Running`, `ReviveQTE`, `GameOver`.
- Tính điểm theo cự ly di chuyển (Mét) và số bánh quà dân gian nhặt được.

---

## 3. Quy tắc Kiểm tra & Nghiệm thu (Verification Strategy)
1. **Zero Error Compilation:** Kiểm tra log Unity qua `unity_get_console_logs` đảm bảo không có bất kỳ warning/error nào.
2. **Playable Sandbox Scene:** Thiết lập `Assets/_Game/Scenes/Playground_EndlessRunner.unity` với đầy đủ camera Cinemachine theo dõi từ trên cao chế độ 3rd person, đường chạy tự động lặp vô tận, đàn con bám đuôi uốn lượn và tính năng Chăm Chăm Chăm hoạt động trực tiếp.
