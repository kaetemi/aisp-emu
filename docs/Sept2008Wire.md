# September 2008 wire deltas

This branch targets the 2008-09-16 `ai sp@ce.exe` (build 0x09). Parent branch is `experimental/july2009` (build 0x45).

Official service launch was 2008-10-15. This client is pre-launch. Login UI is ログインID, not the 2009 niconico email form. Saved `data/save/login.dat` is `login_id,eina`.

## Isolation

| | Value |
| --- | --- |
| Display | Xvfb `:94` (1280×800) |
| Prefix | `/mnt/amber/aispace/sept2008/wineprefix` (`WINEARCH=win32`) |
| Client | `/mnt/amber/aispace/sept2008/client/ai sp@ce/` |
| Scripts | `/mnt/amber/aispace/scripts/sept2008/` |
| Notes | `/mnt/amber/aispace/www/sept2008/` |
| Docker | `aisp-sept2008-server` (ports 50050/50052/50054/8080 — do not run with 2009/2011 local) |

Do not inject the 2011-era `aisp.launch.exe` that shipped in the RAR. Launch `ai sp@ce.exe ./data` until `aisp.hook` addresses are retargeted.

## Recv opcode presence (`cmp eax, imm32` vs 2009)

C2S immediates are not stored as `push imm32` even on the 2009 exe, so a miss there is not evidence. Recv switch arms are.

| Packet | Opcode | 2008 | 2009 |
| --- | --- | --- | --- |
| `recv_check_version_r` | `0xB6B4` | present | present |
| `recv_authenticate_r` | `0xD4AB` | present | present |
| `recv_authenticate_r_failure` | `0xD845` | present | present |
| `recv_get_worldlist_r` | `0xEE7E` | present | present |
| `recv_notify_avatar_data` | `0x7D78` | present | present |
| `recv_talk_forward` | `0x20F6` | present | present |
| `recv_gacha_started` | `0xCC88` | present | present |
| `recv_aipower_data` | `0x59C3` | present | present |
| `recv_notify_change_map` | `0xB315` | **absent** | present (98-byte layout) |
| `recv_notify_change_myroom` | `0x0FA0` | present | present |
| `recv_notify_change_map_failed` | `0x6648` | present | present |

`NotifyChangeMap` `0xB315` is the first confirmed Area recv hole versus July 2009. Map enter will RST or ignore until the 2008 opcode and layout are found.

## MOTD

Leave `Motd__Enabled=false` until DistId `-5`/`-6`/`-7` are proven not to abort this build. 2009 aborts at `0x42642d` for those DistIds.

## VCE

Same family as 2009/2011: vce2, VS2005, Camellia-128, RSA-16 key exchange. RTTI `CProtoAuth_client` / `CProtoMsg_client` / `CProtoArea_client`. No `send_*` / `recv_*` log strings (same as 2009).
