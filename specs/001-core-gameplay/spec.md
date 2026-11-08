# Feature Specification: Core Gameplay Mechanics (Mẹ, Chuỗi 3 Con, Thợ Săn)

**Feature ID**: `001-core-gameplay`  
**Status**: `Approved by User`  
**Target Platform**: Mobile (App điện thoại - Cảm ứng hai tay)  

---

## 1. Tầm nhìn & Mục tiêu Gameplay (Core Vision)
Mô phỏng trò chơi dân gian **Rồng Rắn Lên Mây** theo phong cách 3D hoạt hình (*Stickman Party*), tối ưu trải nghiệm điều khiển trên **màn hình cảm ứng điện thoại**:
- **Bên trái màn hình:** Virtual Joystick di chuyển tự do 360 độ.
- **Bên phải màn hình:** Nút **"Khóa Hướng Đầu" (Lock-Facing)**: Khi giữ nút này, Mẹ giữ nguyên góc nhìn hiện tại và có thể bước lùi, bước ngang mà không bị quay ngược mặt lại (tránh để lộ đuôi các con ra phía trước Thợ Săn).
- **Đội hình:** Mẹ + Chuỗi 3 con (Con 1, Con 2, Con Út ở cuối đuôi).
- **Cân bằng vận tốc:** Thợ Săn và Mẹ có **cùng tốc độ di chuyển**. Trận đấu là cuộc so tài thuần túy về thuật toán lách sườn của AI và kỹ năng chặn đầu của người chơi.
- **Cơ chế bắt:** Thợ Săn **phải tung đòn vồ/chém (Grab Action)** trúng Con Út mới tính là bắt được (không tính va chạm quẹt vô tình).

---

## 2. Đặc tả chi tiết & Tiêu chí nghiệm thu (Acceptance Criteria)

### User Story 1: Hệ thống điều khiển Mẹ Mobile-First
> **Là một** người chơi trên điện thoại,  
> **Tôi muốn** dùng tay trái điều khiển di chuyển và tay phải giữ nút Khóa Hướng,  
> **Để** linh hoạt bước lùi che chắn Thợ Săn mà không bị quay lưng hở đuôi.

- **AC 1.1 (Virtual Joystick bên trái):** Kéo thả ngón tay ở nửa trái màn hình để Mẹ di chuyển 360° với tốc độ chuẩn `6.5 m/s`. Nhân vật xoay mặt theo hướng di chuyển khi thả nút khóa.
- **AC 1.2 (Nút Khóa Hướng bên phải - Lock-Facing):** Khi người chơi giữ nút này bằng ngón cái phải:
  - Góc quay đầu (Facing Angle) của Mẹ được **khóa cố định tuyệt đối**.
  - Nếu kéo Joystick lùi về sau ➔ Mẹ **bước lùi giật cấp**, mặt vẫn nhìn về phía trước, đuôi các con luôn được giấu kín ở phía sau lưng Mẹ.
  - Nếu kéo Joystick sang trái/phải ➔ Mẹ **bước ngang (Strafe)** cản đường.
- **AC 1.3 (Sải tay cản Thợ Săn):** Khi Thợ Săn chạy va vào mặt trước hoặc sải tay dang rộng (2.8m) của Mẹ, Thợ Săn bị cản lại và trượt theo sải tay, không thể đi xuyên người Mẹ.
- **AC 1.4 (Hỗ trợ PC test):** Trong Editor/PC, hỗ trợ song song phím `WASD` / Mũi tên để đi và phím `SPACE` để kích hoạt Khóa Hướng Đầu.

### User Story 2: Chuỗi 3 Con Rồng Rắn (Snake Chain 3 Nodes)
> **Là một** người chơi,  
> **Tôi muốn** đúng 3 người con nối đuôi mềm dẻo sau lưng Mẹ,  
> **Để** kiểm soát đuôi và tính toán góc văng khi né tránh.

- **AC 2.1 (Số lượng chính xác):** Có đúng 3 con: Con 1 nối sau Mẹ, Con 2 nối sau Con 1, Con Út nối sau Con 2.
- **AC 2.2 (Khoảng cách & Lực đàn hồi):** Khoảng cách tiêu chuẩn giữa các con là `1.3m`. Khi Mẹ di chuyển, các con bám sát theo đường đi với độ trễ tự nhiên (Spring-Damper), không bao giờ bị đơ hay tách rời.
- **AC 2.3 (Nhận diện Con Út):** Con Út có vương miện/nơ đặc biệt và vòng hào quang hồng nhận diện mục tiêu cần bảo vệ.

### User Story 3: Thợ Săn Công Bằng & Đòn Vồ Quyết Định (Fair Hunter AI)
> **Là một** người chơi đối đầu với Thợ Săn,  
> **Tôi muốn** Thợ Săn di chuyển công bằng (cùng tốc độ với Mẹ) và phải tung đòn chém trúng Út,  
> **Để** trận đấu kịch tính dựa trên kỹ năng chứ không bị ức chế vì AI "hack tốc độ".

- **AC 3.1 (Vận tốc công bằng):** Tốc độ tối đa của Thợ Săn bằng đúng tốc độ của Mẹ (`6.5 m/s`).
- **AC 3.2 (Thuật toán lách sườn - Flanking):** Thợ Săn luôn tính vector tới Con Út. Nếu Mẹ chắn đường kính 2.5m, Thợ Săn lượn vòng cung sang mép ngoài để vòng ra sau lưng Mẹ.
- **AC 3.3 (Hành động Vồ/Chém - Grab Action):**
  - Khi Thợ Săn áp sát Con Út ở khoảng cách `< 1.6m`, Thợ Săn dừng di chuyển 0.25s để vung tay tung đòn vồ/chém (Swipe Hitbox).
  - Nếu đòn vung tay trúng Con Út ➔ Bắt thành công, kích hoạt màn đối đầu Chăm Chăm Chăm!
  - Nếu Con Út né thoát khỏi tầm vung tay ➔ Đòn vồ hụt, Thợ Săn bị trễ nhịp 0.5s (cooldown) rồi mới tiếp tục đuổi.

---

## 3. Ranh giới kiến trúc & Quy tắc triển khai
1. **Prefab-First:**
   - `Assets/_Game/Prefabs/Characters/Mother.prefab` (Có visual tay dang rộng, CharacterController, MotherController).
   - `Assets/_Game/Prefabs/Characters/Child.prefab` (Visual sphere, ChildFollower, biến `isConUt`).
   - `Assets/_Game/Prefabs/Characters/Hunter.prefab` (Visual thân tím nón lá, HunterAI, GrabHitbox).
2. **UI Touch Canvas:**
   - Prefab UI cảm ứng: Joystick ảo góc trái dưới, Nút Lock-Facing to tròn góc phải dưới dễ bấm bằng ngón cái.
3. **Scene Test độc lập:**
   - Dựng Scene sạch `Playground_CorePhysics.unity` để kiểm thử chính xác 3 User Stories trên trước khi bàn giao.
