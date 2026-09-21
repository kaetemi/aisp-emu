using System.Collections.Concurrent;
using aisp.Network;

namespace aisp.Common.Game;

/// <summary>
/// Remembers the version-check crc from this connection so later packets can
/// follow that client's wire. September 2008 Auth sends crc <c>0xB35DC876</c>
/// extra 2; the lobby (Msg) connection sends crc <c>0x122646E7</c> extra 3;
/// Area sends crc <c>0x9E57B1E4</c> extra 2. July 2009 sends extra <c>0x03FA6EC0</c>.
/// </summary>
public static class ClientWireProfile
{
    public const uint September2008AuthCrc = 0xB35DC876;
    public const uint September2008LobbyCrc = 0x122646E7;
    public const uint September2008AreaCrc = 0x9E57B1E4;

    private static readonly ConcurrentDictionary<Guid, uint> September2008Crc = new();

    public static void RememberVersionCheck(IPlayerSession session, uint crc, uint extra)
    {
        var september2008 =
            (crc == September2008AuthCrc && extra == 2)
            || (crc == September2008LobbyCrc && extra == 3)
            || (crc == September2008AreaCrc && extra == 2);
        if (september2008)
            September2008Crc[session.ConnectionId] = crc;
        else
            September2008Crc.TryRemove(session.ConnectionId, out _);
    }

    public static bool IsSeptember2008(IPlayerSession session) =>
        September2008Crc.ContainsKey(session.ConnectionId);

    /// <summary>
    /// September 2008 and July 2009 both enter a map on <c>0xB315</c>
    /// (<see cref="PacketType.NotifyChangeMap"/>, 98 bytes). On the 2008 exe
    /// that arm is the fall-through after <c>cmp eax, 0xB235</c> (subtract
    /// <c>0xB2A9</c>, then <c>0x6B</c>, then 1; parser <c>0x6a883c</c>).
    /// <c>0xB235</c> itself is an 8-byte two-uint packet (slot <c>0x32</c>).
    /// The 98-byte body fails that consume check and resets Area.
    /// </summary>
    public static PacketType NotifyChangeMapOpcode(IPlayerSession session)
    {
        _ = session;
        return PacketType.NotifyChangeMap;
    }

    /// <summary>
    /// 2008 <c>recv_avatar_data</c>. Same opcode as 2009, different body
    /// (<see cref="aisp.Network.Packets.Msg.AvatarDataResponse.ToSeptember2008Bytes"/>).
    /// <see cref="PacketType.AvatarDestroyResponse"/> is a 4-byte packet on this exe.
    /// </summary>
    public const PacketType September2008AvatarRecord = PacketType.AvatarDataResponse;

    /// <summary>
    /// <c>recv_get_avatar_data_r</c> while the login scene is in state <c>0x5A</c>.
    /// 0 enters state <c>0x578</c>, which shows a stored record or opens the maker
    /// when scene+0x5c is still -1. 100 forces the maker (state <c>0x3E8</c>).
    /// </summary>
    public const uint September2008AvatarListShowRecord = 0;

    public const uint September2008AvatarListForceMaker = 100;

    public const uint September2008AvatarListEmpty = September2008AvatarListShowRecord;
}
