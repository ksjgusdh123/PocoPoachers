using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

// RFC 5389 minimal STUN client.
// Sends a Binding Request and parses XOR-MAPPED-ADDRESS from the response.
public static class StunClient
{
    const uint MagicCookie = 0x2112A442;

    public static bool TryGetPublicEndPoint(string stunHost, int stunPort, Socket boundUdpSocket, out IPEndPoint publicEp)
    {
        publicEp = null;
        try
        {
            IPAddress stunAddr = Dns.GetHostAddresses(stunHost)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (stunAddr == null)
            {
                UnityEngine.Debug.LogError($"[StunClient] {stunHost}에 대한 IPv4 주소를 찾을 수 없습니다.");
                return false;
            }

            var stunEp = new IPEndPoint(stunAddr, stunPort);
            byte[] request = BuildBindingRequest(out byte[] txId);

            boundUdpSocket.SendTo(request, stunEp);
            boundUdpSocket.ReceiveTimeout = 3000;

            byte[] response = new byte[512];
            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            int received = boundUdpSocket.ReceiveFrom(response, ref remoteEp);

            return TryParseResponse(response, received, txId, out publicEp);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[StunClient] {e.Message}");
            return false;
        }
    }

    static byte[] BuildBindingRequest(out byte[] txId)
    {
        txId = new byte[12];
        new Random().NextBytes(txId);

        byte[] msg = new byte[20];
        // Message Type: Binding Request (0x0001)
        msg[0] = 0x00; msg[1] = 0x01;
        // Message Length: 0 (no attributes)
        msg[2] = 0x00; msg[3] = 0x00;
        // Magic Cookie
        byte[] cookie = BitConverter.GetBytes(MagicCookie);
        if (BitConverter.IsLittleEndian) Array.Reverse(cookie);
        Buffer.BlockCopy(cookie, 0, msg, 4, 4);
        // Transaction ID
        Buffer.BlockCopy(txId, 0, msg, 8, 12);
        return msg;
    }

    static bool TryParseResponse(byte[] buf, int len, byte[] txId, out IPEndPoint ep)
    {
        ep = null;
        if (len < 20) return false;

        // Verify Message Type (Binding Success Response = 0x0101)
        if (buf[0] != 0x01 || buf[1] != 0x01) return false;

        // Verify Magic Cookie
        if (buf[4] != 0x21 || buf[5] != 0x12 || buf[6] != 0xA4 || buf[7] != 0x42) return false;

        int msgLen = (buf[2] << 8) | buf[3];
        int offset = 20;

        while (offset + 4 <= 20 + msgLen && offset + 4 <= len)
        {
            int attrType = (buf[offset] << 8) | buf[offset + 1];
            int attrLen  = (buf[offset + 2] << 8) | buf[offset + 3];
            offset += 4;

            // XOR-MAPPED-ADDRESS (0x0020) or MAPPED-ADDRESS (0x0001) fallback
            if ((attrType == 0x0020 || attrType == 0x0001) && attrLen >= 8)
            {
                bool xor    = attrType == 0x0020;
                byte family = buf[offset + 1];
                if (family != 0x01) { offset += attrLen; continue; } // IPv4 only

                int rawPort = (buf[offset + 2] << 8) | buf[offset + 3];
                int port    = xor ? rawPort ^ 0x2112 : rawPort;

                byte[] addrBytes = new byte[4];
                Buffer.BlockCopy(buf, offset + 4, addrBytes, 0, 4);
                if (xor)
                {
                    addrBytes[0] ^= 0x21; addrBytes[1] ^= 0x12;
                    addrBytes[2] ^= 0xA4; addrBytes[3] ^= 0x42;
                }

                ep = new IPEndPoint(new IPAddress(addrBytes), port);
                return true;
            }
            offset += attrLen;
            // Attributes are padded to 4-byte boundaries
            if (attrLen % 4 != 0) offset += 4 - (attrLen % 4);
        }
        return false;
    }
}
