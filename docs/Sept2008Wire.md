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

`recv_check_version_r` `0xB6B4` alloc is **16** bytes on both (Result + 3 uints). Echoing the 12-byte request as Result=0 + those three uints is what 2009 accepts (then `send_authenticate`).

### Two 2008 version-check callbacks

Login FSM state 3 sends the extra=2 check via proto `0x89f600` (`0x6936b0`). That proto’s recv (`0x6b56b0`) handles `0xB6B4` and calls +0x64 **`0x692230`**:

```
if (Result == 0) ok;
else if (Major == 0 && Ver == 2) ok;
else set fail flag +0x105;  // login error 9
```

CProtoAuth (`0x89f604`) has a later check at state 61/62 (`0x693700` sends extra=**3**, crc `0x122646E7`) whose +0x64 is **`0x6924c0`** (same shape but Ver==**3**).

Live 2026-09-21 (reconfirmed): Result=0 echo of extra=2 is consumed. Auth TCP stays ESTAB. No `send_authenticate`. Login state 4 then **E000** 「サーバーに接続できませんでした」. Result=1, Ver=3 hits `0x692230`’s fail path → 「クライアントのバージョンが更新されています」.

2009 does **not** contain crc `0xB35DC876` or extra=3 crc `0x122646E7`. July 2009 has no extra=2 bootstrap proto; it version-checks once on CProtoAuth (`extra=0x03FA6EC0`) and authenticates ~90 ms later.

### Login FSM (`0x60d5c0`, state at `this+8`)

`0x89f600` is a 0x108-byte generated codec (ctor `0x6921e0`, vtable `0x813db4`). `[proto+0x100]` is a self-pointer. `0x89f604` is CAIProtoAuth (size 0x714, session at +0x70c). `0x89f608` is Area (size 0x2050, session at +0x2048).

The extra=2 codec **is** Auth, not a throwaway ping. CProtoAuth extra=3 is a later reconnect after world select.

| State | Handler | What |
| --- | --- | --- |
| 0 | `0x60d62f` | Load leftover Auth host/port (`0x5fe9c0` / `0x5fe9f0`) |
| 3 | `0x60d7ff` | `0x6936b0`: `VCE::Connect` + `send_check_version` extra=2 |
| 4 | `0x60d814` | Wait version-check, then `send_authenticate` `0xF24B` (`0x6b5240`, 3 CStrings max 0x41) |
| 10 | (idle) | No switch arm; worldlist is state 20 |
| 20 | `0x60daf7` | `send_get_worldlist` `0x6676` (`0x6b5370`) |
| 60 | `0x60dcd9` | `0x692fc0` Close extra=2 codec |
| 61 | `0x60dd0a` | `0x693700` Connect CProtoAuth extra=3 |
| 62 | `0x60dd45` | Wait CProtoAuth GetState 3/4, then authenticate on `0x89f604` |

### Login FSM state 4 (`0x60d814`)

Each tick:

1. `0x692f50` — keep waiting if GetState==**1 (CONNECTING)** **or** recv flag `proto+0x104` is still 0.
2. Else `0x692f80` — require `+0x104 != 0` **and** GetState **3 or 4**. Fail → `0x60dd9e` (Close all three codecs, state=0) → Closed callback `0x60d470` → E000.
3. Else if `+0x105` → error 9 (please-update).
4. Else `0x60ce70` (credential strings from `0x5fea30` login-id / `0x5fea60` password) then `0x6b5240` `0xF24B`.

`0x692230` always sets `+0x104=1` when `0xB6B4` is parsed, then sets `+0x105` only on version fail. After a Result=0 echo, step 1 no longer waits unless GetState stays CONNECTING.

Codec GetState (`vtable+0x10` = `0x74a350`) is `inner = this+4` (iSession*); null → 0. Connect attach (`0x76b449`) sets `codec+4` = session and `codec+0xc` = VCE (send allocator). Inner `vtable+0xc` on `iTcpStream` / `iCryptSession<iTcpStream, CamelliaCrypter<128>>` is **`0x769900`**, which maps `[session+0x1c0]` (internal 0–11) to `VCE_STATE`:

