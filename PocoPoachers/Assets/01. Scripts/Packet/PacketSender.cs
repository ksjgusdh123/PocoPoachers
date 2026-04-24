using Google.FlatBuffers;
using UnityEngine;

public static class PacketSender
{
    private static readonly FlatBufferBuilder _builder = new FlatBufferBuilder(1024);
    #region C_* 패킷 생성 시 아래 추가 // - TODO: 추후 자동 생성 스크립트 제작
    public static void CLoginReq(string userName)
    {
        if (!TryGetSession(nameof(CLoginReq), out Session session)) return;
        _builder.Clear();
        
        var nameOff = _builder.CreateString(userName ?? string.Empty);
        var bodyOff = C_LoginReq.CreateC_LoginReq(_builder, nameOff);

        PacketBuilder.Send(session, _builder, PacketType.C_LoginReq, bodyOff.Value);
    }

    public static void CMoveReq(Vector3 pos, float rotation, sbyte moveType)
    {
        if (!TryGetSession(nameof(CMoveReq), out Session session)) return;
        _builder.Clear();
        
        C_MoveReq.StartC_MoveReq(_builder);
        C_MoveReq.AddPos(_builder, Vec3.CreateVec3(_builder, pos.x, pos.y, pos.z));
        C_MoveReq.AddRotation(_builder, rotation);
        C_MoveReq.AddMoveType(_builder, moveType);
        var bodyOff = C_MoveReq.EndC_MoveReq(_builder);

        PacketBuilder.Send(session, _builder, PacketType.C_MoveReq, bodyOff.Value);
    }
    #endregion
    static bool TryGetSession(string operation, out Session session)
    {
        session = NetworkManager.Instance?.Session;
        if (session == null)
        {
            Debug.LogWarning($"[PacketSender] {operation}: no session");
            return false;
        }
        return true;
    }
}