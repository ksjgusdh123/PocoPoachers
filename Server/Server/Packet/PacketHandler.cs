using Google.FlatBuffers;

namespace Server;

public class PacketHandler
{
    public void OnC_LoginReq(ClientSession session, C_LoginReq req)
    {
        string userName = req.Username ?? string.Empty;
        LOG($"OnC_LoginReq: userName='{userName}'");

        bool success = !string.IsNullOrWhiteSpace(userName);

        if (success)
        {
            int playerId = SessionManager.Instance.GenerateId();
            session.Player = PlayerManager.Instance.CreatePlayer(playerId, userName);
            SessionManager.Instance.Add(session);

            LOG($"Login OK: PlayerId={session.PlayerId}, UserName='{session.UserName}'");
        }
        else
        {
            LOG_E($"Login rejected: empty userName");
        }

        var fb = new FlatBufferBuilder(128);
        var nameOff = fb.CreateString(success ? session.UserName! : string.Empty);
        var userInfoOff = UserInfo.CreateUserInfo(fb, id: success ? session.PlayerId : 0, nameOffset: nameOff, level: 1);
        var bodyOff = S_LoginRes.CreateS_LoginRes(fb, success, userInfoOff);
        PacketBuilder.Send(session, fb, PacketType.S_LoginRes, bodyOff.Value);

        if (success)
        {
            var snapshot = session.Player!.Inventory.GetSnapshot();
            var fb2 = new FlatBufferBuilder(256);
            var itemOffsets = new Offset<InventoryItem>[snapshot.Count];
            int idx = 0;
            foreach (var kv in snapshot)
                itemOffsets[idx++] = InventoryItem.CreateInventoryItem(fb2, kv.Key, kv.Value);
            var vecOff = S_InventoryNtf.CreateItemsVector(fb2, itemOffsets);
            var ntfOff = S_InventoryNtf.CreateS_InventoryNtf(fb2, vecOff);
            PacketBuilder.Send(session, fb2, PacketType.S_InventoryNtf, ntfOff.Value);

            WorldItemManager.Instance.SyncTo(session);
        }
    }

    public void OnC_MoveReq(ClientSession session, C_MoveReq req)
    {
        if (session.Player == null)
        {
            LOG_W("MoveReq from unauthenticated session, ignoring");
            return;
        }

        var pos = req.Pos;
        if (!pos.HasValue)
        {
            LOG_W($"MoveReq without Pos, ignoring (PlayerId={session.PlayerId})");
            return;
        }

        float x = pos.Value.X;
        float y = pos.Value.Y;
        float z = pos.Value.Z;
        float rotation = req.Rotation;
        sbyte moveType = req.MoveType;

        LOG($"OnC_MoveReq: PlayerId={session.PlayerId}, Pos=({x:F2},{y:F2},{z:F2}), Rot={rotation:F2}, MoveType={moveType}");

        var fb = new FlatBufferBuilder(128);
        S_MoveNtf.StartS_MoveNtf(fb);
        S_MoveNtf.AddPlayerId(fb, session.PlayerId);
        S_MoveNtf.AddPos(fb, Vec3.CreateVec3(fb, x, y, z));
        S_MoveNtf.AddRotation(fb, rotation);
        S_MoveNtf.AddMoveType(fb, moveType);
        Offset<S_MoveNtf> bodyOff = S_MoveNtf.EndS_MoveNtf(fb);
        SessionManager.Instance.Broadcast(fb, PacketType.S_MoveNtf, bodyOff.Value, except: session);
    }

    public void OnC_AddItemReq(ClientSession session, C_AddItemReq req)
    {
        if (session.Player == null)
        {
            LOG_W("AddItemReq from unauthenticated session, ignoring");
            return;
        }

        int itemId = req.ItemId;
        int amount = req.Amount;
        bool success = session.Player.Inventory.AddItem(itemId, amount);
        LOG($"OnC_AddItemReq: PlayerId={session.PlayerId}, ItemId={itemId}, Amount={amount}, success={success}");

        var fb = new FlatBufferBuilder(64);
        var bodyOff = S_AddItemRes.CreateS_AddItemRes(fb, success, itemId, success ? amount : 0);
        PacketBuilder.Send(session, fb, PacketType.S_AddItemRes, bodyOff.Value);
    }

    public void OnC_RemoveItemReq(ClientSession session, C_RemoveItemReq req)
    {
        if (session.Player == null)
        {
            LOG_W("RemoveItemReq from unauthenticated session, ignoring");
            return;
        }

        int itemId = req.ItemId;
        int amount = req.Amount;
        bool success = session.Player.Inventory.RemoveItem(itemId, amount);
        LOG($"OnC_RemoveItemReq: PlayerId={session.PlayerId}, ItemId={itemId}, Amount={amount}, success={success}");

        var fb = new FlatBufferBuilder(64);
        var bodyOff = S_RemoveItemRes.CreateS_RemoveItemRes(fb, success, itemId, success ? amount : 0);
        PacketBuilder.Send(session, fb, PacketType.S_RemoveItemRes, bodyOff.Value);
    }
}
