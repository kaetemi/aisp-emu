# Character battle data (not TPS)

Two different things were collapsed under the emulator's old `Tps*` names.

1. **Character battle stats** — a nested vitality / parameter / progression record on every `CharaData` (avatars and robos). The July 2009 client already has this record. Protocol names (2011 VCE logs) are `hitpoint`, `heart`, `stamina` (`gauge` / `speed`), `tank`, `ability`, `cosplay`, plus live `recv_notify_update_*` packets. Wire type: `CharaBattleData`.
2. **TPS mode** — a 2011-only `tps::` UI and combat camera. It *displays* layer 1. It did not invent those stats. The July 2009 exe has no `tps` / `CTPS` RTTI, no TPS help or `data/texanim/tps_killshot`, and no immediates for `EventGetTpsMode` (`0xC290` / `0xD758`) or `NotifyTpsUseItemStart` / `End` (`0xFBF6` / `0xA2CD`).

`recv_aipower_data` is a third thing: a loop of `0x4C0`-byte records, not the 155-byte blob.

On this `experimental/july2009` branch the wire matches July 2009. The SQLite table `RoboTpsBattleData` and column `StaminaRecoveryRate` keep their historical names so existing databases still load.

## Wire sizes

| Blob | July 2009 | 2011 |
| --- | --- | --- |
| `AvatarNotifyData` | 821 (4 + 817) | 932 (4 + 928) |
| `AvatarData` | 817 | 928 |
| `CharaData` | 546 | 566 |
| Nested battle record | **155** (`CharaBattleData.WireSize`) | 175 |
| Ability groups on the wire | 3 × 5 uints | 4 × 5 uints (+20) |
| Item-use effects | 7 × 37 | 8 × 37 |
| `RoboVoiceType` + nested `UserStatus` | absent | 1 + 53 |
| `RoboData` | 851 | 961 |

The extra 20 bytes on 2011 `CharaData` are the fourth ability group (`AbilityGroup2`). 2009 is the older record; the fourth group arrived with TPS-era 2011, it is not that 2009 "omits TPS". VCE requires every byte of `recv_notify_avatar_data` to be consumed, so sending the 2011 size RSTs the 2009 client.

2009 parsers: `ReadAvatarData` `0x719910`, `ReadCharaData` `0x719760`, `ReadBattle` `0x719510`, `ReadHP` `0x719340`, `ReadStamina` `0x7193c0`, `ReadTank` `0x71a820`, `ReadAbilities` `0x719420` (3 × 5), flags `0x7194a0`, `ReadCosplay` `0x7194e0`, `LevelProgress` `0x7192d0`. 2011: `ReadCharaData` `0x798ff0`, `ReadBattle` `0x798d80`.

## Packed `CharaBattleData` layout (155 bytes)

Offsets below are on the **packed wire** as `CharaBattleData.ToBytes` emits it. The client's in-memory C++ object may insert alignment padding (HP is 18 bytes on the wire; stamina often sits at `+0x14` in the object).

| Wire offset | Size | Field | Protocol / VCE |
| --- | --- | --- | --- |
| `+0x00` | 18 | `HitPoints` | `hitpoint` / `hitpoint_max`, `heart` / `heart_max` |
| `+0x12` | 16 | `Stamina` | `gauge=` then `speed=` (second float; previously misnamed recovery-rate) |
| `+0x22` | 16 | `Tank` | `tank`, `amount=` — 2011 TPS HUD has `CTankGauge`; 2009 has the same 16-byte block and live tank recvs |
| `+0x32` | 20 | `BaseAbilities` | `ability` + `value=`, index 0–4 |
| `+0x46` | 20 | `AbilityGroup0` | same five-uint shape; emu-invented name was `AbilityModifierType0` |
| `+0x5A` | 20 | `AbilityGroup1` | same; was `AbilityModifierType1` |
| *(not on 2009 wire)* | 20 | `AbilityGroup2` | 2011 fourth group; kept in memory and in `RoboBattleAbilities` (`ModifierType2`) |
| `+0x6E` | 8 | `StatusEffectFlags` | |
| `+0x76` | 4 | `ActionFlags` | |
| `+0x7A` | 4 | `ActiveSkillId` | |
| `+0x7E` | 29 | `Cosplay` | `cosplay` id + `LevelProgressData` (`lv` / `exp` / `next`) |

