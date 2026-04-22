using System;
using System.Net;
using Google.FlatBuffers;
using UnityEngine;
using static NetLog;

public struct LoginResultData
{
    public bool Success;
    public int PlayerId;
    public string UserName;
    public int Level;
}

public struct MoveUpdateData
{
    public int PlayerId;
    public float X;
    public float Y;
    public float Z;
    public float Rotation;
    public sbyte MoveType;

    public Vector3 Position => new Vector3(X, Y, Z);
}

/// <summary>TestClient의 ServerSession과 동일한 패킷 흐름 — Unity용 더미 클라 세션.</summary>
public class ServerSession : PacketSession
{
    public event Action<LoginResultData> OnLoginResult;
    public event Action<MoveUpdateData> OnMoveUpdate;
    public event Action OnConnectedEvent;
    public event Action OnDisconnectedEvent;

    public override void OnConnected(EndPoint endPoint)
    {
        LOG($"OnConnected: {endPoint}");
        MainThreadDispatcher.Enqueue(() => OnConnectedEvent?.Invoke());
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        LOG($"OnDisconnected: {endPoint}");
        MainThreadDispatcher.Enqueue(() => OnDisconnectedEvent?.Invoke());
    }

    public override void OnSend(int numOfBytes) { }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null || buffer.Count <= HeaderSize) return;

        int bodyOffset = buffer.Offset + HeaderSize;
        var bb = new ByteBuffer(buffer.Array, bodyOffset);
        var root = FlatPacket.GetRootAsFlatPacket(bb);

        switch (root.TypeType)
        {
            case PacketType.S_LoginRes:
                HandleLoginRes(root.TypeAsS_LoginRes());
                break;
            case PacketType.S_MoveNtf:
                HandleMoveNtf(root.TypeAsS_MoveNtf());
                break;
            default:
                LOG_W($"Unknown packet type: {root.TypeType}");
                break;
        }
    }

    void HandleLoginRes(S_LoginRes res)
    {
        var ui = res.UserInfo;
        if (ui.HasValue)
        {
            var u = ui.Value;
            LOG($"<-- S_LoginRes: success={res.Success}, id={u.Id}, name='{u.Name}', level={u.Level}");
        }
        else
        {
            LOG($"<-- S_LoginRes: success={res.Success}, user_info=null");
        }

        var data = new LoginResultData
        {
            Success = res.Success,
            PlayerId = ui?.Id ?? 0,
            UserName = ui?.Name ?? string.Empty,
            Level = ui?.Level ?? 0,
        };
        MainThreadDispatcher.Enqueue(() => OnLoginResult?.Invoke(data));
    }

    void HandleMoveNtf(S_MoveNtf ntf)
    {
        float x = ntf.Pos?.X ?? 0f;
        float y = ntf.Pos?.Y ?? 0f;
        float z = ntf.Pos?.Z ?? 0f;
        LOG($"<-- S_MoveNtf: PlayerId={ntf.PlayerId}, Pos=({x:F2},{y:F2},{z:F2}), Rot={ntf.Rotation:F2}, MoveType={ntf.MoveType}");

        var data = new MoveUpdateData
        {
            PlayerId = ntf.PlayerId,
            X = x,
            Y = y,
            Z = z,
            Rotation = ntf.Rotation,
            MoveType = ntf.MoveType,
        };
        MainThreadDispatcher.Enqueue(() => OnMoveUpdate?.Invoke(data));
    }

    public void SendLoginReq(string userName)
    {
        var fb = new FlatBufferBuilder(128);
        var nameOff = fb.CreateString(userName ?? string.Empty);
        var bodyOff = C_LoginReq.CreateC_LoginReq(fb, nameOff);
        Send(PacketBuilder.Build(fb, PacketType.C_LoginReq, bodyOff.Value));
        LOG($"--> C_LoginReq(userName='{userName ?? string.Empty}')");
    }

    public void SendMoveReq(Vector3 pos, float rotation, sbyte moveType)
    {
        var fb = new FlatBufferBuilder(128);
        C_MoveReq.StartC_MoveReq(fb);
        C_MoveReq.AddPos(fb, Vec3.CreateVec3(fb, pos.x, pos.y, pos.z));
        C_MoveReq.AddRotation(fb, rotation);
        C_MoveReq.AddMoveType(fb, moveType);
        var bodyOff = C_MoveReq.EndC_MoveReq(fb);
        Send(PacketBuilder.Build(fb, PacketType.C_MoveReq, bodyOff.Value));
    }
}
