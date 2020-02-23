using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static FanControl.FanTable;

namespace FanControl
{
    public class FanTableUI : IDisposable
    {
        ChartBase chart;
        FanTable fanTable;
        Canvas canvas;
        LineGraph line;
        StackPanel setting_panel;

        LinkedHashMap<FrameworkElement, TD> Btns = new LinkedHashMap<FrameworkElement, TD>();
        public FanTableUI(ChartBase chartBase, int FanNum)
        {
            chart = chartBase;
            fanTable = FanTable.getFanTable(FanNum);
            InitFanTable();
        }


        void InitFanTable()
        {
            canvas = new Canvas();
            canvas.Height = chart._contents.ActualHeight;
            canvas.Width = chart._contents.ActualWidth;

            canvas.MouseEnter += Canvas_MouseEvent;
            canvas.MouseMove += Canvas_MouseEvent;
            canvas.MouseLeave += Canvas_MouseEvent;

            Rectangle rect = new Rectangle();
            rect.Width = chart._contents.ActualWidth;
            rect.Height = chart._contents.ActualHeight;
            rect.Fill = new SolidColorBrush(Colors.White);
            rect.Opacity = 0.01;
            canvas.Children.Add(rect);
            //Init Lines
            line = new LineGraph();
            line.Description = "Fan";
            line.Stroke = new SolidColorBrush(Colors.Black);
            line.StrokeThickness = 1;
            //Init TD points
            foreach (TD point in fanTable.points)
            {
                var btn = new Rectangle();
                btn.Width = 15;
                btn.Height = 15;
                btn.Stroke = new SolidColorBrush(Colors.Black);
                btn.Fill = new SolidColorBrush(Colors.White);

                btn.AddHandler(FrameworkElement.MouseDownEvent, new MouseButtonEventHandler(Btn_MouseDown));
                btn.AddHandler(FrameworkElement.MouseUpEvent, new MouseButtonEventHandler(Btn_MouseUp));
                btn.AddHandler(FrameworkElement.MouseMoveEvent, new MouseEventHandler(Btn_MouseMove));

                setButtonPos(btn, point);
                canvas.Children.Add(btn);
                Btns.Add(btn, point);
            }
            updateLine();
            Config config = SingleInstanceManager.Instance.cfg;
            //Init FanTable Settings
            setting_panel = new StackPanel();
            setting_panel.Margin = new Thickness(2);
            //Fixed checkbox
            CheckBox isFixed = new CheckBox();
            isFixed.Click += IsFixed_Click;
            isFixed.IsChecked = fanTable.isFixed;
            isFixed.Content = "isFixed";
            TextBlock tooltip = new TextBlock();
            tooltip.Text = "Set if the FanTable Auto Change Duty base on the circuit power";
            isFixed.ToolTip = tooltip;
            ToolTipService.SetShowDuration(isFixed, 10000);
            setting_panel.Children.Add(isFixed);
            //Cpu proportion
            TextBlock Desc = new TextBlock();
            Desc.Text = "Cpu";
            setting_panel.Children.Add(Desc);
            //TextBox
            TextBox prop = new TextBox();
            prop.Name = "Cpu";
            prop.Text = fanTable.CpuProportion.ToString();
            prop.LostFocus += Prop_LostFocus;
            tooltip = new TextBlock();
            tooltip.Text = "Set the proportion it takes when calc Fan Duty";
            prop.ToolTip = tooltip;
            ToolTipService.SetShowDuration(prop, 10000);
            setting_panel.Children.Add(prop);
            if (config.GpuCount >= 1)
            {
                //Gpu_1 proportion
                Desc = new TextBlock();
                Desc.Text = "Gpu 1";
                setting_panel.Children.Add(Desc);
                //TextBox
                prop = new TextBox();
                prop.Name = "Gpu_1";
                prop.Text = fanTable.Gpu_1_Proportion.ToString();
                prop.LostFocus += Prop_LostFocus;
                tooltip = new TextBlock();
                tooltip.Text = "Set the proportion it takes when calc Fan Duty";
                prop.ToolTip = tooltip;
                ToolTipService.SetShowDuration(prop, 10000);
                setting_panel.Children.Add(prop);
            }
            if (config.GpuCount == 2)
            {
                //Gpu_2 proportion
                Desc = new TextBlock();
                Desc.Text = "Gpu 2";
                setting_panel.Children.Add(Desc);
                //TextBox
                prop = new TextBox();
                prop.Name = "Gpu_2";
                prop.Text = fanTable.Gpu_2_Proportion.ToString();
                prop.LostFocus += Prop_LostFocus;
                tooltip = new TextBlock();
                tooltip.Text = "Set the proportion it takes when calc Fan Duty";
                prop.ToolTip = tooltip;
                ToolTipService.SetShowDuration(prop, 5000);
                setting_panel.Children.Add(prop);
            }
        }

