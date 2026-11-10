# Technical Implementation Plan: Endless Snake Runner & "Chăm Chăm Chăm" Revive

**Feature ID**: `002-endless-snake-runner`  
**Spec Reference**: [spec.md](./spec.md)  

---

## 1. Sơ đồ Luồng Điều khiển Swerve Drag Follow & Xử lý Trận đấu

```
[Màn hình Cảm ứng Mobile / Chuột PC]
      │
      │ (Touch Down / Drag Delta / Touch Up)
      ▼
[SwerveInputHandler.cs] ─────────────> float NormalizedDeltaX
      │
      ▼
[EndlessSnakeLeaderMotor.cs] ────────> Vận tốc Z (Forward) + Tọa độ X (Swerve Clamped)
      │                                     │
      │ (Lưu vị trí vào Circular Buffer)    ▼ (Cập nhật bước chân thủ tục)
      ▼                                [ChibiProceduralAnimator.cs]
[SnakeTailCentrifugalMotor.cs] ──────> Đàn con bám đuôi + Tính toán độ văng ly tâm (Whip)
      │
      │ (Từng đốt mang SphereCollider)
      ▼
[ObstacleNode.cs] ───────────────────> Phát hiện va chạm (OnTriggerEnter)
      │
      ▼
[ChamChamChamReviveManager.cs] ──────> Kích hoạt Bullet Time 0.05x + UI QTE
      ├── THẮNG (Khác hướng Thần chỉ) ──> Khiên Bất Tử 3s + Đẩy văng cản ──> Tiếp tục chạy!
      └── THUA (Trùng hướng Thần chỉ)   ──> [EndlessRunnerGameManager.GameOver]
```

---

## 2. Thiết kế Kỹ thuật Từng Thành phần (Zero-GC, Mobile-First)

### 2.1. `EndlessBalanceConfig.cs` (ScriptableObject)
- Đường dẫn: `Assets/_Game/Scripts/Configurations/EndlessBalanceConfig.cs`
- Lưu trữ mọi tham số cân bằng: Tốc độ cơ bản, độ nhạy Swerve, khoảng cách đốt con, lực ly tâm, thời gian QTE. Designer có thể tinh chỉnh trực tiếp trong Unity Inspector mà không cần chạm vào code.

### 2.2. `SwerveInputHandler.cs` (Bộ Điều Khiển Rê Tay Swerve)
- Đường dẫn: `Assets/_Game/Scripts/Input/SwerveInputHandler.cs`
- Logic hoạt động:
  - Khi bắt đầu chạm (`TouchPhase.Began` hoặc `Input.GetMouseButtonDown(0)`): Ghi nhận `lastTouchPos = currentTouchPos`.
  - Trong quá trình chạm giữ (`TouchPhase.Moved` hoặc `Input.GetMouseButton(0)`):
    - `deltaX = (currentTouchPos.x - lastTouchPos.x) / Screen.width`.
    - `lastTouchPos = currentTouchPos`.
    - Cung cấp giá trị `MoveDeltaX = deltaX * swerveSensitivity`.
  - Nếu ngón tay đứng yên (`TouchPhase.Stationary`): `MoveDeltaX = 0`.
  - Khi nhấc ngón tay (`TouchPhase.Ended`): `MoveDeltaX = 0`, nhân vật giữ nguyên làn hiện tại.
  - PC Fallback: Nhấn giữ `A`/`D` hoặc `Mũi tên` sẽ cung cấp delta tương đương.

### 2.3. `EndlessSnakeLeaderMotor.cs` (Motor Đầu Đàn)
- Đường dẫn: `Assets/_Game/Scripts/Entities/EndlessSnakeLeaderMotor.cs`
- Điều khiển:
  - Tịnh tiến về phía trước: `transform.position += Vector3.forward * (currentSpeed * Time.deltaTime)`.
  - Điều hướng ngang: `currentX = Mathf.Clamp(currentX + MoveDeltaX * roadHalfWidth, -roadHalfWidth, roadHalfWidth)`.
  - Góc nghiêng thân (Roll/Tilt): Nghiêng nhẹ theo vận tốc ngang để tạo cảm giác đánh lái linh hoạt và chân thực.
  - Gọi `ChibiProceduralAnimator` để đánh chân và lắc đầu theo vận tốc chạy.

