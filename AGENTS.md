# Rồng Rắn Chăm Chăm 3D — Quy trình & nguyên tắc làm việc

Tài liệu này dành cho mọi người và mọi AI agent làm việc trên dự án. Mỗi quy tắc đều kèm lý do, để khi gặp tình huống chưa được nêu thì suy luận theo lý do đó.

Hai điều quan trọng nhất:

1. **Báo cáo trung thực.** "Đã sửa code, chưa playtest được vì Editor không mở" là một báo cáo hợp lệ. Báo "đã đạt" khi chưa đo là không hợp lệ.
2. **Một nguồn sự thật cho mỗi thứ.** Số liệu nằm ở config, luật chơi nằm ở tài liệu này, phạm vi công việc nằm ở `specs/`. Không chép lại ở nơi thứ hai.

---

## 1. Dự án đang ở đâu

Một game được làm theo các giai đoạn, mỗi giai đoạn trả lời một câu hỏi khác nhau:

| Giai đoạn | Câu hỏi cần trả lời | Xong khi |
|---|---|---|
| Prototype | Trò này có vui không? | Một vòng chơi chạy được, có người chơi thử và muốn chơi lại |
| Vertical slice | Một màn hoàn chỉnh trông và chơi ra sao? | Một màn đạt chất lượng phát hành: luật, UI, âm thanh, hoạt cảnh |
| Production | Làm đủ nội dung | Mọi màn đạt mức của vertical slice |
| Alpha → Beta | Còn lỗi gì, cân bằng đã ổn chưa? | Đủ tính năng (alpha), đủ nội dung và hết lỗi chặn (beta) |
| Release | Phát hành và theo dõi | Bản build chạy ổn trên thiết bị mục tiêu |

**Hiện tại: cuối Prototype, đang vào Vertical slice của Màn 1.** Hệ quả:

- Ưu tiên làm cho Màn 1 đúng luật và chơi sướng tay, trước khi mở rộng Màn 2–5.
- Chưa tối ưu sớm và chưa dựng hệ thống lớn cho nội dung chưa tồn tại.

**Việc cần chủ dự án chốt** (agent không tự quyết):

- Bản game chính là **bản master**: scene `Gameplay.unity` và `Man1–5.unity`, do `Assets/Editor/SetupNewRongRanMaster.cs` dựng, dùng logic trong `Assets/_Game/Scripts` với model và bối cảnh trong `Assets/RongRan/Models`. Script `Assets/RongRan/Scripts/RongRanGame.cs` (bản chơi thử đọc JSON, có biên chữ nhật bo góc và vật cản) không được scene nào trong build dùng. Cần chốt: chuyển phần biên/vật cản theo JSON của nó sang `_Game` rồi bỏ, hay bỏ hẳn. Cho tới khi chốt, không xoá.
- Spec `002-endless-snake-runner` còn hiệu lực hay bỏ. Spec đang hoạt động là `specs/001-core-gameplay`.
- `.specify/memory/constitution.md` yêu cầu NavMesh, Cinemachine, Input System và ghi sân 32 m, trận 45 giây; code hiện dùng steering tự viết, `CameraFollow`, parse touch thủ công, sân elip và trận 60 giây. Cần chốt là cập nhật constitution theo code hay đưa code về constitution. Cho tới khi chốt: tài liệu này quyết định luật chơi và quy trình; constitution quyết định lựa chọn công nghệ cho code **mới**, không tự ý viết lại hệ thống đang chạy.

---

## 2. Vòng làm việc cho một thay đổi

1. **Hiểu việc.** Đọc spec liên quan trong `specs/` và code hiện có. Nếu yêu cầu mâu thuẫn với tài liệu này, hỏi lại trước khi làm.
2. **Thay đổi nhỏ, một mục đích.** Một thay đổi chỉ làm một việc (một luật chơi, một màn hình UI, một công cụ editor). Lý do: thay đổi nhỏ thì xem lại được và quay lui được.
3. **Kiểm chứng theo loại thay đổi** (mục 3).
4. **Báo cáo.** Nêu: đã đổi gì, đã kiểm chứng gì kèm số đo, chưa kiểm chứng gì và vì sao.
5. **Commit khi được yêu cầu** (mục 7).

---

## 3. Kiểm chứng theo loại thay đổi

Mức kiểm chứng tỉ lệ với rủi ro. Áp cùng một quy trình nặng cho mọi thay đổi sẽ dẫn tới làm cho có.

