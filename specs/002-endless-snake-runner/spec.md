# Feature Specification: Infinite Cultural Snake Runner & "Chăm Chăm Chăm" Revive

**Feature ID**: `002-endless-snake-runner`  
**Status**: `Approved / In Progress`  
**Target Platform**: Mobile (Touch Swerve Drag Follow 1 ngón) + PC Fallback (Mouse Drag & Phím A/D)  

---

## 1. Tầm nhìn & Cốt lõi Gameplay (Core Vision)

Tựa game tái hiện trò chơi dân gian **Rồng Rắn Lên Mây** và nét văn hóa giao thoa **Việt - Hàn** dưới hình thức **Casual Infinite Runner (Endless Snake Trail Runner)** với nhịp độ cuốn hút, dễ hiểu nhưng đầy thử thách kỹ năng:

1. **Nhập vai Đầu Đàn (Snake Leader):**  
   Người chơi điều khiển nhân vật Đầu Đàn (Mẹ Chibi) liên tục chạy tiến về phía trước trên con đường vô tận. Phía sau là đàn con nối đuôi nhau bám chặt (Đoàn Rồng Rắn).
2. **Cơ chế Điều Khiển Swerve / Floating Drag Follow (Chuẩn Hyper-Casual):**  
   - **Chạm & Giữ ngón tay (Touch Down & Hold):** Người chơi đặt ngón tay vào bất kỳ điểm nào trên vùng điều khiển.  
   - **Tay rê thế nào, nhân vật dịch chuyển thế đấy (1:1 Drag Follow):**  
     - Giữ và rê ngón tay sang trái $\rightarrow$ Đầu Đàn lướt mượt sang trái theo đúng cự ly tay di chuyển.  
     - Giữ và rê ngón tay sang phải $\rightarrow$ Đầu Đàn lướt sang phải tức thì.  
     - Giữ nguyên ngón tay đứng yên một chỗ $\rightarrow$ Nhân vật giữ nguyên tọa độ làn đó và tiếp tục phi thẳng về phía trước.  
     - Nhấc tay ra (Touch Up) $\rightarrow$ Nhân vật duy trì vị trí làn hiện tại, không bị giật về tim đường.
3. **Thử thách Cốt lõi - Đuôi Rắn Ly Tâm (Centrifugal Tail Physics):**  
   - Người chơi không chỉ lái Đầu Đàn tránh chướng ngại vật; điểm độc đáo là **toàn bộ thân đuôi (các con)** bám theo với quán tính ly tâm (Whip effect).  
   - Khi Đầu Đàn đánh lái gắt, đuôi các con văng dạt sang hướng đối diện.  
   - **Điều kiện Thua Cuộc (Fail Condition):** Nếu bất kỳ đốt nào trong thân/đuôi va chạm vào chướng ngại vật bên đường (chum sành Bát Tràng, bụi tre làng, tường đá cung đình Hàn Quốc, cột cờ...), chuỗi rồng rắn sẽ gặp nạn!
4. **Cơ chế Cứu Mạng Thứ 2 "Chăm Chăm Chăm!" (Second-Chance Reflex Challenge):**  
   - Ngay thời khắc va chạm, kích hoạt hiệu ứng **Slow-Motion (Bullet Time 0.05x)**.  
   - Thần Hộ Mệnh / Thầy Thuốc hiện ra hô vang: *"Chăm... Chăm... CHĂM!"*  
   - Người chơi có đúng $1.2\text{ s}$ thực tế để vuốt nhanh sang Trái, Phải hoặc Lên để né hướng chỉ tay của Thần:  
     - **Thắng (Quay khác hướng Thần chỉ - Đúng luật Cham Cham Cham):** Chuỗi rồng rắn bừng sáng hào quang, chướng ngại vật bị hất văng, nhận buff Tăng Tốc & Bất Tử trong $3.0\text{ s}$ để tiếp tục hành trình!  
     - **Thua (Nhìn cùng hướng Thần chỉ hoặc hết giờ):** Game Over chính thức, tổng kết quãng đường chạy được và bánh quà dân gian thu thập được.

---

## 2. Tiêu chí Nghiệm thu Chi tiết (Acceptance Criteria)

### User Story 1: Bộ Điều Khiển Swerve Drag Follow Trực Quan
> **Là một** người chơi casual trên điện thoại,  
> **Tôi muốn** chạm giữ và rê tay sang trái/phải để nhân vật di chuyển tương ứng theo ngón tay,  
> **Để** làm chủ đường chạy ngay từ giây đầu tiên mà không cần suy nghĩ về nút bấm.

- **AC 1.1 (Chạm giữ và rê tay):** Khi ngón tay chạm xuống và kéo ngang, tọa độ ngang (X) của Đầu Đàn thay đổi tương ứng theo cự ly kéo ngón tay nhân với độ nhạy `sensitivity`.
- **AC 1.2 (Giữ yên ngón tay):** Khi ngón tay vẫn chạm giữ nhưng không di chuyển, Đầu Đàn giữ nguyên vị trí X hiện tại và tiếp tục tiến tới theo trục Z.
- **AC 1.3 (Nhấc ngón tay):** Khi nhấc ngón tay, Đầu Đàn duy trì làn hiện tại, không bị hồi về giữa.
- **AC 1.4 (Giới hạn lòng đường):** Tọa độ X được kẹp chặt (Clamped) trong phạm vi $[-RoadHalfWidth, +RoadHalfWidth]$ (mặc định $\pm 4.2\text{ m}$).
- **AC 1.5 (PC / Editor Fallback):** Nhấn giữ chuột trái và rê chuột, hoặc bấm phím `A`/`D` / `Mũi tên`.

