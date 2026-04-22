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
            session.PlayerId = SessionManager.Instance.GenerateId();
            session.UserName = userName;
            SessionManager.Instance.Add(session);
            LOG($"Login OK: PlayerId={session.PlayerId}, UserName='{session.UserName}'");
        }
        else
        {
            LOG_E($"Login rejected: empty userName");
        }

        var fb = new FlatBufferBuilder(128);

        var nameOff = fb.CreateString(success ? session.UserName! : string.Empty);
        var userInfoOff = UserInfo.CreateUserInfo(
            fb,
            id: success ? session.PlayerId : 0,
            nameOffset: nameOff,
            level: 1);
        var bodyOff = S_LoginRes.CreateS_LoginRes(fb, success, userInfoOff);

        PacketBuilder.Send(session, fb, PacketType.S_LoginRes, bodyOff.Value);
    }

    public void OnC_MoveReq(ClientSession session, C_MoveReq req)
    {
        if (session.PlayerId == 0)
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
}
