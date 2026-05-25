using System;

namespace Tcd.App.Define
{
  /// <summary>
  /// PLC 관련 상수. IO맵 주소 외 임계값/타이밍 정의.
  /// </summary>
  public static class PlcDefine
  {
    // ── 진공도 임계값 ──────────────────────────────────────────────────────
    /// <summary>합착 구간 진공도 정상 하한 (kPa×10, 700 kPa = 7000).</summary>
    public const short VacuumOkThreshold = 7000;

    // ── 모니터링 주기 ──────────────────────────────────────────────────────
    /// <summary>IO맵 전체 폴링 주기.</summary>
    public static readonly TimeSpan MonitorInterval =
      TimeSpan.FromMilliseconds(100);

    // ── 서버 기본값 ───────────────────────────────────────────────────────
    public const string DefaultHost = "127.0.0.1";
    public const int    DefaultPort = 7002;
  }
}
