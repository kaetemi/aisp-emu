using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class NotifyChangeMyRoom : IOutgoingPacket
{
    /// <summary>
    /// Offset of <see cref="Room"/> on the July 2009 wire: 30-byte route + FadeFlag + 67-byte
    /// ServerInfo (parser <c>0x71de89</c>).
    /// </summary>
    public const int RoomFieldOffset = NotifyChangeMap.RouteWireSize + 1 + 67;

    public uint ChannelId { get; init; }
    public uint MapId { get; init; }
    public uint MapSerialId { get; init; }
    public uint RouteState { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }

    /// <summary>Facing in degrees; written as wire half-degrees.</summary>
    public int Rotation { get; init; }

    public byte Animation { get; init; }

    /// <summary>2011-only; omitted from the 2009 wire (same as <see cref="NotifyChangeMap.Flag"/>).</summary>
    public byte Flag { get; init; }

    public ServerInfo AreaServerInfo { get; init; } = new("0.0.0.0", 0);
    public MyRoomData Room { get; init; } = new(0, 0, MyRoomStage.SixTatami);
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
        writer.Write(Room.ToBytes());
        return writer.ToBytes();
    }
}
