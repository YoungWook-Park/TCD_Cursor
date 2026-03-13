using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Materials;
using Tcd.Sequence;

namespace Tcd.Simulator
{
    public static class TcdAutoSequenceFactory
    {
        public static SequenceGraph Create(TcdSimulation sim, TimeSpan stageLoadTimeout)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));

            var g = new SequenceGraph(startNodeId: "Start");

            g.AddNode(new ActionNode(
                id: "Start",
                displayName: "Start",
                action: (ctx, ct) => Task.CompletedTask
            ));

            g.AddNode(new ActionNode(
                id: "WaitStageLoaded",
                displayName: "Wait stage1+stage2 loaded",
                action: async (ctx, ct) =>
                {
                    var ok = await sim.Plc.WaitForStageLoadedAsync(stageLoadTimeout, ct).ConfigureAwait(false);
                    if (!ok)
                    {
                        ctx.Alarms.Raise(new Alarm("STAGE_LOAD_TIMEOUT", $"Stage load timeout ({stageLoadTimeout}).", AlarmSeverity.Error, ctx.Time.Now));
                        throw new TimeoutException("Stage load timeout.");
                    }
                }
            ));

            g.AddNode(new DecisionNode(
                id: "UpperChamberEmpty",
                displayName: "Check upper chamber empty",
                predicate: (ctx, ct) => Task.FromResult(sim.Materials.Get(MaterialLocation.UpperChamber) == null)
            ));

            g.AddNode(new ActionNode(
                id: "RobotToStageForUpper",
                displayName: "Robot move to stage (upper film)",
                action: (ctx, ct) => sim.Robot.CommandMoveToAsync(Tcd.Devices.RobotPosition.Stage, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitRobotAtStageForUpper",
                displayName: "Wait robot at stage",
                action: (ctx, ct) => sim.Robot.WaitForPositionAsync(Tcd.Devices.RobotPosition.Stage, TimeSpan.FromSeconds(2), ct),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "PickUpperFromStage1",
                displayName: "Pick upper film from stage1",
                action: (ctx, ct) => sim.Robot.PickAsync(MaterialLocation.Stage1, ct)
            ));

            g.AddNode(new ActionNode(
                id: "RobotToUpperChamberLoad",
                displayName: "Robot move to upper chamber load",
                action: (ctx, ct) => sim.Robot.CommandMoveToAsync(Tcd.Devices.RobotPosition.UpperChamberLoad, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitRobotAtUpperChamberLoad",
                displayName: "Wait robot at upper chamber load",
                action: (ctx, ct) => sim.Robot.WaitForPositionAsync(Tcd.Devices.RobotPosition.UpperChamberLoad, TimeSpan.FromSeconds(2), ct),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "PlaceUpperToUpperChamber",
                displayName: "Place upper film to upper chamber",
                action: (ctx, ct) => sim.Robot.PlaceAsync(MaterialLocation.UpperChamber, ct)
            ));

            g.AddNode(new DecisionNode(
                id: "LowerChamberEmpty",
                displayName: "Check lower chamber empty",
                predicate: (ctx, ct) => Task.FromResult(sim.Materials.Get(MaterialLocation.LowerChamber) == null)
            ));

            g.AddNode(new ActionNode(
                id: "RobotToStageForLower",
                displayName: "Robot move to stage (lower film)",
                action: (ctx, ct) => sim.Robot.CommandMoveToAsync(Tcd.Devices.RobotPosition.Stage, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitRobotAtStageForLower",
                displayName: "Wait robot at stage",
                action: (ctx, ct) => sim.Robot.WaitForPositionAsync(Tcd.Devices.RobotPosition.Stage, TimeSpan.FromSeconds(2), ct),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "PickLowerFromStage2",
                displayName: "Pick lower film from stage2",
                action: (ctx, ct) => sim.Robot.PickAsync(MaterialLocation.Stage2, ct)
            ));

            g.AddNode(new ActionNode(
                id: "RobotToLowerChamberLoad",
                displayName: "Robot move to lower chamber load",
                action: (ctx, ct) => sim.Robot.CommandMoveToAsync(Tcd.Devices.RobotPosition.LowerChamberLoad, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitRobotAtLowerChamberLoadForLower",
                displayName: "Wait robot at lower chamber load",
                action: (ctx, ct) => sim.Robot.WaitForPositionAsync(Tcd.Devices.RobotPosition.LowerChamberLoad, TimeSpan.FromSeconds(2), ct),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "PlaceLowerToLowerChamber",
                displayName: "Place lower film to lower chamber",
                action: (ctx, ct) => sim.Robot.PlaceAsync(MaterialLocation.LowerChamber, ct)
            ));

            // Fork: UVW alignment axes run concurrently (simulation: move to 0)
            g.AddNode(new ForkNode(
                id: "ForkAlignUVW",
                displayName: "Align UVW (fork)",
                branchStartNodeIds: new[] { "AlignU_Cmd", "AlignV_Cmd", "AlignW_Cmd" },
                joinNextNodeId: "JoinAlignUVW"
            ));

            g.AddNode(new ActionNode("AlignU_Cmd", "Command U axis align", (ctx, ct) => sim.LowerMotion.U.CommandMoveToAsync(0, ct)));
            g.AddNode(new ActionNode("AlignV_Cmd", "Command V axis align", (ctx, ct) => sim.LowerMotion.V.CommandMoveToAsync(0, ct)));
            g.AddNode(new ActionNode("AlignW_Cmd", "Command W axis align", (ctx, ct) => sim.LowerMotion.W.CommandMoveToAsync(0, ct)));

            g.AddNode(new ActionNode(
                id: "JoinAlignUVW",
                displayName: "Join: wait UVW in-position",
                action: (ctx, ct) => Task.WhenAll(
                    sim.LowerMotion.U.WaitForInPositionAsync(0, 0.01, TimeSpan.FromSeconds(2), ct),
                    sim.LowerMotion.V.WaitForInPositionAsync(0, 0.01, TimeSpan.FromSeconds(2), ct),
                    sim.LowerMotion.W.WaitForInPositionAsync(0, 0.01, TimeSpan.FromSeconds(2), ct)
                ),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "LowerZUpToBond",
                displayName: "Lower chamber Z up (bond)",
                action: (ctx, ct) => sim.LowerMotion.Z.CommandMoveToAsync(100, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitLowerZAtBond",
                displayName: "Wait lower Z at bond",
                action: (ctx, ct) => sim.LowerMotion.Z.WaitForInPositionAsync(100, 0.01, TimeSpan.FromSeconds(3), ct),
                timeout: TimeSpan.FromSeconds(4)
            ));

            g.AddNode(new ActionNode(
                id: "BondDwell",
                displayName: "Bonding dwell 1s",
                action: (ctx, ct) => sim.Time.Delay(TimeSpan.FromSeconds(1), ct)
            ));

            g.AddNode(new ActionNode(
                id: "LowerZDownToLoad",
                displayName: "Lower chamber Z down (load)",
                action: (ctx, ct) => sim.LowerMotion.Z.CommandMoveToAsync(0, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitLowerZAtLoad",
                displayName: "Wait lower Z at load",
                action: (ctx, ct) => sim.LowerMotion.Z.WaitForInPositionAsync(0, 0.01, TimeSpan.FromSeconds(3), ct),
                timeout: TimeSpan.FromSeconds(4)
            ));

            g.AddNode(new ActionNode(
                id: "CreateBondedProduct",
                displayName: "Create bonded product in lower chamber",
                action: (ctx, ct) =>
                {
                    var upper = sim.Materials.Remove(MaterialLocation.UpperChamber);
                    var lower = sim.Materials.Remove(MaterialLocation.LowerChamber);
                    if (upper == null || lower == null) throw new InvalidOperationException("Missing material in chambers.");

                    var product = new Material(Guid.NewGuid(), MaterialKind.BondedProduct, MaterialState.Completed, MaterialLocation.LowerChamber);
                    sim.Materials.Place(product, MaterialLocation.LowerChamber);
                    return Task.CompletedTask;
                }
            ));

            g.AddNode(new ActionNode(
                id: "RobotToLowerChamberForProduct",
                displayName: "Robot move to lower chamber load (product)",
                action: (ctx, ct) => sim.Robot.CommandMoveToAsync(Tcd.Devices.RobotPosition.LowerChamberLoad, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitRobotAtLowerChamberLoadForProduct",
                displayName: "Wait robot at lower chamber load",
                action: (ctx, ct) => sim.Robot.WaitForPositionAsync(Tcd.Devices.RobotPosition.LowerChamberLoad, TimeSpan.FromSeconds(2), ct),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "PickProductFromLowerChamber",
                displayName: "Pick product from lower chamber",
                action: (ctx, ct) => sim.Robot.PickAsync(MaterialLocation.LowerChamber, ct)
            ));

            g.AddNode(new ActionNode(
                id: "RobotToStageForOutput",
                displayName: "Robot move to stage (output)",
                action: (ctx, ct) => sim.Robot.CommandMoveToAsync(Tcd.Devices.RobotPosition.Stage, ct)
            ));

            g.AddNode(new ActionNode(
                id: "WaitRobotAtStageForOutput",
                displayName: "Wait robot at stage",
                action: (ctx, ct) => sim.Robot.WaitForPositionAsync(Tcd.Devices.RobotPosition.Stage, TimeSpan.FromSeconds(2), ct),
                timeout: TimeSpan.FromSeconds(3)
            ));

            g.AddNode(new ActionNode(
                id: "PlaceProductToStage2",
                displayName: "Place product to stage2",
                action: (ctx, ct) => sim.Robot.PlaceAsync(MaterialLocation.Stage2, ct)
            ));

            g.SetNext("Start", "WaitStageLoaded");
            g.SetNext("WaitStageLoaded", "UpperChamberEmpty");
            g.SetNext("UpperChamberEmpty", "RobotToStageForUpper");
            g.SetNext("RobotToStageForUpper", "WaitRobotAtStageForUpper");
            g.SetNext("WaitRobotAtStageForUpper", "PickUpperFromStage1");
            g.SetNext("PickUpperFromStage1", "RobotToUpperChamberLoad");
            g.SetNext("RobotToUpperChamberLoad", "WaitRobotAtUpperChamberLoad");
            g.SetNext("WaitRobotAtUpperChamberLoad", "PlaceUpperToUpperChamber");
            g.SetNext("PlaceUpperToUpperChamber", "LowerChamberEmpty");
            g.SetNext("LowerChamberEmpty", "RobotToStageForLower");
            g.SetNext("RobotToStageForLower", "WaitRobotAtStageForLower");
            g.SetNext("WaitRobotAtStageForLower", "PickLowerFromStage2");
            g.SetNext("PickLowerFromStage2", "RobotToLowerChamberLoad");
            g.SetNext("RobotToLowerChamberLoad", "WaitRobotAtLowerChamberLoadForLower");
            g.SetNext("WaitRobotAtLowerChamberLoadForLower", "PlaceLowerToLowerChamber");
            g.SetNext("PlaceLowerToLowerChamber", "ForkAlignUVW");

            g.SetNext("JoinAlignUVW", "LowerZUpToBond");
            g.SetNext("LowerZUpToBond", "WaitLowerZAtBond");
            g.SetNext("WaitLowerZAtBond", "BondDwell");
            g.SetNext("BondDwell", "LowerZDownToLoad");
            g.SetNext("LowerZDownToLoad", "WaitLowerZAtLoad");
            g.SetNext("WaitLowerZAtLoad", "CreateBondedProduct");
            g.SetNext("CreateBondedProduct", "RobotToLowerChamberForProduct");
            g.SetNext("RobotToLowerChamberForProduct", "WaitRobotAtLowerChamberLoadForProduct");
            g.SetNext("WaitRobotAtLowerChamberLoadForProduct", "PickProductFromLowerChamber");
            g.SetNext("PickProductFromLowerChamber", "RobotToStageForOutput");
            g.SetNext("RobotToStageForOutput", "WaitRobotAtStageForOutput");
            g.SetNext("WaitRobotAtStageForOutput", "PlaceProductToStage2");
            g.SetNext("PlaceProductToStage2", null);

            return g;
        }
    }
}

