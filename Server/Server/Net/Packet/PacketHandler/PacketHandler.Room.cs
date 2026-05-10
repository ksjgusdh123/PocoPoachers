namespace Server;

public partial class PacketHandler
{
    public void OnC_CreateRoom(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_CreateRoom();
        var myInfo = pkt.MyInfo.HasValue ? pkt.MyInfo.Value.UnPack() : new MemberInfoT();

        var room = RoomManager.Instance.CreateRoom(session, myInfo);

        PacketBuilder.Send(session, new S_CreateRoomT
        {
            SessionCode = room.Code,
            Success = true,
        }, S_CreateRoom.Pack, PacketType.S_CreateRoom);
    }

    public void OnC_JoinRoom(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_JoinRoom();
        string code = pkt.SessionCode ?? string.Empty;
        var myInfo = pkt.MyInfo.HasValue ? pkt.MyInfo.Value.UnPack() : new MemberInfoT();

        var room = RoomManager.Instance.FindRoom(code);
        if (room == null || !room.TryJoin(session, myInfo))
        {
            PacketBuilder.Send(session, new S_JoinRoomT { Success = false }, S_JoinRoom.Pack, PacketType.S_JoinRoom);
            return;
        }

        // 새 게스트에게 호스트 MemberInfo 전달 (Star 토폴로지: 게스트는 호스트에만 연결)
        PacketBuilder.Send(session, new S_JoinRoomT
        {
            Success = true,
            HostInfo = room.HostInfo,
        }, S_JoinRoom.Pack, PacketType.S_JoinRoom);

        // 호스트에게 새 게스트 MemberInfo 전달
        PacketBuilder.Send(room.Host, new S_GuestJoinedT
        {
            Info = myInfo,
        }, S_GuestJoined.Pack, PacketType.S_GuestJoined);
    }
}
