using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Define;
using Tcd.App.Sequences.SemiAuto;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;
using Tcd.Simulator;
using Tcd.Tests.Shared.Fakes;
using Xunit;

namespace Tcd.Simulator.Tests
{
  /// <summary>
  /// 반자동 Pick-Place 시퀀스 통합 테스트 (#5 Manual_Robot + #6 SemiAuto).
  /// TcdSimulation + TcdSequenceRegistry 기반 in-process 실행.
  /// </summary>
  public class SemiAutoSequenceTests
  {
    // ── 공통 픽스처 ──────────────────────────────────────────────────────

    /// <summary>
    /// 원자 시퀀스가 모두 등록된 SequenceManager + TcdSimulation 생성.
    /// SemiAuto 시퀀스 4종을 추가 등록해 반환.
    /// </summary>
    private static (SequenceManager mgr, TcdSimulation sim) CreateEnv()
    {
      var sim      = new TcdSimulation();
      var settings = new AppSettingsProxy(TimeSpan.FromSeconds(3));
      var motion   = new SimMotionService(sim, settings);
      var mgr      = TcdSequenceRegistry.Build(sim, motion);

      mgr.Register(new SemiAutoUpperStagePick_UpperChamberPlace(mgr, sim));
      mgr.Register(new SemiAutoLowerStagePick_LowerChamberPlace(mgr, sim));
      mgr.Register(new SemiAutoUpperChamberPick_UpperStagePlace(mgr, sim));
      mgr.Register(new SemiAutoLowerChamberPick_LowerStagePlace(mgr, sim));

      return (mgr, sim);
    }

    /// <summary>Z Ready 센서 시뮬레이션: LowerChamberAtReady + UpperChamberAtReady ON.</summary>
    private static async Task SetZReadyAsync(IPlc plc)
    {
      await plc.WriteBitAsync(
        (DoBit)(int)DiBit.LowerChamberAtReady, true, CancellationToken.None);
      await plc.WriteBitAsync(
        (DoBit)(int)DiBit.UpperChamberAtReady, true, CancellationToken.None);
    }

    // ════════════════════════════════════════════════════════════════════
    // 1. Upper Stage Pick → Upper Chamber Place
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpperStagePick_UpperChamberPlace_Normal_Succeeds()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      await SetZReadyAsync(sim.Plc);

      var ctx = sim;
      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace,
        ctx, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
      // 자재: Stage1 → UpperChamber
      Assert.Null(sim.Materials.Get(MaterialLocation.Stage1));
      Assert.NotNull(sim.Materials.Get(MaterialLocation.UpperChamber));
      // Robot 홈 복귀
      Assert.Equal(RobotPosition.Home, sim.Robot.CurrentPosition);
    }

