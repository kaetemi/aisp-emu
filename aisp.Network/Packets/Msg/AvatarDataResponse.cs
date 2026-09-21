using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class AvatarDataResponse(
    uint avatarId,
    string name,
    uint modelId,
    uint islandId,
    uint slotId
) : IOutgoingPacket
{
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public List<ItemSlotInfo> Equips = new(30);

    //equips?

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public void AddEquip(
        IEnumerable<CharacterEquipSlot> equipment,
        Func<CharacterEquipSlot, uint> resolveSocket
    )
    {
        for (byte slot = 0; slot < 30; slot++)
        {
            if (!equipment.Any(e => e.SlotIndex == slot))
            {
                AddEquip(0, 0);
                continue;
            }

            var eq = equipment.First(e => e.SlotIndex == slot);
            AddEquip(eq.ItemId, resolveSocket(eq));
        }
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(avatarId); // AvatarId
        writer.Write(name, "utf-8");
        writer.Write(modelId);
        writer.Write(Visual.ToBytes());
        writer.Write(islandId);
        writer.Write(slotId);
        foreach (var equip in Equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 <c>recv_avatar_data</c> <c>0x6747</c> (parser <c>0x6ba28b</c>).
    /// The opcode is not a direct compare; the switch subtracts <c>0x6719</c> then <c>0x2e</c>.
    /// No model-id uint. The visual is the same 19 bytes as create. Slot must be 0
    /// or the client drops the record. Equipment is 29 item ids, not id/socket pairs.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        var writer = new PacketWriter();
        writer.Write(avatarId);
        writer.Write(name, 0x24, "utf-8");
        writer.Write(Visual.ToBytes());
        writer.Write(islandId);
        writer.Write(slotId);
        for (var slot = 0; slot < 29; slot++)
            writer.Write(slot < Equips.Count ? Equips[slot].ItemId : 0u);
        return writer.ToBytes();
    }
}
