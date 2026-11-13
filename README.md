# Castaway Cove

Game câu cá hành động 3D, một người chơi, offline trên Windows. Bạn câu cá, hạ gục con cá
còn sống, bán xác, mua vũ khí, nâng cấp và đối đầu 5 boss để đi hết 5 đảo và về nhà. Game
xây bằng Unity 6000.6.8f1 (built-in render pipeline, uGUI); code/UI/âm thanh là nội dung gốc,
model dùng các bộ asset CC0 của Kenney và Quaternius.

## Yêu cầu hệ thống

- Windows 10/11 x64.
- Để mở/build: Unity 6000.6.8f1 (Unity Hub).
- Bản chơi đã build không cần cài thêm; game chạy offline.

## Build / chạy

Mở thư mục repo này bằng Unity Hub, mở `Assets/Scenes/Main.unity`, bấm **Play**.

Build bản Windows từ thư mục gốc repo:

```
"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod CraftBuild.Build -logFile Logs/build.log
```

File chơi nằm ở `Build/Game.exe`. Trong Editor cũng có menu **Castaway Cove > Build Player**.

## Tính năng chính

- **Câu cá**: ném câu, chờ cá, giật cần, minigame tension/stamina; rod, mồi, túi; cá theo
  từng đảo (8 loài, 5 pool theo độ khó).
- **Chiến đấu**: cá câu lên còn sống, có HP, lao vào tấn công; hạ gục rồi nhặt xác.
- **Sinh tồn**: HP, độ no, ăn cá hồi máu, ngất và hồi sinh tại cầu tàu.
- **Vũ khí**: tay, dao, pistol, shotgun, dynamite; đạn, nạp đạn, bonus headshot/airborne/melee/longshot.
- **5 đảo, 5 boss**: Spider Crab, Giant Piranha, Pufferfish, Albatross, Mutated Whale;
  mỗi boss có charge/stun, trophy và mở đảo tiếp theo.
- **Quest chain + radar**: quest board 5 bước; boat keys và radar sau Spider Crab; radar hiện
  khoảng cách/khóa từng đảo.
- **Kinh tế**: bán cá, shop rod/mồi/túi, Weapon Rack bán vũ khí/đạn/dynamite.
- **Nấu ăn**: grill (1.5x, 15% cháy) và lava cook an toàn trên Isle Five; cá nấu hồi nhiều máu hơn.
- **Bộ sưu tập**: Drip variant (10%, 8x value), Fishipedia (Tab), achievements.
- **Gacha/casino**: Reel of Fortune (Drip hoặc $100 → skin), roulette và slots bằng tiền trong game.
- **Khác**: seagull bắn được, difficulty Normal/Hard, lưu/tải JSON, pause, menu Play/Continue/Quit.

## Phím tắt

| Hành động | Phím |
|---|---|
| Di chuyển / nhìn | WASD / chuột |
| Chạy / nhảy | Shift / Space |
| Tương tác (shop, quest, grill, thuyền, nhặt xác) | E |
| Equip / cất cần câu | 1 |
| Equip tay hoặc dao | 2 |
| Equip pistol | 3 |
| Equip shotgun | 4 |
| Ném dynamite | 5 |
| Tấn công / bắn | Chuột trái |
| Nạp đạn | R |
| Ăn cá (hồi HP và độ no) | F |
| Ném câu / giật cá / kéo cá | Giữ chuột trái, thả chuột |
| Hủy câu | Chuột phải |
| Fishipedia / achievements | Tab |
| Lưu / tải nhanh | F5 / F9 |
| Pause | Esc |

## Kiến trúc / cấu trúc thư mục

- `Assets/Scripts/Core`: bootstrap, dữ liệu game, lưu/tải, âm thanh, self-test.
- `Assets/Scripts/Player`: điều khiển nhân vật và tương tác.
- `Assets/Scripts/Fishing`: vòng lặp câu cá.
- `Assets/Scripts/Combat`: HP/độ no, vũ khí, cá sống, 5 boss, dynamite, seagull.
- `Assets/Scripts/UI`: HUD, shop, weapon rack, grill, Fishipedia, fortune, quest, menu.
- `Assets/Scripts/World`: sinh đảo, thuyền, props, boss altar, lava.
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

Cờ dòng lệnh: `-selfTest` / `--self-test <file>`, `--screenshot <file> --shot-ui <screen>`.

## Giới hạn đã biết

- Chưa có multiplayer, music, settings menu hoặc gamepad.
- Cooking/gacha/casino là bản rút gọn: grill cook hàng loạt, gacha quay skin, casino 2 trò.
- Killscore mới có headshot/airborne/melee/longshot; chưa có 360/no-scope/dogfight.
- Chưa có tùy biến ngoại hình nhân vật; skin chỉ đổi màu vũ khí.
- 5 đảo dùng chung shop; chưa có shop/lure riêng từng đảo.
- Boss dùng chung khung AI charge/stun; chỉ số và model khác nhau.
- Chỉ số cân bằng là ước lượng ban đầu, chưa tinh chỉnh qua playtest dài.

## Lịch sử phiên bản

- **0.2.0** (2026-11-12): 5 đảo + 5 boss, quest chain, radar/boat keys, cooking, Drip,
  Fishipedia, gacha/casino, difficulty, dynamite, seagull, self-test 18 bước.
- **0.1.0** (2026-11-12): bản đầu tiên gồm câu cá, combat, vũ khí, Spider Crab, shop,
  quest, HUD, lưu/tải. Xem [`CHANGELOG.md`](CHANGELOG.md).

## Tải về

Bản build mới nhất nằm ở [GitHub Releases](https://github.com/tinycorn-studio/castaway-cove/releases/latest).
Repo không chứa file cài/build output.

## Credits

Asset CC0 và license: [`CREDITS.md`](CREDITS.md), `Assets/CC0/SOURCES.md`,
`Assets/CC0/Licenses/`. Code và nội dung gốc: TinyCorn Studio, MIT (`LICENSE`).
