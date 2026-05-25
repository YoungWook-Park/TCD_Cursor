using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Tcd.Plc.Simulator.Protocol;

namespace Tcd.Plc.Simulator;

/// <summary>
/// 연결된 WPF 클라이언트 1개를 담당하는 I/O 세션.
/// 수신한 JSON 라인 → PlcSimCore 조회/변경 → 응답 전송.
/// </summary>
internal sealed class PlcClientSession : IDisposable
{
  #region Variable

  private readonly TcpClient    _client;
  private readonly PlcSimCore   _core;
  private readonly StreamWriter _writer;
  private readonly StreamReader _reader;
  private readonly object       _writeLock = new();
  private bool _disposed;

  #endregion

  #region Constructor

  public PlcClientSession(TcpClient client, PlcSimCore core)
  {
    _client = client;
    _core   = core;
    var ns  = client.GetStream();
    _writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
    _reader = new StreamReader(ns, Encoding.UTF8);
  }

  #endregion

  #region Public

  public async Task RunAsync(CancellationToken ct)
  {
    try
    {
      while (!ct.IsCancellationRequested)
      {
        var line = await _reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (line is null) break;
        if (string.IsNullOrWhiteSpace(line)) continue;

        PlcRequest? req;
        try { req = JsonSerializer.Deserialize<PlcRequest>(line, JsonOpts.Default); }
        catch { continue; }
        if (req is null) continue;

        SendJson(HandleRequest(req));
      }
    }
    catch (OperationCanceledException) { }
    catch { /* 클라이언트 연결 끊김 */ }
    finally { Dispose(); }
  }

  #endregion

  #region Request Handling

  private PlcResponse HandleRequest(PlcRequest req)
  {
    switch (req.T)
    {
      case MsgType.ReadAll:
      {
        var (bits, words) = _core.GetSnapshot();
        return new PlcResponse
        {
          T = MsgType.Snapshot, Ok = true, Bits = bits, Words = words,
        };
      }

      case MsgType.ReadBit:
        return new PlcResponse
        {
          T = MsgType.Ack, Ok = true, Val = _core.ReadBit(req.Addr) ? 1 : 0,
        };

      case MsgType.ReadWord:
        return new PlcResponse
        {
          T = MsgType.Ack, Ok = true, Val = _core.ReadWord(req.Addr),
        };

      case MsgType.WriteBit:
        _core.WriteBit(req.Addr, req.Val != 0);
        return new PlcResponse { T = MsgType.Ack, Ok = true };

      case MsgType.WriteWord:
        _core.WriteWord(req.Addr, (short)req.Val);
        return new PlcResponse { T = MsgType.Ack, Ok = true };

      default:
        return new PlcResponse { T = MsgType.Ack, Ok = false };
    }
  }

  private void SendJson(PlcResponse resp)
  {
    if (_disposed) return;
    try
    {
      var json = JsonSerializer.Serialize(resp, JsonOpts.Default);
      lock (_writeLock) _writer.WriteLine(json);
    }
    catch { }
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