| Loại thay đổi | Biên dịch sạch | Test EditMode | Playtest luật chơi | Hoạt cảnh thắng/thua | Xem trên màn hình thật |
|---|:-:|:-:|:-:|:-:|:-:|
| Luật chơi, di chuyển, AI | ✔ | ✔ | ✔ | nếu chạm tới kết trận | |
| Kết trận, camera, hoạt cảnh | ✔ | | | ✔ | |
| UI / HUD / input | ✔ | | | | ✔ ở 2 tỉ lệ màn hình |
| Số cân bằng trong config | ✔ | ✔ | ✔ | | |
| Công cụ editor, pipeline asset | ✔ | | | | chạy công cụ một lần |
| Tài liệu, spec | | | | | |

"Biên dịch sạch" nghĩa là Console không có lỗi và không có cảnh báo mới. Đây là điều kiện cần, chưa bao giờ là đủ cho thay đổi gameplay hay UI.

### Cách playtest

Playtest chạy qua cầu nối HTTP trong `Assets/Editor/UnityMcpBridge.cs`, tại `http://127.0.0.1:8090/`. Cầu nối tự khởi động khi Unity Editor mở dự án. Nếu `ping` không trả lời thì Editor chưa mở: dừng lại và báo "chưa playtest được", không suy đoán kết quả.

| Lệnh | Tác dụng |
|---|---|
| `ping` | Kiểm tra cầu nối (trả lời từ luồng nền, không chứng minh Editor đang chạy) |
| `test_status` | Vị trí Mẹ, Thầy, từng con; trạng thái Editor (chạy trên luồng chính) |
| `open_scene?name=Gameplay` | Mở scene |
| `playmode?on=1` / `playmode?on=0` | Vào / thoát Play Mode |
| `start_match` | Bắt đầu trận |
| `autopilot?mode=walls\|orbit\|stopgo\|zigzag\|idle\|off` | Tự lái Mẹ: lao vào tường 8 hướng, chạy vòng, chạy-dừng, đảo chiều, đứng yên |
| `input?x=&y=&lock=` | Bơm hướng joystick và nút Khiên |
| `autoduel?mode=random\|off`, `duel?dir=left\|straight\|right`, `force_duel` | Điều khiển đấu Chăm Chăm Chăm |
| `apply_buff?type=Pho&snake=1` | Kích hoạt buff để xem hiệu ứng |
| `probe`, `probe_reset` | Số đo tích luỹ từng khung hình: ra ngoài biên, tỉ lệ tốc độ con/Mẹ, khoảng cách chuỗi, FPS, vật phẩm |
| `inspect` | Kiểm tra socket, Animator, prefab vật phẩm, ArenaDefinition trong scene |
| `screenshot?name=x` | Chụp Game view vào `Temp/playtest/x.png` |
| `test_victory`, `test_gameover`, `victory_status` | Ép và đọc trạng thái kết trận |
| `run_tests` | Chạy test EditMode trong `Assets/_Game/Tests` |
| `bake_arena` | Bake lại biên sân, vật cản từ `Assets/RongRan/Configs/ManX.json` vào các scene |
| `wait?ms=` | Chờ |
| `console_logs`, `clear_logs` | Đọc / xoá log |

Trình tự: `test_status` → `clear_logs` → `playmode?on=1` → `start_match` → `probe_reset` → `autopilot` → `wait` → `probe` → `playmode?on=0`. Luôn trả Editor về Edit Mode trước khi báo cáo, vì thay đổi scene trong Play Mode sẽ mất.

**Kênh file (ưu tiên dùng).** Sau khi Unity nạp lại code hoặc vào Play Mode, listener HTTP cũ thường còn sót và giữ cổng, nên lệnh HTTP cần luồng chính có thể không được trả lời. Mọi lệnh trong bảng (trừ `ping`, `console_logs`, `clear_logs`) gửi được qua file: ghi một dòng `<id> <lệnh>?<tham số>` vào `Temp/playtest/cmd.txt`, kết quả xuất hiện ở `Temp/playtest/out_<id>.json`. Lệnh `status` cho biết Editor có đang ở Play Mode không. Cổng HTTP đang dùng được ghi ở `Temp/mcp_bridge_port.txt` (8090 trở lên).

Lệnh `play` cũ dùng `delayCall` và không phản hồi khi cửa sổ Unity không được focus; dùng `playmode` thay thế. Nếu `ping` trả lời mà `test_status` không trả lời thì luồng chính của Editor đang kẹt: dừng lại và báo, không tự kill Unity.

### Tiêu chí đạt

