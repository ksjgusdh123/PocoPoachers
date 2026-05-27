public static partial class PacketHandlers
{
    public static void OnS_CreateRoom(FlatPacket root)
    {
        var pkt = root.TypeAsS_CreateRoom();
        string code = pkt.SessionCode ?? string.Empty;
        bool success = pkt.Success;

        MainThreadDispatcher.Enqueue(() => {
            if (success)
            {
                RoomManager.Instance.SessionCode = code;
                RoomManager.Instance?.NotifyGameStarted();
            }
            else
            {
                RoomManager.Instance?.HandleFailure("방 생성에 실패했습니다.");
            }
        });
    }

    public static void OnS_JoinRoom(FlatPacket root)
    {
        var pkt = root.TypeAsS_JoinRoom();
        bool success = pkt.Success;
        NetInfoT hostInfo = pkt.HostInfo.HasValue ? pkt.HostInfo.Value.UnPack() : null;

        MainThreadDispatcher.Enqueue(() =>
        {
            SessionCodeUI.Instance?.HandleJoinRoom(success);
            if (!success || hostInfo == null)
                RoomManager.Instance?.HandleFailure("입장 실패");
            else
                RoomManager.Instance?.ConnectToGuest(hostInfo);
        });
    }

    public static void OnS_GuestJoined(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsS_GuestJoined();
        NetInfoT guestInfo = pkt.Info.HasValue ? pkt.Info.Value.UnPack() : null;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (guestInfo != null)
                RoomManager.Instance?.ConnectToGuest(guestInfo);
        });
    }
}
