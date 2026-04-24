using Google.FlatBuffers;
using UnityEngine;

public static class PacketHandlers
{
    // S_* 패킷 수신 시 아래 추가
    // - TODO: 추후 자동 함수형태(로직은 직접 구현..) 생성 스크립트 제작

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

    public static void OnS_MoveNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_MoveNtf();
        float x = pkt.Pos?.X ?? 0f;
        float y = pkt.Pos?.Y ?? 0f;
        float z = pkt.Pos?.Z ?? 0f;
        int playerId = pkt.PlayerId;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = pkt.Rotation;
        sbyte moveType = pkt.MoveType;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, playerId, pos, rotation, moveType);
    }

}
