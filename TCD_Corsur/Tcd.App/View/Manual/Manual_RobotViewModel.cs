using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tcd.App.Core;
using Tcd.App.Mvvm;
using Tcd.Devices;

namespace Tcd.App;

public sealed class Manual_RobotViewModel : NotifyPropertyChangedBase
{
    private readonly MainCore _core = MainCore.Instance;

    private string _status = "";
    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public Manual_RobotViewModel()
    {
    }

    // Robot moves (discrete)
    private BiRelayCommand? cmd_RobotMoveStage;
    public ICommand Cmd_RobotMoveStage => cmd_RobotMoveStage ??= new BiRelayCommand(_ => RobotMove(RobotPosition.Stage));

    private BiRelayCommand? cmd_RobotMoveUpper;
    public ICommand Cmd_RobotMoveUpper => cmd_RobotMoveUpper ??= new BiRelayCommand(_ => RobotMove(RobotPosition.UpperChamberLoad));

    private BiRelayCommand? cmd_RobotMoveLower;
    public ICommand Cmd_RobotMoveLower => cmd_RobotMoveLower ??= new BiRelayCommand(_ => RobotMove(RobotPosition.LowerChamberLoad));

    private void RobotMove(RobotPosition pos)
    {
        _ = RunAsync(async ct =>
        {
            await _core.Simulation.Robot.CommandMoveToAsync(pos, ct).ConfigureAwait(false);
            await _core.Simulation.Robot.WaitForPositionAsync(pos, _core.Settings.RobotMoveTimeout, ct).ConfigureAwait(false);
            Status = $"Robot at {pos}";
        });
    }

    private Task RunAsync(Func<CancellationToken, Task> body)
    {
        return Task.Run(async () =>
        {
            try
            {
                await body(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        });
    }
}

