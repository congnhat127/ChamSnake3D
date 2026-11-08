# Rồng Rắn Chăm Chăm 3D - Project Constitution

## Core Principles

### I. Prefab-First & Modular Asset Architecture (NON-NEGOTIABLE)
- **Tất cả thực thể game (Mẹ, Con, Thợ Săn, Sân đấu, Item) PHẢI là Prefab hoàn chỉnh** được lưu trữ trong `Assets/_Game/Prefabs/`.
- **Tuyệt đối CẤM** dùng script C# để tự động tạo thủ công primitive objects (Capsule, Cylinder), tự xóa Collider hay chắp vá GameObject runtime trong scene.
- Mọi thông số (Speed, Spacing, Colliders, Materials) phải được trực quan hóa trên Inspector của Prefab để dễ dàng cân chỉnh và bảo trì.

### II. One Mechanic at a Time & Isolation Testing
- Phát triển theo thứ tự phân rã, **xong và chạy hoàn hảo cơ chế này mới chuyển sang cơ chế tiếp theo**:
  1. Mother Movement & Free Run 360° / Strafe Mode
  2. Snake Chain IK (Đàn con bám đuôi uốn lượn mượt mà)
  3. Hunter AI (Lượn sườn bắt Con Út, né Mẹ)
  4. 4 Items & 7s Buff System
  5. 3D Cham Cham Cham POV Duel Face-off
- Mỗi cơ chế phải được kiểm thử độc lập trong Sandbox/Playground trước khi tích hợp vào Scene Gameplay chính.

### III. Verification Before Handoff (Cấm bàn giao theo cảm tính)
- **AI Agent tuyệt đối KHÔNG được bảo người dùng "bấm test thử" khi bản thân chưa kiểm chứng**:
  - Console Unity phải sạch 100% (0 compiler errors, 0 runtime exceptions).
  - Phải kiểm tra log thực tế trong `D:/Projects/RongRanChamCham3D/Logs/Editor.log`.
  - Phải kiểm tra trạng thái thực tế của các component qua MCP Bridge (`http://127.0.0.1:8090/`).

### IV. Spec-Driven & Task Checklist Discipline
- Mỗi tính năng phải có tài liệu đặc tả rõ ràng:
  - **Spec**: Định nghĩa rõ input, output, luật chơi, cảm giác điều khiển (Game Feel).
  - **Plan**: Sơ đồ lớp, component, prefab dependencies.
  - **Tasks**: Chia nhỏ thành các task nguyên tử (atomic), có tiêu chí nghiệm thu (Acceptance Criteria) cụ thể.
- Hoàn thành task nào thì đánh dấu [x] task đó kèm bằng chứng kiểm thử.

### V. Performance & Clean Unity Standards
- Mọi gameplay script phải nằm gọn trong namespace `RongRanChamCham` và assembly `RongRanChamCham.Runtime.asmdef` để đảm bảo tốc độ biên dịch < 0.5s.
- Hạn chế tối đa rác bộ nhớ (GC Allocation) trong các vòng lặp `Update` và `FixedUpdate` (không dùng LINQ, không new mảng/danh sách mỗi frame).
- Giữ dự án nhẹ và thuần Unity (Pure Unity 6 + URP), không cài plugin bên ngoài bừa bãi.

## Governance & Quality Gates
- Bản Hiến pháp này là kim chỉ nam tối cao cho toàn bộ quá trình lập trình của dự án.
- Mọi dòng code và asset được thêm vào dự án đều phải tuân thủ nghiêm ngặt 5 nguyên tắc trên.

**Version**: 1.0.0 | **Ratified**: 2026-11-07 | **Project**: RongRanChamCham3D (Unity 6.6 LTS)
