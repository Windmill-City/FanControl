using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FanControl
{
    /// <summary>
    /// FanMonitor.xaml 的交互逻辑
    /// </summary>
    public partial class FanInfo : UserControl
    {
        public Storyboard storyboard;

        public FanInfo()
        {
            InitializeComponent();
            Init_StoryBoard();
        }

        void Init_StoryBoard()
        {
            this.storyboard = new Storyboard();
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            RotateTransform renderTransform = new RotateTransform();
            this.R_FanLayer2.RenderTransform = renderTransform;
            this.R_FanLayer3.RenderTransform = renderTransform;
            this.storyboard.RepeatBehavior = RepeatBehavior.Forever;
            this.storyboard.SpeedRatio = 1;
            doubleAnimation.From = 360.0;
            doubleAnimation.To = 0.0;
            doubleAnimation.Duration = new Duration(new TimeSpan(0, 0, 20));
            Storyboard.SetTarget(doubleAnimation, this.R_FanLayer2);
            Storyboard.SetTarget(doubleAnimation, this.R_FanLayer3);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.Angle", Array.Empty<object>()));
            this.storyboard.Children.Add(doubleAnimation);
            this.storyboard.Begin();
            this.storyboard.Pause();
        }

        public void UpdataFanInfo(double rpm, int duty)
        {
            rpm = RpmConvert(rpm);
            Info.Info_1_Data.Text = rpm.ToString();
            Info.Info_2_Data.Text = DutyToStrConvert(duty);
            if (rpm == 0)
            {
                storyboard.Pause();
                R_FanLayer2.Visibility = Visibility.Hidden;
            }
            else if (storyboard.GetIsPaused())
            {
                storyboard.Resume();
                R_FanLayer2.Visibility = Visibility.Visible;
                storyboard.SetSpeedRatio(Math.Pow((rpm / 5400) + 1, 5));
            }
        }

        public static double RpmConvert(object value)
        {
            var rpm = (double)value;
            if (rpm != 0.0)
            {
                rpm = 60.0 / (5.565217391304348E-05 * rpm);
                rpm *= 2.0;
                rpm = Math.Round(rpm);
            }
            return rpm;
        }

        public static string DutyToStrConvert(object value)
        {
            var num = Math.Round((int)value / 255.0 * 100.0);
            return string.Format("{0}%", num);

        }
    }
}
