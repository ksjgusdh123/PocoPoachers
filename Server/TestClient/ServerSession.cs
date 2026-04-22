using Google.FlatBuffers;
using ServerCore;
using System.Net;

namespace TestClient
{
    class ServerSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            LOG($"OnConnected: {endPoint}");
            SendLoginReq("TestUser");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            LOG($"OnDisconnected: {endPoint}");
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null || buffer.Count <= PacketSession.HeaderSize)
                return;

            int bodyOffset = buffer.Offset + PacketSession.HeaderSize;
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

        public override void OnSend(int numOfBytes)
        {
        }

        void SendLoginReq(string userName)
        {
            var fb = new FlatBufferBuilder(128);
            var nameOff = fb.CreateString(userName);
            var bodyOff = C_LoginReq.CreateC_LoginReq(fb, nameOff);

            Send(BuildPacket(fb, PacketType.C_LoginReq, bodyOff.Value));
            LOG($"--> C_LoginReq(userName='{userName}')");
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
        }

        void HandleMoveNtf(S_MoveNtf ntf)
        {
            float x = ntf.Pos?.X ?? 0f;
            float y = ntf.Pos?.Y ?? 0f;
            float z = ntf.Pos?.Z ?? 0f;
            LOG($"<-- S_MoveNtf: PlayerId={ntf.PlayerId}, Pos=({x:F2},{y:F2},{z:F2}), Rot={ntf.Rotation:F2}, MoveType={ntf.MoveType}");
        }

        static ArraySegment<byte> BuildPacket(FlatBufferBuilder builder, PacketType type, int innerOffset)
        {
            Offset<FlatPacket> rootOffset = FlatPacket.CreateFlatPacket(builder, type, innerOffset);
            FlatPacket.FinishFlatPacketBuffer(builder, rootOffset);

            byte[] payload = builder.SizedByteArray();
            int totalSize = payload.Length + PacketSession.HeaderSize;
            if (totalSize > ushort.MaxValue)
                throw new InvalidOperationException($"Packet too large: {totalSize} bytes (type={type}).");

            byte[] sendBuffer = new byte[totalSize];
            BitConverter.GetBytes((ushort)totalSize).CopyTo(sendBuffer, 0);
            Buffer.BlockCopy(payload, 0, sendBuffer, PacketSession.HeaderSize, payload.Length);
            return new ArraySegment<byte>(sendBuffer);
        }
    }

}
