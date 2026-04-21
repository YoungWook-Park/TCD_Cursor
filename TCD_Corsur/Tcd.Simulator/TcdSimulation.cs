using System;
using System.Threading;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;
using Tcd.Sequence;

namespace Tcd.Simulator
{
    public sealed class TcdSimulation : ISequenceContext
    {
        public TcdSimulation()
        {
            Alarms = new AlarmManager();
            Time = new SystemTimeProvider();
            Materials = new InMemoryMaterialTracker();

            Robot = new SimRobot(Time, Materials);
            LowerMotion = new SimLowerChamberMotion(Time);
            Plc = new SimPlc(Time, Materials);
        }

        public IAlarmSink Alarms { get; }
        public ITimeProvider Time { get; }
        public CancellationToken StopToken { get; private set; }

        public IMaterialTracker Materials { get; }
        public IRobot Robot { get; }
        public SimLowerChamberMotion LowerMotion { get; }
        public IPlc Plc { get; }

        public void BindStopToken(CancellationToken stopToken)
        {
            StopToken = stopToken;
        }

        public void LoadStage(MaterialKind stage1Kind = MaterialKind.UpperFilm, MaterialKind stage2Kind = MaterialKind.LowerFilm)
        {
            Materials.Clear();
            Materials.Place(new Material(Guid.NewGuid(), stage1Kind, MaterialState.Loaded, MaterialLocation.Stage1), MaterialLocation.Stage1);
            Materials.Place(new Material(Guid.NewGuid(), stage2Kind, MaterialState.Loaded, MaterialLocation.Stage2), MaterialLocation.Stage2);
        }

        public void Reset()
        {
            Materials.Clear();
        }
    }
}
