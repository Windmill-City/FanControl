using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;

namespace FanControl
{
    public class FanTable
    {
        public struct TD//Temp and Duty
        {
            public int T;
            public int D;
            public TD(int temp, int duty)
            {
                T = temp;
                D = duty;
            }
            public TD(double temp, double duty)
            {
                T = (int)temp;
                D = (int)duty;
            }
        }
        //Single instance
        static HashMap<int, FanTable> tables = new HashMap<int, FanTable>();
        //TD datas
        public LinkedList<TD> points = new LinkedList<TD>();

        public string TableSection;
        public string SettingSection;

        NumberCheck check = new NumberCheck(100, 0);
        Config config = SingleInstanceManager.Instance.cfg;

        //Settings
        bool? _isFixed;
        public bool isFixed
        {
            get
            {
                if (_isFixed == null)
                    try
                    {
                        var value = config.getValue(SettingSection, "Fixed");
                        _isFixed = Convert.ToBoolean(value);
                    }
                    catch (Exception)
                    {
                        _isFixed = false;
                        isFixed = (bool)_isFixed;
                    }
                return (bool)_isFixed;
            }
            set
            {
                _isFixed = value;
                config.saveValue(SettingSection, "Fixed", value);
            }
        }
        int? Cpu_prop;
        public int CpuProportion
        {
            get
            {
                if (Cpu_prop == null)
                    try
                    {
                        var value = config.getValue(SettingSection, "CpuProportion");
                        Cpu_prop = Convert.ToInt32(value);
                        Assert.IsTrue(Cpu_prop >= 0);
                    }
                    catch (Exception)
                    {
                        Cpu_prop = 1;
                        CpuProportion = (int)Cpu_prop;
                    }
                return (int)Cpu_prop;
            }
            set
            {
                Cpu_prop = value;
                config.saveValue(SettingSection, "CpuProportion", value);
            }
        }
        int? Gpu_1_prop;
        public int Gpu_1_Proportion
        {
            get
            {
                if (Gpu_1_prop == null)
                    try
                    {
                        var value = config.getValue(SettingSection, "Gpu_1_Proportion");
                        Gpu_1_prop = Convert.ToInt32(value);
                        Assert.IsTrue(Gpu_1_prop >= 0);
                    }
                    catch (Exception)
                    {
                        Gpu_1_prop = 1;
                        Gpu_1_Proportion = (int)Gpu_1_prop;
                    }
                return (int)Gpu_1_prop;
            }
            set
            {
                Gpu_1_prop = value;
                config.saveValue(SettingSection, "Gpu_1_Proportion", value);
            }
        }
        int? Gpu_2_prop;
        public int Gpu_2_Proportion
        {
            get
            {
                if (Gpu_2_prop == null)
                    try
                    {
                        var value = config.getValue(SettingSection, "Gpu_2_Proportion");
                        Gpu_2_prop = Convert.ToInt32(value);
                        Assert.IsTrue(Gpu_2_prop >= 0);
                    }
                    catch (Exception)
                    {
                        Gpu_2_prop = 0;
                        Gpu_2_Proportion = (int)Gpu_2_prop;
                    }
                return (int)Gpu_2_prop;
            }
            set
            {
                Gpu_2_prop = value;
                config.saveValue(SettingSection, "Gpu_2_Proportion", value);
            }
        }

        public static FanTable getFanTable(int FanNum)
        {
            FanTable table;
            lock (tables)
            {
                tables.TryGetValue(FanNum, out table);
                if (table == null)
                {
                    table = new FanTable(FanNum);
                    tables.Add(FanNum, table);
                }
            }
            return table;
        }
        FanTable(int FanNum)
        {
            var prefix = "Fan_" + FanNum + "_";
            SettingSection = prefix + "Setting";
            TableSection = prefix + "Table";
            LoadPoints();
        }
        void LoadPoints()
        {
            StringCollection keys = config.ReadSection(TableSection);
            double prev = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                var key_T = keys[i];
                i++;
                var key_D = keys[i];
                var T = config.getValue(TableSection, key_T);
                var D = config.getValue(TableSection, key_D);
                if (check.Validate(T, CultureInfo.CurrentCulture).IsValid && check.Validate(D, CultureInfo.CurrentCulture).IsValid)
                {
                    var duty = Convert.ToInt32(D);
                    var temp = Convert.ToInt32(T);
                    if (temp < prev)
                    {
                        LoadDefaultPoints();
                        break;
                    }
                    points.AddLast(new TD(Math.Min(temp, 100), Math.Min(duty, 100)));
                }
                else
                {
                    LoadDefaultPoints();
                    break;
                }
            }
            if (points.Count < 2)
                LoadDefaultPoints();
            savePoints();
        }

        public void savePoints()
        {
            config.ClearSection(TableSection);
            int i = 0;
            foreach (TD point in points)
            {
                i++;
                config.saveValue(TableSection, "T" + i, string.Format("{0:0}", point.T));
                config.saveValue(TableSection, "D" + i, string.Format("{0:0}", point.D));
            }
        }

        public void LoadDefaultPoints()
        {
            //Temp - Duty
            points.Clear();
            points.AddLast(new TD(20, 20));
            points.AddLast(new TD(40, 40));
            points.AddLast(new TD(70, 70));
            points.AddLast(new TD(85, 100));
        }
        public int Y_FromX(int x)
        {
            var E_Points = points.GetEnumerator();

            E_Points.MoveNext();
            TD prev = E_Points.Current;

            if (prev.T > x)
                return 0;

            TD next = new TD(101, 101);
            while (E_Points.MoveNext())
            {
                if (E_Points.Current.T > x)
                {
                    next = E_Points.Current;
                    break;
                }
                prev = E_Points.Current;
            }
            double k = (double)(prev.D - next.D) / (prev.T - next.T);
            return (int)(k * (x - prev.T) + prev.D);
        }
    }
}
