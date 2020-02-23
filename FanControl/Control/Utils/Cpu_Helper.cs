using System;
using System.Diagnostics;
using System.Management;


public class Cpu_Helper
{
    PerformanceCounter Cpu_Counter;
    public string Name;

    public Cpu_Helper()
    {
        Cpu_Counter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
        ManagementObjectSearcher Searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
        foreach (ManagementObject obj in Searcher.Get())
        {
            var cpuName = obj["Name"].ToString();
            char[] chs = { ' ' };
            string[] res = cpuName.Split(chs, options: StringSplitOptions.RemoveEmptyEntries);
            Name = res[0] + " " + res[2];
            break;
        }
    }

    private CounterSample oldValue;
    private CounterSample newValue;

    public int GetCPUTotalUsage()
    {
        int num = 0;
        try
        {
            if (this.newValue.BaseValue == 0L)
            {
                this.newValue = Cpu_Counter.NextSample();
                return 0;
            }
            this.oldValue = this.newValue;
            this.newValue = Cpu_Counter.NextSample();
            float num2 = CounterSample.Calculate(this.oldValue, this.newValue);
            num = (int)num2;
            if (num2 * 10f % 10f >= 5f)
            {
                num = (int)num2 + 1;
            }
            if (num > 100)
            {
                num = 100;
            }
        }
        catch (Exception)
        {
            num = -1;
        }
        return num;
    }
}