`LevelProgressData` (25 bytes: level, status points, exp, exp-to-next) sits immediately after the battle blob on `CharaData`.

Live 2009 tank readers also at `0x73276a` / `80b` / `89a`; extra cosplay readers `0x71eb6e`, `0x736c11`, `0x744614`.

## Unlabeled `CharaData` fields (not battle, not TPS)

These sit on `CharaData` between equipment / name plate and the battle blob. They had `TpsAction*` names; 2009 has no TPS action manager. No VCE field names.

| Field | What we know |
| --- | --- |
| `ActionReferenceX` / `ActionReferenceY` | Two floats after `chrmap`. The same two-float helper is also used by a 12-byte packet (2011 `0x7e35e0`). |
| `ActionProfileId` | Uint after `NamePlate`. |
| `CollisionRadius` | Unlabeled float. Collision meshes live under `tools/collision/`; not proven to be a radius. |
| `ActionVerticalRange` | Float after `CollisionRadius`. |

`NamePlate` **is** named: client `CChara::SetNamePlate` (0 none, 1 celebrity, 2 GM, 3 penalised, 4 ordinary NPC, 5 official NPC, 6 event user, `0xFFFFFFFF` staff).

## 2011 TPS packets that must not be treated as the blob

These opcodes are real 2011 TPS *mode* packets. Keep the `Tps` names. The July 2009 exe does not implement them, except the `0x6841` recv slot.

| Opcode | 2011 VCE name | July 2009 |
| --- | --- | --- |
| `0xD758` | `recv_event_get_tps_mode` | absent |
| `0xC290` | `send_event_get_tps_mode_r` | absent |
| `0xFBF6` | `recv_notify_tps_use_item_start` | absent |
| `0xA2CD` | `recv_notify_tps_use_item_end` | absent |
| `0x96B9` | `send_get_tps_use_item_list` | no send; earlier "hits" were `mov ecx, 0x96` false positives |
| `0x6841` | `recv_get_tps_use_item_list_r` (alloc `0x2CF`) | Area recv at `0x72b6ea` allocates **8 bytes** and reads two uints |

Do not send `EventGetTpsModeNotify` on the 2009 wire.

## 2009 UI that is *not* this blob

