namespace Tcd.Plc.Simulator;

/// <summary>
/// PLC 시뮬레이터 내부 상태 관리.
/// 50ms 타이머로 진공도(W4), 챔버 압력(W0), 로드셀(W2) 값을 랜덤 갱신.
/// 스레드 세이프: 모든 공유 상태는 _lock으로 보호.
/// </summary>
internal sealed class PlcSimCore
{
  #region Constants

  private const int BitBytes  = 8;   // B0~B6 + 여유 1바이트
  private const int WordCount = 32;  // W0~W31
  private const int UpdateMs  = 50;  // 랜덤값 갱신 주기

  // DI 비트 주소 상수 (PlcIoMap.DiBit와 동일 값)
  private const int Addr_EStop_OK   = 0;   // B0.0
  private const int Addr_DoorClosed = 1;   // B0.1

  // AI 워드 주소 상수 (PlcIoMap.AiWord와 동일 값)
  private const int W_ChamberPressure = 0;  // kPa×100
  private const int W_Loadcell        = 2;  // N×10
  private const int W_ChamberVacuum   = 4;  // kPa×10

  #endregion

  #region State

  private readonly byte[]  _bits  = new byte[BitBytes];
  private readonly short[] _words = new short[WordCount];
  private readonly Random  _rng   = new();
  private readonly object  _lock  = new();
  private Timer?           _timer;

  #endregion

  #region Constructor

  public PlcSimCore()
  {
    // 초기 DI 기본 상태: 비상정지 해제, 도어 닫힘
    SetBitUnsafe(Addr_EStop_OK,   true);
    SetBitUnsafe(Addr_DoorClosed, true);
  }

  #endregion

  #region Lifecycle

  public void Start()
  {
    _timer = new Timer(_ => UpdateValues(), null, 0, UpdateMs);
    Console.WriteLine("[PlcSimCore] 랜덤값 갱신 타이머 시작 (50ms)");
  }

  public void Stop()
  {
    _timer?.Dispose();
    _timer = null;
  }

  #endregion

  #region Public API

  public bool ReadBit(int addr)
  {
    lock (_lock)
    {
      var byteIdx = addr / 8;
      var bitIdx  = addr % 8;
      if (byteIdx >= _bits.Length) return false;
      return (_bits[byteIdx] & (1 << bitIdx)) != 0;
    }
  }

  public void WriteBit(int addr, bool value)
  {
    lock (_lock) SetBitUnsafe(addr, value);
  }

  public short ReadWord(int addr)
  {
    lock (_lock)
    {
      if (addr >= _words.Length) return 0;
      return _words[addr];
    }
  }

  public void WriteWord(int addr, short value)
  {
    lock (_lock)
    {
      if (addr >= _words.Length) return;
      _words[addr] = value;
    }
  }

  public (byte[] bits, short[] words) GetSnapshot()
  {
    lock (_lock)
    {
      return ((byte[])_bits.Clone(), (short[])_words.Clone());
    }
  }

  #endregion

  #region Private

  /// <summary>50ms 타이머 콜백 — AI 워드를 랜덤 갱신.</summary>
  private void UpdateValues()
  {
    lock (_lock)
    {
      // W4: 진공도 (kPa×10, 700~950 kPa 범위 = 7000~9500)
      _words[W_ChamberVacuum] = (short)_rng.Next(7000, 9501);

      // W0: 챔버 압력 (kPa×100, 음수 = 진공 상태, -5000~-1000)
      _words[W_ChamberPressure] = (short)_rng.Next(-5000, -999);

      // W2: 로드셀 (N×10, 0~500)
      _words[W_Loadcell] = (short)_rng.Next(0, 501);
    }
  }

  /// <summary>잠금 없이 비트 쓰기 (lock 내부에서만 호출).</summary>
  private void SetBitUnsafe(int addr, bool value)
  {
    var byteIdx = addr / 8;
    var bitIdx  = addr % 8;
    if (byteIdx >= _bits.Length) return;
    if (value)
      _bits[byteIdx] |= (byte)(1 << bitIdx);
    else
      _bits[byteIdx] &= (byte)~(1 << bitIdx);
  }

  #endregion
}
