using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnS_LoginResult(FlatPacket root)
    {
        var pkt = root.TypeAsS_LoginResult();
        bool success = pkt.Success;
        int playerId = pkt.PlayerId;

        MainThreadDispatcher.Enqueue(() =>
        {
            NetworkManager.Instance?.OnLoginResult(success, playerId);
        });
    }
}