- `CMyStatusWindow` (IF factory `0x4A`, ctor `0x5d77e0`) is マイステータス, the profile editor.
- `CAipowerWindow` (IF `0x4B`, ctor `0x5d3820`, alloc `0xF40`) is **AIパワー**, an unfinished charadoll support/cheer sheet, not a fight HUD and not a shipped launch-day feature. PAS `ai_power00.xml` titles it `aiパワー`. Tabs are 応援画面 and 要望箱. Each card has a **キャラクターヴィジュアル** (256×256 DXT5 portrait from `aipower.hed`, not a 3D doll), profile (height / weight / three-size / likes / dislikes), a dialogue balloon, two gauges, and **six heart pips** (`ハート-1`…`ハート-6` plus `ハート-max`). Buttons spend デレ (`User.AiPoints`, not a separate AIパワー currency) or nico points (`応援する！`, `一口応援`). The texture atlas is stamped **開発中！**. 2009 help never mentions it; it is not on ウィンドウメニュー. Public copy used three vocabularies for the same unshipped idea (2ch 募金, 伴’s 2008-11-08 blog 育成, client 一口応援); local HTML: `www/july2009/aipower.html`.
- The window opens when Area sends `recv_aipower_data` (`0x59C3`, handler `0x72ABA0`): 8-byte header (`result` then `count`) + `N` records of `0x4C0` bytes, `N ≤ 0x12C` (300). After parse it calls vtable `+0x518`. Internal UI message `0x30001` can also construct type `0x4B` locally. `recv_close_aipower_window_r` (`0x6B53`) is a 4-byte result (vtable `+0x524`). `recv_support_aipower_aipoint_r` (`0x5DB8`) is 12 bytes; `recv_support_aipower_nicopoint_r` is the nico twin (2011 VCE log still has both stubs; only the aipoint recv is in `PacketType` today). 2011 deleted `CAipowerWindow` RTTI but kept the PAS/DDS and VCE log stubs; TPS `tps_status00.xml` (`TPS戦闘-HPステータス`) is the 2011 heart/stamina HUD. `/aipower` on this branch pushes that notify (optional count 0–300; `empty` for a header-only dump).
- Card apply is `CAipowerCharaSheet` `0x5d2320`. `Id` is **not** an avatar id: `str_table` `(100,940,1)` is `./aipower/%05d.dds`. A miss hides the sheet (that is why a card with `Id=1` opened an empty overlay). July 2009 `aipower.hed` (key `6E3C`) holds three DXT5 256×256 portraits: `10000` 月島小恋, `10001` 白河ななか, `10002` 朝倉由夢 — PAS calls this キャラクターヴィジュアル; it is a 2D portrait, not a 3D doll. `Param0` / `Param1` are 0–100 gauge percents (`0x5d1920` / `0x5d1840`); heart pip = PAS `380 + Param0/20`, and `Param0 ≥ 100` shows MAX. `Param2` is added into the upper gauge when a cheer tick overflows the lower one. The 601-byte tail is a UTF-8 C string (MultiByteToWideChar 65001); `<BR>` (wide needle `0x8a4810`) splits the five プロフィール rows (PAS 190–194). The five 91-byte profile strings are balloon-line candidates; `0x5d1bd0` picks one at random for 台詞.
- Hearts on this sheet are **not** the two `HitPointData` bytes. Those `CurrentHearts` / `MaximumHearts` bytes still sit on every `CharaData` for later TPS; AIパワー pips come from `Param0`. 2009 has live `recv_notify_update_hitpoint` (`0x9CC0`, 8 bytes) and `recv_notify_update_hitpoint_max` (`0xAEEF`) but **no** `recv_notify_update_heart`. No 2009 HP overlay HUD.
- `recv_notify_avatar_data` at `0x70ff50` `memcpy`s the full `0x370`-byte `AvatarData` snapshot to `0x970320`. HUD consumers read that snapshot, not field-by-field HP packets, until a later `notify_update_*`.

## Persistence

`Robo.TpsBattleData` → table `RoboTpsBattleData`. Ability rows use `RoboBattleAbilitySet` (`Base`, `ModifierType0`/`1`/`2`) stored as bytes. `ModifierType2` is still written so a later 2011 wire can grow without another schema change. `StaminaRecoveryRate` stores `StaminaData.Speed`.

## Studio lineage (unused hitpoints / avatar slots)

The 2009 client already parses hitpoints and lives on a multi-door `SlotId` character select, with no fight HUD. That is MMORPG-shaped, not dress-up.

Two threads, keep them distinct:

1. **VCE2** — Community Engine transport (`vce::Codec`, Camellia, exact-size consume). Official C++ headers: `/mnt/amber/aispace/reference/vce2-book/vce/darwin8644/` (clone of [kengonakajima/book](https://github.com/kengonakajima/book)). ja.Wikipedia's VCE adopter list includes ECO and Depth Fantasia; it does **not** list ai sp@ce or Pandora Saga.
2. **LibPST** — Headlock in-house persistence codegen (`dbmap.csv` → GMSV/DBSV stubs). CEDEC 2009 「MMOGゲームサーバプログラミングの実際」 (`reference/cedec2009/NW46.pdf`) states it was used in **both** Pandora Saga and ai sp@ce. That is the better donor for a leftover `hitpoint` column: generated stubs are cheaper to leave than to prune.

Headlock also developed ECO (4Gamer 2008-06-13). Pandora Saga (Feb 2008) and ai sp@ce (Oct 2008) overlap. This is circumstantial plus the CEDEC quote, not a source diff. Local HTML: `www/july2009/lineage.html`. AIパワー is a separate unfinished cheer sheet (デレ or nico), not this blob.
