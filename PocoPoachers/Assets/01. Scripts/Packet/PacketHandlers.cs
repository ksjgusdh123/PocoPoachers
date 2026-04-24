using Google.FlatBuffers;
using UnityEngine;

public static class PacketHandlers
{
    #region S_* 패킷 수신 시 아래 추가
    // - TODO: 추후 자동 함수형태(로직은 직접 구현..) 생성 스크립트 제작

    public static void OnS_LoginRes(FlatPacket root)
    {
        var res = root.TypeAsS_LoginRes();
        var ui = res.UserInfo;
        bool success = res.Success;
        int playerId = ui?.Id ?? 0;
        string userName = ui?.Name ?? string.Empty;
        int level = ui?.Level ?? 0;

        MainThreadDispatcher.Enqueue(() =>
        {
            NetworkManager.Instance?.OnLoginResult(success, playerId, userName, level);
        });
    }

    public static void OnS_MoveNtf(FlatPacket root)
    {
        var ntf = root.TypeAsS_MoveNtf();
        float x = ntf.Pos?.X ?? 0f;
        float y = ntf.Pos?.Y ?? 0f;
        float z = ntf.Pos?.Z ?? 0f;
        int playerId = ntf.PlayerId;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = ntf.Rotation;
        sbyte moveType = ntf.MoveType;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, playerId, pos, rotation, moveType);
    }

    #endregion
}