| Internal | GetState |
| --- | --- |
| 0–3 | 2 PREPROCESS |
| 4–5 | 1 CONNECTING |
| 6–10 | 3 ESTABLISHED |
| 11 | 5 CLOSED |
| else / null inner | 0 UNKNOWN |

State 4 waits only on CONNECTING (internal 4–5). Internal 2–3 still read as PREPROCESS, so the FSM does **not** wait and E000s.

Live 2026-09-21 (32-bit `LD_PRELOAD` hook on `0x74a350`, on-disk PE not patched): after `VCE::Connect`, `codec+4` is a live `iCryptSession` and **GetState is already 3 ESTABLISHED**. Extra=2 `0xB6B4` is parsed (`+0x104=1`). E000 still happens and **`0xF24B` is never sent**. GetState is not the remaining hole.

### HTTPS niconico login (the E000 after extra=2)

State 4 then calls `0x60ce70` → `0x61b370`, which is WinINet, not VCE:

| Call | Live log |
| --- | --- |
| `InternetOpenW` | agent `aispace` |
| `InternetConnectW` | host **`secure.nicovideo.jp`** port **443** service 3 (HTTP) |
| `HttpOpenRequestW` | verb **`POST`** path **`secure/login`** flags `0x4800000` (`INTERNET_FLAG_SECURE\|NO_CACHE_WRITE`) |

That is `https://secure.nicovideo.jp/secure/login` (`Content-Type: application/x-www-form-urlencoded`). Body is written with `InternetWriteFile`; status via `HttpQueryInfoW` (`HTTP_QUERY_STATUS_CODE`); body via `InternetReadFile` into `0x61b310`. The body is XML: first element `status` attribute `ok`/`fail`, child `ticket` on success, `error`/`code` on fail.

`scripts/sept2008/run.sh` LD_PRELOADs 32-bit `aisp-https-redirect.so` into the wine process (not wineserver64). It rewrites `nicovideo`/`niconico` hosts to `AISP_NICO_LOGIN_HOST`:`AISP_NICO_LOGIN_PORT` (default `127.0.0.1:8081`) and strips `INTERNET_FLAG_SECURE` so Wine talks HTTP to the emulator.

Live POST body: `site=aispace&mail=<login-id>&password=<password>`. `POST /secure/login` returns XML with `status="ok"` and `<ticket>` equal to `mail`, because `send_authenticate` `0xF24B` is `<ticket>\0<password>\0`.

Failures in WinINet set error 1–9 and `0x60ce70` returns 0 → E000. A hardcoded ticket of `local` reaches `0xF24B` but Auth looks up user `local` and the client shows 「サーバーからエラーが返されました。(2)」. 2009 dropped this HTTP step (niconico is the launcher).

ESTABLISHED writers include `0x76bc01` / `0x76c41d` (→6) and `0x76bed1` (→7). iCryptSession keyex lives at `+0x1e8` (`KEYEX_*`); GetState does not read it.

Patching `0x692f80` to `return 1` on disk makes this exe `Initialize failed. [ Code : -1 ]` (likely a .text checksum) — do not patch the binary. Runtime IAT hooks after Initialize are fine (`sept2008/aisp-https-redirect.c`).

## Avatar create info (live 2026-09-21)

Empty `recv_get_avatar_data_r` (`0xB055`, 4 bytes, result 0) is accepted. The client then asks `send_get_avatar_create_info` `0x04F6` on the lobby connection (CProtoAuth, the same object that version-checked with extra=3). The 2009 five-list body (builds, faces, hair, colours, equip) is rejected: parser `0x6bb39f` calls vtable+0x5c with `(0x0d, 1)` and RSTs Msg. The make map `10900100` stays on screen with no doll and no `chrmake` widgets.

`0xA5AD` on this build is four lists per gender, male then female. Counts above the max fail the parse. Bytes are packed with no alignment pad. The reader requires the cursor to land on the end.

