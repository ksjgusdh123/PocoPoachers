using Google.FlatBuffers;
using UnityEngine;
using static UnityEditor.Progress;

public static class PacketSender
{
    private static readonly FlatBufferBuilder _builder = new FlatBufferBuilder(1024);

    public static void CLoginReq(string userName)
    {
        if (!TryGetSession(out Session session)) return;
        _builder.Clear();

        var nameOff = _builder.CreateString(userName ?? string.Empty);
        var bodyOff = C_LoginReq.CreateC_LoginReq(_builder, nameOff);

        PacketBuilder.Send(session, _builder, PacketType.C_LoginReq, bodyOff.Value);
    }

    public static void CMoveReq(Vector3 pos, float rotation, sbyte moveType)
    {
        if (!TryGetSession(out Session session)) return;
        _builder.Clear();

        C_MoveReq.StartC_MoveReq(_builder);
        C_MoveReq.AddPos(_builder, Vec3.CreateVec3(_builder, pos.x, pos.y, pos.z));
        C_MoveReq.AddRotation(_builder, rotation);
        C_MoveReq.AddMoveType(_builder, moveType);
        var bodyOff = C_MoveReq.EndC_MoveReq(_builder);

        PacketBuilder.Send(session, _builder, PacketType.C_MoveReq, bodyOff.Value);
    }

    public static void CGainItemReq(int boxId, int itemId, int amount = 1, bool isPlayer = false, int gainedSlotIndex = -1, int removedSlotIndex = -1)
    {
        if (!TryGetSession(out Session session)) return;
        _builder.Clear();

        var bodyOff = C_GainItemReq.CreateC_GainItemReq(_builder, isPlayer, boxId ,itemId, amount, gainedSlotIndex, removedSlotIndex);
        PacketBuilder.Send(session, _builder, PacketType.C_GainItemReq, bodyOff.Value);
    }


    public static void CShootReq(Vector3 origin, Vector3 direction, float bulletSpeed, float damage, float maxRange)
    {
        if (!TryGetSession(out Session session)) return;
        _builder.Clear();

        C_ShootReq.StartC_ShootReq(_builder);
        C_ShootReq.AddOrigin(_builder, Vec3.CreateVec3(_builder, origin.x, origin.y, origin.z));
        C_ShootReq.AddDirection(_builder, Vec3.CreateVec3(_builder, direction.x, direction.y, direction.z));
        C_ShootReq.AddBulletSpeed(_builder, bulletSpeed);
        C_ShootReq.AddDamage(_builder, damage);
        C_ShootReq.AddMaxRange(_builder, maxRange);
        var bodyOff = C_ShootReq.EndC_ShootReq(_builder);

        PacketBuilder.Send(session, _builder, PacketType.C_ShootReq, bodyOff.Value);
    }

    public static void CExchangeItemReq(int boxUid, int playerItemId, int playerItemAmount, int playerSlotIndex,
        int boxItemId, int boxItemAmount, int boxSlotIndex)
    {
        if (!TryGetSession(out Session session)) return;
        _builder.Clear();

        var bodyOff = C_ExchangeItemReq.CreateC_ExchangeItemReq(_builder, boxUid, playerItemId, playerItemAmount, playerSlotIndex,
            boxItemId, boxItemAmount, boxSlotIndex);
        PacketBuilder.Send(session, _builder, PacketType.C_ExchangeItemReq, bodyOff.Value);
    }

    static bool TryGetSession(out Session session)
    {
        session = NetworkManager.Instance?.Session;
        if (session == null) return false;
        return true;
    }

}