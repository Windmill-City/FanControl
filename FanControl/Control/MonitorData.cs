using System;
using System.Windows.Media;
using static FanControl.EC;

namespace FanControl
{
    public class MonitorData
    {
        public Brush Stroke { get; set; }
        public string Desc { get; set; }
        public TimeSpan DataSpan { get; set; }

        public TimeSpan Duraion { get; set; }

        public CircularQueue Data { get; set; }

        public object Current { get; set; }

        public MonitorData(string desc, TimeSpan dataspan, Brush stroke, TimeSpan duration)
        {
            this.Stroke = stroke;
            this.Desc = desc;
            this.DataSpan = dataspan;
            this.Duraion = duration;
            Data = new CircularQueue((int)(duration.TotalMilliseconds / dataspan.TotalMilliseconds), true);
        }

        public object[] getByRange(int starttime, int endtime, TimeUnit unit)
        {
            if (starttime < 0 || endtime < 0)
            {
                throw new ArgumentException("nagetive starttime or endtime");
            }
            if (endtime - starttime < 0)
            {
                throw new ArgumentException("starttime larger than endtime");
            }

            starttime = TimeUnitHelper.UnitConvert(unit, TimeUnit.Millisecond, starttime);
            endtime = TimeUnitHelper.UnitConvert(unit, TimeUnit.Millisecond, endtime);
            if (endtime > Duraion.TotalMilliseconds)
            {
                throw new ArgumentException("endtime larger than duantion");
            }

            var start = (int)(starttime / DataSpan.TotalMilliseconds);
            var end = (int)(endtime / DataSpan.TotalMilliseconds);
            if (!(start < Data.Count))
            {
                return new object[0];
            }
            var datanum = Math.Min(end - start + 1, Data.Count);
            var result = new object[datanum];
            Data.CopyTo(result, start, 0, datanum);
            return result;
        }
        public void addData(object value)
        {
            Data.Enqueue(value);
            Current = value;
            updateDataTrend();
        }

        //Data Trend
        //Region Data
        public int General;
        public DataTrend General_Trend = DataTrend.Wave;
        int TrendStableTime = 0;

        public double Region = 0;
        public double Region_Var = 0;
        DataTrend Region_trend = DataTrend.Wave;

        int Region_RiseOrDec = 0;
        int Abrupt_Change_Time = 0;
        public int Region_Time = 0;

        double lastDelta = 0;
        void updateDataTrend()
        {
            var cur = Convert.ToDouble(Current);
            //Handle Region Time
            Region_Time++;
            bool RegionOld = Region_Time * DataSpan.TotalSeconds > 30;
            TrendStableTime++;
            //Handle Current Data
            DataTrend cur_trend = DataTrend.Stable;
            if (Math.Abs(cur - Region) > Region_Var || (Math.Abs(cur - Region) < Region_Var - 1 && Region_Var - 1 > 0))
            {
                cur_trend = DataTrend.AbruptChange;
            }
            //Update Region State
            switch (Region_trend)
            {
                case DataTrend.Rise:
                case DataTrend.Decline://Check if keep rising or falling, if not go back to stable
                    switch (cur_trend)
                    {
                        case DataTrend.Stable:
                            if (Abrupt_Change_Time > 0)
                                Abrupt_Change_Time--;
                            var avg = (Region_Time * Region + cur) / (Region_Time + 1);

                            var delta = Region - avg;
                            if (Math.Abs(delta) > 0.01)
                                Region_RiseOrDec += delta < 0 ? 1 : -1;//Check if it is raising or falling
                            else
                                Region_RiseOrDec += Region_RiseOrDec > 0 ? -1 : 1;
                            Region = avg;
                            break;
                        case DataTrend.AbruptChange:
                            delta = Math.Abs(cur - Region);
                            if (delta >= lastDelta)//getting far away from or keep above avg
                                Abrupt_Change_Time++;
                            lastDelta = delta;
                            break;
                    }
                    if (Math.Abs(Region_RiseOrDec) < 3 / DataSpan.TotalSeconds)
                    {
                        Region_trend = DataTrend.Stable;
                        Region_RiseOrDec = 0;
                        TrendStableTime = 0;
                    }
                    if (Abrupt_Change_Time > 5 / DataSpan.TotalSeconds)
                    {
                        Region_trend = DataTrend.Wave;
                        Abrupt_Change_Time = 0;
                        Region_RiseOrDec = 0;
                        lastDelta = 0;
                        TrendStableTime = 0;
                        Region = cur;
                    }
                    break;
                case DataTrend.Wave:
                    //Calc Average
                    Region = (Region_Time * Region + cur) / (Region_Time + 1);
                    switch (cur_trend)
                    {
                        case DataTrend.Stable:
                            if (Abrupt_Change_Time > 0)
                                Abrupt_Change_Time--;
                            if (Abrupt_Change_Time == 0)
                            {
                                Region_trend = DataTrend.Stable;
                                TrendStableTime = 0;
                            }
                            break;
                        case DataTrend.AbruptChange:
                            if (Abrupt_Change_Time == 0)
                                Region_Var = 0;
                            Region_Var = Math.Max(Math.Abs(cur - Region), Region_Var);//Expend Var
                            Abrupt_Change_Time++;
                            break;
                    }
                    break;
                case DataTrend.Stable:
                    switch (cur_trend)
                    {
                        case DataTrend.Stable:
                            lastDelta = 0;//Clear delta
                            if (Abrupt_Change_Time > 0)
                                Abrupt_Change_Time--;

                            var avg = (Region_Time * Region + cur) / (Region_Time + 1);

                            var delta = Region - avg;
                            if (Math.Abs(delta) > 0.01)
                                Region_RiseOrDec += delta < 0 ? 1 : -1;//Check if it is raising or falling
                            Region = avg;
                            break;
                        case DataTrend.AbruptChange:
                            delta = Math.Abs(cur - Region);
                            if (delta >= lastDelta)//getting far away from or keep above avg
                                Abrupt_Change_Time++;
                            lastDelta = delta;
                            break;
                    }
                    if (Math.Abs(Region_RiseOrDec) > 3 / DataSpan.TotalSeconds)
                    {
                        Region_trend = Region_RiseOrDec > 0 ? DataTrend.Rise : DataTrend.Decline;
                        TrendStableTime = 0;
                    }
                    if (Abrupt_Change_Time > 5 / DataSpan.TotalSeconds)
                    {
                        Region_trend = DataTrend.Wave;
                        Abrupt_Change_Time = 0;
                        Region_RiseOrDec = 0;
                        lastDelta = 0;
                        TrendStableTime = 0;
                        Region = cur;
                    }
                    break;
            }
            if (TrendStableTime * DataSpan.TotalSeconds > SingleInstanceManager.Instance.cfg.TrendStableTime)
            {
                General = (int)Region;
                General_Trend = Region_trend;
            }
            if (RegionOld)
            {
                Region_RiseOrDec = 0;
                Abrupt_Change_Time = 0;
                Region_Time = 0;
                lastDelta = 0;
            }
        }
    }
    public enum DataTrend
    {
        //General
        Rise,
        Decline,
        Stable,
        Wave,
        //Small region
        AbruptChange,
    }
}