| | Male / female | Max | Doll use |
| --- | --- | --- | --- |
| Faces | byte | 4 | face id at doll+0x3c |
| Hair bases | uint | 4 | |
| Color offsets | byte | 5 | hair id = base + offset, doll+0x40 |
| Equipment | uint id, uint socket | 29 | fixed 29 slots at +0x118 / +0x200 |

Body models are not in the packet. Gender 1 spawns `1001011`, gender 2 spawns `1002011`. Hair bases `10920010`/`10920020`/`10920030`/`10920040` and `10930010` + 0x10 steps, plus offsets 0..4, match the catalog rows (ショート系 水色/茶/ピンク/金/黒 and the same step on the other three named styles).

July 2009 inserted a leading build-uint list (max 3) and raised the equip cap to 30. Sessions whose version-check crc is `0x122646E7` extra 3 (or Auth `0xB35DC876` extra 2) get the four-list body. Anything else, including 2009 extra `0x03FA6EC0`, keeps the five-list body.

`これでよし！` on the maker plays the local intro train, then the profile sheet (名前 / 誕生日 / 血液型). `send_avatar_create` `0x29A4` is name, the same 19-byte `CharaVisual`, and slot. There is no model-id uint in front. Gender 2 is the female body `1002011`. A captured create (`eina`, 9/8, blood A, face 2, hair `10930020`, slot 0) is 28 bytes. Reading a model id first consumes the visual and the slot read fails.

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

`recv_notify_change_map` on this exe is opcode **`0xB235`** (area parser `0x6a8663`, alloc `0x68`), not `0xB315`. The body is the 98-byte July 2009 layout: 30-byte route, fade byte, port uint16, 65-byte IP. `0xB315` is absent. 2011 reused `0xB235` as `recv_robo_rest_r`. Area version-check is extra 2, crc `0x9E57B1E4` (`0x693792`). `recv_notify_maplink_data` `0x5755` is present (25 bytes, same as the 2011 reader). `recv_notify_select_map` `0x68A5` is absent.

`recv_avatar_create_r` `0x788F` is one uint (parser `0x6ba77b`, alloc 4). Result 0 closes the maker. The name-in-use and blocked-name strings are selected only for results `0xFFFFFF90` and `0xFFFFFF8F`; any other nonzero result shows 「アバター作成に失敗しました」. `recv_avatar_data` `0x6747` does not occur in the exe. An unknown lobby opcode is handed to `0x6b82e0`, which reports error `0x61` unless the body begins with word `0xC202`, so `0x6747` must not be sent.

The lobby avatar record on this build is opcode **`0x6587`** (parser `0x6ba18a`, alloc `0x128`). 2011 reused that value as `recv_avatar_destroy_r`. Wire: uint id, CString name (scan limit `0x25`), the 19-byte visual, uint island, uint slot, then 29 item ids. The callback keeps the record only when slot is 0. `recv_get_avatar_data_r` `0xB055` is still one uint, and only while the login scene state is `0x5A`: 0 moves to the maker (`0x578`), 100 moves to character select (`0x3E8`), anything else is the error state. Do not send `0x6587` yet. Live, both an early push and a reply to `send_get_avatar_data` close Msg, and the make map comes back with no widgets. `0xB055` alone stays up: 0 and 100 both reach the gender maker (100 is scene state `0x3E8`, which asks for the create catalog). Create result 0 alone advances to the アンケート screen. The saved character is female `eina`, model `1002011`, face 2, hair `10930020`.

## MOTD

Leave `Motd__Enabled=false` until DistId `-5`/`-6`/`-7` are proven not to abort this build. 2009 aborts at `0x42642d` for those DistIds.

## VCE

Same family as 2009/2011: vce2, VS2005, Camellia-128, RSA-16 key exchange. RTTI `CProtoAuth_client` / `CProtoMsg_client` / `CProtoArea_client`. No `send_*` / `recv_*` log strings (same as 2009).
