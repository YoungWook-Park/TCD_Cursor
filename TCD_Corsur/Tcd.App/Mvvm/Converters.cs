using System;
using System.Globalization;
using System.Windows.Data;

namespace Tcd.App;

/// <summary>
/// (AxisName: string, Point: AxisTeachPoint) → AxisTeachPointParam.
/// Teach/Move 버튼 CommandParameter 바인딩용.
/// </summary>
public sealed class AxisTeachPointParamConverter : IMultiValueConverter
{
  public object? Convert(
    object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    if (values.Length != 2) return null;
    var axisName = values[0] as string ?? "";
    var point    = values[1] as Manual_MotorViewModel.AxisTeachPoint;
    return new Manual_MotorViewModel.AxisTeachPointParam
    {
      AxisName = axisName,
      Point    = point,
    };
  }

  public object[] ConvertBack(
    object value, Type[] targetTypes, object parameter, CultureInfo culture)
    => throw new NotSupportedException();
}
