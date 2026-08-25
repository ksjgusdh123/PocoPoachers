using UnityEngine;

public static partial class PacketHandlers
{
    // 호스트가 알려주는 화로 내용물 — 게스트의 화로는 이걸로만 갱신된다.
    public static void OnH_FurnaceState(FlatPacket root)
    {
        if (Furnace.Instance == null) return;

        var packet = root.TypeAsH_FurnaceState();
        Furnace.Instance.ApplyState(packet.InputItemId, packet.InputCount,
                                    packet.OutputItemId, packet.OutputCount, packet.Elapsed);
    }

    // 화로가 나에게 아이템을 내주는 시점 — 결과물 수령/광석 회수/투입 거절 환불 공용.
    // 게스트는 요청 시점에 인벤을 건드리지 않으므로, 실제로 들어오는 건 여기뿐이다.
    public static void OnH_FurnaceGive(FlatPacket root)
    {
        var packet = root.TypeAsH_FurnaceGive();
        if (packet.Amount <= 0) return;

        var item = ItemTable.Instance.Get(packet.ItemId);
        if (item == null) return;

        var inventory = Object.FindAnyObjectByType<PlayerController>()?.PlayerInventory;
        if (inventory == null) return;

        // 가방이 꽉 찬 경우 들어가는 만큼만 받는다. 화로 쪽은 이미 비워진 뒤라 되돌릴 수 없어,
        // 남는 수량은 발밑에 떨어뜨리는 대신 그냥 유실을 감수하지 않도록 최대한 담는다.
        inventory.AddItem(item, packet.Amount);
    }
}
