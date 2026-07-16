public static partial class PacketHandlers
{
    // G_Equip 응답 — 호스트가 보관 중이던 탄약/파츠를 방금 장착한 내 총에 복원한다 (게스트에서 실행)
    // 내구도는 H_Durability가 따로 담당한다
    public static void OnH_GunState(FlatPacket root)
    {
        var packet = root.TypeAsH_GunState();
        if (EquippableItemBase.FindByUid(packet.GunUid) is not GunBase gun) return;

        // 파츠를 먼저 — EquipPart가 RecalculateStat을 호출해 최대 장탄수를 갱신하므로 탄약 복원보다 앞서야 한다
        for (int i = 0; i < packet.PartIdsLength; i++)
        {
            var part = GunPartTable.Instance.Get(packet.PartIds(i));
            if (part == null) continue;
            gun.EquipPart(WorldEquipmentManager.BuildEnhancedGunPart(part, packet.PartLevels(i)));
        }

        // 저장된 탄약이 없으면 장착 시점의 풀장전 기본값을 그대로 둔다
        if (packet.HasAmmo)
            gun.SetAmmo(packet.CurrentAmmo);
    }
}