- **Biên sân**: lấy `test_status` ít nhất 20 lần trong một trận 60 giây. Mọi toạ độ của Mẹ, Thầy, các con đều nằm trong biên sân của màn đó.
- **Luật vồ**: mọi dòng log "chạm tay" trong trận có khoảng cách tay–thân không vượt ngưỡng chạm trong config. Không có lần vồ trúng nào khi Mẹ đang chắn.
- **Tốc độ con**: khi Mẹ đang chạy, không con nào vượt trần tốc độ quy định ở mục 4.
- **Kết trận**: sau `test_victory` và `test_gameover`, bảng kết quả chỉ xuất hiện sau khoảng lặng điện ảnh; nhân vật ở đúng trạng thái ăn mừng / ủ rũ.

Báo cáo ghi số đo thật (ví dụ "14 lần vồ, tay–thân lớn nhất 0.62 m"), không ghi "đã kiểm tra".

### Test tự động

Các luật đo được bằng toán (biên sân, trần tốc độ, điều kiện vồ trúng, che chắn) cần có test EditMode trong `Assets/_Game/Tests/`. Lý do: luật chỉ nằm trong tài liệu thì trôi dần mà không ai biết. Khi sửa một luật loại này, thêm hoặc cập nhật test của nó trong cùng thay đổi.

---

## 4. Luật chơi (bất biến thiết kế)

Số liệu cụ thể nằm trong `Assets/_Game/Settings/GameBalanceConfig.asset`. Tài liệu này chỉ nêu luật và lý do.

1. **Vồ phải chạm thật.** Đòn vồ chỉ trúng khi bàn tay Thầy Thuốc (`socketGrabHand`) chạm thân mục tiêu trong ngưỡng chạm. Không tính trúng theo khoảng cách thân–thân. *Lý do: người chơi thấy tay huơ vào không khí mà vẫn mất con thì cảm giác bị xử ép.*
2. **Mẹ là vật chắn.** Thầy Thuốc không vồ xuyên qua Mẹ. Khi Mẹ đứng giữa Thầy và Con Út, đòn vồ bị cản và Thầy bị đẩy dội lại; muốn bắt Út phải luồn qua hai bên sườn. *Lý do: che chắn là lối chơi cốt lõi của trò dân gian này.*
3. **Không ai ra khỏi sân.** Mọi thực thể (Mẹ, con, Thầy, vật phẩm) đi qua `ArenaBoundary.ClampToArena`. Biên sân lấy từ hình học thật của từng màn, không dùng chung một hình cho mọi màn.
4. **Con theo Mẹ, không vượt Mẹ.** Khi Mẹ đang di chuyển, tốc độ mỗi con không quá 1.15 lần tốc độ Mẹ. Khi Mẹ dừng, đàn con được phép một khoảng ngắn (dưới 1 giây) để xếp lại hàng rồi dừng hẳn. *Lý do: con phóng nhanh hơn Mẹ trông giật và sai với hình ảnh nối đuôi.*
5. **Nối đuôi khít.** Khoảng cách giữa các con giữ ở mức nối gót tự nhiên (khoảng 0.9–1.0 m), không giãn kiểu dây thun.
6. **Dáng chạy nhẹ nhàng.** Độ lắc khi chạy ở tần số thấp, biên độ nhỏ; không rung nhanh tới mức nhân vật trông như co giật.

### Số liệu

- Tham số cân bằng đặt trong `GameBalanceConfig`; script runtime đọc từ đó. `GameConstants` chỉ là giá trị mặc định khi config chưa được gán.
- Một con số mới trong code runtime cần hoặc là vào config (nếu là thứ sẽ tinh chỉnh), hoặc là hằng có tên kèm chú thích nguồn gốc (đo từ mesh, suy ra từ số khác). *Lý do: số không tên không ai dám sửa và không ai biết nó từ đâu ra.*
- Số đo hình học lấy từ mesh hoặc collider thật, không ước lượng.

---

## 5. UI/UX di động

1. **Icon trước chữ.** Nút chức năng dùng biểu tượng: ▶ vào trận, ⏸ tạm dừng, 🏠 menu, 🔄 chơi lại, ✕ đóng, ❓ luật chơi, 👘 tủ đồ, khiên cho nút chắn, ba mũi tên pha lê cho QTE Chăm Chăm Chăm. Chữ trên màn hình giữ ở mức nhãn ngắn. *Lý do: màn hình nhỏ, và người chơi không cần biết đọc tiếng Việt.*
2. **Kích thước theo màn hình.** Mọi kích thước tính theo `refScale = Screen.height / 1080f`, không kẹp bằng số pixel cố định. *Lý do: kẹp pixel làm nút bé tí trên màn hình 2K/4K.*
3. **Vừa ngón tay.** Nút hành động chính trong trận (Khiên) cao 20–24% màn hình. Nút phụ (tạm dừng, menu) nhỏ hơn nhưng vùng chạm không dưới 9% chiều cao màn hình.
4. **Bố cục hai ngón cái.** Nửa trái là joystick, có vòng mờ định vị khi nhàn rỗi. Nửa phải là nút Khiên.
5. **Kiểm tra ở hai tỉ lệ**: 16:9 và một màn hình dài (khoảng 20:9), để chắc không có gì bị cắt hay chồng lên nhau.

