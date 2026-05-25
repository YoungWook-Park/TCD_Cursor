using System.Net;
using System.Net.Sockets;

namespace Tcd.Plc.Simulator;

/// <summary>
/// PLC 시뮬레이터 TCP 서버.
/// 폴링 방식 — 클라이언트 요청마다 PlcSimCore에서 값을 읽어 응답.
/// </summary>
public sealed class PlcSimServer : IDisposable
{
  #region Variable

  private readonly TcpListener             _listener;
  private readonly PlcSimCore              _core;
  private readonly CancellationTokenSource _cts = new();

  #endregion

  #region Constructor

  public PlcSimServer(int port)
  {
    _listener = new TcpListener(IPAddress.Any, port);
    _core     = new PlcSimCore();
  }

  #endregion

  #region Public

  public async Task RunAsync()
  {
    _core.Start();
    _listener.Start();
    Console.WriteLine(
      $"[PlcSim] Listening on {_listener.LocalEndpoint}  (Ctrl+C to stop)");

    while (!_cts.Token.IsCancellationRequested)
    {
      try
      {
        var tcp     = await _listener.AcceptTcpClientAsync(_cts.Token)
                                     .ConfigureAwait(false);
        var session = new PlcClientSession(tcp, _core);
        Console.WriteLine(
          $"[PlcSim] Client connected: {tcp.Client.RemoteEndPoint}");

        _ = session.RunAsync(_cts.Token)
                   .ContinueWith(_ =>
                   {
                     Console.WriteLine("[PlcSim] Client disconnected.");
                   });
      }
      catch (OperationCanceledException) { break; }
      catch (Exception ex)
      {
        Console.WriteLine($"[PlcSim] Accept error: {ex.Message}");
      }
    }
  }

  #endregion

  #region IDisposable

  public void Dispose()
  {
    _core.Stop();
    _cts.Cancel();
    _listener.Stop();
  }

  #endregion
}
