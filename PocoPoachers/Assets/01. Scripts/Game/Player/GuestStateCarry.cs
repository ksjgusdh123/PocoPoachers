using System.Collections.Generic;

// 게스트 전용 — 씬 전환 사이에 자기 상태를 메모리로 넘긴다. 게스트는 개인 세이브를 쓰지 않고,
// 호스트가 H_GuestRestore로 되돌려주기를 기다리면 UdpReliable이 순서를 보장하지 않아
// 새 씬의 G_SceneReady가 스냅샷 업로드를 추월할 때 옛 상태로 롤백된다.
// 호스트 세이브에 올리는 스냅샷은 오토세이브/재접속 복원용이고, 실제 복원은 이쪽을 쓴다.
public static class GuestStateCarry
{
    public static List<SaveManager.SlotSaveEntry> Inventory { get; private set; }
    public static List<SaveManager.EquipSlotEntry> Equips { get; private set; }
    public static List<SaveManager.SlotSaveEntry> QuickSlots { get; private set; }

    public static bool HasPending => Inventory != null;

    public static void Store(
        List<SaveManager.SlotSaveEntry> inventory,
        List<SaveManager.EquipSlotEntry> equips,
        List<SaveManager.SlotSaveEntry> quickSlots)
    {
        Inventory = inventory;
        Equips = equips;
        QuickSlots = quickSlots;
    }

    public static void Clear()
    {
        Inventory = null;
        Equips = null;
        QuickSlots = null;
    }
}
