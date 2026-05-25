using Tcd.App.Mvvm;

namespace Tcd.App.Core;

public sealed class TeachPosition : NotifyPropertyChangedBase
{
  private string _name = "";
  private double _u, _v, _w, _zLower, _zUpper;
  private double _velocity = 100;
  private double _acc = 1000;

  public string Name     { get => _name;     set => Set(ref _name, value); }
  public double U        { get => _u;        set => Set(ref _u, value); }
  public double V        { get => _v;        set => Set(ref _v, value); }
  public double W        { get => _w;        set => Set(ref _w, value); }
  public double ZLower   { get => _zLower;   set => Set(ref _zLower, value); }
  public double ZUpper   { get => _zUpper;   set => Set(ref _zUpper, value); }
  public double Velocity { get => _velocity; set => Set(ref _velocity, value); }
  public double Acc      { get => _acc;      set => Set(ref _acc, value); }

  public double GetAxis(string axis) => axis switch
  {
    AxisDefine.U      => U,
    AxisDefine.V      => V,
    AxisDefine.W      => W,
    AxisDefine.ZLower => ZLower,
    AxisDefine.ZUpper => ZUpper,
    _                 => 0
  };
}
