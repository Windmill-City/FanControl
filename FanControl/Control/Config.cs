using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FanControl
{
    public class Config
    {
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retStr, int bufferSize, string filePath);
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, Byte[] buffer, int bufferSize, string filePath);
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        string lang = "en-US";
        private string path;
        int Size = 255;
        string Default = "null";
        string DefaultSection = "General";
        public bool isAutoStart
        {
            get
            {
                bool value;
                try
                {
                    value = Convert.ToBoolean(getValue(DefaultSection, "isAutoStart"));
                }
                catch (Exception e)
                {
                    value = false;
                    isAutoStart = value;
                }
                return value;
            }
            set
            {
                saveValue(DefaultSection, "isAutoStart", value);
            }
        }
        public int FanCount
        {
            get
            {
                int value;
                try
                {
                    value = Convert.ToInt32(getValue(DefaultSection, "FanCount"));
                    Assert.IsTrue(value > 0 && value <= 3, "Wrong Fan Count");
                }
                catch (Exception e)
                {
                    value = SingleInstanceManager.Instance.ec.EC_FanCount;
                    FanCount = value;
                }
                return value;
            }
            set
            {
                saveValue(DefaultSection, "FanCount", value);
            }
        }
        public int GpuCount
        {
            get
            {
                int value;
                try
                {
                    value = Convert.ToInt32(getValue(DefaultSection, "GpuCount"));
                    Assert.IsTrue(value >= 0 && value <= 2, "Wrong Gpu Count");
                }
                catch (Exception e)
                {
                    uint count;
                    if (SingleInstanceManager.Instance.supportNVSMI)
                        NV_Queries.nv_getCount(out count);
                    else
                        count = 0;
                    value = (int)count;
                    GpuCount = value;
                }
                return value;
            }
            set
            {
                saveValue(DefaultSection, "GpuCount", value);
            }
        }
        public int PollSpan
        {
            get
            {
                int value;
                try
                {
                    value = Convert.ToInt32(getValue(DefaultSection, "PollSpan"));
                    Assert.IsTrue(value > 0, "Nagetive value");
                }
                catch (Exception e)
                {
                    value = 1000;
                    PollSpan = value;
                }
                return value;
            }
            set
            {
                saveValue(DefaultSection, "PollSpan", value);
            }
        }
        int? _TrendStableTime;
        public int TrendStableTime
        {
            get
            {
                if (_TrendStableTime == null)
                    try
                    {
                        _TrendStableTime = Convert.ToInt32(getValue(DefaultSection, "TrendStableTime"));
                        Assert.IsTrue(_TrendStableTime > 0, "Nagetive value");
                    }
                    catch (Exception e)
                    {
                        _TrendStableTime = 5;
                        TrendStableTime = (int)_TrendStableTime;

                    }
                return (int)_TrendStableTime;
            }
            set
            {
                saveValue(DefaultSection, "TrendStableTime", value);
            }
        }
        public int FanMode
        {
            get
            {
                int value;
                try
                {
                    value = Convert.ToInt32(getValue(DefaultSection, "FanMode"));
                    Assert.IsTrue(value >= 0 && value <= 6, "Unknow Fan Mode");
                }
                catch (Exception e)
                {
                    value = 0;
                    FanMode = value;
                }
                return value;
            }
            set
            {
                saveValue(DefaultSection, "FanMode", value);
            }
        }
        string mtlanguageName = "mtlanguage.ini";
        string configName = "config.ini";
        public Config()
        {
            this.path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\";
            this.lang = CultureInfo.CurrentCulture.Name.ToLower();
            if (!File.Exists(path + mtlanguageName))
                File.Create(path + mtlanguageName);
            if (!File.Exists(path + configName))
                File.Create(path + configName);
        }
        public string Translation(string Text)
        {
            StringBuilder outStr = new StringBuilder(Size);
            GetPrivateProfileString(this.lang, Text, this.Default, outStr, this.Size, this.path + mtlanguageName);
            return outStr.ToString();
        }

        public string getValue(string section, string key)
        {
            StringBuilder outStr = new StringBuilder(Size);
            GetPrivateProfileString(section, key, this.Default, outStr, this.Size, this.path + configName);
            return outStr.ToString();
        }

        public void saveValue(string section, string key, object val)
        {
            WritePrivateProfileString(section, key, val.ToString(), this.path + configName);
        }

        public StringCollection ReadSection(string section)
        {
            Byte[] buffer = new Byte[Size];
            int bufLen = GetPrivateProfileString(section, null, this.Default, buffer, this.Size, this.path + configName);
            StringCollection keys = new StringCollection();
            int start = 0;
            for (int i = 0; i < bufLen; i++)
            {
                if ((buffer[i] == 0) && ((i - start) > 0))
                {
                    String s = Encoding.GetEncoding(0).GetString(buffer, start, i - start);
                    keys.Add(s);
                    start = i + 1;
                }
            }
            return keys;
        }

        public void ClearSection(string section)
        {
            WritePrivateProfileString(section, null, null, this.path + configName);
        }
    }
}
