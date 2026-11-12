# Castaway Cove

Game câu cá hành động 3D, một người chơi, chơi offline trên Windows. Bạn câu cá, hạ gục con
cá còn sống, bán xác, mua vũ khí và đối đầu boss để mở các đảo mới. Game xây bằng Unity
6000.6.8f1, built-in render pipeline và uGUI; toàn bộ code, UI và âm thanh là nội dung gốc,
model lấy từ các bộ asset CC0 (Kenney, Quaternius).

## Yêu cầu hệ thống

- Windows 10/11 x64.
- Để mở/build: Unity 6000.6.8f1 (Unity Hub).
- Bản chơi đã build không cần cài thêm gì; game chạy offline.

## Build / chạy

Mở thư mục repo này bằng Unity Hub, mở `Assets/Scenes/Main.unity` và bấm **Play**.

Build bản Windows từ thư mục gốc repo:

```
"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod CraftBuild.Build -logFile Logs/build.log
```

File chơi nằm ở `Build/Game.exe`. Trong Editor cũng có menu **Castaway Cove > Build Player**.

## Tính năng chính

- Câu cá: ném câu, chờ cá, giật cần, minigame kéo cá với tension và stamina.
- Chiến đấu: cá câu lên còn sống, có HP, lao vào tấn công; hạ gục rồi nhặt xác.
- Sinh tồn: HP, độ no, ăn cá hồi máu, ngất và hồi sinh tại cầu tàu.
- Vũ khí: tay, dao, pistol, shotgun; đạn, nạp đạn, bonus headshot/airborne/melee.
- Kinh tế: bán cá, shop rod/mồi/túi, Weapon Rack bán vũ khí và đạn.
- Boss: Spider Crab với đòn lao, cửa sổ stun và shell trophy mở Isle Two.
- Thế giới: 3 đảo, thuyền chèo, quest board, 3 mục tiêu tiến trình.
- Lưu/tải JSON, autosave, pause, menu Play/Continue/Quit.
- Asset CC0: Kenney Nature/Pirate/Survival/Mini Characters, Quaternius Animated Fish.

## Phím tắt

| Hành động | Phím |
|---|---|
| Di chuyển / nhìn | WASD / chuột |
| Chạy / nhảy | Shift / Space |
| Tương tác (shop, quest, thuyền, nhặt xác) | E |
| Equip / cất cần câu | 1 |
| Equip tay hoặc dao | 2 |
| Equip pistol | 3 |
| Equip shotgun | 4 |
| Tấn công / bắn | Chuột trái |
| Nạp đạn | R |
| Ăn cá (hồi HP và độ no) | F |
| Ném câu / giật cá / kéo cá | Giữ chuột trái, thả chuột |
| Hủy câu | Chuột phải |
| Lưu / tải nhanh | F5 / F9 |
| Pause | Esc |

## Kiến trúc / cấu trúc thư mục

- `Assets/Scripts/Core`: bootstrap, dữ liệu game, lưu/tải, âm thanh, self-test.
- `Assets/Scripts/Player`: điều khiển nhân vật và tương tác.
- `Assets/Scripts/Fishing`: vòng lặp câu cá.
- `Assets/Scripts/Combat`: HP/độ no, vũ khí, cá sống, boss.
- `Assets/Scripts/UI`: HUD, shop, Weapon Rack, quest, menu.
- `Assets/Scripts/World`: sinh đảo, thuyền, props, thời tiết.
- `Assets/Editor`: import CC0 và build player.
- `Assets/CC0`: model/texture CC0 gốc kèm license.
- `Assets/Resources/CC0`: prefab/material sinh tự động cho runtime.
- `shots/`: ảnh chụp màn hình; `test-results/self-test.json`: kết quả self-test gần nhất.
- `MECHANICS.md`, `mechanics.json`: bảng số liệu gameplay.
- Chi tiết tiếng Anh: [`README.en.md`](README.en.md); ghi chú triển khai: [`NOTES.md`](NOTES.md).

## Cấu hình

Game không cần file cấu hình. Save nằm tại:

```
%USERPROFILE%\AppData\LocalLow\Cove Studio\Castaway Cove\save.json
```

Các cờ dòng lệnh: `-selfTest` / `--self-test <file>`, `--screenshot <file> --shot-ui <screen>`.

## Giới hạn đã biết

- Chỉ có 3 đảo và một boss; chưa có 5 đảo/quest chain đầy đủ như bản tham khảo.
- Chưa có cooking/grill, gacha/roulette/slots, Drip variants, Fishipedia, skin, achievements.
- Chưa có multiplayer, music, settings menu hoặc gamepad.
- Chỉ số cân bằng là ước lượng ban đầu, chưa tinh chỉnh qua playtest dài.

## Lịch sử phiên bản

- **0.1.0** (2026-11-12): bản đầu tiên gồm câu cá, combat, vũ khí, boss Spider Crab, shop,
  quest, HUD, lưu/tải và self-test 16 bước. Xem [`CHANGELOG.md`](CHANGELOG.md).

## Tải về

Bản build mới nhất nằm ở [GitHub Releases](https://github.com/tinycorn-studio/castaway-cove/releases/latest).
Repo không chứa file cài/build output.

## Credits

Asset CC0 và license: [`CREDITS.md`](CREDITS.md), `Assets/CC0/SOURCES.md`,
`Assets/CC0/Licenses/`. Code và nội dung gốc: TinyCorn Studio, MIT (`LICENSE`).
