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
            int partId = packet.PartIds(i);
            var part = GunPartTable.Instance.Get(partId);
            if (part == null) continue;

            int level = packet.PartLevels(i);
            int partUid = i < packet.PartUidsLength ? packet.PartUids(i) : 0;

            // 게스트 로컬 WEM에도 파츠 매핑/강화 레벨을 저장해, 이후 파츠 패널의 uid 복원(GetPartUid)과
            // 해제 시 강화 유지가 게스트에서도 동작하게 한다 (씬 리로드로 복원된 총도 포함)
            if (partUid != 0)
            {
                WorldEquipmentManager.SetPart(packet.GunUid, part.SlotType, partId, partUid);
                WorldEquipmentManager.SetEnhancementLevel(partUid, level, partId);
            }

            gun.EquipPart(WorldEquipmentManager.BuildEnhancedGunPart(part, level));
        }

        // 저장된 탄약이 없으면 장착 시점의 풀장전 기본값을 그대로 둔다
        if (packet.HasAmmo)
            gun.SetAmmo(packet.CurrentAmmo);

        // 인벤 무기 파츠 패널이 프리뷰 총을 갱신할 수 있도록 알린다
        GunBase.RaiseGunStateSynced(packet.GunUid);
    }
}
