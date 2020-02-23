using System;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FanControl
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Cpu_Helper cpu_Helper = new Cpu_Helper();
        Timer infoUpdateTimer = new Timer(Math.Max(2000, SingleInstanceManager.Instance.cfg.PollSpan));
        MonitorGraph monitorGraph;
        FanTableUI tableUI;
        public MainWindow()
        {
            InitializeComponent();
            Window_Init();
        }

        private void Window_Init()
        {
            String usage = "Usage:";
            String temp = "Temp:";
            //Cpu
            Cpu.ItemName.Text = cpu_Helper.Name;
            Cpu.Info_1_Desc.Text = usage;
            Cpu.Info_2_Desc.Text = temp;
            //Gpu
            Gpu_1.Visibility = Visibility.Collapsed;
            Gpu_2.Visibility = Visibility.Collapsed;
            if (SingleInstanceManager.Instance.supportNVSMI)
            {
                uint count;
                NV_Queries.nv_getCount(out count);
                if (count >= 1)
                {
                    Gpu_1.Visibility = Visibility.Visible;
                    Byte[] data = new Byte[64];
                    int size;
                    NV_Queries.nv_getNameByIndex(0, ref data[0], out size);
                    Gpu_1.ItemName.Text = System.Text.Encoding.Default.GetString(data);
                    Gpu_1.Info_1_Desc.Text = usage;
                    Gpu_1.Info_2_Desc.Text = temp;
                    if (count == 2)
                    {
                        Gpu_2.Visibility = Visibility.Visible;
                        Gpu_2.Info_1_Desc.Text = usage;
                        Gpu_2.Info_2_Desc.Text = temp;
                    }
                }
            }

            InitFans();

            //Update ui
            infoUpdateTimer.Elapsed += onTimer;
            Config config = SingleInstanceManager.Instance.cfg;
            //init config
            _Count.Value = config.FanCount;
            _Span.Text = config.PollSpan.ToString();
            _TrendStableTime.Text = config.TrendStableTime.ToString();
            _Mode.SelectedIndex = config.FanMode;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitMonitorGraph();
            _MonitorGraph.IsChecked = true;
            infoUpdateTimer.Start();
        }

        private void InitFans()
        {
            Fan_2.Visibility = Visibility.Collapsed;
            Fan_3.Visibility = Visibility.Collapsed;
            Config config = SingleInstanceManager.Instance.cfg;
            String rpm = "Rpm:";
            String duty = "Duty:";
            //Fan
            Fan_1.Info.ItemName.Text = "Fan 1";
            Fan_1.Info.Info_1_Desc.Text = rpm;
            Fan_1.Info.Info_2_Desc.Text = duty;
            if (config.FanCount >= 2)
            {
                Fan_2.Visibility = Visibility.Visible;
                Fan_2.Info.ItemName.Text = "Fan 2";
                Fan_2.Info.Info_1_Desc.Text = rpm;
                Fan_2.Info.Info_2_Desc.Text = duty;
                if (config.FanCount == 3)
                {
                    Fan_3.Visibility = Visibility.Visible;
                    Fan_3.Info.ItemName.Text = "Fan 3";
                    Fan_3.Info.Info_1_Desc.Text = rpm;
                    Fan_3.Info.Info_2_Desc.Text = duty;
                }
            }
        }

        private void InitMonitorGraph()
        {
            if (monitorGraph != null)
                monitorGraph.Dispose();
            monitorGraph = new MonitorGraph(_ChartBase);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            SingleInstanceManager.Instance.Restart();
        }

        void onTimer(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateInfo();
                if (!_ChartBase.IsPaused)
                    monitorGraph.updateLines();
            }));
        }

        void UpdateInfo()
        {
            EC ec = SingleInstanceManager.Instance.ec;
            //Cpu
            Cpu.Info_1_Data.Text = PercentToStrConvert(cpu_Helper.GetCPUTotalUsage());
            Cpu.Info_2_Data.Text = TempToStrConvert(ec.Cpu_Temp);
            //Gpu
            if (Gpu_1.Visibility == Visibility.Visible)
            {
                uint gpu, mem;
                NV_Queries.nv_getUtilizationRatesByIndex(0, out mem, out gpu);
                Gpu_1.Info_1_Data.Text = PercentToStrConvert(gpu);
                Gpu_1.Info_2_Data.Text = TempToStrConvert(ec.Gpu1_Temp);
            }
            if (Gpu_2.Visibility == Visibility.Visible)
            {
                uint gpu, mem;
                NV_Queries.nv_getUtilizationRatesByIndex(1, out mem, out gpu);
                Gpu_2.Info_1_Data.Text = PercentToStrConvert(gpu);
                Gpu_2.Info_2_Data.Text = TempToStrConvert(ec.Gpu2_Temp);
            }
            //Fan
            Fan_1.UpdataFanInfo(ec.Fan_1.RPM, ec.Fan_1.Duty);
            if (Fan_2.Visibility == Visibility.Visible)
            {
                Fan_2.UpdataFanInfo(ec.Fan_2.RPM, ec.Fan_2.Duty);
            }
            if (Fan_3.Visibility == Visibility.Visible)
            {
                Fan_3.UpdataFanInfo(ec.Fan_3.RPM, ec.Fan_3.Duty);
            }
        }
        private void _Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EC ec = SingleInstanceManager.Instance.ec;
            var config = SingleInstanceManager.Instance.cfg;
            var item = sender as ComboBox;
            lock (SingleInstanceManager.Instance.controller)
            {
                if (item.SelectedIndex == 4)
                {
                    SingleInstanceManager.Instance.controller.ShouldUpdate = true;
                }
                else
                {
                    if (config.FanMode == 4)
                        ec.SetFanModeAuto();
                    SingleInstanceManager.Instance.controller.ShouldUpdate = false;
                }
            }
            if (item.SelectedIndex >= 0 && item.SelectedIndex < 7 && item.SelectedIndex != 4)
                ec.SetWMI(121, 1, Convert.ToUInt32(item.SelectedIndex));
            config.FanMode = item.SelectedIndex;
        }
        private void _SelectGraph_Event(object sender, RoutedEventArgs e)
        {
            var btn = sender as RadioButton;
            _ChartBase.Title.Text = btn.Content.ToString();
            if (btn.Name == "_MonitorGraph")
            {
                _ChartBase.IsPaused = false;
                _ChartBase.BtnPause.IsEnabled = true;
                //Show graph and legend
                monitorGraph.ShowGraphs();
                if (tableUI != null)
                    tableUI.Dispose();
            }
            else
            {
                //Set paused
                _ChartBase.BtnPause.IsEnabled = false;
                _ChartBase.IsPaused = true;
                //Hide linegraph
                monitorGraph.HideGraphs();
                if (tableUI != null)
                    tableUI.Dispose();
                int num = 0;
                switch (btn.Name)
                {
                    case "_Fan_1":
                        num = 1;
                        break;
                    case "_Fan_2":
                        num = 2;
                        break;
                    case "_Fan_3":
                        num = 3;
                        break;
                }
                if (num != 0)
                {
                    tableUI = new FanTableUI(_ChartBase, num);
                    tableUI.ShowFanTable();
                }
            }
        }
        private void Config_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _Config.Focus();
        }

        public static string PercentToStrConvert(object value)
        {
            return string.Format("{0}%", value);
        }

        public static string TempToStrConvert(object value)
        {
            return string.Format("{0}℃", value);
        }

        private void _Count_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
            {
                Config config = SingleInstanceManager.Instance.cfg;
                var count = (int)((Slider)sender).Value;
                config.FanCount = count;
                InitFans();
                SingleInstanceManager.Instance.controller.Dispose();
                SingleInstanceManager.Instance.controller = new Controller(config.FanCount, TimeSpan.FromMilliseconds(config.PollSpan));
            }
        }
        NumberCheck check = new NumberCheck(Double.PositiveInfinity, 1);
        private void _Span_LostFocus(object sender, RoutedEventArgs e)
        {
            Config config = SingleInstanceManager.Instance.cfg;
            var box = (TextBox)sender;
            if (check.Validate(((TextBox)sender).Text, CultureInfo.CurrentCulture).IsValid)
            {
                config.PollSpan = Convert.ToInt32(((TextBox)sender).Text);
                SingleInstanceManager.Instance.InitMonitor();
                SingleInstanceManager.Instance.controller.Dispose();
                SingleInstanceManager.Instance.controller = new Controller(config.FanCount, TimeSpan.FromMilliseconds(config.PollSpan));
                InitMonitorGraph();
                if ((bool)_MonitorGraph.IsChecked)
                    monitorGraph.ShowGraphs();
                infoUpdateTimer.Interval = Math.Max(2000, config.PollSpan);
                box.BorderBrush = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                box.BorderBrush = new SolidColorBrush(Colors.Red);
            }
        }
        private void _TrendStableTime_LostFocus(object sender, RoutedEventArgs e)
        {
            Config config = SingleInstanceManager.Instance.cfg;
            var box = (TextBox)sender;
            if (check.Validate(((TextBox)sender).Text, CultureInfo.CurrentCulture).IsValid)
            {
                config.TrendStableTime = Convert.ToInt32(((TextBox)sender).Text);
                box.BorderBrush = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                box.BorderBrush = new SolidColorBrush(Colors.Red);
            }
        }
    }
}
