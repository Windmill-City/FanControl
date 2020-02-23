using System;
using System.Management;

namespace FanControl
{
    public class EC
    {
        private static ManagementObject CLEVO = new ManagementObject("root\\WMI", "CLEVO_GET.InstanceName='ACPI\\PNP0C14\\0_0'", null);
        private ManagementEventWatcher watcher;

        public event EventHandler<EC.CustomEventArgs> RaiseCustomEvent;

        public byte[] ECData_WMI13;

        public BatteryInfo battery;

        public Fan Fan_1;
        public Fan Fan_2;
        public Fan Fan_3;

        public int Cpu_Temp;
        public int Gpu1_Temp;
        public int Gpu2_Temp;

        public long lastUpdate;

        public struct Fan
        {
            public byte Duty;
            public int RPM;
        }

        public struct BatteryInfo
        {
            public int TotalCurrent;

            public int Voltage;

            public int Current;
        }

        public int EC_FanCount
        {
            get
            {
                return ECData_WMI13 != null ? ECData_WMI13[12] : 0;
            }
        }

        public EC()
        {
            GetWMIPackage(13, "WMI_13", out ECData_WMI13);
            Update_ECLiveInfo();

            ManagementScope managementScope = new ManagementScope(string.Format("root\\WMI", Array.Empty<object>()));
            managementScope.Connect();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM CLEVO_EVENT");
            this.watcher = new ManagementEventWatcher(managementScope, query);
            this.watcher.EventArrived += this.watcher_EventArrived;
            this.watcher.Start();
        }

        private void watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject outParams = CLEVO.InvokeMethod("GetEvent", null, null);
                string s = outParams["Data"].ToString();
                this.OnRaiseCustomEvent(new EC.CustomEventArgs(s));
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while trying to execute the WMI method: " + ex.Message);
            }
        }
        protected virtual void OnRaiseCustomEvent(EC.CustomEventArgs e)
        {
            RaiseCustomEvent?.Invoke(this, e);
        }

        public void Update_ECLiveInfo()
        {
            byte[] data;
            if (GetWMIPackage(12, "WMI_12", out data))
            {

                Cpu_Temp = data[18];
                Gpu1_Temp = data[21];
                Gpu2_Temp = data[24];

                Fan_1.Duty = data[16];
                Fan_2.Duty = data[19];
                Fan_3.Duty = data[22];

                Fan_1.RPM = data[3] + (data[2] << 8);
                Fan_2.RPM = data[5] + (data[4] << 8);
                Fan_3.RPM = data[7] + (data[6] << 8);

                //battery.Voltage = data[7] + (data[6] << 8);
                //battery.Current = data[14] + (data[9] << 15);
                //battery.TotalCurrent = data[10] + (data[11] << 8) + data[12] + (data[13] << 8);

                //lastUpdate = DateTime.Now.Ticks;
            }
        }

        public void SetFanModeAuto()
        {
            SetWMI(121, 1, 0u);
        }

        public int SetWMI(int command, int SubCommand, uint data)
        {
            string methodName = null;
            data = (uint)((SubCommand << 24) + (int)data);
            string value = data.ToString();
            int num;
            try
            {
                switch (command)
                {
                    case 121:
                        methodName = "SystemControlFunction";
                        break;
                    case 118:
                        methodName = "TalkECTime";
                        break;
                    case 103:
                        methodName = "SetKBLED";
                        break;
                    case 104:
                        methodName = "SetFanDuty";
                        break;
                }
                ManagementBaseObject methodParameters = CLEVO.GetMethodParameters(methodName);
                methodParameters["Data"] = value;
                num = Convert.ToInt32(CLEVO.InvokeMethod(methodName, methodParameters, null)["Data1"].ToString());
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("An error occurred while trying to execute the WMI method: " + ex.Message);
                num = 0;
            }
            return num;
        }

        public bool GetWMIPackage(int command, string key, out byte[] data)
        {
            string methodName = null;
            try
            {
                switch (command)
                {
                    case 2:
                        methodName = "Package1";
                        break;
                    case 12:
                        methodName = "GetECLiveInfo";
                        break;
                    case 13:
                        methodName = "PackageReadEC";
                        break;
                    default:
                        data = null;
                        Console.WriteLine("Wrong command: ", command);
                        return false;
                }

                ManagementBaseObject outParams = CLEVO.InvokeMethod(methodName, null, null);
                data = (byte[])((ManagementBaseObject)outParams["Data"]).GetPropertyValue("bytes");
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("An error occurred while trying to execute the WMI method: " + ex.Message);
                data = null;
                return false;
            }
            return true;
        }
        public class CustomEventArgs : EventArgs
        {
            public string Message
            {
                get
                {
                    return this.message;
                }
                set
                {
                    this.message = value;
                }
            }

            public CustomEventArgs(string s)
            {
                this.message = s;
            }

            private string message;
        }
    }
}
