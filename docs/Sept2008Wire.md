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
| Docker | `aisp-sept2008-server` on **60050/60052/60054** and HTTP **8081** (2009 keeps 50050) |

Do not inject the 2011-era `aisp.launch.exe` that shipped in the RAR. Launch `ai sp@ce.exe ./data` until `aisp.hook` addresses are retargeted.

The 2008 exe does **not** read `connection.txt` (that file is aisp.launch / aisp.hook). Direct launch connects to leftover production Auth on **`119.75.227.0/24`**, seen as **`119.75.227.142:50050`** and **`119.75.227.141:50051`**. `sept2008/aisp-redirect-connect.so` is `LD_PRELOAD`'d into **wineserver64 only** (a 64-bit .so on the 32-bit wine process is ignored and can skip the wineserver socket path) and maps that /24 to `127.0.0.1`, with 50050/50051→60050, 50052/50053→60052, 50054/50055→60054 so the July 2009 server can stay on 50050.

## Auth version check (live 2026-09-21)

VCE RSA-16 + Camellia-128 handshake succeeds. First packet is `send_check_version` `0x62BC`, **12 bytes**, same opcode as 2009.

| | 2008-09-16 Auth | 2009-07-07 Auth |
| --- | --- | --- |
| Payload | `00 00 00 00` + `76 C8 5D B3` + `02 00 00 00` | `00 00 00 00` + `D0 12 8E A9` + `C0 6E FA 03` |
| Fields | uint 0, crc `0xB35DC876`, extra **2** | uint 0, crc `0xA98E12D0`, extra `0x03FA6EC0` |
| Send site | `push 2; push crc; push 0` at `0x6936e3` | `push 0x03FA6EC0; push crc; push 0` at `0x713733` |

`recv_check_version_r` `0xB6B4` alloc is **16** bytes on both (Result + 3 uints). Echoing the 12-byte request as Result=0 + those three uints is what 2009 accepts (then `send_authenticate`). This 2008 build stays on Auth TCP, never sends authenticate, and shows **E000** 「サーバーに接続できませんでした」. Not a VCE RST (connection stays ESTAB). Callback after the 16-byte parse is the next hole.

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
