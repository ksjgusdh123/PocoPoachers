using System;
using Google.FlatBuffers;
using UnityEngine;

public static class MakePacket
{
    const int DefaultSize = 128;

    public static ArraySegment<byte> CLoginReq(string userName)
    {
        var fb = new FlatBufferBuilder(DefaultSize);
        var nameOff = fb.CreateString(userName ?? string.Empty);
        var bodyOff = C_LoginReq.CreateC_LoginReq(fb, nameOff);
        return PacketBuilder.Build(fb, PacketType.C_LoginReq, bodyOff.Value);
    }

    public static ArraySegment<byte> CMoveReq(Vector3 pos, float rotation, sbyte moveType)
    {
        var fb = new FlatBufferBuilder(DefaultSize);
        C_MoveReq.StartC_MoveReq(fb);
        C_MoveReq.AddPos(fb, Vec3.CreateVec3(fb, pos.x, pos.y, pos.z));
        C_MoveReq.AddRotation(fb, rotation);
        C_MoveReq.AddMoveType(fb, moveType);
        var bodyOff = C_MoveReq.EndC_MoveReq(fb);
        return PacketBuilder.Build(fb, PacketType.C_MoveReq, bodyOff.Value);
    }
}
