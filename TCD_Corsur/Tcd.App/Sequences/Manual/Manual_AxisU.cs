using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core.Logging;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual;

/// <summary>U축 수동 시퀀스 팩토리. 축별 인터락과 동작을 한 파일에 관리.</summary>
public static class Manual_AxisU
{
    private const string Axis = AxisDefine.U;

    public static void RegisterAll(SequenceManager mgr)
    {
        mgr.Register(AbsMove());
        mgr.Register(IncMove());
        mgr.Register(JogMove());
        mgr.Register(Stop());
        mgr.Register(Home());
        mgr.Register(FaultReset());
        mgr.Register(ServoOn());
        mgr.Register(ServoOff());
    }

    public static ISequence AbsMove() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_AbsMove, $"{Axis} AbsMove",
        async (ctx, p, ct) =>
        {
            CheckMotionInterlock(ctx);
            var core = MainCore.Instance;
            core.LogContext = new LogContext { SequenceKey = TcdSequenceKeys.Manual_Motor_U_AbsMove, RunId = Guid.NewGuid(), AxisName = Axis };
            var target = core.Recipes.Current?.GetAxis(Axis, 0) ?? 0;
            core.Log.Info(core.LogContext, "Start", $"AbsMove target={target}");
            await core.Motion.AbsMoveAsync(Axis, target, ct).ConfigureAwait(false);
            core.Log.Info(core.LogContext, "End", "AbsMove 완료");
        });

    public static ISequence IncMove() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_IncMove, $"{Axis} IncMove",
        async (ctx, p, ct) =>
        {
            CheckMotionInterlock(ctx);
            var core = MainCore.Instance;
            core.LogContext = new LogContext { SequenceKey = TcdSequenceKeys.Manual_Motor_U_IncMove, RunId = Guid.NewGuid(), AxisName = Axis };
            double delta = p is double d ? d : (p is IConvertible c ? c.ToDouble(null) : 0);
            core.Log.Info(core.LogContext, "Start", $"IncMove delta={delta}");
            await core.Motion.IncMoveAsync(Axis, delta, ct).ConfigureAwait(false);
            core.Log.Info(core.LogContext, "End", "IncMove 완료");
        });

    public static ISequence JogMove() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_JogMove, $"{Axis} JogMove",
        async (ctx, p, ct) =>
        {
            CheckMotionInterlock(ctx);
            var core = MainCore.Instance;
            core.LogContext = new LogContext { SequenceKey = TcdSequenceKeys.Manual_Motor_U_JogMove, RunId = Guid.NewGuid(), AxisName = Axis };
            double velocity = p is double v ? v : (p is IConvertible c ? c.ToDouble(null) : 0);
            core.Log.Info(core.LogContext, "Start", $"JogMove velocity={velocity}");
            await core.Motion.JogAsync(Axis, velocity, ct).ConfigureAwait(false);
            core.Log.Info(core.LogContext, "End", "JogMove 종료");
        });

    public static ISequence Stop() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_Stop, $"{Axis} Stop",
        async (ctx, p, ct) =>
        {
            await MainCore.Instance.Motion.StopAsync(Axis, ct).ConfigureAwait(false);
        });

    public static ISequence Home() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_Home, $"{Axis} Home",
        async (ctx, p, ct) =>
        {
            CheckMotionInterlock(ctx);
            // TODO: U축 Home 전용 인터락 조건 추가 (예: 로봇 간섭 확인 등)
            var core = MainCore.Instance;
            core.LogContext = new LogContext { SequenceKey = TcdSequenceKeys.Manual_Motor_U_Home, RunId = Guid.NewGuid(), AxisName = Axis };
            core.Log.Info(core.LogContext, "Start", "Home 시작");
            await core.Motion.HomeAsync(Axis, ct).ConfigureAwait(false);
            core.Log.Info(core.LogContext, "End", "Home 완료");
        });

    public static ISequence FaultReset() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_FaultReset, $"{Axis} FaultReset",
        async (ctx, p, ct) =>
        {
            await MainCore.Instance.Motion.FaultClearAsync(Axis, ct).ConfigureAwait(false);
        });

    public static ISequence ServoOn() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_ServoOn, $"{Axis} ServoOn",
        async (ctx, p, ct) =>
        {
            await MainCore.Instance.Motion.ServoOnAsync(Axis, ct).ConfigureAwait(false);
        });

    public static ISequence ServoOff() => new DelegateSequence(
        TcdSequenceKeys.Manual_Motor_U_ServoOff, $"{Axis} ServoOff",
        async (ctx, p, ct) =>
        {
            await MainCore.Instance.Motion.ServoOffAsync(Axis, ct).ConfigureAwait(false);
        });

    /// <summary>U축 공통 모션 인터락. 이동 계열 동작 전 호출.</summary>
    private static void CheckMotionInterlock(Tcd.Sequence.ISequenceContext ctx)
    {
        ctx.StopToken.ThrowIfCancellationRequested();
        // TODO: U축 specific 인터락 조건
        // 예) 로봇 간섭 영역 확인, 클램프 상태 확인 등
    }
}
