using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpHolePuncher
{
    public event Action<UdpHolePuncher> OnSuccess;
    public event Action<UdpHolePuncher, string> OnFailed;

    private readonly Socket _socket;
    private IPEndPoint _remoteEp;
    private Thread _punchThread;
    private bool _isRunning;

    private const byte PUNCH_SIGNAL = 0x01;
    private const int INTERVAL_MS = 500;
    private const int TIMEOUT_SEC = 5;

    public UdpHolePuncher(Socket sharedSocket)
    {
        _socket = sharedSocket;
    }

    public void Start(IPEndPoint remoteEp)
    {
        _remoteEp = remoteEp;
        _isRunning = true;

        _punchThread = new Thread(PunchLoop) { IsBackground = true };
        _punchThread.Start();
    }

    private void PunchLoop()
    {
        DateTime startTime = DateTime.Now;
        byte[] punchData = { PUNCH_SIGNAL };
        byte[] buffer = new byte[1024];

        while (_isRunning)
        {
            try
            {
                // 1. 상대방에게 펀칭 패킷 전송 (UDP 구멍 뚫기)
                _socket.SendTo(punchData, _remoteEp);

                // 2. 수신 데이터 확인 (Non-blocking 체크)
                if (_socket.Poll(1000, SelectMode.SelectRead))
                {
                    EndPoint fromEp = new IPEndPoint(IPAddress.Any, 0);
                    int received = _socket.ReceiveFrom(buffer, ref fromEp);

                    // 상대방으로부터 펀칭 신호(0x01)를 받으면 성공
                    if (received > 0 && buffer[0] == PUNCH_SIGNAL)
                    {
                        Success();
                        return;
                    }
                }

                // 3. 타임아웃 체크
                if ((DateTime.Now - startTime).TotalSeconds > TIMEOUT_SEC)
                {
                    Fail("Hole Punching Timeout");
                    return;
                }

                Thread.Sleep(INTERVAL_MS);
            }
            catch (Exception e)
            {
                Fail(e.Message);
                return;
            }
        }
    }

    private void Success()
    {
        _isRunning = false;
        OnSuccess?.Invoke(this);
    }

    private void Fail(string reason)
    {
        _isRunning = false;
        OnFailed?.Invoke(this, reason);
    }

    public void Stop()
    {
        _isRunning = false;
        _punchThread?.Join(100);
    }
}