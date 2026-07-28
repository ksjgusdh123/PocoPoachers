using System.Collections.Generic;
using UnityEngine;

// 플레이어 사망 시 인벤토리 + 장착 중이던 아이템을 상자에 담아 스폰 (EnemyItemBoxDropper의 플레이어 버전)
// 장착 아이템의 내구도/탄약/파츠는 uid 기준으로 WorldEquipmentManager에 그대로 남아있으므로
// 여기선 itemId+uid만 상자로 옮겨 담으면 되고, 실제 해제(내구도 저장 포함)는 이어지는 UnequipAll이 처리한다
// 반드시 UnequipAll보다 먼저 호출해야 장착 중인 아이템을 조회할 수 있음 (PlayerController.HandleDeath 참고)
public class PlayerItemBoxDropper : MonoBehaviour
{
    // LootBox 전용 TypeId — 필드/적 박스(301)·창고(302)와 구분해 호스트·게스트 양쪽이 "비면 사라지는 박스"로 식별한다.
    // ItemTable에 이 id로 LootBox 프리팹이 매핑돼 있어야 한다.
    private const int BoxTypeId = 303;
    private static int _nextUid = 9000; // 필드 스포너(1000+), 적 드롭(5000+)과 겹치지 않는 범위

    private Inventory _inventory;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
    }

    // TODO: 게스트가 죽는 경우는 아직 미지원 — 호스트 로컬만 상자를 만들고, 게스트는 그냥 인벤토리만 비움
    public void SpawnLootBox()
    {
        if (_inventory == null) return;
        if (!RoomManager.IsHost) { _inventory.Clear(); return; }

        var itemIds = new List<int>();
        var itemCounts = new List<int>();
        var itemUids = new List<int>();

        foreach (var slot in _inventory.Slots)
        {
            if (slot.IsEmpty) continue;
            itemIds.Add(slot.ItemData.id);
            itemCounts.Add(slot.Amount);
            itemUids.Add(slot.Uid);
        }

        CollectEquippedItems(itemIds, itemCounts, itemUids);

        _inventory.Clear();
        SpawnBox(itemIds, itemCounts, itemUids);
    }

    // UI 밖으로 드래그해 버린 슬롯 아이템(드래그한 수량)을 플레이어 위치에 상자로 스폰한다.
    // v1: 호스트/솔로만 지원 — 게스트는 아무것도 하지 않는다(아이템 그대로 유지).
    public void DropSlotToWorld(int slotIndex, int amount)
    {
        if (_inventory == null) return;
        if (slotIndex < 0 || slotIndex >= _inventory.Slots.Count) return;

        var slot = _inventory.Slots[slotIndex];
        if (slot.IsEmpty) return;

        int dropCount = Mathf.Clamp(amount, 1, slot.Amount);
        int itemId = slot.ItemData.id;
        int itemUid = slot.Uid;

        // 로컬 인벤에서 먼저 제거 (호스트/게스트 공통, 낙관적)
        _inventory.RemoveItemAtSlot(slotIndex, slot.ItemData, dropCount);

        Vector3 pos = transform.position;
        float rot = transform.eulerAngles.y;

        if (RoomManager.IsHost)
            SpawnLootBoxAt(new List<int> { itemId }, new List<int> { dropCount }, new List<int> { itemUid }, pos, rot);
        else
            // 게스트: 호스트에 요청 → 호스트가 스폰 후 H_ItemSpawn으로 되받아 로컬 생성
            RoomSync.DropItem(itemId, dropCount, itemUid, pos, rot);
    }

    // 이 플레이어 위치에 LootBox로 스폰 (사망 드롭 등 호스트 로컬 경로)
    private void SpawnBox(List<int> itemIds, List<int> itemCounts, List<int> itemUids)
        => SpawnLootBoxAt(itemIds, itemCounts, itemUids, transform.position, transform.eulerAngles.y);

    // 지정 위치에 LootBox를 스폰하고 스냅샷 등록 + 게스트 전파까지 처리한다. 호스트 권위 —
    // 호스트 로컬 드롭과 게스트 드롭 요청(OnG_DropItem) 양쪽에서 재사용해 uid 할당을 한 곳에 모은다.
    public static void SpawnLootBoxAt(List<int> itemIds, List<int> itemCounts, List<int> itemUids, Vector3 pos, float rot)
    {
        if (itemIds.Count == 0) return;

        var omgr = ObjectManager.Instance;
        if (omgr == null) return;

        int uid = _nextUid++;

        omgr.SpawnItemBox(uid, BoxTypeId, pos, rot)
            ?.Initialize(itemIds.ToArray(), itemCounts.ToArray(), itemUids.ToArray(), capacity: itemIds.Count, skipReveal: true);

        omgr.RegisterSpawnedBox(new H_ItemSpawnT
        {
            Uid = uid,
            TypeId = BoxTypeId,
            Pos = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rot,
            ItemIds = itemIds,
            ItemCount = itemCounts,
            ItemUids = itemUids,
        });
        RoomSync.ItemSpawn(uid, BoxTypeId, pos, rot, itemIds, itemCounts, itemUids);
    }

    // 현재 장착 중인 무기(슬롯 0~1)/방어구/가방을 모아 리스트에 추가
    // WeaponController는 실제 슬롯이 2개라 둘 다 조회하고, 방어구/가방 컨트롤러는 slotIndex를 쓰지 않으므로 0 하나만 조회하면 충분
    private void CollectEquippedItems(List<int> itemIds, List<int> itemCounts, List<int> itemUids)
    {
        foreach (var equip in GetComponents<EquipableController>())
        {
            int slotCount = equip is WeaponController ? 2 : 1;
            for (int slot = 0; slot < slotCount; slot++)
            {
                int id = equip.GetEquippedId(slot);
                if (id == 0) continue;

                itemIds.Add(id);
                itemCounts.Add(1);
                itemUids.Add(equip.GetEquippedUid(slot));
            }
        }
    }
}
