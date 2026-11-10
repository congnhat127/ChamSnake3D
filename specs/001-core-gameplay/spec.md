# Feature Specification: Arena Chase, Cham Cham Cham & Folk Food Power-ups

**Feature ID**: `001-core-gameplay`  
**Status**: `Implemented & Verified`  
**Target Platform**: Mobile (Virtual Joystick 360° + Nút Khóa Hướng) + PC (WASD + Space)  

---

## 1. Tầm nhìn & Cốt lõi Gameplay (Core Vision)

Tái hiện trò chơi dân gian **Rồng Rắn Lên Mây** trong sân đấu đấu trường casual:
1. **Rắn Đầu Đàn (Mẹ):** Người chơi điều khiển Mẹ dang rộng sải tay bảo vệ đàn con (3 bé con nối đuôi nhau).
2. **Thầy Thuốc (Thợ Săn):** AI di chuyển cùng tốc độ, tìm cách lách sườn vòng ra sau lưng Mẹ để vồ lấy Con Út ở cuối đuôi.
3. **Màn Đối Đầu "Chăm Chăm Chăm" (1 Hiệp Quyết Định):** 
   - Khi Thầy Thuốc vồ trúng Con Út $\rightarrow$ Màn đối đầu Chăm Chăm Chăm kích hoạt tức thì.
   - Con Út và Thầy Thuốc cùng chọn 1 trong 3 hướng (`Trái`, `Thẳng`, `Phải`).
   - **Rắn Con THẮNG (Nhìn khác hướng tay Thầy Thuốc):** Con được giải cứu! Tổng 3 trái tim bị trừ nhẹ $0.5$ tim (còn $2.5$ tim). Thầy Thuốc bị hất lùi $9\text{m}$ và choáng.
   - **Rắn Con THUA (Nhìn cùng hướng tay Thầy Thuốc hoặc hết giờ):** Con Út bị bắt và **OUT khỏi cuộc chơi**. Chuỗi rồng rắn ngắn lại 1 đốt, bé đứng trước trở thành Con Út mới.
4. **Điều Kiện Thắng / Thua (Win / Loss Condition):**
   - **Chiến Thắng:** Sống sót hết thời gian đếm ngược $60\text{ s}$ mà vẫn bảo vệ được **ít nhất 1 bé con**.
   - **Thua Cuộc:** Thầy Thuốc vồ sạch toàn bộ đàn con, chỉ còn trơ trọi lại rắn đầu đàn (Mẹ).

---

## 2. Hệ Thống 4 Vật Phẩm Dân Gian (Food Power-ups Cho Cả 2 Phe)

Tất cả vật phẩm có thời gian hiệu lực **7 giây** (riêng Bánh Gạo Cay là **5 giây**):

| Vật Phẩm | Tác Dụng Lên Rắn (Mẹ) | Tác Dụng Lên Thầy Thuốc | Tỷ Lệ Xuất Hiện |
| :--- | :--- | :--- | :--- |
| 🥖 **Bánh Mì** | Nhận **Lá chắn đỡ 1 lần vồ**. | **Vồ Xuyên Lá Chắn** (nếu Mẹ có khiên thì phá khiên bắt luôn; nếu không có khiên thì chắc chắn vồ trúng). | Phổ biến ($35\%$) |
| 🍜 **Phở** | Tăng tốc chạy $+35\%$ trong 7s. | Tăng tốc chạy $+35\%$ trong 7s. | Phổ biến ($35\%$) |
| 🍙 **Cơm Cuộn (Kimbap)** | Tăng kích thước $+30\%$ (Sải tay Mẹ dài hơn, che chắn dễ hơn). | Tăng kích thước $+30\%$ (Tầm với vồ rộng hơn). | Trung bình ($20\%$) |
| 🥘 **Bánh Gạo Cay (Tteokbokki)** | Tạo **1 Phân Thân Mẹ** chạy song song trong 5s (Sau 2s mờ dần để nhận biết). | Tạo **1 Phân Thân Thầy Thuốc** cùng đuổi bắt trong 5s (Sau 2s mờ dần để nhận biết). | Cực hiếm ($10\%$) |

---

## 3. Sửa Lỗi Đồ Họa & Logic Nhân Vật (Visual & Anatomy Fixes)
1. **Model Mẹ (`Chibi_Mother.obj`):** Đảo trục Z đúng giải phẫu: Hàng cúc áo bà ba nằm ở ngực trước ($+Z$), hai cánh tay cong vòm hướng về phía trước tạo rào chắn tự nhiên.
2. **Model Bé Con (`Chibi_Child.obj`):** Hai tay vươn ra phía trước ($+Z$) ôm vào eo bạn đi trước đúng động tác rồng rắn dân gian.
3. **Model Thầy Thuốc (`Chibi_Hunter.obj`):** Vạt áo hướng ra trước, tay vồ vươn dài về phía trước.
4. **Vật Phẩm 3D (`Food_Plate_Token.obj`):** Kích thước chuẩn $0.45\text{m}$, xoay tròn lơ lửng nhẹ nhàng $0.12\text{m}$ trên sàn.
