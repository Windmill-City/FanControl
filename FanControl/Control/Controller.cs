using System;
using System.Collections.Generic;
using System.Threading;
using static FanControl.EC;

namespace FanControl
{
    public class Controller : IDisposable
    {
        LinkedList<FanTable> tables = new LinkedList<FanTable>();
        int Update_Interval;
        Thread Control_Thread;
        public bool ShouldUpdate = false;

        public Controller(int FanNum, TimeSpan updateSpan)
        {
            Update_Interval = (int)updateSpan.TotalMilliseconds;
            for (int i = 1; i <= FanNum; i++)
            {
                tables.AddLast(FanTable.getFanTable(i));
            }
            ShouldUpdate = SingleInstanceManager.Instance.cfg.FanMode == 4 ? true : false;
            Control_Thread = new Thread(new ThreadStart(update));
            Control_Thread.Name = "Control_Thread";
            Control_Thread.Start();

            SingleInstanceManager.Instance.ec.RaiseCustomEvent += onEC_Event;
        }
        private void onEC_Event(object sender, CustomEventArgs e)
        {
            Config config = SingleInstanceManager.Instance.cfg;
            EC ec = SingleInstanceManager.Instance.ec;
            lock (this)
            {
                int fanmode = 0;
                switch (e.Message)
                {
                    case "106"://MaxQ
                        fanmode = 5;
                        ec.SetWMI(121, 0, (uint)fanmode);
                        goto Save;
                    case "107"://EC Custom
                        fanmode = 6;
                        ec.SetWMI(121, 0, (uint)fanmode);
                        goto Save;
                    case "112"://EC Auto(default value)
                        if (config.FanMode == 4)
                            ShouldUpdate = true;//switch to our fan
                        else
                        {
                            fanmode = 0;
                            ec.SetWMI(121, 0, (uint)fanmode);
                        }
                        goto Save;
                    case "143"://Max
                        fanmode = 1;
                        if(config.FanMode == 4)
                            ec.SetWMI(121, 0, (uint)0);//need to set auto first,or it may struck sometime
                        ec.SetWMI(121, 0, (uint)fanmode);
                        goto Save;
                }
                return;
                Save:
                if (!(config.FanMode == 4))
                {
                    config.FanMode = fanmode;//Not our fan, save config
                }else if(fanmode != 0)
                {
                    ShouldUpdate = false;
                }
            }
        }

        void update()
        {
            while (true)
            {
                lock (this)
                {
                    if (ShouldUpdate)
                        updateFanDuty();
                }
                Thread.Sleep(Update_Interval);
            }
        }
        private void updateFanDuty()
        {
            var monitor = SingleInstanceManager.Instance.monitor;
            var ec = SingleInstanceManager.Instance.ec;
            var cpu = monitor.Cpu_T.General;
            var gpu1 = monitor.Gpu_1_Temp != null ? monitor.Gpu_1_Temp.General : 0;
            var gpu2 = monitor.Gpu_2_Temp != null ? monitor.Gpu_1_Temp.General : 0;
            int i = 0;
            UInt32 Command = 0;
            foreach (var table in tables)
            {
                i++;
                //weighted avg
                int sum = (table.CpuProportion + table.Gpu_1_Proportion + table.Gpu_2_Proportion);
                sum = sum == 0 ? 1 : sum;//prevent /0 exception
                int temp = (table.CpuProportion * cpu + table.Gpu_1_Proportion * gpu1 + table.Gpu_2_Proportion * gpu2) /
                    sum;
                int move = 0;
                Fan Current;
                switch (i)
                {
                    case 1:
                        Current = ec.Fan_1;
                        break;
                    case 2:
                        Current = ec.Fan_1;
                        move = 8;
                        break;
                    case 3:
                        Current = ec.Fan_1;
                        move = 16;
                        break;
                    default:
                        throw new ArgumentException("Illegal fan num");
                }
                int duty = table.Y_FromX(temp);
                duty = duty != 0 && duty < table.StartingDuty && Current.RPM == 0 && table.StartingDuty > 0 ? table.StartingDuty : duty;
                Command += (uint)((byte)(duty * 2.55) << move);
            }
            SingleInstanceManager.Instance.ec.SetWMI(104, 0, Command);
        }

        public void Dispose()
        {
            Control_Thread.Abort();
            SingleInstanceManager.Instance.ec.RaiseCustomEvent -= onEC_Event;
        }
    }
}
