# Task List: Endless Snake Runner & "Chăm Chăm Chăm" Revive

**Feature ID**: `002-endless-snake-runner`  
**Spec Reference**: [spec.md](./spec.md) | **Plan Reference**: [plan.md](./plan.md)  

---

## Phase 1: Nền tảng Điều khiển & Cấu hình (Foundation & Controls)
- [ ] **Task 1.1: Tạo `EndlessBalanceConfig.cs` và Asset cấu hình**  
  - Tạo ScriptableObject lưu các tham số: Tốc độ tiến cơ bản, độ nhạy vuốt ngang, giới hạn lòng đường, khoảng cách đốt con, thời gian QTE Chăm Chăm Chăm.  
  - Tạo asset `Assets/_Game/Configurations/EndlessBalanceConfig.asset`.
- [ ] **Task 1.2: Tạo `OneFingerRunnerInput.cs`**  
  - Hỗ trợ vuốt 1 ngón trên màn hình cảm ứng điện thoại (Touch Drag).  
  - Hỗ trợ Fallback trên PC/Editor (Kéo chuột trái hoặc phím A/D/Mũi tên).
- [ ] **Task 1.3: Tạo `EndlessSnakeLeaderMotor.cs`**  
  - Tự động chạy tiến tới theo trục Z với vận tốc `currentSpeed`.  
  - Dịch chuyển mượt sang hai bên theo input vuốt ngang, kẹp chặt trong biên độ lòng đường.  
  - Nghiêng người sống động khi cua rẽ và kích hoạt `ChibiProceduralAnimator`.

---

## Phase 2: Đuôi Rắn Quán Tính Ly Tâm & Va chạm (Tail Physics & Collision)
- [ ] **Task 2.1: Tạo `SnakeTailCentrifugalMotor.cs` (Zero-GC Circular Buffer)**  
  - Lưu vết lịch sử di chuyển của Đầu Đàn vào bộ đệm tĩnh vòng tròn (không cấp phát Garbage Collection).  
  - Kéo theo chuỗi 3 đến 5 nhân vật con với độ trễ tự nhiên.  
  - Tính toán lực văng ly tâm: Khi Đầu Đàn đánh lái gấp, đuôi vắt mạnh sang hướng đối diện.
- [ ] **Task 2.2: Tạo `ObstacleNode.cs` và Cơ chế Nhận diện Va chạm Đuôi**  
  - Gắn Collider cho từng đốt con và chướng ngại vật.  
  - Bắt sự kiện va chạm từ bất kỳ đốt nào để gửi thông điệp kích hoạt cứu mạng.

---

## Phase 3: Sinh Đường Vô Tận & Chủ đề Văn Hóa Việt - Hàn (Chunk Pooling)
- [ ] **Task 3.1: Tạo `TrackChunk.cs` và `TrackChunkSpawner.cs`**  
  - Chia đường thành các đoạn 30m, quản lý qua Object Pool (chỉ giữ 4-5 chunk đồng thời).  
  - Tự động thu hồi chunk cũ phía sau và đặt lên phía trước Đầu Đàn khi di chuyển.
- [ ] **Task 3.2: Thiết kế các Modular Chunks Văn Hóa**  
  - **Theme Việt Nam:** Đường gạch Bát Tràng, bụi tre làng, chum vại sành, đèn lồng Hội An.  
  - **Theme Hàn Quốc:** Đường lát đá Cung Đình, tường Dancheong, đèn lồng Cheongsachorong, chum tương Onggi.

---

## Phase 4: Cơ Chế Cứu Mạng "Chăm Chăm Chăm!" (QTE Reflex Revive)
- [ ] **Task 4.1: Tạo `ChamChamChamReviveManager.cs`**  
  - Khi va chạm xảy ra: Bật Bullet Time (`Time.timeScale = 0.05f`).  
  - Đưa ra tín hiệu Thần Hộ Mệnh chỉ hướng ngẫu nhiên (Trái / Phải / Lên).  
  - Bắt thao tác vuốt phản xạ của người chơi trong 1.2s.  
  - Nếu khác hướng $\rightarrow$ Thắng (Khôi phục game, cấp khiên bất tử 3s, hất văng chướng ngại vật).  
  - Nếu trùng hướng / hết giờ $\rightarrow$ Thua (Game Over).
- [ ] **Task 4.2: Thiết kế Giao diện QTE Chăm Chăm Chăm**  
  - Hiệu ứng chữ "Chăm... Chăm... CHĂM!" xuất hiện nổi bật.  
  - Mũi tên hướng dẫn và thanh thời gian đếm ngược trực quan.

---

## Phase 5: Tích hợp Scene & Nghiệm thu Hoàn chỉnh
- [ ] **Task 5.1: Thiết lập Scene `Playground_EndlessRunner.unity`**  
  - Thiết lập Camera Cinemachine 3.x bám theo Đầu Đàn từ góc nhìn thứ 3 chéo từ trên xuống.  
  - Đặt Đầu Đàn + Đàn Con vào vạch xuất phát, kết nối Spawner sinh đường tự động.
- [ ] **Task 5.2: Kiểm tra Độc lập & Nghiệm thu (Verification)**  
  - Đảm bảo Console Unity 0 lỗi, 0 exception.  
  - Chạy thử nghiệm mượt mà trên môi trường Editor.
