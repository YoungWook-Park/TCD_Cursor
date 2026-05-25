using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;

namespace Tcd.Simulator
{
  /// <summary>
  /// IPlc 인프로세스 시뮬레이션 구현.
  /// TCP 통신 없이 MaterialTracker와 내부 비트/워드 배열로 동작.
  /// 모니터링 루프 관련 멤버는 no-op (인프로세스에서는 불필요).
  /// </summary>
  public sealed class SimPlc : IPlc
  {
    #region Constants

    private const int BitBytes  = 8;
    private const int WordCount = 32;

    #endregion

    #region Variable

    private readonly ITimeProvider     _time;
    private readonly IMaterialTracker  _materials;

    private readonly byte[]  _bits  = new byte[BitBytes];
    private readonly short[] _words = new short[WordCount];
    private readonly object  _lock  = new object();

    #endregion

    #region Constructor

    public SimPlc(ITimeProvider time, IMaterialTracker materials)
    {
      _time      = time      ?? throw new ArgumentNullException("time");
      _materials = materials ?? throw new ArgumentNullException("materials");
    }

    #endregion

    #region IPlc — WaitForStageLoaded

    public async Task<bool> WaitForStageLoadedAsync(
      TimeSpan timeout, CancellationToken cancellationToken)
    {
      var start = _time.Now;
      while (_time.Now - start < timeout)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var s1 = _materials.Get(MaterialLocation.Stage1);
        var s2 = _materials.Get(MaterialLocation.Stage2);
        if (s1 != null && s2 != null) return true;

        await _time.Delay(
          TimeSpan.FromMilliseconds(100), cancellationToken)
          .ConfigureAwait(false);
      }
      return false;
    }

    #endregion

    #region IPlc — Read / Write

    public Task<bool> ReadBitAsync(DiBit address, CancellationToken ct)
    {
      lock (_lock)
      {
        var addr    = (int)address;
        var byteIdx = addr / 8;
        var bitIdx  = addr % 8;
        var result  = byteIdx < _bits.Length
                      && (_bits[byteIdx] & (1 << bitIdx)) != 0;
        return Task.FromResult(result);
      }
    }

    public Task WriteBitAsync(DoBit address, bool value, CancellationToken ct)
    {
      lock (_lock)
      {
        var addr    = (int)address;
        var byteIdx = addr / 8;
        var bitIdx  = addr % 8;
        if (byteIdx >= _bits.Length) return Task.CompletedTask;
        if (value)
          _bits[byteIdx] |= (byte)(1 << bitIdx);
        else
          _bits[byteIdx] &= (byte)~(1 << bitIdx);
      }
      return Task.CompletedTask;
    }

    public Task<short> ReadWordAsync(AiWord address, CancellationToken ct)
    {
      lock (_lock)
      {
        var addr = (int)address;
        return Task.FromResult(
          addr < _words.Length ? _words[addr] : (short)0);
      }
    }

    public Task WriteWordAsync(
      AoWord address, short value, CancellationToken ct)
    {
      lock (_lock)
      {
        var addr = (int)address;
        if (addr < _words.Length) _words[addr] = value;
      }
      return Task.CompletedTask;
    }

    #endregion

    #region IPlc — 모니터링 루프 (인프로세스에서는 no-op)

    public void StartMonitoring(TimeSpan interval) { }

    public void StopMonitoring() { }

    public event EventHandler<PlcSnapshotArgs> SnapshotUpdated
    {
      add    { }
      remove { }
    }

    #endregion
  }
}
