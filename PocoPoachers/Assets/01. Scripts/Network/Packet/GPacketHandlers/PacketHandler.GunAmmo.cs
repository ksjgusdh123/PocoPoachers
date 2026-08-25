public static partial class PacketHandlers
{
    public static void OnG_GunAmmoSave(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_GunAmmoSave();
        WorldEquipmentManager.SetAmmo(pkt.GunUid, pkt.CurrentAmmo, pkt.MaxMagazine);

        // 영속 저장소만 고치면 부족하다 — TryAuthorizeHostShot은 호스트 씬에 살아있는
        // 게스트 총 사본의 탄약을 깎으므로, 그 사본도 같이 맞춰줘야 재장전 후에도 사격이 인증된다.
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;
        if (!GuestValidator.TryGetGuestWeapon(guestId, out var gun)) return;
        if (gun.Uid != pkt.GunUid) return;

        gun.SetAmmo(pkt.CurrentAmmo);
    }
}
