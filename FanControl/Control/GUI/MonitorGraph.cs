using InteractiveDataDisplay.WPF;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static FanControl.EC;

namespace FanControl
{
    public class MonitorGraph : IDisposable
    {
        ChartBase chart;
        IEnumerable Points_X;
        TimeSpan duration_Span;
        double Default_PlotHeight;
        Grid grid = new Grid();
        Canvas Hud = new Canvas();

        HashMap<LineGraph, MonitorData> lines = new HashMap<LineGraph, MonitorData>();
        public MonitorGraph(ChartBase chartBase)
        {
            chart = chartBase;
            Default_PlotHeight = chart.GraphChart.PlotHeight;
            InitLines();
        }
        void InitLines()
        {
            //MouseEvent capturer
            Rectangle rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(Colors.White);
            rectangle.Opacity = 0.01;
            rectangle.Width = chart._contents.ActualWidth;
            rectangle.Height = chart._contents.ActualHeight;
            rectangle.MouseMove += Graph_MouseEvent;
            rectangle.MouseLeave += Graph_MouseEvent;
            rectangle.MouseEnter += Graph_MouseEvent;
            grid.Children.Add(rectangle);

            Hud.IsHitTestVisible = false;

            Monitor monitor = SingleInstanceManager.Instance.monitor;
            Type type = monitor.GetType();
            double duration = 0.0;
            foreach (FieldInfo item in type.GetFields())
            {
                if (item.FieldType == typeof(MonitorData))
                {
                    var data = item.GetValue(monitor) as MonitorData;
                    if (data != null)
                    {
                        LineGraph line = new LineGraph();
                        line.Stroke = data.Stroke;
                        line.Description = data.Desc;
                        line.IsHitTestVisible = false;
                        lines.Add(line, data);
                        grid.Children.Add(line);

                        //Init Hud
                        if (!data.Data.Contains("Fan"))
                        {
                            var region_hud = new Rectangle();
                            region_hud.Name = data.Desc.Replace(" ", "_");
                            region_hud.Fill = data.Stroke;
                            region_hud.Stroke = data.Stroke;
                            region_hud.StrokeThickness = 1;
                            region_hud.Opacity = 0.4;
                            region_hud.MinHeight = 2;
                            Hud.Children.Add(region_hud);
                        }

                        this.duration_Span = data.Duraion;
                    }
                }
            }

            duration = duration_Span.TotalMilliseconds;
            if (duration != 0.0)
            {
                int points_num = (int)(duration / SingleInstanceManager.Instance.cfg.PollSpan + 1);
                double delta_x = duration_Span.TotalMinutes / (points_num - 1);
                Points_X = Enumerable.Range(0, points_num).Select(i => i * delta_x);
            }
        }
        private void Graph_MouseEvent(object sender, MouseEventArgs e)
        {
            chart.Indicators.RaiseEvent(e);
            updateToolTip();
            e.Handled = true;
        }

        void ResetPlotWidth()
        {
            chart.GraphChart.PlotWidth = duration_Span.TotalMinutes;
        }

        void ResetPlotHeight()
        {
            chart.GraphChart.PlotHeight = Default_PlotHeight;
        }

        public void HideGraphs()
        {
            chart._contents.Children.Remove(grid);
            chart._contents.Children.Remove(Hud);
            ResetPlotHeight();
        }

        public void ShowGraphs()
        {
            ResetPlotWidth();
            chart._contents.Children.Add(grid);
            chart._contents.Children.Add(Hud);
        }

        public void updateLines()
        {
            double maxHeight = Default_PlotHeight;
            foreach (LineGraph line in lines.Keys)
            {
                MonitorData data;
                lines.TryGetValue(line, out data);
                object[] datas = data.getByRange(0, (int)data.Duraion.TotalMilliseconds, TimeUnit.Millisecond);

                PointCollection points = new PointCollection(datas.Length);
                IEnumerator enum_X = Points_X.GetEnumerator();
                foreach (var item in datas)
                {
                    enum_X.MoveNext();
                    double Y;
                    double X = (double)enum_X.Current;
                    if (line.Description.Contains("Fan"))
                    {
                        Y = Math.Round(Convert.ToDouble(item) / 255.0 * 100.0);
                    }
                    else
                    {
                        Y = Convert.ToDouble(item);
                    }
                    if (maxHeight < Y && Y < 150)
                        maxHeight = Y;
                    Point point = new Point(X, Y);
                    points.Add(point);
                }
                line.Points = points;
            }
            chart.GraphChart.PlotHeight = maxHeight;
            updateToolTip();
        }

        void updateToolTip()
        {
            chart.InfoContent.Inlines.Clear();
            if (chart.X_Indicator.Visibility == Visibility.Visible)
            {
                Point pos = Mouse.GetPosition(chart._contents);
                var Mouse_X = pos.X / grid.ActualWidth * chart.GraphChart.PlotWidth;
                foreach (var item in grid.Children)
                {
                    if (item is LineGraph && ((LineGraph)item).IsVisible)
                    {
                        var line = item as LineGraph;
                        var points = line.Points;
                        foreach (Point point in points)
                        {
                            if (Mouse_X <= point.X)
                            {
                                var format = line.Description.Contains("Fan") ? " {0}% " : line.Description.Contains("T") ? " {0}℃ " : " {0:0.00}W ";
                                chart.InfoContent.Inlines.Add(new Run(line.Description) { Foreground = line.Stroke });
                                chart.InfoContent.Inlines.Add(new Run(string.Format(format, point.Y)));
                                break;
                            }
                        }
                    }
                }
            }
            //Update Hud
            Monitor monitor = SingleInstanceManager.Instance.monitor;
            Type type = monitor.GetType();
            foreach (FieldInfo item in type.GetFields())
            {
                if (item.FieldType == typeof(MonitorData))
                {
                    var data = item.GetValue(monitor) as MonitorData;
                    if (data != null && !data.Desc.Contains("Fan"))
                    {
                        if (!(chart.X_Indicator.Visibility == Visibility.Visible))
                        {
                            chart.InfoContent.Inlines.Add(new Run(data.Desc) { Foreground = data.Stroke });
                            chart.InfoContent.Inlines.Add(new Run(string.Format(" {0} ", data.General_Trend.ToString())));
                        }

                        var region_hud = LogicalTreeHelper.FindLogicalNode(Hud, data.Desc.Replace(" ", "_")) as Rectangle;
                        region_hud.Width = data.Region_Time * data.DataSpan.TotalMinutes / chart.GraphChart.PlotWidth * chart._contents.ActualWidth;
                        region_hud.Height = data.Region_Var / chart.GraphChart.PlotHeight * chart._contents.ActualHeight;
                        Canvas.SetTop(region_hud, (1 - data.Region / chart.GraphChart.PlotHeight) * chart._contents.ActualHeight - region_hud.Height / 2);
                    }
                }
            }
        }

        public void Dispose()
        {
            HideGraphs();
        }
    }
}
