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

/// <summary>IO 수동 시퀀스. 챔버 진공/ESC는 인터락 포함, 나머지는 단순 On/Off.</summary>
public sealed class Manual_Io
{
    #region Variables
    private readonly MainCore _core;
    #endregion

    public Manual_Io(MainCore core) => _core = core;

    public void RegisterAll(SequenceManager mgr)
    {
        mgr.Register(new LowStageVacOnSequence(_core));
        mgr.Register(new LowStageVacOffSequence(_core));
        mgr.Register(new HighStageVacOnSequence(_core));
        mgr.Register(new HighStageVacOffSequence(_core));
        mgr.Register(new RobotGripVacOnSequence(_core));
        mgr.Register(new RobotGripVacOffSequence(_core));
        mgr.Register(new LowerChamberVacOnSequence(_core));
        mgr.Register(new LowerChamberVacOffSequence(_core));
        mgr.Register(new UpperChamberVacOnSequence(_core));
        mgr.Register(new UpperChamberVacOffSequence(_core));
        mgr.Register(new UpperEscEnableSequence(_core));
        mgr.Register(new UpperEscDisableSequence(_core));
        mgr.Register(new LowerEscEnableSequence(_core));
        mgr.Register(new LowerEscDisableSequence(_core));
        mgr.Register(new ChamberPumpVacOnSequence(_core));
        mgr.Register(new ChamberPumpVacOffSequence(_core));
        mgr.Register(new LowStageBlowOnSequence(_core));
        mgr.Register(new LowStageBlowOffSequence(_core));
        mgr.Register(new HighStageBlowOnSequence(_core));
        mgr.Register(new HighStageBlowOffSequence(_core));
        mgr.Register(new UpperChamberBlowOnSequence(_core));
        mgr.Register(new UpperChamberBlowOffSequence(_core));
        mgr.Register(new LowerChamberBlowOnSequence(_core));
        mgr.Register(new LowerChamberBlowOffSequence(_core));
        mgr.Register(new RobotGripBlowOnSequence(_core));
        mgr.Register(new RobotGripBlowOffSequence(_core));
    }

    private static async Task CheckChamberClosedAsync(IPlc plc, CancellationToken ct)
    {
        var lowerAtBond = await plc.ReadBitAsync(DiBit.LowerChamberAtBond, ct).ConfigureAwait(false);
        var upperAtBond = await plc.ReadBitAsync(DiBit.UpperChamberAtBond, ct).ConfigureAwait(false);
        if (!lowerAtBond || !upperAtBond)
            throw new InvalidOperationException(AlarmKeys.ChamberNotClosed);
    }

    // ── Stage Vac (인터락 없음) ───────────────────────────────────────────────

