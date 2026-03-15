using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Shapes;
using Tcd.App;
using Tcd.Devices;

namespace Tcd.App.View;

public partial class EquipmentDiagramView
{
    private const double RobotHomeX = 306;
    private const double RobotHomeY = 6;
    private const double RobotHandSize = 28;
    private const double RobotCenterOffset = RobotHandSize / 2; // 14
    private const double RobotStageX = 120;
    private const double RobotStageY = 140;
    private const double RobotUpperLoadX = 120;
    private const double RobotUpperLoadY = 60;
    private const double RobotLowerLoadX = 120;
    private const double RobotLowerLoadY = 220;
    private const double AnimDurationSec = 0.25;

    public EquipmentDiagramView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        SetRobotPosition(vm.CurrentRobotPosition, animate: false);
        SetZBarPosition(vm.ZPosition, animate: false);

        vm.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += (_, _) => vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel vm) return;

        if (e.PropertyName == nameof(MainWindowViewModel.CurrentRobotPosition))
            SetRobotPosition(vm.CurrentRobotPosition, animate: true);
        else if (e.PropertyName == nameof(MainWindowViewModel.ZPosition))
            SetZBarPosition(vm.ZPosition, animate: true);
    }

    private void SetRobotPosition(RobotPosition position, bool animate)
    {
        var (x, y) = position switch
        {
            RobotPosition.Home => (RobotHomeX, RobotHomeY),
            RobotPosition.Stage => (RobotStageX, RobotStageY),
            RobotPosition.UpperChamberLoad => (RobotUpperLoadX, RobotUpperLoadY),
            RobotPosition.LowerChamberLoad => (RobotLowerLoadX, RobotLowerLoadY),
            _ => (RobotHomeX, RobotHomeY)
        };

        double centerX = x + RobotCenterOffset;
        double centerY = y + RobotCenterOffset;
        double baseCenterX = RobotHomeX + RobotCenterOffset;
        double baseCenterY = RobotHomeY + RobotCenterOffset;

        if (animate)
        {
            AnimateTo(RobotEllipse, Canvas.LeftProperty, x, AnimDurationSec);
            AnimateTo(RobotEllipse, Canvas.TopProperty, y, AnimDurationSec);
            AnimateTo(RobotArmLine, Line.X2Property, centerX, AnimDurationSec);
            AnimateTo(RobotArmLine, Line.Y2Property, centerY, AnimDurationSec);
        }
        else
        {
            Canvas.SetLeft(RobotEllipse, x);
            Canvas.SetTop(RobotEllipse, y);
            RobotArmLine.X1 = baseCenterX;
            RobotArmLine.Y1 = baseCenterY;
            RobotArmLine.X2 = centerX;
            RobotArmLine.Y2 = centerY;
        }
    }

    private void SetZBarPosition(double zPosition, bool animate)
    {
        const double barHeight = 80;
        const double trackHeight = 120;
        const double minY = 230;
        // Z 0 = bottom, Z 100 = top (thumb moves within track)
        double top = minY + (trackHeight - barHeight) - (zPosition / 100.0) * (trackHeight - barHeight);

        if (animate)
        {
            AnimateTo(ZBarThumb, Canvas.TopProperty, top, AnimDurationSec);
        }
        else
        {
            Canvas.SetTop(ZBarThumb, top);
        }
    }

    private static void AnimateTo(UIElement element, DependencyProperty property, double toValue, double durationSec)
    {
        var anim = new DoubleAnimation(toValue, TimeSpan.FromSeconds(durationSec))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(property, anim);
    }
}
