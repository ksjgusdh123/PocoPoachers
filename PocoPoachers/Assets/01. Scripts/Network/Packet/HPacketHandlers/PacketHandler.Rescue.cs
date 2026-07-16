using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_Rescue(FlatPacket root)
    {
        var packet = root.TypeAsH_Rescue();

        // 호스트는 구출자·대상에게만 보내므로 여기 도달했다면 내가 둘 중 하나다
        var state = (RescueState)packet.State;
        Debug.Log($"[H_Rescue] 구출 {state} 구출자={packet.RescuerId} 대상={packet.TargetId}");
        RescueInteractable.RaiseProgress(state, packet.Duration);
    }
}
