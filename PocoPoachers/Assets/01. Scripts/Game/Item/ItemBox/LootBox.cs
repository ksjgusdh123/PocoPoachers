using UnityEngine;

// 아이템을 모두 가져가면 스스로 사라지는 박스 (플레이어 사망 드롭 전용).
// 필드/적 박스(ItemBox)와 달리 비워지면 디스폰된다. 전용 TypeId로 스폰되므로
// 이 프리팹은 사망 드롭에만 쓰이고, 여기 붙은 인스턴스는 항상 "비면 사라짐"으로 동작한다.
//
// 동기화: 호스트/게스트가 각자 자기 인벤토리가 비는 순간 독립적으로 디스폰한다.
// 게스트 인벤토리는 기존 H_ItemBoxUpdate 흐름으로 호스트와 함께 비워지므로 별도 제거 패킷이 필요 없다.
// 호스트는 late-join 게스트 스냅샷 목록(_spawnedBoxes)에서도 함께 제거한다.
public class LootBox : ItemBox
{
    private Inventory _inventory;
    private int _uid;
    private bool _isOpen;

    protected override void AfterInitialize(Inventory inventory)
    {
        _inventory = inventory;
        _uid = GetComponent<WorldObject>()?.Id ?? 0;

        foreach (var slot in _inventory.Slots)
            slot.OnChanged += OnSlotChanged;
    }

    public override void OnInteract(PlayerController player)
    {
        base.OnInteract(player);
        _isOpen = true;
    }

    public override void OnInteractExit(PlayerController player)
    {
        base.OnInteractExit(player);
        _isOpen = false;
        // 마지막 아이템을 꺼내며 연 상태로 비운 경우, 닫는 시점에 디스폰
        TryDespawn();
    }

    private void OnSlotChanged()
    {
        // 열려 있는 동안(루팅 중)엔 디스폰을 미루고 닫힐 때 처리 — UI 아래에서 오브젝트가 파괴되는 것 방지
        if (_isOpen) return;
        TryDespawn();
    }

    private void TryDespawn()
    {
        if (_inventory == null || !IsEmpty()) return;

        var omgr = ObjectManager.Instance;
        if (omgr == null) return;

        if (RoomManager.IsHost)
            omgr.UnregisterSpawnedBox(_uid);

        omgr.Despawn(ObjectKind.ItemBox, _uid);
    }

    private bool IsEmpty()
    {
        foreach (var slot in _inventory.Slots)
            if (!slot.IsEmpty) return false;
        return true;
    }
}
