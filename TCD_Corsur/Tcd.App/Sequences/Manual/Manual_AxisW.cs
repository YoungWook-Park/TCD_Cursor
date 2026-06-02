using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.App.Sequences;
using Tcd.Core.Logging;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual;

public sealed class Manual_AxisW
{
    #region Variables
    private readonly MainCore _core;
    private const string Axis = AxisDefine.W;
    #endregion

    public Manual_AxisW(MainCore core) => _core = core;

    public void RegisterAll(SequenceManager mgr)
    {
        mgr.Register(new AbsMoveSequence(_core));
        mgr.Register(new IncMoveSequence(_core));
        mgr.Register(new JogMoveSequence(_core));
        mgr.Register(new StopSequence(_core));
        mgr.Register(new HomeSequence(_core));
        mgr.Register(new FaultResetSequence(_core));
        mgr.Register(new ServoOnSequence(_core));
        mgr.Register(new ServoOffSequence(_core));
    }

    // ── AbsMove ────────────────────────────────────────────────────────────

    private sealed class AbsMoveSequence : SequenceBase
    {
        public AbsMoveSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_AbsMove;
        public override string DisplayName => $"{Axis} AbsMove";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            _core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid(), AxisName = Axis };
            double target = ResolveAbsTarget(parameter);
            _core.Log.Info(_core.LogContext, "Start", $"AbsMove target={target}");
            await _core.Motion.AbsMoveAsync(Axis, target, ct).ConfigureAwait(false);
            _core.Log.Info(_core.LogContext, "End", "AbsMove 완료");
        }

        protected override Task PostActionAsync(ISequenceContext context, CancellationToken ct)
        {
            var state = _core.AxisStateProvider.GetAxisState(Axis);
            if (state.IsMoving)
                throw new InvalidOperationException($"{Axis} AbsMove 미완료: 아직 이동 중");
            if (state.IsFault)
                throw new InvalidOperationException($"{Axis} AbsMove 후 폴트 감지");
            return Task.CompletedTask;
        }

        private double ResolveAbsTarget(object parameter)
        {
            if (parameter is double d) return d;
            if (parameter is IConvertible c) return c.ToDouble(null);
            return _core.Recipes.Current?.GetAxis(Axis, 0) ?? 0;
        }
    }

    // ── IncMove ────────────────────────────────────────────────────────────

    private sealed class IncMoveSequence : SequenceBase
    {
        public IncMoveSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_IncMove;
        public override string DisplayName => $"{Axis} IncMove";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            _core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid(), AxisName = Axis };
            double delta = ResolveDelta(parameter);
            _core.Log.Info(_core.LogContext, "Start", $"IncMove delta={delta}");
            await _core.Motion.IncMoveAsync(Axis, delta, ct).ConfigureAwait(false);
            _core.Log.Info(_core.LogContext, "End", "IncMove 완료");
        }

        protected override Task PostActionAsync(ISequenceContext context, CancellationToken ct)
        {
            var state = _core.AxisStateProvider.GetAxisState(Axis);
            if (state.IsMoving)
                throw new InvalidOperationException($"{Axis} IncMove 미완료: 아직 이동 중");
            if (state.IsFault)
                throw new InvalidOperationException($"{Axis} IncMove 후 폴트 감지");
            return Task.CompletedTask;
        }

        private static double ResolveDelta(object parameter)
        {
            if (parameter is double d) return d;
            if (parameter is IConvertible c) return c.ToDouble(null);
            return 0;
        }
    }

    // ── JogMove ────────────────────────────────────────────────────────────

    private sealed class JogMoveSequence : SequenceBase
    {
        public JogMoveSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_JogMove;
        public override string DisplayName => $"{Axis} JogMove";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            _core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid(), AxisName = Axis };
            double velocity = ResolveVelocity(parameter);
            _core.Log.Info(_core.LogContext, "Start", $"JogMove velocity={velocity}");
            await _core.Motion.JogAsync(Axis, velocity, ct).ConfigureAwait(false);
            _core.Log.Info(_core.LogContext, "End", "JogMove 종료");
        }

        private static double ResolveVelocity(object parameter)
        {
            if (parameter is double d) return d;
            if (parameter is IConvertible c) return c.ToDouble(null);
            return 0;
        }
    }

    // ── Stop ───────────────────────────────────────────────────────────────

    private sealed class StopSequence : SequenceBase
    {
        public StopSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_Stop;
        public override string DisplayName => $"{Axis} Stop";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Motion.StopAsync(Axis, ct).ConfigureAwait(false);
        }
    }

    // ── Home ───────────────────────────────────────────────────────────────

    private sealed class HomeSequence : SequenceBase
    {
        public HomeSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_Home;
        public override string DisplayName => $"{Axis} Home";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            // TODO: W축 specific 인터락 조건
            _core.LogContext = new LogContext { SequenceKey = Key, RunId = Guid.NewGuid(), AxisName = Axis };
            _core.Log.Info(_core.LogContext, "Start", "Home 시작");
            await _core.Motion.HomeAsync(Axis, ct).ConfigureAwait(false);
            _core.Log.Info(_core.LogContext, "End", "Home 완료");
        }

        protected override Task PostActionAsync(ISequenceContext context, CancellationToken ct)
        {
            var state = _core.AxisStateProvider.GetAxisState(Axis);
            if (!state.IsHome)
                throw new InvalidOperationException($"{Axis} Home 미완료: IsHome=false");
            return Task.CompletedTask;
        }
    }

    // ── FaultReset ─────────────────────────────────────────────────────────

    private sealed class FaultResetSequence : SequenceBase
    {
        public FaultResetSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_FaultReset;
        public override string DisplayName => $"{Axis} FaultReset";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Motion.FaultClearAsync(Axis, ct).ConfigureAwait(false);
        }

        protected override Task PostActionAsync(ISequenceContext context, CancellationToken ct)
        {
            var state = _core.AxisStateProvider.GetAxisState(Axis);
            if (state.IsFault)
                throw new InvalidOperationException($"{Axis} FaultReset 미완료: 폴트 미해제");
            return Task.CompletedTask;
        }
    }

    // ── ServoOn ────────────────────────────────────────────────────────────

    private sealed class ServoOnSequence : SequenceBase
    {
        public ServoOnSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_ServoOn;
        public override string DisplayName => $"{Axis} ServoOn";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Motion.ServoOnAsync(Axis, ct).ConfigureAwait(false);
        }

        protected override Task PostActionAsync(ISequenceContext context, CancellationToken ct)
        {
            var state = _core.AxisStateProvider.GetAxisState(Axis);
            if (!state.IsServoOn)
                throw new InvalidOperationException($"{Axis} ServoOn 미완료: IsServoOn=false");
            return Task.CompletedTask;
        }
    }

    // ── ServoOff ───────────────────────────────────────────────────────────

    private sealed class ServoOffSequence : SequenceBase
    {
        public ServoOffSequence(MainCore core) : base(core) { }

        public override string Key         => TcdSequenceKeys.Manual_Motor_W_ServoOff;
        public override string DisplayName => $"{Axis} ServoOff";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Motion.ServoOffAsync(Axis, ct).ConfigureAwait(false);
        }

        protected override Task PostActionAsync(ISequenceContext context, CancellationToken ct)
        {
            var state = _core.AxisStateProvider.GetAxisState(Axis);
            if (state.IsServoOn)
                throw new InvalidOperationException($"{Axis} ServoOff 미완료: IsServoOn=true");
            return Task.CompletedTask;
        }
    }
}
