using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client map change command (recv_notify_change_map, 0xB315).
/// July 2009 wire (parser <c>0x738f8d</c>, alloc 0x68): 30-byte route, FadeFlag, then
/// ServerInfo. The 2011 extra <see cref="Flag"/> byte and trailing Fade are not on this
/// wire — sending the 99-byte 2011 layout fails VCE exact-size consume and dumps the
/// client to login.
/// <see cref="Rotation"/> is degrees; written as wire half-degrees.
/// </summary>
public sealed class NotifyChangeMap : IOutgoingPacket
{
    /// <summary>Packed 2009 payload: 4×uint + XYZ + rot + anim + fade + port + IP[65] = 98.</summary>
    public const int PacketSize = 98;

    /// <summary>ChannelId..Animation, before FadeFlag. 2009 <c>0x718f50</c> / <c>0x718eb0</c>.</summary>
    public const int RouteWireSize = 30;

    public uint ChannelId { get; init; }
    public uint MapId { get; init; }
    public uint MapSerialId { get; init; }
    public uint RouteState { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }

    /// <summary>Facing in degrees.</summary>
    public int Rotation { get; init; }

    public byte Animation { get; init; }

    /// <summary>
    /// 2011-only extra byte after Animation (bit 0x2 on the later client). Omitted from the
    /// 2009 wire; kept so callers can still set it without changing C# shape.
    /// </summary>
    public byte Flag { get; init; }

    public ServerInfo AreaServerInfo { get; init; } = new("0.0.0.0", 0);
    public byte FadeFlag { get; init; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ChannelId);
        writer.Write(MapId);
        writer.Write(MapSerialId);
        writer.Write(RouteState);
        writer.Write(PositionX);
        writer.Write(PositionY);
        writer.Write(PositionZ);
        writer.Write(YawEncoding.ToWireSByte(Rotation));
        writer.Write(Animation);
        writer.Write(FadeFlag);
        writer.Write(AreaServerInfo.Port);
        writer.WriteFixedAsciiString(AreaServerInfo.IP, 65);
        return writer.ToBytes();
    }
}
