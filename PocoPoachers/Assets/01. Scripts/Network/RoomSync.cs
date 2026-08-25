using System.Collections.Generic;
using UnityEngine;

public static class RoomSync
{
    private static int MyId => NetworkManager.Instance?.MyPlayerId ?? 0;

    // 드론처럼 "내 것"을 playerId로 등록해야 하는 쪽에서 쓴다
    public static int MyPlayerId => MyId;
    private static bool IsSolo => RoomManager.IsHost && !RoomManager.HasGuests;

    public static void Move(Vector3 pos, float yaw, sbyte moveType, float velX, float velZ, bool isSprinting, bool isRolling, bool isAiming = false, bool isReloading = false, bool isDown = false)
    {
        if (IsSolo) return;

        var vec = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z };
        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_MoveT { PlayerId = id, Pos = vec, Rotation = yaw, MoveType = moveType, VelocityX = velX, VelocityZ = velZ, IsSprinting = isSprinting, IsRolling = isRolling, IsAiming = isAiming, IsReloading = isReloading, IsDown = isDown },
                H_Move.Pack, PacketType.H_Move);
        else
            PacketBuilder.SendToHost(
                new G_MoveT { PlayerId = id, Pos = vec, Rotation = yaw, MoveType = moveType, VelocityX = velX, VelocityZ = velZ, IsSprinting = isSprinting, IsRolling = isRolling, IsAiming = isAiming, IsReloading = isReloading, IsDown = isDown },
                G_Move.Pack, PacketType.G_Move);
    }

    // pelletDirections: 샷건 등 다발 발사의 펠릿별 방향. null/비어 있으면 direction 단발로 처리
    public static void Shoot(Vector3 origin, Vector3 direction, GunStatData stat, IReadOnlyList<Vector3> pelletDirections = null, bool isHeadshot = false, List<int> bulletSeqs = null)
    {
        if (IsSolo) return;

        var originT = new Vec3T { X = origin.x, Y = origin.y, Z = origin.z };
        var dirT    = new Vec3T { X = direction.x, Y = direction.y, Z = direction.z };
        List<Vec3T> dirsT = ToVec3TList(pelletDirections);
        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_ShootT { PlayerId = id, Origin = originT, Direction = dirT, BulletSpeed = stat.BulletSpeed, Damage = stat.Damage, MaxRange = stat.BulletRange, Directions = dirsT, IsHeadshot = isHeadshot, BulletSeqs = bulletSeqs },
                H_Shoot.Pack, PacketType.H_Shoot);
        else
            PacketBuilder.SendToHost(
                new G_ShootT { PlayerId = id, Origin = originT, Direction = dirT, BulletSpeed = stat.BulletSpeed, Damage = stat.Damage, MaxRange = stat.BulletRange, SoundRange = stat.SoundRange, Directions = dirsT, IsHeadshot = isHeadshot, BulletSeqs = bulletSeqs },
                G_Shoot.Pack, PacketType.G_Shoot);
    }

    private static List<Vec3T> ToVec3TList(IReadOnlyList<Vector3> dirs)
    {
        if (dirs == null || dirs.Count == 0) return null;
        var list = new List<Vec3T>(dirs.Count);
        for (int i = 0; i < dirs.Count; i++)
            list.Add(new Vec3T { X = dirs[i].x, Y = dirs[i].y, Z = dirs[i].z });
        return list;
    }

    public static void Equip(int itemId, int slotIndex, int itemUid = 0)
    {
        if (IsSolo) return;

        int id = MyId;

        // 장착은 상태 변화(1회성)라 유실되면 복구 경로가 없다 — 게스트 경로와 동일하게 신뢰 전송
        if (RoomManager.IsHost)
            PacketBuilder.BroadcastReliableToGuests(
                new H_EquipT { PlayerId = id, ItemId = itemId, ItemUid = itemUid, SlotIndex = slotIndex },
                H_Equip.Pack, PacketType.H_Equip);
        else
            PacketBuilder.SendReliableToHost(
                new G_EquipT { PlayerId = id, ItemId = itemId, ItemUid = itemUid, SlotIndex = slotIndex },
                G_Equip.Pack, PacketType.G_Equip);
    }

    // 장비 내구도 변화(발사/피격 등) — 호스트가 실제 값을 계산해서 결과를 브로드캐스트
    public static void Durability(int itemUid, int itemId, float amount, float defaultMaxDurability)
    {
        if (itemUid == 0) return; // uid가 없는(추적되지 않는) 아이템은 동기화하지 않음

        if (RoomManager.IsHost)
        {
            var (current, max) = WorldEquipmentManager.ApplyChange(itemUid, itemId, amount, defaultMaxDurability);
            if (RoomManager.HasGuests)
                PacketBuilder.BroadcastToGuests(new H_DurabilityT { ItemUid = itemUid, Current = current, Max = max },
                    H_Durability.Pack, PacketType.H_Durability);
        }
        else
        {
            PacketBuilder.SendReliableToHost(new G_DurabilityT { ItemUid = itemUid, ItemId = itemId, Amount = amount },
                G_Durability.Pack, PacketType.G_Durability);
        }
    }

    // 구출 진행 알림 — 구출자가 보내면 호스트가 구출자·대상 두 명에게만 되돌려준다
    // 진행 시간 판정은 구출자 로컬 코루틴이 하고, 이 패킷은 양쪽 게이지 UI 표시용이다
    public static void Rescue(int targetId, RescueState state, float duration)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            RescueRelay.Relay(MyId, targetId, state, duration);
        else
            PacketBuilder.SendReliableToHost(new G_RescueT
            {
                TargetId = targetId,
                State    = (sbyte)state,
                Duration = duration,
            }, G_Rescue.Pack, PacketType.G_Rescue);
    }

    // 완전 사망 시 호송 빔 연출을 팀원에게도 재생시킨다 — 로컬 재생은 호출부(PlayerController)가 별도로 처리한다
    public static void RescueBeamPlay()
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            BroadcastRescueBeamPlay(MyId);
        else
            PacketBuilder.SendReliableToHost(new G_RescueBeamPlayT(), G_RescueBeamPlay.Pack, PacketType.G_RescueBeamPlay);
    }

    // 호스트가 특정 플레이어의 호송 빔 연출을 전체 게스트에 전파 (게스트 요청 중계 또는 호스트 자신의 사망)
    public static void BroadcastRescueBeamPlay(int playerId)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastReliableToGuests(new H_RescueBeamPlayT { PlayerId = playerId }, H_RescueBeamPlay.Pack, PacketType.H_RescueBeamPlay);
    }

    // 무기 해제 시점의 탄약 저장 — 호스트는 직접 저장하고, 게스트는 호스트에게 요청한다
    public static void GunAmmoSave(int gunUid, int currentAmmo, int maxMagazine)
    {
        if (gunUid == 0) return;

        if (RoomManager.IsHost)
        {
            Debug.Log($"[AmmoSave] 저장 uid={gunUid} ammo={currentAmmo}/{maxMagazine}"); // TODO: 디버그 후 제거
            WorldEquipmentManager.SetAmmo(gunUid, currentAmmo, maxMagazine);
        }
        else
        {
            PacketBuilder.SendReliableToHost(new G_GunAmmoSaveT
            {
                GunUid      = gunUid,
                CurrentAmmo = currentAmmo,
                MaxMagazine = maxMagazine,
            }, G_GunAmmoSave.Pack, PacketType.G_GunAmmoSave);
        }
    }

    // 총 파츠 장착/해제 — partId=0이면 해제. 호스트는 직접 저장하고, 게스트는 호스트에게 요청한다
    // currentAmmo/maxMagazine은 파츠 변경 직후 호출자(자기 자신) 총의 실제 값을 그대로 전달한다
    // partLevel: 파츠의 강화 레벨. 게스트가 로컬에서 계산해 호스트에 알려준다(호스트는 partUid 기준으로 저장).
    // 호스트가 직접 호출할 땐 이미 자기 WEM에 레벨이 있으므로 무시된다.
    public static void GunPartEquip(int gunUid, SlotType slotType, int partId, int partUid, int partLevel, int currentAmmo, int maxMagazine)
    {
        if (gunUid == 0) return;

        if (RoomManager.IsHost)
        {
            if (partId != 0) WorldEquipmentManager.SetPart(gunUid, slotType, partId, partUid);
            else WorldEquipmentManager.RemovePart(gunUid, slotType);
            WorldEquipmentManager.SetAmmo(gunUid, currentAmmo, maxMagazine);
        }
        else
        {
            PacketBuilder.SendReliableToHost(new G_GunPartEquipT
            {
                GunUid      = gunUid,
                SlotType    = (int)slotType,
                PartId      = partId,
                PartUid     = partUid,
                PartLevel   = partLevel,
                CurrentAmmo = currentAmmo,
                MaxMagazine = maxMagazine,
            }, G_GunPartEquip.Pack, PacketType.G_GunPartEquip);
        }
    }

    // 아이템 강화 즉시 동기화 — 게스트가 강화한 순간 호스트에 알려 uid 기준으로 레벨을 저장하게 한다.
    // 호스트/솔로는 이미 로컬 WEM에 반영돼 있으므로 보내지 않는다.
    public static void EnhanceItem(int itemUid, int itemId, int level)
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(new G_EnhanceItemT
        {
            ItemUid = itemUid,
            ItemId  = itemId,
            Level   = level,
        }, G_EnhanceItem.Pack, PacketType.G_EnhanceItem);
    }

    // 인벤토리(미장착) 무기의 파츠 상태를 호스트에 요청 (게스트 전용).
    // 응답은 H_GunState로 오고, uid가 일치하는 프리뷰 총에 적용된다.
    public static void RequestGunState(int gunUid)
    {
        if (RoomManager.IsHost || gunUid == 0) return;

        PacketBuilder.SendReliableToHost(new G_RequestGunStateT
        {
            PlayerId = MyId,
            GunUid   = gunUid,
        }, G_RequestGunState.Pack, PacketType.G_RequestGunState);
    }

    public static void ItemGain(bool isPlayerGained, int boxUid, int itemTypeId, int itemUid, int amount, int addedSlotIndex, int removedSlotIndex)
    {
        if (IsSolo) return;
        PacketBuilder.SendReliableToHost(new G_ItemGainT
        {
            IsPlayerGained   = isPlayerGained,
            BoxUid           = boxUid,
            ItemTypeId       = itemTypeId,
            ItemUid          = itemUid,
            Amount           = amount,
            AddedSlotIndex   = addedSlotIndex,
            RemovedSlotIndex = removedSlotIndex,
        }, G_ItemGain.Pack, PacketType.G_ItemGain);
    }

    public static void ItemExchange(int boxUid, int playerItemId, int playerItemAmount, int playerItemUid, int playerSlotIndex, int boxItemId, int boxItemAmount, int boxItemUid, int boxSlotIndex)
    {
        if (IsSolo) return;
        PacketBuilder.SendReliableToHost(new G_ItemExchangeT
        {
            BoxUid           = boxUid,
            PlayerItemId     = playerItemId,
            PlayerItemAmount = playerItemAmount,
            PlayerItemUid    = playerItemUid,
            PlayerSlotIndex  = playerSlotIndex,
            BoxItemId        = boxItemId,
            BoxItemAmount    = boxItemAmount,
            BoxItemUid       = boxItemUid,
            BoxSlotIndex     = boxSlotIndex,
        }, G_ItemExchange.Pack, PacketType.G_ItemExchange);
    }

    // 게스트가 인벤 아이템을 월드에 버리기 요청 (게스트→호스트). 호스트가 LootBox를 스폰하고 H_ItemSpawn으로 전파한다.
    public static void DropItem(int itemId, int amount, int itemUid, Vector3 pos, float rotation)
    {
        if (IsSolo) return;
        PacketBuilder.SendReliableToHost(new G_DropItemT
        {
            ItemId   = itemId,
            Amount   = amount,
            ItemUid  = itemUid,
            Pos      = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
        }, G_DropItem.Pack, PacketType.G_DropItem);
    }

    public static void ItemBoxUpdate(int boxUid, int itemTypeId, int amount, int slotIndex, int itemUid = 0)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
        {
            BoxUid     = boxUid,
            ItemTypeId = itemTypeId,
            ItemUid    = itemUid,
            Amount     = amount,
            SlotIndex  = slotIndex,
        }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
    }

    public static void StatSync(float hp, float maxHp, float stamina = 0f, float battery = 0f, float defense = 0f)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(new H_StatSyncT
            {
                PlayerId = MyId,
                Hp       = hp,
                MaxHp    = maxHp,
                Stamina  = stamina,
                Battery  = battery,
                Defense  = defense,
            }, H_StatSync.Pack, PacketType.H_StatSync);
        else
            PacketBuilder.SendToHost(new G_StatSyncT
            {
                Hp      = hp,
                MaxHp   = maxHp,
                Stamina = stamina,
                Battery = battery,
                Defense = defense,
            }, G_StatSync.Pack, PacketType.G_StatSync);
    }

    public static void Leave()
    {
        int id = MyId;
        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_LeaveT { PlayerId = id, IsHost = true },
                H_Leave.Pack, PacketType.H_Leave);
        else
            PacketBuilder.SendToHost(
                new G_LeaveT { PlayerId = id },
                G_Leave.Pack, PacketType.G_Leave);
    }

    public static void ShelterLevel(int level)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_ShelterLevelT { Level = level },
                H_ShelterLevel.Pack, PacketType.H_ShelterLevel);
        else
            PacketBuilder.SendReliableToHost(
                new G_ShelterLevelT { Level = level },
                G_ShelterLevel.Pack, PacketType.G_ShelterLevel);
    }

    // 퀘스트 수락 동기화 - ShelterLevel과 동일한 패턴(파티 공유 상태). 호스트면 전원에게 브로드캐스트,
    // 게스트면 호스트에게 요청만 보내고 호스트가 확인 후 다시 전원에게 브로드캐스트한다.
    public static void QuestAccept(int questId)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastReliableToGuests(
                new H_QuestAcceptT { QuestId = questId },
                H_QuestAccept.Pack, PacketType.H_QuestAccept);
        else
            PacketBuilder.SendReliableToHost(
                new G_QuestAcceptT { QuestId = questId },
                G_QuestAccept.Pack, PacketType.G_QuestAccept);
    }

    // 퀘스트 완료 동기화 - QuestAccept와 동일한 패턴(상태를 Completed로 맞추는 거라 멱등).
    public static void QuestComplete(int questId)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastReliableToGuests(
                new H_QuestCompleteT { QuestId = questId },
                H_QuestComplete.Pack, PacketType.H_QuestComplete);
        else
            PacketBuilder.SendReliableToHost(
                new G_QuestCompleteT { QuestId = questId },
                G_QuestComplete.Pack, PacketType.G_QuestComplete);
    }

    // 퀘스트 제출 동기화 - QuestAccept와 달리 "누적값에 더하는" 연산이라 멱등이 아니다.
    // 그래서 이 메서드는 순수하게 "전송"만 한다 - 호출 쪽(QuestDescriptionUI)이 호스트/솔로일 때만
    // QuestManager.AddSubmitted를 직접 부르고, 게스트는 이 전송만 하고 로컬 적용은 H_QuestSubmit을
    // 받을 때(OnH_QuestSubmit)까지 미룬다. 안 그러면 호스트의 확인 브로드캐스트가 돌아올 때 이중 집계된다.
    public static void QuestSubmit(int questId, int itemId, int amount)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastReliableToGuests(
                new H_QuestSubmitT { QuestId = questId, ItemId = itemId, Amount = amount },
                H_QuestSubmit.Pack, PacketType.H_QuestSubmit);
        else
            PacketBuilder.SendReliableToHost(
                new G_QuestSubmitT { QuestId = questId, ItemId = itemId, Amount = amount },
                G_QuestSubmit.Pack, PacketType.G_QuestSubmit);
    }

    // 게스트가 씬 전환 직전에 자기 상태를 호스트 세이브에 올린다(오토세이브 + 인벤 미러 동기화).
    // 자기 복원은 GuestStateCarry가 하므로, 이 패킷이 늦게 도착해도 게스트 쪽은 영향이 없다.
    public static void GuestSnapshot(
        List<SaveManager.SlotSaveEntry> inventory,
        List<SaveManager.EquipSlotEntry> equips,
        List<SaveManager.SlotSaveEntry> quickSlots)
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(new G_GuestSnapshotT
        {
            PlayerId   = MyId,
            Inventory  = ToInvEntries(inventory),
            Equips     = ToEquipEntries(equips),
            QuickSlots = ToInvEntries(quickSlots),
        }, G_GuestSnapshot.Pack, PacketType.G_GuestSnapshot);
    }

    static List<GuestInvEntryT> ToInvEntries(List<SaveManager.SlotSaveEntry> slots)
    {
        var result = new List<GuestInvEntryT>();
        if (slots == null) return result;

        foreach (var s in slots)
            result.Add(new GuestInvEntryT { SlotIndex = s.slotIndex, ItemId = s.itemId, Amount = s.amount, ItemUid = s.uid });
        return result;
    }

    static List<GuestEquipEntryT> ToEquipEntries(List<SaveManager.EquipSlotEntry> slots)
    {
        var result = new List<GuestEquipEntryT>();
        if (slots == null) return result;

        foreach (var s in slots)
            result.Add(new GuestEquipEntryT { SlotIndex = s.slotIndex, ItemId = s.itemId, ItemUid = s.uid });
        return result;
    }

    // 호스트가 팀 이동 전에 게스트 동의를 구한다. 응답은 G_MoveReply로 돌아온다.
    public static void MoveRequest(string sceneName, SpawnId spawnId)
    {
        PacketBuilder.BroadcastReliableToGuests(
            new H_MoveRequestT { SceneName = sceneName, SpawnId = (int)spawnId },
            H_MoveRequest.Pack, PacketType.H_MoveRequest);
    }

    // 게스트가 이동 제안에 답한다.
    public static void MoveReply(bool accepted)
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(
            new G_MoveReplyT { PlayerId = MyId, Accepted = accepted },
            G_MoveReply.Pack, PacketType.G_MoveReply);
    }

    // 투표 현황을 게스트에게 뿌린다. 게스트도 같은 인원 아이콘 열을 그린다.
    public static void MoveProgress(List<int> memberIds, List<bool> accepted)
    {
        PacketBuilder.BroadcastReliableToGuests(
            new H_MoveProgressT { MemberIds = new List<int>(memberIds), Accepted = new List<bool>(accepted) },
            H_MoveProgress.Pack, PacketType.H_MoveProgress);
    }

    // 탈출 구역 상태 — 게스트의 알림 UI(인원 아이콘·게이지)와 결과창을 호스트 판정에 맞춘다.
    public static void EscapeState(bool active, float duration, bool completed, List<bool> inside, bool charging, List<int> memberIds, int zoneId)
    {
        PacketBuilder.BroadcastReliableToGuests(new H_EscapeStateT
        {
            Active    = active,
            Duration  = duration,
            Completed = completed,
            Inside    = inside    != null ? new List<bool>(inside)    : new List<bool>(),
            Charging  = charging,
            MemberIds = memberIds != null ? new List<int>(memberIds)  : new List<int>(),
            ZoneId    = zoneId,
        }, H_EscapeState.Pack, PacketType.H_EscapeState);
    }

    // 이동이 무산됐음을 알려 게스트 쪽 대기 UI를 닫게 한다.
    public static void MoveCancel(MoveVoteCancelReason reason)
    {
        PacketBuilder.BroadcastReliableToGuests(
            new H_MoveCancelT { Reason = (int)reason },
            H_MoveCancel.Pack, PacketType.H_MoveCancel);
    }

    // 게스트가 접속 직후 자기 닉네임을 호스트에 보고. 호스트는 명부에 넣고 전원에게 다시 뿌린다.
    public static void Nickname(string nickname)
    {
        if (RoomManager.IsHost) return;
        if (string.IsNullOrEmpty(nickname)) return;

        PacketBuilder.SendReliableToHost(
            new G_NicknameT { PlayerId = MyId, Nickname = nickname },
            G_Nickname.Pack, PacketType.G_Nickname);
    }

    // 호스트가 방 전체 닉네임 명부를 뿌린다. 게스트 지정 시 그 게스트에게만(신규 접속자 초기화).
    public static void Roster(int guestPlayerId = 0)
    {
        if (!RoomManager.IsHost) return;

        PlayerNameRegistry.BuildRoster(out var ids, out var names);
        if (ids.Count == 0) return;

        var packet = new H_RosterT { PlayerIds = ids, Nicknames = names };
        if (guestPlayerId > 0)
            PacketBuilder.SendReliableToGuest(guestPlayerId, packet, H_Roster.Pack, PacketType.H_Roster);
        else
            PacketBuilder.BroadcastReliableToGuests(packet, H_Roster.Pack, PacketType.H_Roster);
    }

    // 게스트가 씬 로드 완료를 호스트에 알림 (호스트가 박스/적 스냅샷을 보내는 트리거)
    public static void SceneReady()
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(
            new G_SceneReadyT { PlayerId = MyId },
            G_SceneReady.Pack, PacketType.G_SceneReady);
    }

    public static void EnemySpawnToGuest(int guestPlayerId, int enemyTypeId, int enemyId, Vector3 pos, float rotation, float hp, float maxHp, int weaponId, int helmetId)
    {
        PacketBuilder.SendReliableToGuest(guestPlayerId, new H_EnemySpawnT
        {
            EnemyTypeId = enemyTypeId,
            EnemyId  = enemyId,
            Pos      = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
            Hp       = hp,
            MaxHp    = maxHp,
            WeaponId = weaponId,
            HelmetId = helmetId,
        }, H_EnemySpawn.Pack, PacketType.H_EnemySpawn);
    }

    public static void EnemyMove(int enemyId, Vector3 pos, float rotation, int animState)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyMoveT
        {
            EnemyId   = enemyId,
            Pos       = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation  = rotation,
            AnimState = animState,
        }, H_EnemyMove.Pack, PacketType.H_EnemyMove);
    }

    public static void EnemyHit(int enemyId, float hp, float maxHp, float damage)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyHitT
        {
            EnemyId = enemyId,
            Hp      = hp,
            MaxHp   = maxHp,
            Damage = damage,
        }, H_EnemyHit.Pack, PacketType.H_EnemyHit);
    }

    public static void EnemyDie(int enemyId, int killerPlayerId = 0)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyDieT
        {
            EnemyId = enemyId,
            KillerPlayerId = killerPlayerId,
        }, H_EnemyDie.Pack, PacketType.H_EnemyDie);
    }

    public static void EnemySpeak(int enemyId, string message, float duration)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemySpeakT
        {
            EnemyId  = enemyId,
            Message  = message,
            Duration = duration,
        }, H_EnemySpeak.Pack, PacketType.H_EnemySpeak);
    }

    // 적 사격 전파 — 적은 호스트에서만 발사되므로 호스트→게스트 단방향.
    // 게스트는 enemyId로 발사 적을 찾아 attacker=적(Enemy 레이어)으로 스폰해야 적끼리 오사가 안 난다.
    public static void EnemyShoot(int enemyId, Vector3 origin, Vector3 direction, GunStatData stat, IReadOnlyList<Vector3> pelletDirections = null)
    {
        if (!RoomManager.IsHost || !RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyShootT
        {
            EnemyId     = enemyId,
            Origin      = new Vec3T { X = origin.x, Y = origin.y, Z = origin.z },
            Direction   = new Vec3T { X = direction.x, Y = direction.y, Z = direction.z },
            BulletSpeed = stat.BulletSpeed,
            Damage      = stat.Damage,
            MaxRange    = stat.BulletRange,
            Directions  = ToVec3TList(pelletDirections),
        }, H_EnemyShoot.Pack, PacketType.H_EnemyShoot);
    }

    public static void ItemSpawn(int uid, int typeId, Vector3 pos, float rotation, List<int> itemIds, List<int> itemCounts = null, List<int> itemUids = null)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_ItemSpawnT
        {
            Uid     = uid,
            TypeId  = typeId,
            Pos     = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
            ItemIds = itemIds,
            ItemCount = itemCounts,
            ItemUids = itemUids,
        }, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
    }

    // ── 화로 ──────────────────────────────────────────────────
    // 내용물과 제련 진행의 권위는 호스트에 있다. 게스트는 요청만 보내고 결과를 되돌려받는다.

    // 게스트 → 호스트: 광석 투입 요청 (게스트는 이미 자기 인벤에서 뺀 상태 — 거절되면 환불받는다)
    public static void FurnaceInsert(int itemId, int amount)
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(new G_FurnaceInsertT
        {
            ItemId = itemId,
            Amount = amount,
        }, G_FurnaceInsert.Pack, PacketType.G_FurnaceInsert);
    }

    // 게스트 → 호스트: 결과물 수령(true) / 안 녹은 광석 회수(false) 요청
    public static void FurnaceTake(bool takeOutput)
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(new G_FurnaceTakeT
        {
            TakeOutput = takeOutput,
        }, G_FurnaceTake.Pack, PacketType.G_FurnaceTake);
    }

    // 호스트 → 전체: 화로 내용물이 바뀔 때만 보낸다 (게이지는 게스트가 로컬로 이어 센다)
    public static void FurnaceState(int inputItemId, int inputCount, int outputItemId, int outputCount, float elapsed)
    {
        if (!RoomManager.HasGuests) return;

        PacketBuilder.BroadcastReliableToGuests(BuildFurnaceState(inputItemId, inputCount, outputItemId, outputCount, elapsed),
            H_FurnaceState.Pack, PacketType.H_FurnaceState);
    }

    // 호스트 → 특정 게스트: 입장/씬 진입 스냅샷
    public static void FurnaceStateTo(int guestId, int inputItemId, int inputCount, int outputItemId, int outputCount, float elapsed)
    {
        PacketBuilder.SendReliableToGuest(guestId, BuildFurnaceState(inputItemId, inputCount, outputItemId, outputCount, elapsed),
            H_FurnaceState.Pack, PacketType.H_FurnaceState);
    }

    private static H_FurnaceStateT BuildFurnaceState(int inputItemId, int inputCount, int outputItemId, int outputCount, float elapsed)
    {
        return new H_FurnaceStateT
        {
            InputItemId  = inputItemId,
            InputCount   = inputCount,
            OutputItemId = outputItemId,
            OutputCount  = outputCount,
            Elapsed      = elapsed,
        };
    }

    // 호스트 → 요청한 게스트: 결과물 수령 / 광석 회수 / 투입 거절 환불 공용
    public static void FurnaceGive(int guestId, int itemId, int amount)
    {
        PacketBuilder.SendReliableToGuest(guestId, new H_FurnaceGiveT
        {
            ItemId = itemId,
            Amount = amount,
        }, H_FurnaceGive.Pack, PacketType.H_FurnaceGive);
    }

    // 탄환 명중 전파 (호스트 전용). 게스트 탄환은 적 충돌을 판정하지 않으므로
    // 혈흔과 탄환 소멸이 전부 이 통보로 일어난다. 빗나가면 보내지 않는다.
    public static void BulletHit(int shooterId, int seq, Vector3 point, Vector3 normal, bool isKill, bool isHeadshot)
    {
        if (IsSolo || !RoomManager.IsHost) return;

        PacketBuilder.BroadcastToGuests(new H_BulletHitT
        {
            ShooterId  = shooterId,
            Seq        = seq,
            Pos        = new Vec3T { X = point.x,  Y = point.y,  Z = point.z },
            Normal     = new Vec3T { X = normal.x, Y = normal.y, Z = normal.z },
            IsKill     = isKill,
            IsHeadshot = isHeadshot,
        }, H_BulletHit.Pack, PacketType.H_BulletHit);
    }

    // ── 무적 ──────────────────────────────────────────────────
    // 플레이어 HP는 호스트 권위다 — 호스트가 자기 씬의 RemotePlayerStat에 데미지를 넣고
    // H_StatSync로 결과를 돌려준다. 그래서 게스트가 구르기 무적을 알리지 않으면
    // 호스트는 모르고 그대로 데미지를 넣어, 게스트만 회피가 작동하지 않는다.
    //
    // 반대 방향(호스트→게스트)은 필요 없다. 게스트 탄환은 적과의 충돌을 판정하지 않고
    // 호스트의 H_BulletHit만 보고 처리하므로, 남의 무적 상태를 알 이유가 없다.
    public static void Invincible(StatBase stat, bool value)
    {
        if (IsSolo || RoomManager.IsHost) return;

        // 내 캐릭터만 보고한다 — 적/남의 무적은 호스트가 정한다
        if (stat is not PlayerStat) return;

        PacketBuilder.SendReliableToHost(new G_InvincibleT { Value = value },
            G_Invincible.Pack, PacketType.G_Invincible);
    }

    // ── 추가탄 드론 ────────────────────────────────────────────
    // 드론은 플레이어를 따라다니는 장식이라 위치 동기화가 필요 없다.
    // 켜짐/꺼짐만 알리면 각 클라이언트가 그 플레이어 옆에 로컬로 띄운다.

    public static void DroneState(bool active, float damage)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastReliableToGuests(new H_DroneStateT
            {
                PlayerId = MyId,
                Active   = active,
                Damage   = damage,
            }, H_DroneState.Pack, PacketType.H_DroneState);
        else
            PacketBuilder.SendReliableToHost(new G_DroneStateT
            {
                Active = active,
                Damage = damage,
            }, G_DroneState.Pack, PacketType.G_DroneState);
    }

    // 드론 유도탄 발사 전파 (호스트 전용).
    // 소유자 본인에게도 보낸다 — 게스트는 추측 발사를 하지 않고 이 통보만 보고 그리기 때문이다.
    public static void DroneShoot(int ownerPlayerId, int enemyId)
    {
        if (IsSolo || !RoomManager.IsHost) return;

        PacketBuilder.BroadcastToGuests(new H_DroneShootT
        {
            PlayerId = ownerPlayerId,
            EnemyId  = enemyId,
        }, H_DroneShoot.Pack, PacketType.H_DroneShoot);
    }

    // 호스트가 게스트의 드론 상태를 나머지에게 중계 (요청자 본인은 이미 로컬에 띄워둠)
    public static void DroneStateRelay(int ownerPlayerId, bool active, float damage)
    {
        PacketBuilder.BroadcastReliableToGuests(ownerPlayerId, new H_DroneStateT
        {
            PlayerId = ownerPlayerId,
            Active   = active,
            Damage   = damage,
        }, H_DroneState.Pack, PacketType.H_DroneState);
    }
}