---

## 6. Kết trận

1. **Khoảng lặng điện ảnh 2.5–3 giây** trước khi hiện bảng kết quả. *Lý do: đập bảng ngay lập tức cắt mất khoảnh khắc cảm xúc.*
2. **Thắng**: camera hạ xuống cận cảnh ngang tầm mắt trước Mẹ và đàn con; Mẹ và con quay về phía camera, nhảy ăn mừng (`IsVictory`); Thầy Thuốc ở xa giậm chân tức tối (`IsAngry`).
3. **Thua**: camera hạ xuống trước Thầy Thuốc đang đắc thắng; Mẹ và con cúi đầu ủ rũ.
4. **Bảng kết quả gọn**: hai nút icon 🔄 và 🏠, không che cảnh 3D.

---

## 7. Kiến trúc & hiệu năng

1. **Tách trách nhiệm.** Visual (model, Animator, socket; không chứa logic) — Motor (`CharacterController`, di chuyển) — Brain (input hoặc AI) — `GameManager` (trạng thái trận). Thay đổi trạng thái trận phát qua sự kiện; các thực thể không tự dò `GameManager` mỗi frame.
2. **Không cấp phát trong `Update` / `FixedUpdate`.** Không LINQ, không `new List`/mảng, không nối chuỗi mỗi frame; cache `GetComponent` trong `Awake`. Mục tiêu 60 FPS trên di động.
3. **UI runtime** dùng uGUI hoặc UI Toolkit. HUD hiện tại viết bằng IMGUI (`OnGUI`) là nợ kỹ thuật từ prototype; không viết thêm UI mới bằng IMGUI.

---

## 8. Git

- **Commit nhỏ, một mục đích**, thông điệp dạng `feat:`, `fix:`, `refactor:`, `docs:`, `chore:` kèm phạm vi, ví dụ `fix(hunter): vồ chỉ tính khi tay chạm thân`.
- **Không trộn** asset sinh tự động (model, animation, texture) với thay đổi code trong cùng một commit.
- **Push ít nhất cuối mỗi buổi làm việc.** *Lý do: commit chỉ nằm trên một máy thì chưa phải là sao lưu.*
- Agent chỉ commit hoặc push khi chủ dự án yêu cầu.
- File sinh ra khi chạy (log, `__pycache__`, bản build) không đưa vào git. File nhị phân lớn (`.glb`, `.fbx`, `.png`) đi qua Git LFS.
- Script sinh asset đặt trong `Tools/`, không để ở gốc repo.

---

## 9. Chỗ code hiện chưa khớp tài liệu này

Danh sách này để không ai mặc định là code đã đúng luật. Sửa xong mục nào thì xoá mục đó.

- [ ] Biên sân theo màn, vật cản tĩnh/di động, vị trí sinh vật phẩm (`ArenaDefinition`, `ArenaBoundary`, `ItemSpawner`): đã playtest Màn 1, 2, 4 (không ai ra ngoài biên ở Màn 1; lệch tối đa 0.11 m ở Màn 4 khi con bị kéo vào cầu tuyết). Chưa playtest Màn 3, 5.
- [ ] `LevelData.childCount` chưa được dùng: mọi màn vẫn 3 con vì scene dựng sẵn 3 con.
- [ ] Buff Bánh Gạo Cay (phân thân) chưa có hiệu ứng; `ItemSpawner.spawnCloneFood` đang bật sinh món này nhưng chưa có clone mesh decoy.
- [ ] HUD mới kiểm tra ở 16:9 (3840×2160); chưa kiểm tra màn hình dài 20:9 và màn hình thấp hơn 1080p (mục 5.5).
- [ ] Texture nút nạp bằng `AssetDatabase`/đọc file trong `Assets`, và icon là emoji trên font mặc định: cả hai chưa được kiểm chứng trên bản build.
- [ ] Test EditMode mới chỉ có cho biên sân; chưa có cho luật vồ, che chắn, trần tốc độ (mục 3).
- [ ] Gốc repo còn log và script Python; `.glb` chưa vào LFS (mục 8).
