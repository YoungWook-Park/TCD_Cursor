using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Devices;

namespace Tcd.App.Devices;

/// <summary>
/// IRobotDevice 의 TCP 클라이언트 구현.
/// 프로토콜: UTF-8 개행 구분 JSON.
///
/// 상태 수신 방식:
///   서버가 300ms 주기 Push(heartbeat) + 이동 완료/에러 시 즉시 Push.
///   클라이언트는 ReadLoopAsync 에서 수신 → StateChanged 이벤트.
///
/// 커맨드 전송:
///   SetVelocity → Move 순서로 호출하면 서버가 속도 적용 후 이동.
///   MoveAsync / StopAsync 는 Ack 수신 후 반환 (fire-and-forget 아님).
/// </summary>
public sealed class RobotTcpClient : IRobotDevice, IDisposable
{
    #region Variable

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private TcpClient?    _tcp;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private CancellationTokenSource? _readCts;

    private readonly object _stateLock = new();
    private readonly object _writeLock = new();

    // ── 로컬 캐시 상태 (ReadLoop가 갱신) ──────────────────────────────────
    private bool          _connected;
    private bool          _running;
    private bool          _home;
    private bool          _error;
    private int           _pos;
    private string?       _errorMessage;

    #endregion

    #region IRobotDevice Properties

    public bool IsConnected   { get { lock (_stateLock) return _connected; } }
    public bool IsRunning     { get { lock (_stateLock) return _running; } }
    public bool IsHome        { get { lock (_stateLock) return _home; } }
    public bool IsError       { get { lock (_stateLock) return _error; } }
    public RobotPosition CurrentPosition
        { get { lock (_stateLock) return (RobotPosition)_pos; } }
    public string? ErrorMessage { get { lock (_stateLock) return _errorMessage; } }

    public event EventHandler<RobotDeviceStateArgs>? StateChanged;

    #endregion

    #region Connect / Disconnect

    public async Task ConnectAsync(
        string host, int port, CancellationToken ct = default)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);

        var ns  = _tcp.GetStream();
        _writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
        _reader = new StreamReader(ns, Encoding.UTF8);

        lock (_stateLock) _connected = true;

        _readCts = new CancellationTokenSource();
        _ = Task.Run(() => ReadLoopAsync(_readCts.Token));
    }

    public void Disconnect()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        _readCts = null;

        try { _tcp?.Dispose(); } catch { }
        _tcp = null;

        lock (_stateLock)
        {
            _connected = false;
            _running   = false;
        }
        RaiseStateChanged();
    }

    #endregion

    #region Commands

    public async Task<bool> SetVelocityAsync(
        RobotPosition position, int pct, CancellationToken ct = default)
    {
        if (!IsConnected) return false;
        return await SendRequestAsync(
            new Req { T = "SetVelocity", Pos = (int)position, Pct = pct }, ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> MoveAsync(
        RobotPosition position, CancellationToken ct = default)
    {
        if (!IsConnected) return false;
        return await SendRequestAsync(
            new Req { T = "Move", Pos = (int)position }, ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> StopAsync(CancellationToken ct = default)
    {
        if (!IsConnected) return false;
        return await SendRequestAsync(
            new Req { T = "Stop" }, ct)
            .ConfigureAwait(false);
    }

    public async Task WaitForPositionAsync(
        RobotPosition position, TimeSpan timeout, CancellationToken ct)
    {
        var start = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - start < timeout)
        {
            ct.ThrowIfCancellationRequested();
            if (CurrentPosition == position && !IsRunning) return;
            await Task.Delay(50, ct).ConfigureAwait(false);
        }
        throw new TimeoutException(
            $"Robot timeout waiting for {position} (current={CurrentPosition}).");
    }

    #endregion

    #region Send

    /// <summary>요청 JSON 전송. 에러 시 false 반환.</summary>
    private Task<bool> SendRequestAsync(Req req, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var json = JsonSerializer.Serialize(req, JsonOpts);
            lock (_writeLock) _writer!.WriteLine(json);
            return Task.FromResult(true);
        }
        catch { return Task.FromResult(false); }
    }

    #endregion

    #region Read Loop (상태 수신)

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await _reader!.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                Resp? resp;
                try { resp = JsonSerializer.Deserialize<Resp>(line, JsonOpts); }
                catch { continue; }
                if (resp is null) continue;

                // "State" 또는 "Arrived" 프레임으로 로컬 캐시 갱신
                if (resp.T == "State" || resp.T == "Arrived")
                {
                    lock (_stateLock)
                    {
                        _connected    = resp.Connected;
                        _running      = resp.Running;
                        _home         = resp.Home;
                        _error        = resp.Error;
                        _pos          = resp.Pos;
                        _errorMessage = resp.ErrMsg;
                    }
                    RaiseStateChanged();
                }
                // "Ack" 프레임은 현재 추적하지 않음 (필요 시 요청-응답 ID 매핑 확장)
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            lock (_stateLock) { _connected = false; _running = false; }
            RaiseStateChanged();
        }
    }

    #endregion

    #region Helpers

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new RobotDeviceStateArgs
        {
            IsConnected     = IsConnected,
            IsRunning       = IsRunning,
            IsHome          = IsHome,
            IsError         = IsError,
            CurrentPosition = CurrentPosition,
            ErrorMessage    = ErrorMessage,
        });
    }

    #endregion

    #region IDisposable

    public void Dispose() => Disconnect();

    #endregion

    // ── 경량 직렬화 전용 DTO (공개할 필요 없음) ──────────────────────────
    private sealed class Req
    {
        [JsonPropertyName("T")]     public string T     { get; set; } = "";
        [JsonPropertyName("Pos")]   public int    Pos   { get; set; }
        [JsonPropertyName("Pct")]   public int    Pct   { get; set; }
        [JsonPropertyName("X")]     public double X     { get; set; }
        [JsonPropertyName("Y")]     public double Y     { get; set; }
        [JsonPropertyName("Theta")] public double Theta { get; set; }
    }

    private sealed class Resp
    {
        [JsonPropertyName("T")]         public string  T         { get; set; } = "";
        [JsonPropertyName("Cmd")]       public string? Cmd       { get; set; }
        [JsonPropertyName("Ok")]        public bool    Ok        { get; set; }
        [JsonPropertyName("Err")]       public string? Err       { get; set; }
        [JsonPropertyName("Connected")] public bool    Connected { get; set; }
        [JsonPropertyName("Running")]   public bool    Running   { get; set; }
        [JsonPropertyName("Home")]      public bool    Home      { get; set; }
        [JsonPropertyName("Error")]     public bool    Error     { get; set; }
        [JsonPropertyName("Pos")]       public int     Pos       { get; set; }
        [JsonPropertyName("ErrMsg")]    public string? ErrMsg    { get; set; }
    }
}