        private void Canvas_MouseEvent(object sender, MouseEventArgs e)
        {
            chart.Indicators.RaiseEvent(e);
        }

        NumberCheck check = new NumberCheck(Double.PositiveInfinity, 0);
        private void Prop_LostFocus(object sender, RoutedEventArgs e)
        {
            Config config = SingleInstanceManager.Instance.cfg;
            var box = sender as TextBox;
            var value = box.Text;
            int prop = -1;
            if (check.Validate(value, CultureInfo.CurrentCulture).IsValid)
            {
                prop = Convert.ToInt32(value);
                box.BorderBrush = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                box.BorderBrush = new SolidColorBrush(Colors.Red);
            }
            if (prop != -1)
                switch (box.Name)
                {
                    case "Cpu":
                        fanTable.CpuProportion = prop;
                        break;
                    case "Gpu_1":
                        fanTable.Gpu_1_Proportion = prop;
                        break;
                    case "Gpu_2":
                        fanTable.Gpu_2_Proportion = prop;
                        break;
                }
        }

        private void IsFixed_Click(object sender, RoutedEventArgs e)
        {
            fanTable.isFixed = !fanTable.isFixed;
            ((CheckBox)sender).IsChecked = fanTable.isFixed;
            e.Handled = true;
        }

        bool MouseDown = false;
        SolidColorBrush Stroke_Blue = new SolidColorBrush(Colors.RoyalBlue);
        SolidColorBrush Stroke_Red = new SolidColorBrush(Colors.Red);
        SolidColorBrush Stroke_Black = new SolidColorBrush(Colors.Black);
        TD prev;
        TD next;
        private void Btn_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseDown)
            {
                var btn = sender as Rectangle;
                Point pos = e.GetPosition(chart._contents);

                var X = line.XFromLeft(pos.X);
                var Y = pos.Y < 0 ? 0 : pos.Y > chart._contents.ActualHeight ? chart._contents.ActualHeight : pos.Y;

                X = (int)(X > prev.T + 2 ? (X < next.T - 2 ? X : next.T - 2) : prev.T + 2);
                Y = (int)line.YFromTop(Y);

                TD point = new TD(X, Y);
                setButtonPos(btn, point);
                btn.Stroke = (X % 2) == (Y % 2) ? Stroke_Blue : Stroke_Red;
                Btns[btn] = point;
                updateLine();

                chart.InfoContent.Text = string.Format("Temp:{0}℃ Duty:{1}%", X, Y);
                chart.Indicators.RaiseEvent(e);

                e.Handled = true;
            }
        }

        private void Btn_MouseUp(object sender, MouseEventArgs e)
        {
            MouseDown = false;
            var btn = sender as Rectangle;
            btn.ReleaseMouseCapture();
            btn.Stroke = Stroke_Black;
            //Save FanTable
            fanTable.points = new LinkedList<TD>(Btns.Values);
            fanTable.savePoints();

            chart.InfoContent.Text = "";

            e.Handled = true;
        }

        private void Btn_MouseDown(object sender, MouseEventArgs e)
        {
            MouseDown = true;
            var btn = sender as FrameworkElement;
            btn.CaptureMouse();
            var E_Btns = Btns.GetEnumerator();
            prev = new TD(-2, 0);
            next = new TD(102, 0);
            while (E_Btns.MoveNext())
            {
                var cur = E_Btns.Current;
                if (cur.Key == btn)
                {
                    if (E_Btns.MoveNext())
                        next = E_Btns.Current.Value;
                    break;
                }
                prev = E_Btns.Current.Value;
            }
            e.Handled = true;
        }

        void updateLine()
        {
            var points = new PointCollection(Btns.Values.Count);
            foreach (var item in Btns.Values)
            {
                points.Add(new Point(item.T, item.D));
            }
            line.Points = points;
        }

        public void ShowFanTable()
        {
            chart.GraphChart.PlotWidth = 100;
            chart.GraphChart.LeftTitle = "Duty";
            chart.GraphChart.BottomTitle = "Temp";
            chart._contents.Children.Add(line);
            chart._contents.Children.Add(canvas);
            chart.Settings.Children.Add(setting_panel);
        }

        public void HideFanTable()
        {
            chart.GraphChart.LeftTitle = "";
            chart.GraphChart.BottomTitle = "";
            chart._contents.Children.Remove(canvas);
            chart._contents.Children.Remove(line);
            chart.Settings.Children.Remove(setting_panel);
        }

        void setButtonPos(FrameworkElement btn, TD point)
        {
            Canvas.SetTop(btn, (1 - point.D / 100.0) * canvas.Height - btn.Height / 2);
            Canvas.SetLeft(btn, point.T / 100.0 * canvas.Width - btn.Height / 2);
        }

        public void Dispose()
        {
            HideFanTable();
        }
    }
}
