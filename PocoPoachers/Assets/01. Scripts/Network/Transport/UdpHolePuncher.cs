using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpHolePuncher
{
    public event Action<UdpHolePuncher> OnSuccess;
    public event Action<UdpHolePuncher, string> OnFailed;

    private readonly UdpSession _session;
    private IPEndPoint _remoteEp;
    private Thread _punchThread;
    private volatile bool _isRunning;
    private int _succeeded; // Interlocked — 중복 성공 방지

    private const byte PUNCH_SIGNAL = 0x01;
    private const int INTERVAL_MS  = 500;
    private const int TIMEOUT_SEC  = 30;

    public UdpHolePuncher(UdpSession session) => _session = session;

    public void Start(IPEndPoint remoteEp)
    {
        _remoteEp = remoteEp;
        _isRunning = true;
        _session.OnPunchReceived += HandlePunchReceived;
        _punchThread = new Thread(PunchLoop) { IsBackground = true, Name = "Udp-Punch" };
        _punchThread.Start();
    }

    private void HandlePunchReceived(IPEndPoint from)
    {
        bool match = from.Equals(_remoteEp) || from.Port == _remoteEp.Port;
        UnityEngine.Debug.Log($"[Puncher] from={from} remote={_remoteEp} match={match}");
        if (match)
            TrySuccess();
    }

    private void PunchLoop()
    {
        DateTime startTime = DateTime.Now;
        byte[] punchData = { PUNCH_SIGNAL };

        while (_isRunning)
        {
            try { _session.Socket.SendTo(punchData, _remoteEp); }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset) { }
            catch (Exception e) { DoFail(e.Message); return; }

            if ((DateTime.Now - startTime).TotalSeconds > TIMEOUT_SEC)
            {
                DoFail("Hole Punching Timeout");
                return;
            }

            Thread.Sleep(INTERVAL_MS);
        }
    }

    private void TrySuccess()
    {
        if (Interlocked.Exchange(ref _succeeded, 1) != 0) return;
        _isRunning = false;
        _session.OnPunchReceived -= HandlePunchReceived;
        OnSuccess?.Invoke(this);
    }

    // 타임아웃: PunchLoop만 중단, HandlePunchReceived 구독은 유지
    // → 늦게 도착하는 신호도 TrySuccess()로 연결 완료 가능
    private void DoFail(string reason)
    {
        if (!_isRunning) return;
        _isRunning = false;
        OnFailed?.Invoke(this, reason);
    }

    public void Stop()
    {
        _isRunning = false;
        if (Interlocked.Exchange(ref _succeeded, 1) == 0)
            _session.OnPunchReceived -= HandlePunchReceived;
        _punchThread?.Join(100);
    }
}
