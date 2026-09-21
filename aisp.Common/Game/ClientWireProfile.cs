using System.Collections.Concurrent;

namespace aisp.Common.Game;

/// <summary>
/// Remembers the version-check crc from this connection so later packets can
/// follow that client's wire. September 2008 Auth sends crc <c>0xB35DC876</c>
/// extra 2; the lobby (Msg) connection sends crc <c>0x122646E7</c> extra 3.
/// July 2009 sends extra <c>0x03FA6EC0</c>.
/// </summary>
public static class ClientWireProfile
{
    public const uint September2008AuthCrc = 0xB35DC876;
    public const uint September2008LobbyCrc = 0x122646E7;

    private static readonly ConcurrentDictionary<Guid, uint> September2008Crc = new();

    public static void RememberVersionCheck(IPlayerSession session, uint crc, uint extra)
    {
        var september2008 =
            (crc == September2008AuthCrc && extra == 2)
            || (crc == September2008LobbyCrc && extra == 3);
        if (september2008)
            September2008Crc[session.ConnectionId] = crc;
        else
            September2008Crc.TryRemove(session.ConnectionId, out _);
    }

    public static bool IsSeptember2008(IPlayerSession session) =>
        September2008Crc.ContainsKey(session.ConnectionId);
}
