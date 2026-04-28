using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnS_LoginRes(FlatPacket root)
    {
        var pkt = root.TypeAsS_LoginRes();
        bool success = pkt.Success;
        int playerId = pkt.UserInfo?.Id ?? 0;
        string userName = pkt.UserInfo?.Name ?? string.Empty;
        int level = pkt.UserInfo?.Level ?? 0;

        MainThreadDispatcher.Enqueue(() =>
        {
            NetworkManager.Instance?.OnLoginResult(success, playerId, userName, level);
        });
    }
}
