using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.App.Define;
using Tcd.App.Sequences;
using Tcd.Devices;
using Tcd.Sequence;
using Tcd.Simulator;

namespace Tcd.App.Sequences.Manual;

/// <summary>
/// 로봇 수동 이동 시퀀스. 챔버 진입 포지션은 Z Ready 인터락 포함.
/// 공통 플로우(SetVelocity → Move → WaitForPosition)는 ExecuteMoveAsync로 재사용.
/// </summary>
public sealed class Manual_Robot
{
    #region Variables
    private readonly MainCore _core;
    #endregion

    public Manual_Robot(MainCore core) => _core = core;

    public void RegisterAll(SequenceManager mgr)
    {
        mgr.Register(new MoveHomeSequence(_core));
        mgr.Register(new MoveUpperStageWaitSequence(_core));
        mgr.Register(new MoveUpperStageContactSequence(_core));
        mgr.Register(new MoveLowerStageWaitSequence(_core));
        mgr.Register(new MoveLowerStageContactSequence(_core));
        mgr.Register(new MoveUpperChamberWaitSequence(_core));
        mgr.Register(new MoveUpperChamberContactSequence(_core));
        mgr.Register(new MoveLowerChamberWaitSequence(_core));
        mgr.Register(new MoveLowerChamberContactSequence(_core));
    }

    private static async Task ExecuteMoveAsync(MainCore core, RobotPosition position, object? param, CancellationToken ct)
    {
        var robot    = core.RobotDevice;
        var timeout  = core.Settings.RobotMoveTimeout;
        var velocity = param is int v ? v : RobotVelocityDefault.ForPosition(position);
        await robot.SetVelocityAsync(position, velocity, ct).ConfigureAwait(false);
        var ok = await robot.MoveAsync(position, ct).ConfigureAwait(false);
        if (!ok)
            throw new InvalidOperationException($"Robot rejected move to {RobotPositionName.FromPosition(position)}");
        await robot.WaitForPositionAsync(position, timeout, ct).ConfigureAwait(false);
    }

    private static async Task CheckZReadyAsync(MainCore core, CancellationToken ct)
    {
        var plc        = core.Simulation.Plc;
        var lowerReady = await plc.ReadBitAsync(DiBit.LowerChamberAtReady, ct).ConfigureAwait(false);
        var upperReady = await plc.ReadBitAsync(DiBit.UpperChamberAtReady, ct).ConfigureAwait(false);
        if (!lowerReady || !upperReady)
            throw new InvalidOperationException(AlarmKeys.ZNotAtReadyForRobotEntry);
    }

    // ── 스테이지 / Home (인터락 없음) ─────────────────────────────────────────

    private sealed class MoveHomeSequence : SequenceBase
    {
        public MoveHomeSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_Home;
        public override string DisplayName => "Robot Move Home";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await ExecuteMoveAsync(_core, RobotPosition.Home, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveUpperStageWaitSequence : SequenceBase
    {
        public MoveUpperStageWaitSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_UpperStageWait;
        public override string DisplayName => "Robot Move Upper Stage Wait";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await ExecuteMoveAsync(_core, RobotPosition.UpperStageWait, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveUpperStageContactSequence : SequenceBase
    {
        public MoveUpperStageContactSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_UpperStageContact;
        public override string DisplayName => "Robot Move Upper Stage Contact";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await ExecuteMoveAsync(_core, RobotPosition.UpperStageContact, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveLowerStageWaitSequence : SequenceBase
    {
        public MoveLowerStageWaitSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_LowerStageWait;
        public override string DisplayName => "Robot Move Lower Stage Wait";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await ExecuteMoveAsync(_core, RobotPosition.LowerStageWait, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveLowerStageContactSequence : SequenceBase
    {
        public MoveLowerStageContactSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_LowerStageContact;
        public override string DisplayName => "Robot Move Lower Stage Contact";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await ExecuteMoveAsync(_core, RobotPosition.LowerStageContact, parameter, ct).ConfigureAwait(false);
        }
    }

    // ── 챔버 진입 (인터락: LowerChamberAtReady && UpperChamberAtReady) ─────────

    private sealed class MoveUpperChamberWaitSequence : SequenceBase
    {
        public MoveUpperChamberWaitSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_UpperChamberWait;
        public override string DisplayName => "Robot Move Upper Chamber Wait";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            await CheckZReadyAsync(_core, ct).ConfigureAwait(false);
            await ExecuteMoveAsync(_core, RobotPosition.UpperChamberWait, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveUpperChamberContactSequence : SequenceBase
    {
        public MoveUpperChamberContactSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_UpperChamberContact;
        public override string DisplayName => "Robot Move Upper Chamber Contact";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            await CheckZReadyAsync(_core, ct).ConfigureAwait(false);
            await ExecuteMoveAsync(_core, RobotPosition.UpperChamberContact, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveLowerChamberWaitSequence : SequenceBase
    {
        public MoveLowerChamberWaitSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_LowerChamberWait;
        public override string DisplayName => "Robot Move Lower Chamber Wait";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            await CheckZReadyAsync(_core, ct).ConfigureAwait(false);
            await ExecuteMoveAsync(_core, RobotPosition.LowerChamberWait, parameter, ct).ConfigureAwait(false);
        }
    }

    private sealed class MoveLowerChamberContactSequence : SequenceBase
    {
        public MoveLowerChamberContactSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Robot_LowerChamberContact;
        public override string DisplayName => "Robot Move Lower Chamber Contact";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            CheckInterlock(context);
            await CheckZReadyAsync(_core, ct).ConfigureAwait(false);
            await ExecuteMoveAsync(_core, RobotPosition.LowerChamberContact, parameter, ct).ConfigureAwait(false);
        }
    }
}