### User Story 2: Đuôi Rắn Quán Tính Ly Tâm & Nhận diện Va chạm
> **Là một** người chơi,  
> **Tôi muốn** đàn con bám sau lưng uốn lượn và văng đuôi khi tôi bẻ lái,  
> **Để** tạo ra thử thách tính toán khoảng cách né chướng ngại vật cho cả đàn.

- **AC 2.1 (Zero-GC Circular History Buffer):** Vị trí của Đầu Đàn được ghi nhận liên tục vào bộ đệm tĩnh. Các đốt con lấy mẫu vị trí với khoảng cách cố định $1.2\text{ m}$.
- **AC 2.2 (Lực văng quán tính):** Khi vận tốc ngang của Đầu Đàn thay đổi đột ngột ($\Delta X / \Delta t$), các đốt con bị tác động thêm một lực văng ly tâm lệch hướng để tạo hiệu ứng quất đuôi (Tail Whip).
- **AC 2.3 (Hitbox va chạm):** Mỗi đốt con sở hữu một `SphereCollider` độc lập. Va chạm với bất kỳ vật cản nào có Layer `Obstacle` đều kích hoạt sự kiện `OnSnakeCollision`.

### User Story 3: Cơ chế Cứu Mạng "Chăm Chăm Chăm!"
> **Là một** người chơi vừa bị va quẹt vật cản,  
> **Tôi muốn** một cơ hội phản xạ chớp nhoáng theo trò chơi "Chăm Chăm Chăm" để hồi sinh và tiếp tục chuỗi điểm,  
> **Để** vỡ òa cảm xúc và có động lực bứt phá kỷ lục.

- **AC 3.1 (Slow-Motion 0.05x):** Khi va chạm xảy ra, game giảm `Time.timeScale` xuống $0.05$ trong $0.2\text{ s}$ thực tế, camera cận cảnh vào vị trí đối đầu.
- **AC 3.2 (QTE Nhận diện Vuốt):** Bắt cử chỉ vuốt của người chơi trong $1.2\text{ s}$ thực tế (Trái, Phải, Lên).
- **AC 3.3 (Quy tắc Thắng / Thua):** 
  - Nếu hướng vuốt $\neq$ hướng Thần chỉ: Chiến Thắng $\rightarrow$ Bật khiên bất tử $3.0\text{ s}$, hất văng vật cản, trả `Time.timeScale = 1.0f`, tiếp tục chạy.
  - Nếu hướng vuốt $=$ hướng Thần chỉ hoặc hết giờ: Thất Bại $\rightarrow$ Kích hoạt Game Over.
- **AC 3.4 (Giới hạn lượt cứu):** Mỗi lượt chạy chỉ được kích hoạt Chăm Chăm Chăm miễn phí 1 lần.

### User Story 4: Sinh Đường Vô Tận & Bản Sắc Văn Hóa Việt - Hàn
> **Là một** người chơi,  
> **Tôi muốn** chạy qua các phân đoạn đường mang đậm nét văn hóa Việt Nam và Hàn Quốc đổi mới liên tục,  
> **Để** trải nghiệm thị giác luôn tươi mới và hấp dẫn.

- **AC 4.1 (Object Pooling 30m Chunks):** Đường chạy chia thành các chunk $30\text{ m}$, duy trì $4$ chunk đồng thời trên scene. Tái sử dụng chunk cũ phía sau đưa lên phía trước một cách liền mạch, không drop FPS.
- **AC 4.2 (Chủ đề Làng Quê Việt Nam):** Đường lát gạch nung Bát Tràng, bụi tre làng, cụm chum sành hoa văn rạn, cổng đình, đèn lồng Hội An.
- **AC 4.3 (Chủ đề Cung Đình Hàn Quốc):** Đường lát đá hoa cương cung điện, tường hoa văn Dancheong, đèn lồng Cheongsachorong đỏ-xanh, chum muối tương Onggi.

---

## 3. Bảng Cấu hình Cân bằng (`EndlessBalanceConfig.asset`)

| Biến số | Kiểu dữ liệu | Giá trị Mặc định | Mục đích |
|---|---|---|---|
| `baseForwardSpeed` | `float` | `8.0` (m/s) | Vận tốc chạy thẳng ban đầu |
| `speedGainPerKm` | `float` | `0.4` (m/s) | Mức tăng tốc độ sau mỗi 1000m |
| `swerveSensitivity` | `float` | `1.8` | Độ nhạy rê tay điều hướng ngang |
| `roadHalfWidth` | `float` | `4.2` (m) | Giới hạn nửa độ rộng mặt đường |
| `tailSpacing` | `float` | `1.2` (m) | Khoảng cách tiêu chuẩn giữa các đốt con |
| `centrifugalWhipPower` | `float` | `0.35` | Cường độ văng ly tâm khi bẻ lái gấp |
| `chamChamTimeLimit` | `float` | `1.2` (s) | Thời gian phản xạ QTE Chăm Chăm Chăm |
| `reviveShieldDuration` | `float` | `3.0` (s) | Thời gian khiên bất tử sau khi hồi sinh |