    [Fact]
    public async Task UpperStagePick_UpperChamberPlace_ChamberNotEmpty_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      // 챔버에 자재 선배치 → 인터락 발동
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.UpperFilm,
          MaterialState.Loaded, MaterialLocation.UpperChamber),
        MaterialLocation.UpperChamber);

      var alarms = (AlarmManager)sim.Alarms;
      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task UpperStagePick_UpperChamberPlace_RobotNotAtHome_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      // 로봇을 Home이 아닌 위치로 이동
      await sim.Robot.CommandMoveToAsync(RobotPosition.Stage, CancellationToken.None);
      await sim.Robot.WaitForPositionAsync(
        RobotPosition.Stage, TimeSpan.FromSeconds(1), CancellationToken.None);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task UpperStagePick_UpperChamberPlace_ZNotReady_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      // Z Ready 센서 OFF (설정 안 함 → 초기값 false)

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
      Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task UpperStagePick_UpperChamberPlace_Cancelled_Stops()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      await SetZReadyAsync(sim.Plc);

      using var cts = new CancellationTokenSource();
      cts.Cancel();

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace,
        sim, null, cts.Token);

      Assert.Equal(SequenceStatus.Stopped, result.Status);
    }

    // ════════════════════════════════════════════════════════════════════
    // 2. Lower Stage Pick → Lower Chamber Place
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LowerStagePick_LowerChamberPlace_Normal_Succeeds()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      await SetZReadyAsync(sim.Plc);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
      Assert.Null(sim.Materials.Get(MaterialLocation.Stage2));
      Assert.NotNull(sim.Materials.Get(MaterialLocation.LowerChamber));
      Assert.Equal(RobotPosition.Home, sim.Robot.CurrentPosition);
    }

    [Fact]
    public async Task LowerStagePick_LowerChamberPlace_ChamberNotEmpty_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.LowerFilm,
          MaterialState.Loaded, MaterialLocation.LowerChamber),
        MaterialLocation.LowerChamber);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task LowerStagePick_LowerChamberPlace_ZNotReady_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      // Z Ready 설정 안 함

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    // ════════════════════════════════════════════════════════════════════
    // 3. Upper Chamber Pick → Upper Stage Place (언로드)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpperChamberPick_UpperStagePlace_Normal_Succeeds()
    {
      var (mgr, sim) = CreateEnv();
      // 챔버에 자재 배치 (언로드 대상)
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.UpperFilm,
          MaterialState.Loaded, MaterialLocation.UpperChamber),
        MaterialLocation.UpperChamber);
      await SetZReadyAsync(sim.Plc);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
      Assert.Null(sim.Materials.Get(MaterialLocation.UpperChamber));
      Assert.NotNull(sim.Materials.Get(MaterialLocation.Stage1));
      Assert.Equal(RobotPosition.Home, sim.Robot.CurrentPosition);
    }

    [Fact]
    public async Task UpperChamberPick_UpperStagePlace_ChamberEmpty_Fails()
    {
      var (mgr, sim) = CreateEnv();
      // 챔버 비어있음 → 인터락 발동 (자재 없음)
      await SetZReadyAsync(sim.Plc);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task UpperChamberPick_UpperStagePlace_RobotNotAtHome_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.UpperFilm,
          MaterialState.Loaded, MaterialLocation.UpperChamber),
        MaterialLocation.UpperChamber);
      await SetZReadyAsync(sim.Plc);
      // 로봇 이동
      await sim.Robot.CommandMoveToAsync(RobotPosition.Stage, CancellationToken.None);
      await sim.Robot.WaitForPositionAsync(
        RobotPosition.Stage, TimeSpan.FromSeconds(1), CancellationToken.None);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task UpperChamberPick_UpperStagePlace_ZNotReady_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.UpperFilm,
          MaterialState.Loaded, MaterialLocation.UpperChamber),
        MaterialLocation.UpperChamber);
      // Z Ready 없음

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task UpperChamberPick_UpperStagePlace_Cancelled_Stops()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.UpperFilm,
          MaterialState.Loaded, MaterialLocation.UpperChamber),
        MaterialLocation.UpperChamber);
      await SetZReadyAsync(sim.Plc);

      using var cts = new CancellationTokenSource();
      cts.Cancel();

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace,
        sim, null, cts.Token);

      Assert.Equal(SequenceStatus.Stopped, result.Status);
    }

    // ════════════════════════════════════════════════════════════════════
    // 4. Lower Chamber Pick → Lower Stage Place (언로드)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LowerChamberPick_LowerStagePlace_Normal_Succeeds()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.LowerFilm,
          MaterialState.Loaded, MaterialLocation.LowerChamber),
        MaterialLocation.LowerChamber);
      await SetZReadyAsync(sim.Plc);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerChamberPick_LowerStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Succeeded, result.Status);
      Assert.Null(sim.Materials.Get(MaterialLocation.LowerChamber));
      Assert.NotNull(sim.Materials.Get(MaterialLocation.Stage2));
      Assert.Equal(RobotPosition.Home, sim.Robot.CurrentPosition);
    }

    [Fact]
    public async Task LowerChamberPick_LowerStagePlace_ChamberEmpty_Fails()
    {
      var (mgr, sim) = CreateEnv();
      await SetZReadyAsync(sim.Plc);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerChamberPick_LowerStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    [Fact]
    public async Task LowerChamberPick_LowerStagePlace_ZNotReady_Fails()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.LowerFilm,
          MaterialState.Loaded, MaterialLocation.LowerChamber),
        MaterialLocation.LowerChamber);

      var result = await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerChamberPick_LowerStagePlace,
        sim, null, CancellationToken.None);

      Assert.Equal(SequenceStatus.Failed, result.Status);
    }

    // ════════════════════════════════════════════════════════════════════
    // 5. IO 상태 검증
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpperStagePick_UpperChamberPlace_IoState_AfterLoad()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      await SetZReadyAsync(sim.Plc);

      await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperStagePick_UpperChamberPlace,
        sim, null, CancellationToken.None);

      // 시퀀스 완료 후 ESC Enable ON, Vac ON, Grip Off 상태여야 함
      var escOn = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.UpperEscEnable, CancellationToken.None);
      var vacOn = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.UpperChamberVacOn, CancellationToken.None);
      var gripOn = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.RobotGripVacOn, CancellationToken.None);

      Assert.True(escOn,   "UpperEscEnable should be ON after load");
      Assert.True(vacOn,   "UpperChamberVacOn should be ON after load");
      Assert.False(gripOn, "RobotGripVacOn should be OFF after place");
    }

    [Fact]
    public async Task UpperChamberPick_UpperStagePlace_IoState_AfterUnload()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.UpperFilm,
          MaterialState.Loaded, MaterialLocation.UpperChamber),
        MaterialLocation.UpperChamber);
      await SetZReadyAsync(sim.Plc);

      await mgr.RunAsync(
        TcdSequenceKeys.SEMI_UpperChamberPick_UpperStagePlace,
        sim, null, CancellationToken.None);

      // 언로드 후 챔버 Vac OFF, StageVac ON
      var chamberVac = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.UpperChamberVacOn, CancellationToken.None);
      var stageVac   = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.HighStageVacOn, CancellationToken.None);

      Assert.False(chamberVac, "UpperChamberVacOn should be OFF after unload");
      Assert.True(stageVac,    "HighStageVacOn should be ON after unload");
    }

    [Fact]
    public async Task LowerStagePick_LowerChamberPlace_IoState_AfterLoad()
    {
      var (mgr, sim) = CreateEnv();
      sim.LoadStage();
      await SetZReadyAsync(sim.Plc);

      await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerStagePick_LowerChamberPlace,
        sim, null, CancellationToken.None);

      // 시퀀스 완료 후 ESC Enable ON, Vac ON, Grip Off 상태여야 함
      var escOn = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.LowerEscEnable, CancellationToken.None);
      var vacOn = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.LowerChamberVacOn, CancellationToken.None);
      var gripOn = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.RobotGripVacOn, CancellationToken.None);

      Assert.True(escOn,   "LowerEscEnable should be ON after load");
      Assert.True(vacOn,   "LowerChamberVacOn should be ON after load");
      Assert.False(gripOn, "RobotGripVacOn should be OFF after place");
    }

    [Fact]
    public async Task LowerChamberPick_LowerStagePlace_IoState_AfterUnload()
    {
      var (mgr, sim) = CreateEnv();
      sim.Materials.Place(
        new Material(Guid.NewGuid(), MaterialKind.LowerFilm,
          MaterialState.Loaded, MaterialLocation.LowerChamber),
        MaterialLocation.LowerChamber);
      await SetZReadyAsync(sim.Plc);

      await mgr.RunAsync(
        TcdSequenceKeys.SEMI_LowerChamberPick_LowerStagePlace,
        sim, null, CancellationToken.None);

      // 언로드 후 챔버 Vac OFF, StageVac ON
      var chamberVac = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.LowerChamberVacOn, CancellationToken.None);
      var stageVac   = await sim.Plc.ReadBitAsync(
        (DiBit)(int)DoBit.LowStageVacOn, CancellationToken.None);

      Assert.False(chamberVac, "LowerChamberVacOn should be OFF after unload");
      Assert.True(stageVac,    "LowStageVacOn should be ON after unload");
    }

    // ════════════════════════════════════════════════════════════════════
    // 6. Manual_Robot Z Ready 인터락 (DelegateSequence 직접 실행)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ManualRobot_ChamberEntry_MoveAndWait_Succeeds()
    {
      var (mgr, sim) = CreateEnv();

      // Move 명령 + Wait 시퀀스 연속 실행 → 최종 위치 확인
      var move = await mgr.RunAsync(
        TcdSequenceKeys.Robot_Move_UpperChamberWait,
        sim, null, CancellationToken.None);
      Assert.Equal(SequenceStatus.Succeeded, move.Status);

      var wait = await mgr.RunAsync(
        TcdSequenceKeys.Robot_Wait_UpperChamberWait,
        sim, TimeSpan.FromSeconds(2), CancellationToken.None);
      Assert.Equal(SequenceStatus.Succeeded, wait.Status);
      Assert.Equal(RobotPosition.UpperChamberWait, sim.Robot.CurrentPosition);
    }

    [Fact]
    public async Task ManualRobot_StagePositions_NoInterlock_Succeeds()
    {
      var (mgr, sim) = CreateEnv();

      // 스테이지 포지션은 Z Ready 인터락 없이 실행 가능
      foreach (var key in new[]
      {
        TcdSequenceKeys.Robot_Move_UpperStageWait,
        TcdSequenceKeys.Robot_Move_UpperStageContact,
        TcdSequenceKeys.Robot_Move_LowerStageWait,
        TcdSequenceKeys.Robot_Move_LowerStageContact,
        TcdSequenceKeys.Robot_Move_Home,
      })
      {
        var r = await mgr.RunAsync(key, sim, null, CancellationToken.None);
        Assert.Equal(SequenceStatus.Succeeded, r.Status);
      }
    }
  }
}
