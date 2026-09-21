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
    /// 2008 area recv enters a map on <c>0xB235</c> (later reused as
    /// <see cref="PacketType.RoboRestResponse"/>). The body is the 98-byte 2009
    /// <c>NotifyChangeMap</c>. 2009 moved that recv to <c>0xB315</c>.
    /// </summary>
    public static PacketType NotifyChangeMapOpcode(IPlayerSession session) =>
        IsSeptember2008(session) ? PacketType.RoboRestResponse : PacketType.NotifyChangeMap;

    /// <summary>
    /// 2008 lobby avatar record. Parser <c>0x6ba18a</c> reads it and keeps slot 0.
    /// <see cref="PacketType.AvatarGetDataResponse"/> then carries 0 (open the maker)
    /// or 100 (open character select). 2011 reused this opcode as
    /// <see cref="PacketType.AvatarDestroyResponse"/>. <c>0x6747</c> is not in the exe.
    /// </summary>
    public const PacketType September2008AvatarRecord = PacketType.AvatarDestroyResponse;

    public const uint September2008AvatarListEmpty = 0;
    public const uint September2008AvatarListReady = 100;
}
