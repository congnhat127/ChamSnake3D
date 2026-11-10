# Feature Specification: Infinite Cultural Snake Runner & "Chăm Chăm Chăm" Revive

**Feature ID**: `002-endless-snake-runner`  
**Status**: `Draft / Ready for Implementation`  
**Target Platform**: Mobile (iOS & Android - Điều khiển vuốt 1 ngón Portrait / One-Finger Drag) + PC fallback (A/D, Arrow keys, Mouse Drag)  

---

## 1. Tầm nhìn & Cốt lõi Gameplay (Core Gameplay Vision)

Chuyển đổi từ mô hình đấu đối kháng trong sân kín sang thể loại **Casual Infinite Runner (Endless Snake Trail Runner)** với nhịp độ cuốn hút và dễ tiếp cận như *Flappy Bird* hay *Subway Surfers*, kết hợp vật lý uốn lượn của trò chơi dân gian **Rồng Rắn Lên Mây** và nét văn hóa giao thoa **Việt - Hàn**:

1. **Nhập vai Đầu Đàn (Snake Leader):**  
   Người chơi điều khiển nhân vật Đầu Đàn (Mẹ / Trưởng đoàn Chibi) chạy tiến về phía trước trên một con đường vô tận uốn lượn. Đằng sau là một chuỗi các bé nối đuôi nhau ôm eo (Đoàn Rồng Rắn).
2. **Cơ chế Điều khiển 1 Ngón (One-Finger Drag / Swipe Steering):**  
   - Nhân vật tự động chạy thẳng về phía trước với vận tốc cơ bản (tăng dần theo khoảng cách).  
   - Người chơi chỉ cần **chạm và vuốt ngón tay sang trái/phải** để điều hướng Đầu Đàn lách qua các chướng ngại vật.
3. **Thử thách Cốt lõi - Lực Quán Tính Quét Đuôi (Centrifugal Tail Physics):**  
   - Không chỉ Đầu Đàn cần né chướng ngại vật; điểm ăn tiền và thử thách lớn nhất là **toàn bộ thân đuôi (các con)** bám theo với quán tính ly tâm.  
   - Khi Đầu Đàn cua gắt, đuôi sẽ bị văng sang phía ngược lại.  
   - **Điều kiện Thua Cuộc (Fail Condition):** Nếu bất kỳ đốt nào trong thân/đuôi va chạm vào chướng ngại vật bên đường (chum sành Bát Tràng, bụi tre làng, tường đá cung đình Hàn Quốc, cột cờ...), chuỗi rồng rắn sẽ bị đứt!
4. **Cơ chế Đột Phá "Chăm Chăm Chăm!" (Second-Chance Revive Challenge):**  
   - Ngay thời khắc va chạm, thời gian kích hoạt hiệu ứng **Slow-Motion (Bullet Time)**.  
   - Vị thần hộ mệnh / Thầy thuốc cổ truyền hiện ra đối diện màn hình hô vang hiệu lệnh dân gian Hàn tộc: **"Chăm... Chăm... CHĂM!"**  
   - Người chơi có đúng 1.2 giây để vuốt màn hình sang Trái, Phải hoặc Lên để quay đầu né hướng chỉ tay của Thần:  
     - **Thắng (Quay khác hướng tay Thần):** Chuỗi rồng rắn được giải thoát với hào quang rực rỡ, chướng ngại vật bị hất tung, ban buff Tăng Tốc Bất Tử trong 3 giây để tiếp tục cuộc chạy!  
     - **Thua (Nhìn cùng hướng tay Thần):** Game Over chính thức, tổng kết quãng đường, điểm số và bánh quà dân gian thu thập được.

---

## 2. Đặc tả Chi tiết & Tiêu chí Nghiệm thu (Acceptance Criteria)

### User Story 1: Điều khiển 1 Ngón Mượt mà & Vận tốc Tiến về Phía trước
> **Là một** người chơi casual di động,  
> **Tôi muốn** dùng 1 ngón tay vuốt ngang nhẹ nhàng để dẫn dắt đoàn rồng rắn uốn lượn,  
> **Để** trải nghiệm game tức thì trong vài giây mà không cần học nút bấm phức tạp.

- **AC 1.1 (Tự động tiến tới):** Nhân vật luôn duy trì vận tốc tiến tới $V_{forward}$ (mặc định $8.0\text{ m/s}$ trong `EndlessBalanceConfig.asset`, tăng dần nhẹ theo thời gian).
- **AC 1.2 (One-Finger Lateral Drag):** 
  - Chạm và kéo ngón tay trên màn hình để dịch chuyển Đầu Đàn theo trục ngang (X-axis).
  - Tốc độ phản hồi nhạy, mượt mà, giới hạn trong biên độ mặt đường ($X_{min}$ tới $X_{max}$).
  - Fallback Editor/PC: Nhận phím `A`/`D`, `Mũi tên`, hoặc giữ chuột trái kéo.

### User Story 2: Vật lý Đuôi Rắn Ly Tâm (Snake Chain Inertia & Collision)
> **Là một** người chơi,  
> **Tôi muốn** cảm nhận rõ ràng độ dẻo và lực văng của đàn con khi ôm cua,  
> **Để** căn thời gian bẻ lái khéo léo sao cho đuôi không bị quẹt vào chướng ngại vật.