    private sealed class LowStageVacOnSequence : SequenceBase
    {
        public LowStageVacOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowStageVacOn;
        public override string DisplayName => "Lower Stage Vac On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowStageVacOn, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowStageVacOffSequence : SequenceBase
    {
        public LowStageVacOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowStageVacOff;
        public override string DisplayName => "Lower Stage Vac Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowStageVacOn, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class HighStageVacOnSequence : SequenceBase
    {
        public HighStageVacOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_HighStageVacOn;
        public override string DisplayName => "Upper Stage Vac On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.HighStageVacOn, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class HighStageVacOffSequence : SequenceBase
    {
        public HighStageVacOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_HighStageVacOff;
        public override string DisplayName => "Upper Stage Vac Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.HighStageVacOn, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class RobotGripVacOnSequence : SequenceBase
    {
        public RobotGripVacOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_RobotGripVacOn;
        public override string DisplayName => "Robot Grip Vac On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.RobotGripVacOn, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class RobotGripVacOffSequence : SequenceBase
    {
        public RobotGripVacOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_RobotGripVacOff;
        public override string DisplayName => "Robot Grip Vac Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.RobotGripVacOn, false, ct).ConfigureAwait(false);
        }
    }

    // ── Chamber Vac (자재 흡착용, 인터락 없음) ───────────────────────────────

    private sealed class LowerChamberVacOnSequence : SequenceBase
    {
        public LowerChamberVacOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowerChamberVacOn;
        public override string DisplayName => "Lower Chamber Vac On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowerChamberVacOn, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowerChamberVacOffSequence : SequenceBase
    {
        public LowerChamberVacOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowerChamberVacOff;
        public override string DisplayName => "Lower Chamber Vac Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowerChamberVacOn, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class UpperChamberVacOnSequence : SequenceBase
    {
        public UpperChamberVacOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_UpperChamberVacOn;
        public override string DisplayName => "Upper Chamber Vac On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.UpperChamberVacOn, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class UpperChamberVacOffSequence : SequenceBase
    {
        public UpperChamberVacOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_UpperChamberVacOff;
        public override string DisplayName => "Upper Chamber Vac Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.UpperChamberVacOn, false, ct).ConfigureAwait(false);
        }
    }

    // ── ESC (인터락: 해당 챔버 진공 On 확인) ─────────────────────────────────

    private sealed class UpperEscEnableSequence : SequenceBase
    {
        public UpperEscEnableSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_UpperEscEnable;
        public override string DisplayName => "Upper ESC Enable";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            var plc = _core.Simulation.Plc;
            var vacOn = await plc.ReadBitAsync(DiBit.UpperChamberVac, ct).ConfigureAwait(false);
            if (!vacOn)
                throw new InvalidOperationException(AlarmKeys.UpperChamberVacNotReady);
            await plc.WriteBitAsync(DoBit.UpperEscEnable, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class UpperEscDisableSequence : SequenceBase
    {
        public UpperEscDisableSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_UpperEscDisable;
        public override string DisplayName => "Upper ESC Disable";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            var plc = _core.Simulation.Plc;
            await plc.WriteBitAsync(DoBit.UpperEscEnable, false, ct).ConfigureAwait(false);
            await plc.WriteBitAsync(DoBit.UpperEscDisable, true, ct).ConfigureAwait(false);
            await Task.Delay(200, ct).ConfigureAwait(false);
            await plc.WriteBitAsync(DoBit.UpperEscDisable, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowerEscEnableSequence : SequenceBase
    {
        public LowerEscEnableSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowerEscEnable;
        public override string DisplayName => "Lower ESC Enable";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            var plc = _core.Simulation.Plc;
            var vacOn = await plc.ReadBitAsync(DiBit.LowerChamberVac, ct).ConfigureAwait(false);
            if (!vacOn)
                throw new InvalidOperationException(AlarmKeys.LowerChamberVacNotReady);
            await plc.WriteBitAsync(DoBit.LowerEscEnable, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowerEscDisableSequence : SequenceBase
    {
        public LowerEscDisableSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowerEscDisable;
        public override string DisplayName => "Lower ESC Disable";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            var plc = _core.Simulation.Plc;
            await plc.WriteBitAsync(DoBit.LowerEscEnable, false, ct).ConfigureAwait(false);
            await plc.WriteBitAsync(DoBit.LowerEscDisable, true, ct).ConfigureAwait(false);
            await Task.Delay(200, ct).ConfigureAwait(false);
            await plc.WriteBitAsync(DoBit.LowerEscDisable, false, ct).ConfigureAwait(false);
        }
    }

    // ── 챔버 진공 펌프 (인터락: 챔버 닫힘 — 열린 상태 작동 시 펌프 손상) ─────

    private sealed class ChamberPumpVacOnSequence : SequenceBase
    {
        public ChamberPumpVacOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_ChamberPumpVacOn;
        public override string DisplayName => "Chamber Pump Vac On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            var plc = _core.Simulation.Plc;
            await CheckChamberClosedAsync(plc, ct).ConfigureAwait(false);
            await plc.WriteBitAsync(DoBit.VacPumpRequest, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class ChamberPumpVacOffSequence : SequenceBase
    {
        public ChamberPumpVacOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_ChamberPumpVacOff;
        public override string DisplayName => "Chamber Pump Vac Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.VacPumpRequest, false, ct).ConfigureAwait(false);
        }
    }

    // ── Stage / Grip Blow (인터락 없음) ────────────────────────────────────────

    private sealed class LowStageBlowOnSequence : SequenceBase
    {
        public LowStageBlowOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowStageBlowOn;
        public override string DisplayName => "Lower Stage Blow On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowStageBlow, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowStageBlowOffSequence : SequenceBase
    {
        public LowStageBlowOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowStageBlowOff;
        public override string DisplayName => "Lower Stage Blow Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowStageBlow, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class HighStageBlowOnSequence : SequenceBase
    {
        public HighStageBlowOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_HighStageBlowOn;
        public override string DisplayName => "Upper Stage Blow On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.HighStageBlow, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class HighStageBlowOffSequence : SequenceBase
    {
        public HighStageBlowOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_HighStageBlowOff;
        public override string DisplayName => "Upper Stage Blow Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.HighStageBlow, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class UpperChamberBlowOnSequence : SequenceBase
    {
        public UpperChamberBlowOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_UpperChamberBlowOn;
        public override string DisplayName => "Upper Chamber Blow On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.UpperChamberBlow, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class UpperChamberBlowOffSequence : SequenceBase
    {
        public UpperChamberBlowOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_UpperChamberBlowOff;
        public override string DisplayName => "Upper Chamber Blow Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.UpperChamberBlow, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowerChamberBlowOnSequence : SequenceBase
    {
        public LowerChamberBlowOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowerChamberBlowOn;
        public override string DisplayName => "Lower Chamber Blow On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowerChamberBlow, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class LowerChamberBlowOffSequence : SequenceBase
    {
        public LowerChamberBlowOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_LowerChamberBlowOff;
        public override string DisplayName => "Lower Chamber Blow Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.LowerChamberBlow, false, ct).ConfigureAwait(false);
        }
    }

    private sealed class RobotGripBlowOnSequence : SequenceBase
    {
        public RobotGripBlowOnSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_RobotGripBlowOn;
        public override string DisplayName => "Robot Grip Blow On";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.RobotGripBlow, true, ct).ConfigureAwait(false);
        }
    }

    private sealed class RobotGripBlowOffSequence : SequenceBase
    {
        public RobotGripBlowOffSequence(MainCore core) : base(core) { }
        public override string Key         => TcdSequenceKeys.Manual_Io_RobotGripBlowOff;
        public override string DisplayName => "Robot Grip Blow Off";

        protected override async Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken ct)
        {
            await _core.Simulation.Plc.WriteBitAsync(DoBit.RobotGripBlow, false, ct).ConfigureAwait(false);
        }
    }
}
