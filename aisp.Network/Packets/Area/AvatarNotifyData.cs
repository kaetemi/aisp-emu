using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class AvatarNotifyData(uint Result, AvatarData avatarData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(avatarData.ToBytes());
        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 <c>recv_notify_avatar_data</c> <c>0x7D78</c> (parser <c>0x6a3f70</c>).
    /// Result, then the avatar id the select screen stored, then a short character
    /// record: two ids, a 37-byte name, the 19-byte visual, one uint, the 30-byte
    /// map, and 29 equip pairs. Five 16-byte blocks and two uints follow.
    /// The 2009/2011 body leaves unread bytes and the area socket closes.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        var character = avatarData.Character;
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(avatarData.AvatarId);
        writer.Write(character.SlotId);
        writer.Write(character.ModelId);
        writer.WriteFixedString(character.Name, 37);
        writer.Write(character.Visual.ToBytes());
        writer.Write(character.CharacterParameterId);
        writer.Write(character.Map.ToBytes());
        for (var slot = 0; slot < 29; slot++)
        {
            if (slot < character.Equips.Count)
            {
                writer.Write(character.Equips[slot].ItemId);
                writer.Write(character.Equips[slot].Socket);
            }
            else
            {
                writer.Write(0u);
                writer.Write(0u);
            }
        }

        for (var i = 0; i < 22; i++)
            writer.Write(0u);
        return writer.ToBytes();
    }
}
