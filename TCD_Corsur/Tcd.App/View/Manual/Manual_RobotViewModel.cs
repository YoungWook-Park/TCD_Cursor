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
    #region Variable

    private readonly MainCore _core = MainCore.Instance;
    private string _status = "";

    #endregion

    #region Constructor

    public Manual_RobotViewModel()
    {
    }

    #endregion

    #region Property

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    #endregion

    #region Function

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
            try { await body(CancellationToken.None).ConfigureAwait(false); }
            catch (Exception ex) { Status = ex.Message; }
        });
    }

    #endregion

    #region UI Function

    private RelayCommand? cmd_RobotMoveStage;
    public ICommand Cmd_RobotMoveStage => cmd_RobotMoveStage ??= new RelayCommand(_ => RobotMove(RobotPosition.Stage));

    private RelayCommand? cmd_RobotMoveUpper;
    public ICommand Cmd_RobotMoveUpper => cmd_RobotMoveUpper ??= new RelayCommand(_ => RobotMove(RobotPosition.UpperChamberLoad));

    private RelayCommand? cmd_RobotMoveLower;
    public ICommand Cmd_RobotMoveLower => cmd_RobotMoveLower ??= new RelayCommand(_ => RobotMove(RobotPosition.LowerChamberLoad));

    #endregion
}
