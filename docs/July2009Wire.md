# July 2009 wire deltas (DistId / change-map)

This branch targets the 2009-07-07 `ai sp@ce.exe` (build 0x45). Two mismatches against the 2011 layout crash or kick that client after Area enter.

## System / Notice DistId (`recv_talk_forward` `0x20F6`)

`TalkForwardNotify` layout is the same on both clients: `FromId`, `DistId`, NUL-terminated message (max `0x181` bytes), `BalloonId`. Parser `0x753287` (alloc `0x610`).

The DistId → chat-filter mapper is not.

| DistId | 2009 `0x41fd90` | 2011 `sub_428B10` |
| --- | --- | --- |
| `> 0` | type 3 | type 3 |
| `-1` | type 1 | type 1 |
| `-2` | type 4 | type 4 |
| `-3` | type 2 | type 2 |
| `-4` | type 3 | type 3 |
| **`-5`** | **type 0 (public)** | **type 5 (System / Notice)** |
| **`-6`** | **type 5 (System / Notice)** | type 6 |
| `-7` | type 6 | (not in the simple table) |
| `0` | type 0 | type 0 |

2009 inverse `0x41fe00` type 5 → DistId `-6`. Sending MOTD / `SystemNotice` as DistId `-5` (the 2011 value) is treated as public chat from `FromId=0`. The 2009 UI then aborts at `0x42642d` (`call eax` of vfunc `+0x168`, exception `40000015` / `STATUS_FATAL_APP_EXIT`, fault module `ai sp@ce.exe`).

`SystemNotice.DistId` on this branch is therefore `-6`. Message cap is still `MaxLineBytes` 360 plus CRLF inside the `0x181` buffer.

## `recv_notify_change_map` (`0xB315`)

2009 parser `0x738f8d`, alloc `0x68`. Readers: `0x718f50` (four uints) + `0x718eb0` (XYZ + rot + anim = 14) + `0x718050` (byte) + `0x718da0` (ushort port + 65-byte IP). Exact-size consume after that.

| | 2011 (99 bytes) | July 2009 (98 bytes) |
| --- | --- | --- |
| Route | Channel, Map, Serial, RouteState, XYZ, Rot, Anim, **Flag** (31) | Channel, Map, Serial, RouteState, XYZ, Rot, Anim (30) |
| FadeFlag | **last byte** | **immediately after Animation** |
| ServerInfo | port + IP[65] before Fade | port + IP[65] after Fade |
| Flag | extra byte after Animation | **not on the wire** |

A 99-byte 2011 packet leaves one leftover byte. VCE treats that as a malformed RPC and resets Area → login screen. That is the maplink kick.

`NotifyChangeMyRoom` (`0x0FA0`, parser `0x71de89`) uses the same 2009 prefix (route + fade + ServerInfo) then `MyRoomData` (75 bytes) at offset 98. No trailing Fade, no Flag.

`recv_notify_maplink_data` `0x5755` and `recv_notify_select_map` `0x68A5` are **absent** from the 2009 Area recv switch. Maplink volumes come from map data; the client sends `MapEnterRequest` with the *current* map and the server answers with `NotifyChangeMap`.
