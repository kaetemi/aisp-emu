using aisp.Network;
using aisp.Network.Data;

public class NpcNotifyData(uint result, uint npcObjectId, CharaData charaData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(npcObjectId);
        writer.Write(charaData.ToBytes());
        writer.Write((byte)0); // trailing byte read by ReadNpcData
        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 <c>recv_notify_monster_data</c> <c>0xCD67</c> (parser <c>0x6ab4d8</c>).
    /// Result, the NPC object id, then the same short character record as the
    /// avatar notify (<c>0x696e20</c>): two ids, a 37-byte name, the 19-byte
    /// visual, one uint, the 30-byte map, and 29 equip pairs. No trailing blocks.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(npcObjectId);
        writer.Write(charaData.SlotId);
        writer.Write(charaData.ModelId);
        writer.WriteFixedString(charaData.Name, 37);
        writer.Write(charaData.Visual.ToBytes());
        writer.Write(charaData.CharacterParameterId);
        writer.Write(charaData.Map.ToBytes());
        for (var slot = 0; slot < 29; slot++)
        {
            if (slot < charaData.Equips.Count)
            {
                writer.Write(charaData.Equips[slot].ItemId);
                writer.Write(charaData.Equips[slot].Socket);
            }
            else
            {
                writer.Write(0u);
                writer.Write(0u);
            }
        }

        return writer.ToBytes();
    }
}