### 2.4. `SnakeTailCentrifugalMotor.cs` (Bộ Đệm Vòng Tròn & Vật Lý Quán Tính)
- Đường dẫn: `Assets/_Game/Scripts/Systems/SnakeTailCentrifugalMotor.cs`
- **Zero GC Circular Buffer:**
  - Cấp phát mảng tĩnh cố định `Vector3[] positionHistory = new Vector3[500]` và `Vector3[] rotationHistory`.
  - Ghi nhận vị trí Đầu Đàn mỗi FixedUpdate/Update mà không sinh bất kỳ rác bộ nhớ nào (No Heap Allocations).
- **Thuật toán Bám Đuôi & Quán Tính Ly Tâm:**
  - Mỗi đốt con $i$ lấy mẫu vị trí trong buffer lùi lại một quãng đường $i \times 1.2\text{ m}$.
  - Tính vận tốc ngang tức thời của đốt trước $\rightarrow$ Áp dụng offset ly tâm ngược chiều:
    $$\Delta X_{whip} = -\text{Sign}(\text{AngularVelocity}) \times \text{Factor} \times \text{Speed}$$
  - Mỗi đốt con gắn `SphereCollider` (IsTrigger = true) và script `TailSegmentTrigger.cs`.

### 2.5. `TrackChunkSpawner.cs` (Sinh Đường Vô Tận & Object Pooling)
- Đường dẫn: `Assets/_Game/Scripts/Environment/TrackChunkSpawner.cs`
- Cơ chế quản lý:
  - Giữ danh sách xoay vòng gồm 4 chunk $30\text{ m}$ (Tổng chiều dài nhìn thấy $120\text{ m}$).
  - Khi Đầu Đàn đi qua chunk đầu tiên $\rightarrow$ Thu hồi chunk cũ ở sau lưng và chuyển dịch tọa độ lên phía trước đầu đoàn.
- **Biomes Văn Hóa Được Tích Hợp Sẵn:**
  - Prefab `Chunk_Vietnam_Village`: Lát gạch Bát Tràng, bụi tre hai bên, chướng ngại vật là cụm chum sành hoa văn rạn và đống rơm vàng.
  - Prefab `Chunk_Korea_Palace`: Lát đá hoa cương, tường Dancheong ngũ sắc, chướng ngại vật là chum tương Onggi và cột đèn Cheongsachorong.
  - Prefab `Chunk_HoiAn_Lantern`: Lát đá cổ, cột treo đèn lồng Hội An đung đưa.

### 2.6. `ChamChamChamReviveManager.cs` (Cơ Chế Cứu Mạng Phản Xạ)
- Đường dẫn: `Assets/_Game/Scripts/Systems/ChamChamChamReviveManager.cs`
- Quy trình kích hoạt:
  1. Khi nhận sự kiện va chạm từ `TailSegmentTrigger`:
  2. Bật Bullet Time (`Time.timeScale = 0.05f`).
  3. Bật Canvas QTE "Chăm Chăm Chăm!":
     - Hiệu ứng âm thanh / chữ hô: *"Chăm... Chăm... CHĂM!"*
     - Thần Hộ Mệnh giơ tay chỉ ngẫu nhiên: `Trái` (-1), `Phải` (+1), `Lên` (0).
     - Đếm ngược thanh thời gian $1.2\text{ s}$ thực tế.
  4. Người chơi vuốt phản xạ:
     - **Thắng (Khác hướng Thần chỉ):** Trả `Time.timeScale = 1.0f`, nổ hiệu ứng hào quang, kích hoạt khiên bất tử $3.0\text{ s}$, hất văng vật cản gây tai nạn.
     - **Thua (Trùng hướng Thần chỉ hoặc hết giờ):** Kích hoạt trạng thái `GameOver`.

---

## 3. Quy chuẩn Kiểm thử & Nghiệm thu (Verification Strategy)
1. **Kiểm tra Biên dịch Unity:** Xác nhận `Editor.log` hoàn toàn sạch sẽ, không có bất kỳ CS error hay unhandled exception nào.
2. **Playground Scene:** Thiết lập hoàn chỉnh `Playground_EndlessRunner.unity` với Cinemachine 3.x bám theo Đầu Đàn, đường chạy vô tận cuộn mượt mà và cơ chế Chăm Chăm Chăm thử nghiệm trực tiếp được.
