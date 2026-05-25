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
/// IPlc 의 TCP 클라이언트 구현.
/// 프로토콜: UTF-8 개행 구분 JSON, 폴링(Request-Response) 방식.
///
/// 통신 직렬화:
///   _reqSem(SemaphoreSlim 1,1)으로 모니터링 루프와 개별 요청이
///   reader/writer를 동시에 사용하지 못하도록 보장.
///
/// 모니터링 루프:
///   StartMonitoring 호출 시 100ms 주기로 ReadAll → 캐시 갱신 → SnapshotUpdated.
///   ReadWordAsync 는 모니터링 루프와 직렬화되어 최신값을 즉시 읽어옴.
/// </summary>
public sealed class PlcTcpClient : IPlc, IDisposable
{
  #region Variable

  private static readonly JsonSerializerOptions JsonOpts = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  };

  private TcpClient?    _tcp;
  private StreamWriter? _writer;
  private StreamReader? _reader;

  private readonly SemaphoreSlim _reqSem   = new(1, 1);
  private CancellationTokenSource? _monitorCts;

  // ── 스냅샷 캐시 (모니터링 루프가 갱신) ──────────────────────────────
  private readonly object _cacheLock    = new();
  private byte[]  _cachedBits           = new byte[8];
  private short[] _cachedWords          = new short[32];

  #endregion

  #region Properties

  public bool IsConnected { get; private set; }

  public event EventHandler<PlcSnapshotArgs>? SnapshotUpdated;

  // IPlc explicit (nullable 제거 래퍼)
  event EventHandler<PlcSnapshotArgs> IPlc.SnapshotUpdated
  {
    add    => SnapshotUpdated += value;
    remove => SnapshotUpdated -= value;
  }

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
    IsConnected = true;
  }

  public void Disconnect()
  {
    StopMonitoring();
    try { _tcp?.Dispose(); } catch { }
    _tcp        = null;
    IsConnected = false;
  }

  #endregion

  #region IPlc — 개별 Read / Write

  public async Task<bool> WaitForStageLoadedAsync(
    TimeSpan timeout, CancellationToken ct)
  {
    var start = DateTimeOffset.UtcNow;
    while (DateTimeOffset.UtcNow - start < timeout)
    {
      ct.ThrowIfCancellationRequested();
      bool low, high;
      lock (_cacheLock)
      {
        low  = GetCachedBit(DiBit.MaterialLowStage);
        high = GetCachedBit(DiBit.MaterialHighStage);
      }
      if (low && high) return true;
      await Task.Delay(100, ct).ConfigureAwait(false);
    }
    return false;
  }

  public async Task<bool> ReadBitAsync(DiBit address, CancellationToken ct)
  {
    var resp = await SendAsync(
      new Req { T = "ReadBit", Addr = (int)address }, ct)
      .ConfigureAwait(false);
    return resp?.Val == 1;
  }

  public async Task WriteBitAsync(
    DoBit address, bool value, CancellationToken ct)
  {
    await SendAsync(
      new Req { T = "WriteBit", Addr = (int)address, Val = value ? 1 : 0 }, ct)
      .ConfigureAwait(false);
  }

  public async Task<short> ReadWordAsync(AiWord address, CancellationToken ct)
  {
    var resp = await SendAsync(
      new Req { T = "ReadWord", Addr = (int)address }, ct)
      .ConfigureAwait(false);
    return resp?.Val is int v ? (short)v : (short)0;
  }

  public async Task WriteWordAsync(
    AoWord address, short value, CancellationToken ct)
  {
    await SendAsync(
      new Req { T = "WriteWord", Addr = (int)address, Val = value }, ct)
      .ConfigureAwait(false);
  }

  #endregion

  #region IPlc — 모니터링 루프

  public void StartMonitoring(TimeSpan interval)
  {
    _monitorCts = new CancellationTokenSource();
    _ = Task.Run(() => MonitorLoopAsync(interval, _monitorCts.Token));
  }

  public void StopMonitoring()
  {
    _monitorCts?.Cancel();
    _monitorCts?.Dispose();
    _monitorCts = null;
  }

  private async Task MonitorLoopAsync(
    TimeSpan interval, CancellationToken ct)
  {
    while (!ct.IsCancellationRequested && IsConnected)
    {
      try
      {
        var resp = await SendAsync(
          new Req { T = "ReadAll" }, ct).ConfigureAwait(false);

        if (resp?.T == "Snapshot"
            && resp.Bits  is byte[]  bits
            && resp.Words is short[] words)
        {
          lock (_cacheLock)
          {
            _cachedBits  = bits;
            _cachedWords = words;
          }
          SnapshotUpdated?.Invoke(
            this, new PlcSnapshotArgs(bits, words));
        }

        await Task.Delay(interval, ct).ConfigureAwait(false);
      }
      catch (OperationCanceledException) { break; }
      catch
      {
        IsConnected = false;
        break;
      }
    }
  }

  #endregion

  #region Send / Receive (직렬화 보장)

  /// <summary>
  /// 요청 전송 → 응답 수신. SemaphoreSlim으로 동시 접근 방지.
  /// </summary>
  private async Task<Resp?> SendAsync(Req req, CancellationToken ct)
  {
    if (!IsConnected) return null;
    await _reqSem.WaitAsync(ct).ConfigureAwait(false);
    try
    {
      var json = JsonSerializer.Serialize(req, JsonOpts);
      _writer!.WriteLine(json);

      var line = await _reader!.ReadLineAsync(ct).ConfigureAwait(false);
      if (line is null) { IsConnected = false; return null; }

      return JsonSerializer.Deserialize<Resp>(line, JsonOpts);
    }
    catch (OperationCanceledException) { throw; }
    catch
    {
      IsConnected = false;
      return null;
    }
    finally { _reqSem.Release(); }
  }

  #endregion

  #region Cache Helpers

  private bool GetCachedBit(DiBit addr)
  {
    var a       = (int)addr;
    var byteIdx = a / 8;
    var bitIdx  = a % 8;
    if (byteIdx >= _cachedBits.Length) return false;
    return (_cachedBits[byteIdx] & (1 << bitIdx)) != 0;
  }

  #endregion

  #region IDisposable

  public void Dispose() => Disconnect();

  #endregion

  // ── 경량 직렬화 전용 DTO ─────────────────────────────────────────────
  private sealed class Req
  {
    [JsonPropertyName("T")]    public string T    { get; set; } = "";
    [JsonPropertyName("Addr")] public int    Addr { get; set; }
    [JsonPropertyName("Val")]  public int    Val  { get; set; }
  }

  private sealed class Resp
  {
    [JsonPropertyName("T")]     public string   T     { get; set; } = "";
    [JsonPropertyName("Ok")]    public bool     Ok    { get; set; }
    [JsonPropertyName("Val")]   public int?     Val   { get; set; }
    [JsonPropertyName("Bits")]  public byte[]?  Bits  { get; set; }
    [JsonPropertyName("Words")] public short[]? Words { get; set; }
  }
}
