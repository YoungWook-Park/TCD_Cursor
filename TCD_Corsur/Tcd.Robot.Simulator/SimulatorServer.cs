using System.Net;
using System.Net.Sockets;
using Tcd.Robot.Simulator.Protocol;

namespace Tcd.Robot.Simulator;

/// <summary>
/// TCP 서버. 복수 HMI 클라이언트를 수용하고 상태 Push를 브로드캐스트한다.
/// <list type="bullet">
///   <item>연결 수락 → ClientSession 생성 → 비동기 수신 루프</item>
///   <item>RobotSimCore.StatePushed → 전체 클라이언트에 브로드캐스트</item>
///   <item>Heartbeat 300ms 주기로 State Push (연결 감시 겸용)</item>
/// </list>
/// </summary>
public sealed class SimulatorServer : IDisposable
{
    #region Variable

    private const int HeartbeatMs = 300;

    private readonly TcpListener              _listener;
    private readonly RobotSimCore             _core;
    private readonly CancellationTokenSource  _cts = new();
    private readonly List<ClientSession>      _sessions = new();
    private readonly object                   _sessLock = new();

    #endregion

    #region Constructor

    public SimulatorServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _core     = new RobotSimCore();
        _core.StatePushed += OnStatePushed;
    }

    #endregion

    #region Public

    public async Task RunAsync()
    {
        _listener.Start();
        Console.WriteLine(
            $"[RobotSim] Listening on {_listener.LocalEndpoint}  (Ctrl+C to stop)");

        _ = Task.Run(() => HeartbeatLoopAsync(_cts.Token));

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var tcp     = await _listener.AcceptTcpClientAsync(_cts.Token)
                                             .ConfigureAwait(false);
                var session = new ClientSession(tcp, _core);
                lock (_sessLock) _sessions.Add(session);
                Console.WriteLine(
                    $"[RobotSim] Client connected: {tcp.Client.RemoteEndPoint}  " +
                    $"(total={_sessions.Count})");

                _ = session.RunAsync(_cts.Token)
                           .ContinueWith(_ =>
                           {
                               lock (_sessLock) _sessions.Remove(session);
                               Console.WriteLine(
                                   $"[RobotSim] Client disconnected. " +
                                   $"(total={_sessions.Count})");
                           });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[RobotSim] Accept error: {ex.Message}");
            }
        }
    }

    #endregion

    #region Heartbeat & Broadcast

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(HeartbeatMs, ct).ConfigureAwait(false);
            Broadcast(_core.GetCurrentState());
        }
    }

    private void OnStatePushed(RobotResponse resp) => Broadcast(resp);

    private void Broadcast(RobotResponse resp)
    {
        List<ClientSession> snapshot;
        lock (_sessLock) snapshot = new List<ClientSession>(_sessions);
        foreach (var s in snapshot) s.SendJson(resp);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        lock (_sessLock) foreach (var s in _sessions) s.Dispose();
    }

    #endregion
}