- **AC 2.1 (Chain Dynamics):** Các đốt con ($N = 3$ đến $5$ con) nối đuôi nhau với khoảng cách chuẩn $1.2\text{ m}$. Đốt sau bám theo vị trí đốt trước với độ trễ quán tính (Elastic Follow + Centrifugal Drift).
- **AC 2.2 (Tail Hitbox & Detection):** Mỗi đốt con sở hữu một Collider hình cầu độc lập (`SphereCollider`). 
- **AC 2.3 (Collision Trigger):** Khi bất kỳ đốt nào (từ Đầu Đàn tới Con Út) va chạm chướng ngại vật có Layer `Obstacle`, game lập tức kích hoạt trạng thái **Crash Interception**.

### User Story 3: Cơ chế Cứu Mạng "Chăm Chăm Chăm!" (Revive Reflex Mini-Game)
> **Là một** người chơi vừa bị va quẹt,  
> **Tôi muốn** một cơ hội phản xạ chớp nhoáng theo trò chơi "Chăm Chăm Chăm" để hồi sinh và tiếp tục chuỗi điểm,  
> **Để** tạo cảm giác gay cấn tột độ ("suýt chết mà sống lại").

- **AC 3.1 (Slow-Motion Transition):** Khi xảy ra va chạm, `Time.timeScale` giảm xuống $0.05$ trong vòng $0.2\text{ s}$ thực tế, camera zoom cận cảnh Đầu Đàn và Thần Hộ Mệnh.
- **AC 3.2 (QTE Gesture Match):**
  - Giao diện hiện bàn tay Thần chỉ ngẫu nhiên: `Trái` (-1), `Phải` (+1), `Lên` (0).
  - Người chơi vuốt ngón tay chọn 1 trong 3 hướng trong cửa sổ thời gian $1.2\text{ s}$.
  - **Quy tắc Thắng:** Hướng người chơi chọn **KHÁC** hướng Thần chỉ (đúng luật Cham Cham Cham truyền thống: tránh nhìn theo ngón tay đối thủ).
- **AC 3.3 (Hồi sinh & Buff Bất Tử):**
  - Nếu Thắng: Kích hoạt hiệu ứng âm thanh reo hò, xua tan chướng ngại vật trong bán kính $5\text{ m}$, cấp khiên `Invulnerable` và tăng $30\%$ tốc độ trong $3.0\text{ s}$.
  - Nếu Thua: Đoàn rồng rắn ngã lăn, màn hình hiển thị bảng Game Over và nút Chơi Lại / Xem Kỷ Lục.
- **AC 3.4 (Giới hạn hồi sinh):** Mỗi lượt chạy chỉ được kích hoạt Chăm Chăm Chăm miễn phí 1 lần (các lần sau cần vật phẩm Bùa Hộ Mệnh hoặc xem video hồi sinh).

### User Story 4: Sinh Đường Vô Tận & Giao Thoa Văn Hóa Việt - Hàn
> **Là một** người chơi thưởng thức đồ họa,  
> **Tôi muốn** chạy qua các cung đường mang đậm bản sắc văn hóa Việt Nam và Hàn Quốc biến đổi liên tục,  
> **Để** không cảm thấy nhàm chán khi chơi đường dài.

- **AC 4.1 (Chunk Pooling System):** Mặt đường được chia thành các phân đoạn (Track Chunk) dài $30\text{ m}$. Luôn có $4$ chunk hoạt động đồng thời (tổng tầm nhìn $120\text{ m}$). Khi người chơi đi qua, chunk cũ được đưa về Object Pool và chunk mới được sinh ra ở phía trước.
- **AC 4.2 (Chủ đề Văn hóa Đan xen):**
  - **Theme Việt Nam:** Đường gạch Bát Tràng viền tre làng, chướng ngại vật là Chum Gốm Rạn, Cổng Làng Tam Quan, Xe Kéo Cổ, Đèn Lồng Hội An.
  - **Theme Hàn Quốc:** Đường lát đá Cung Đình Gyeongbokgung, chướng ngại vật là Chum Muối Kim Chi (Onggi), Tường Đá Dancheong, Cột Đèn Lồng Cheongsachorong xanh đỏ, Cổng Hanok.
- **AC 4.3 (Vật phẩm Thu thập Dân gian):** Rải rác trên đường là Bánh Chưng vuông nhỏ (Việt) và Bánh Gạo Songpyeon hình bán nguyệt (Hàn) tích lũy điểm thưởng và tiền tệ nâng cấp.

---

## 3. Cấu hình Tham số Cân bằng (ScriptableObject Parameters)

Tất cả thông số KHÔNG ĐƯỢC HARDCODE, lưu tại:  
`Assets/_Game/Configurations/EndlessBalanceConfig.asset`

| Tham số | Giá trị Mặc định | Ý nghĩa |
|---|---|---|
| `BaseForwardSpeed` | `8.0 m/s` | Vận tốc chạy thẳng ban đầu |
| `SpeedIncrementPerKm` | `0.5 m/s` | Gia tốc tăng dần theo mỗi km đường |
| `LateralSensitivity` | `14.0` | Độ nhạy vuốt ngang sang hai bên |
| `RoadHalfWidth` | `4.2 m` | Giới hạn nửa độ rộng con đường |
| `NodeFollowDamping` | `12.0` | Độ bám mượt mà của các đốt con sau Đầu Đàn |
| `CentrifugalFactor` | `0.35` | Hệ số văng ly tâm khi cua gắt |
| `ChamChamTimeWindow` | `1.2 s` | Thời gian người chơi được vuốt QTE |
| `ReviveShieldDuration`| `3.0 s` | Thời gian khiên bất tử sau khi hồi sinh |
