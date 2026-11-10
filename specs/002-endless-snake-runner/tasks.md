# Task List: Endless Snake Runner & "Chăm Chăm Chăm" Revive

**Feature ID**: `002-endless-snake-runner`  
**Spec Reference**: [spec.md](./spec.md) | **Plan Reference**: [plan.md](./plan.md)  

---

## Phase 1: Nền tảng Điều khiển Swerve Drag Follow & Motor Đầu Đàn
- [ ] **Task 1.1: Tạo `EndlessBalanceConfig.cs` và Asset cấu hình**  
  - Tạo ScriptableObject lưu các biến số cân bằng (tốc độ, độ nhạy swerve, giới hạn lòng đường, khoảng cách đốt con, thời gian QTE).  
  - Tạo asset `Assets/_Game/Configurations/EndlessBalanceConfig.asset`.
- [ ] **Task 1.2: Tạo `SwerveInputHandler.cs` (Chuẩn Hyper-Casual Drag Follow)**  
  - Nhận diện chạm giữ ngón tay và kéo ngang (Touch Down & Drag).  
  - Tay di chuyển thế nào, cung cấp delta tương ứng tức thì; tay giữ nguyên vị trí thì nhân vật giữ làn; nhấc tay thì duy trì tọa độ X.  
  - Hỗ trợ chuột và phím A/D / Mũi tên trên PC/Editor.
- [ ] **Task 1.3: Tạo `EndlessSnakeLeaderMotor.cs` (Motor Đầu Đàn)**  
  - Tự động chạy tiến tới theo trục Z với tốc độ cơ bản.  
  - Dịch chuyển ngang mượt mà theo delta từ SwerveInputHandler, kẹp chặt trong giới hạn lòng đường.  
  - Nghiêng người sống động khi cua rẽ và gọi `ChibiProceduralAnimator` để bước chân theo vận tốc.

---

## Phase 2: Đuôi Rắn Quán Tính Ly Tâm & Va chạm (Tail Physics & Collision)
- [ ] **Task 2.1: Tạo `SnakeTailCentrifugalMotor.cs` (Zero-GC Circular Buffer)**  
  - Lưu vết lịch sử di chuyển của Đầu Đàn vào bộ đệm tĩnh vòng tròn (không cấp phát bộ nhớ rác).  
  - Kéo theo chuỗi 3 đến 5 nhân vật con với độ trễ tự nhiên theo khoảng cách chuẩn 1.2m.  
  - Áp dụng lực văng ly tâm: Khi Đầu Đàn đánh lái gắt, các đốt con văng dạt sang hướng đối diện (Whip effect).
- [ ] **Task 2.2: Tạo `TailSegmentTrigger.cs` & `ObstacleNode.cs`**  
  - Gắn Collider cho từng đốt con và chướng ngại vật trên đường.  
  - Bắt sự kiện va chạm từ bất kỳ đốt nào (từ Đầu Đàn tới Con Út) để kích hoạt cơ chế Cứu Mạng.

---

## Phase 3: Sinh Đường Vô Tận & Chủ đề Văn Hóa Việt - Hàn (Chunk Pooling)
- [ ] **Task 3.1: Tạo `TrackChunk.cs` và `TrackChunkSpawner.cs`**  
  - Chia đường thành các đoạn chuẩn 30m, quản lý qua Object Pool (chỉ giữ 4-5 chunk đồng thời).  
  - Tự động thu hồi chunk cũ phía sau và đặt lên phía trước Đầu Đàn khi di chuyển, đảm bảo mượt mà 60 FPS.
- [ ] **Task 3.2: Thiết kế các Modular Chunks Văn Hóa**  
  - **Theme Việt Nam:** Đường gạch Bát Tràng, bụi tre làng, cụm chum sành hoa văn rạn, cổng tam quan, đèn lồng Hội An.  
  - **Theme Hàn Quốc:** Đường lát đá Cung Đình, tường Dancheong, đèn lồng Cheongsachorong xanh đỏ, chum tương Onggi.

---

## Phase 4: Cơ Chế Cứu Mạng "Chăm Chăm Chăm!" (QTE Reflex Revive)
- [ ] **Task 4.1: Tạo `ChamChamChamReviveManager.cs`**  
  - Khi va chạm xảy ra: Bật Bullet Time (`Time.timeScale = 0.05f`).  
  - Thần Hộ Mệnh giơ tay chỉ hướng ngẫu nhiên: Trái (-1), Phải (+1), Lên (0).  
  - Bắt thao tác vuốt phản xạ của người chơi trong 1.2s.  
  - Nếu khác hướng $\rightarrow$ Thắng (Khôi phục game, cấp khiên bất tử 3s, hất văng chướng ngại vật).  
  - Nếu trùng hướng hoặc hết giờ $\rightarrow$ Thua (Game Over).
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
