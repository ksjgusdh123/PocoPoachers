public static partial class PacketHandlers
{
    public static void OnG_GunAmmoSave(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_GunAmmoSave();
        WorldEquipmentManager.SetAmmo(pkt.GunUid, pkt.CurrentAmmo, pkt.MaxMagazine);
    }
}
