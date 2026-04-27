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

        PacketSender.SLoginRes(
            session,
            success,
            success ? session.PlayerId : 0,
            success ? session.UserName : string.Empty,
            success ? session.Player!.Stat.Level : 1);

        if (success)
        {
            var snapshot = session.Player!.Inventory.GetSnapshot();
            PacketSender.SInventoryNtf(session, snapshot);
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

        PacketSender.SMoveNtfBroadcast(session, session.PlayerId, x, y, z, rotation, moveType);
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

        PacketSender.SAddItemRes(session, success, itemId, success ? amount : 0);
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

        PacketSender.SRemoveItemRes(session, success, itemId, success ? amount : 0);
    }

    public void OnC_OpenBox(ClientSession session, C_OpenBox req)
    {
        if (session.Player == null)
        {
            LOG_W("RemoveItemReq from unauthenticated session, ignoring");
            return;
        }
    }

    public void OnC_GainItemReq(ClientSession session, C_GainItemReq req)
    {
        if (session.Player == null)
        {
            LOG_W("RemoveItemReq from unauthenticated session, ignoring");
            return;
        }
        int boxUid = req.BoxUid;
        int itemTypeId = req.ItemUid;
        int amount = req.Amount;

        if (!WorldItemManager.Instance.TryTakeItem(boxUid, itemTypeId, amount, out int taken))
        {
            LOG_W($"GainItemReq 실패: boxUid={boxUid}, itemTypeId={itemTypeId}, amount={amount}");
            return;
        }

        session.Player.Inventory.AddItem(itemTypeId, taken);
        LOG($"GainItem: PlayerId={session.PlayerId}, boxUid={boxUid}, itemTypeId={itemTypeId}, taken={taken}");
        PacketSender.SChangeItemBox(session, boxUid, itemTypeId, taken);
        PacketSender.SSuccessGainItemNtf(session, boxUid, itemTypeId, taken);
    }
}
