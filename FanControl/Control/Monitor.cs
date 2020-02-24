using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows.Media;

namespace FanControl
{
    public class Monitor : IDisposable
    {
        public MonitorData Plt_P;
        public MonitorData Cpu_T;
        public MonitorData Cpu_P;
        public MonitorData Gpu_1_Temp;
        public MonitorData Gpu_1_Power;
        public MonitorData Gpu_2_Temp;
        public MonitorData Gpu_2_Power;

        public MonitorData Fan_1;
        public MonitorData Fan_2;
        public MonitorData Fan_3;

        Thread MonitorThread;

        EC ec;

        int Update_Interval;
        double Power_Unit;
        double Enery_Status_Unit;
        bool Sys_Sleep = false;

        public Monitor(EC ec, int Gpu_Num, int FanNum, TimeSpan updatespan, TimeSpan duration)
        {
            this.ec = ec;
            Update_Interval = (int)updatespan.TotalMilliseconds;
            if (!(Gpu_Num <= 2 && Gpu_Num >= 0))
            {
                throw new ArgumentException("Gpu_Num");
            }
            //Intel Power unit
            if (SingleInstanceManager.Instance.supportMSR)
            {
                uint index = 0x606, eax = 0, edx = 0;
                SingleInstanceManager.Instance.ols.Rdmsr(index, ref eax, ref edx);
                Power_Unit = Math.Pow(0.5, eax & 0xF);
                Enery_Status_Unit = Math.Pow(0.5, (eax >> 8) & 0xF);

                //Init Power data
                getPowerByIndex(0x64D);
                getPowerByIndex(0x611);

                if (lastJ[1] != 0)//0 means not support
                    Plt_P = new MonitorData("Plt_P", updatespan, new SolidColorBrush(Colors.DarkSlateGray), duration);
            }
            //Cpu Monitor
            Cpu_T = new MonitorData("Cpu_T", updatespan, new SolidColorBrush(Colors.Red), duration);
            if (SingleInstanceManager.Instance.supportMSR)
                Cpu_P = new MonitorData("Cpu_P", updatespan, new SolidColorBrush(Colors.DarkRed), duration);
            //Gpu Monitor
            if (Gpu_Num > 0)
            {
                Gpu_1_Temp = new MonitorData("Gpu 1_T", updatespan, new SolidColorBrush(Colors.Orange), duration);
                if (SingleInstanceManager.Instance.supportNVSMI)
                    Gpu_1_Power = new MonitorData("Gpu 1_P", updatespan, new SolidColorBrush(Colors.DarkOrange), duration);
                if (Gpu_Num == 2)
                {
                    Gpu_2_Temp = new MonitorData("Gpu 2_T", updatespan, new SolidColorBrush(Colors.Blue), duration);
                    if (SingleInstanceManager.Instance.supportNVSMI)
                        Gpu_2_Power = new MonitorData("Gpu 2_P", updatespan, new SolidColorBrush(Colors.DarkBlue), duration);
                }
            }

            Fan_1 = new MonitorData("Fan 1", updatespan, new SolidColorBrush(Colors.Green), duration);
            if (FanNum >= 2)
                Fan_2 = new MonitorData("Fan 2", updatespan, new SolidColorBrush(Colors.Chocolate), duration);
            if (FanNum == 3)
                Fan_3 = new MonitorData("Fan 3", updatespan, new SolidColorBrush(Colors.RoyalBlue), duration);

            //Listen Sleep Event
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            MonitorThread = new Thread(new ThreadStart(MonitorMethod));
            MonitorThread.Name = "MonitorThread";
            MonitorThread.Priority = ThreadPriority.Highest;
            MonitorThread.Start();
        }
        Timer Resume_Timer;
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    Sys_Sleep = true;
                    break;
                case PowerModes.Resume:
                    Resume_Timer = new Timer(new TimerCallback((obj) =>
                    {
                        if (SingleInstanceManager.Instance.supportMSR)
                        {
                            //Refresh Power data
                            getPowerByIndex(0x64D);
                            getPowerByIndex(0x611);
                        }
                        Sys_Sleep = false;
                        Resume_Timer.Dispose();
                    }), null, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
                    break;
            }
        }
        public void Dispose()
        {
            MonitorThread.Abort();
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        }

        private void MonitorMethod()
        {
            while (true)
            {
                if (!Sys_Sleep)
                {
                    ec.Update_ECLiveInfo();
                    Cpu_T.addData(ec.Cpu_Temp);
                    Plt_P.addData(getPowerByIndex(0x64D));
                    if (Cpu_P != null)
                    {
                        Cpu_P.addData(getPowerByIndex(0x611));
                    }
                    if (Gpu_1_Temp != null)
                    {
                        Gpu_1_Temp.addData(ec.Gpu1_Temp);
                        if (Gpu_1_Power != null)
                        {
                            Gpu_1_Power.addData(getNVPowerByIndex(0));
                        }
                    }
                    if (Gpu_2_Temp != null)
                    {
                        Gpu_2_Temp.addData(ec.Gpu2_Temp);
                        if (Gpu_1_Power != null)
                        {
                            Gpu_2_Power.addData(getNVPowerByIndex(1));
                        }
                    }
                    if (Fan_1 != null)
                        Fan_1.addData(ec.Fan_1.Duty);
                    if (Fan_2 != null)
                        Fan_2.addData(ec.Fan_2.Duty);
                    if (Fan_3 != null)
                        Fan_3.addData(ec.Fan_3.Duty);
                }
                Thread.Sleep(Update_Interval);
            }
        }
        double getNVPowerByIndex(int index)
        {
            uint power;
            NV_Queries.nv_getPowerUsageByIndex(index, out power);
            return Math.Round(power / 1000.0, 2);
        }

        int[] time = new int[2];
        uint[] lastJ = new uint[2];
        double getPowerByIndex(uint index)
        {
            uint eax = 0, edx = 0;
            int i = Convert.ToInt32(index == 0x64D);
            SingleInstanceManager.Instance.ols.Rdmsr(index, ref eax, ref edx);

            uint delta = lastJ[i] > eax ? UInt32.MaxValue - lastJ[i] + eax : eax - lastJ[i];
            lastJ[i] = eax;

            var span = Math.Abs(Environment.TickCount - time[i]);
            time[i] = Environment.TickCount;

            return Math.Round(delta / span * 1000 * Enery_Status_Unit, 2);
        }
    }
}
