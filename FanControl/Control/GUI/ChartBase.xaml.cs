using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FanControl
{
    /// <summary>
    /// Chart.xaml 的交互逻辑
    /// </summary>
    public partial class ChartBase : UserControl
    {
        public ChartBase()
        {
            InitializeComponent();
        }

        public void SetPlotWidth(int width)
        {
            GraphChart.PlotWidth = width;
        }

        public bool IsPaused
        {
            get
            {
                return _Pause.Visibility != Visibility.Visible;
            }
            set
            {
                if (value)
                {
                    _Pause.Visibility = Visibility.Collapsed;
                    _Play.Visibility = Visibility.Visible;
                }
                else
                {
                    _Pause.Visibility = Visibility.Visible;
                    _Play.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            IsPaused = !IsPaused;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.RoutedEvent == MouseLeaveEvent)
            {
                X_Indicator.Visibility = Visibility.Collapsed;
                Pos_X_Value.Visibility = Visibility.Collapsed;
                Y_Indicator.Visibility = Visibility.Collapsed;
                Pos_Y_Value.Visibility = Visibility.Collapsed;
            }
            else if (e.RoutedEvent == MouseEnterEvent)
            {
                X_Indicator.Visibility = Visibility.Visible;
                Pos_X_Value.Visibility = Visibility.Visible;
                Y_Indicator.Visibility = Visibility.Visible;
                Pos_Y_Value.Visibility = Visibility.Visible;
            }
            else
            {
                Point pos = e.GetPosition(Indicators);
                Canvas.SetLeft(X_Indicator, pos.X);
                Canvas.SetTop(Y_Indicator, pos.Y);
                var x_value = GraphChart.PlotWidth * pos.X / Indicators.ActualWidth;
                var y_value = GraphChart.PlotHeight * (1 - pos.Y / Indicators.ActualHeight);
                Canvas.SetLeft(Pos_X_Value, pos.X - Pos_X_Value.ActualWidth / 2);
                Canvas.SetTop(Pos_Y_Value, pos.Y - Pos_Y_Value.ActualHeight / 2);
                X_Value.Text = string.Format("{0:0.00}", x_value);
                Y_Value.Text = string.Format("{0:0.00}", y_value);
            }
        }

        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Settings.Focus();
        }
    }
    public class VisibilityToCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
