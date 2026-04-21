using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;

namespace Tcd.Simulator
{
    public static class TcdSequenceRegistry
    {
        public static SequenceManager Build(TcdSimulation sim, IMotionService motion)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            if (motion == null) throw new ArgumentNullException(nameof(motion));

            var mgr = new SequenceManager();

            RegisterManual(sim, mgr, motion);
            return mgr;
        }

        private static void RegisterManual(TcdSimulation sim, SequenceManager mgr, IMotionService motion)
        {
            // PLC wait (both stage1+stage2 must be loaded)
            mgr.Register(new DelegateSequence(
                TcdSequenceKeys.Plc_Wait_StageLoaded,
                "Wait stage1+stage2 loaded",
                async (ctx, param, ct) =>
                {
                    var timeout = param is TimeSpan ts ? ts : TimeSpan.FromSeconds(5);
                    var ok = await sim.Plc.WaitForStageLoadedAsync(timeout, ct).ConfigureAwait(false);
                    if (!ok) throw new TimeoutException($"Stage load timeout ({timeout}).");
                }));

            // Robot moves
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Move_Stage, "Robot CMD move stage",
                (ctx, p, ct) => sim.Robot.CommandMoveToAsync(RobotPosition.Stage, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Move_Home, "Robot CMD move home",
                (ctx, p, ct) => sim.Robot.CommandMoveToAsync(RobotPosition.Home, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Wait_Home, "Robot WAIT home",
                (ctx, p, ct) => sim.Robot.WaitForPositionAsync(RobotPosition.Home, Timeout(p, 2), ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Move_UpperLoad, "Robot CMD move upper load",
                (ctx, p, ct) => sim.Robot.CommandMoveToAsync(RobotPosition.UpperChamberLoad, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Move_LowerLoad, "Robot CMD move lower load",
                (ctx, p, ct) => sim.Robot.CommandMoveToAsync(RobotPosition.LowerChamberLoad, ct)));

            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Wait_Stage, "Robot WAIT stage",
                (ctx, p, ct) => sim.Robot.WaitForPositionAsync(RobotPosition.Stage, Timeout(p, 2), ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Wait_UpperLoad, "Robot WAIT upper load",
                (ctx, p, ct) => sim.Robot.WaitForPositionAsync(RobotPosition.UpperChamberLoad, Timeout(p, 2), ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Wait_LowerLoad, "Robot WAIT lower load",
                (ctx, p, ct) => sim.Robot.WaitForPositionAsync(RobotPosition.LowerChamberLoad, Timeout(p, 2), ct)));

            // Robot pick/place
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Pick_Stage1, "Robot pick stage1",
                (ctx, p, ct) => sim.Robot.PickAsync(MaterialLocation.Stage1, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Pick_Stage2, "Robot pick stage2",
                (ctx, p, ct) => sim.Robot.PickAsync(MaterialLocation.Stage2, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Place_UpperChamber, "Robot place upper chamber",
                (ctx, p, ct) => sim.Robot.PlaceAsync(MaterialLocation.UpperChamber, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Place_LowerChamber, "Robot place lower chamber",
                (ctx, p, ct) => sim.Robot.PlaceAsync(MaterialLocation.LowerChamber, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Pick_LowerChamber, "Robot pick lower chamber",
                (ctx, p, ct) => sim.Robot.PickAsync(MaterialLocation.LowerChamber, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.Robot_Place_Stage2, "Robot place stage2",
                (ctx, p, ct) => sim.Robot.PlaceAsync(MaterialLocation.Stage2, ct)));

            // UVW align (command & wait)
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisU_Command_Zero, "U CMD move 0",
                (ctx, p, ct) => sim.LowerMotion.U.CommandMoveToAsync(0, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisV_Command_Zero, "V CMD move 0",
                (ctx, p, ct) => sim.LowerMotion.V.CommandMoveToAsync(0, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisW_Command_Zero, "W CMD move 0",
                (ctx, p, ct) => sim.LowerMotion.W.CommandMoveToAsync(0, ct)));

            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisU_Wait_Zero, "U WAIT 0",
                (ctx, p, ct) => sim.LowerMotion.U.WaitForInPositionAsync(0, 0.01, Timeout(p, 2), ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisV_Wait_Zero, "V WAIT 0",
                (ctx, p, ct) => sim.LowerMotion.V.WaitForInPositionAsync(0, 0.01, Timeout(p, 2), ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisW_Wait_Zero, "W WAIT 0",
                (ctx, p, ct) => sim.LowerMotion.W.WaitForInPositionAsync(0, 0.01, Timeout(p, 2), ct)));

            // Z up/down (bond/load)
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisZ_Command_Bond, "Z CMD move bond(100)",
                (ctx, p, ct) => sim.LowerMotion.Z.CommandMoveToAsync(100, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisZ_Wait_Bond, "Z WAIT bond(100)",
                (ctx, p, ct) => sim.LowerMotion.Z.WaitForInPositionAsync(100, 0.01, Timeout(p, 3), ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisZ_Command_Load, "Z CMD move load(0)",
                (ctx, p, ct) => sim.LowerMotion.Z.CommandMoveToAsync(0, ct)));
            mgr.Register(new DelegateSequence(TcdSequenceKeys.AxisZ_Wait_Load, "Z WAIT load(0)",
                (ctx, p, ct) => sim.LowerMotion.Z.WaitForInPositionAsync(0, 0.01, Timeout(p, 3), ct)));

            mgr.Register(new DelegateSequence(TcdSequenceKeys.Delay_Bond_Dwell1s, "Bond dwell 1s",
                (ctx, p, ct) => sim.Time.Delay(TimeSpan.FromSeconds(1), ct)));

            // 0번 모터(U) 매뉴얼 시퀀스는 Tcd.App.Sequences.Manual 클래스로 등록됨 (MainCore에서 Register)

            mgr.Register(new DelegateSequence(
                TcdSequenceKeys.Material_Create_Bonded,
                "Create bonded product (consume films)",
                (ctx, p, ct) =>
                {
                    var upper = sim.Materials.Remove(MaterialLocation.UpperChamber);
                    var lower = sim.Materials.Remove(MaterialLocation.LowerChamber);
                    if (upper == null || lower == null) throw new InvalidOperationException("Missing material in chambers.");

                    var product = new Material(Guid.NewGuid(), MaterialKind.BondedProduct, MaterialState.Completed, MaterialLocation.LowerChamber);
                    sim.Materials.Place(product, MaterialLocation.LowerChamber);
                    return Task.CompletedTask;
                }));
        }

        private static TimeSpan Timeout(object parameter, int secondsDefault)
        {
            return parameter is TimeSpan ts ? ts : TimeSpan.FromSeconds(secondsDefault);
        }
    }
}

