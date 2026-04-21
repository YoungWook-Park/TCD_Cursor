using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Tcd.Robot.Simulator.Protocol;

namespace Tcd.Robot.Simulator;

/// <summary>
/// 연결된 HMI 클라이언트 1개를 담당하는 I/O 세션.
/// 수신한 JSON 라인 → RobotSimCore.HandleRequest → 응답 전송.
/// </summary>
internal sealed class ClientSession : IDisposable
{
    #region Variable

    private readonly TcpClient    _client;
    private readonly RobotSimCore _core;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;
    private readonly object       _writeLock = new();
    private bool _disposed;

    #endregion

    #region Constructor

    public ClientSession(TcpClient client, RobotSimCore core)
    {
        _client = client;
        _core   = core;
        var ns  = client.GetStream();
        _writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
        _reader = new StreamReader(ns, Encoding.UTF8);
    }

    #endregion

    #region Public

    /// <summary>클라이언트 수신 루프. 연결 종료 또는 취소 시 반환.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        // 접속 직후 현재 상태 Push
        SendJson(_core.GetCurrentState());

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;               // 연결 종료
                if (string.IsNullOrWhiteSpace(line)) continue;

                RobotRequest? req;
                try { req = JsonSerializer.Deserialize<RobotRequest>(line, JsonOpts.Default); }
                catch { continue; }                    // 파싱 오류 무시
                if (req is null) continue;

                var resp = _core.HandleRequest(req);
                SendJson(resp);
            }
        }
        catch (OperationCanceledException) { }
        catch { /* 클라이언트 연결 끊김 */ }
        finally { Dispose(); }
    }

    /// <summary>서버 측 Push (heartbeat, 상태 변화) 전송.</summary>
    public void SendJson(RobotResponse resp)
    {
        if (_disposed) return;
        try
        {
            var json = JsonSerializer.Serialize(resp, JsonOpts.Default);
            lock (_writeLock)
                _writer.WriteLine(json);
        }
        catch { /* 클라이언트 이미 종료 */ }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _client.Dispose(); } catch { }
    }

    #endregion
}
