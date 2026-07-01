public static partial class PacketHandlers
{
    public static void OnS_CreateRoom(FlatPacket root)
    {
        var packet = root.TypeAsS_CreateRoom();
        string code = packet.SessionCode ?? string.Empty;
        bool success = packet.Success;

        MainThreadDispatcher.Enqueue(() => {
            if (success)
            {
                RoomManager.Instance.SessionCode = code;
                RoomManager.Instance?.NotifySessionCodeReceived(code);
                RoomManager.Instance?.NotifyGameStarted();
            }
            else
            {
                RoomManager.Instance?.HandleFailure("\ubc29 \uc0dd\uc131\uc5d0 \uc2e4\ud328\ud588\uc2b5\ub2c8\ub2e4.");
            }
        });
    }

    public static void OnS_JoinRoom(FlatPacket root)
    {
        var packet = root.TypeAsS_JoinRoom();
        bool success = packet.Success;
        NetInfoT hostInfo = packet.HostInfo.HasValue ? packet.HostInfo.Value.UnPack() : null;

        MainThreadDispatcher.Enqueue(() =>
        {
            JoinCodeUI.Instance?.HandleJoinRoom(success);
            if (!success || hostInfo == null)
                RoomManager.Instance?.HandleFailure("\uc785\uc7a5 \uc2e4\ud328");
            else
                RoomManager.Instance?.StartUdpPunch(hostInfo);
        });
    }

    public static void OnS_GuestJoined(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsS_GuestJoined();
        NetInfoT guestInfo = packet.Info.HasValue ? packet.Info.Value.UnPack() : null;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (guestInfo != null)
                RoomManager.Instance?.StartUdpPunch(guestInfo);
        });
    }
}
