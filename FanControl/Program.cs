using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using OpenLibSys;
using System;
using System.Windows.Forms;

namespace FanControl
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceManager manager = new SingleInstanceManager();
            manager.Run(args);
        }
    }


    // Using VB bits to detect single instances and process accordingly:
    //  * OnStartup is fired when the first instance loads
    //  * OnStartupNextInstance is fired when the application is re-run again
    //    NOTE: it is redirected to this instance thanks to IsSingleInstance
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        public static SingleInstanceManager Instance;
        public Monitor monitor;
        public Controller controller;
        public EC ec;
        public MainWindow window;
        public Config cfg;
        public Tray tray;
        public Ols ols;
        public bool supportNVSMI;
        public bool supportMSR;

        public SingleInstanceManager()
        {
            this.IsSingleInstance = true;
            Instance = this;
        }

        protected override bool OnStartup(StartupEventArgs e)
        {
            // First time app is launched
            ec = new EC();
            cfg = new Config();
            tray = new Tray();
            ols = new Ols();

            supportMSR = ols.GetStatus() == (uint)Ols.Status.NO_ERROR && ols.GetDllStatus() == (uint)Ols.OlsDllStatus.OLS_DLL_NO_ERROR;
            supportNVSMI = InitNVSMI();
            InitMonitor();
            tray.AddTrayIcon();
            controller = new Controller(cfg.FanCount, TimeSpan.FromMilliseconds(cfg.PollSpan));

            SetAutoStart();

            Application.ApplicationExit += onExit;
            Application.Run();
            return false;
        }

        public void SetAutoStart()
        {
            string taskPath = "FanControl_AutoStart";
            if (cfg.isAutoStart)
            {
                TaskService service = TaskService.Instance;
                if (service.GetTask(taskPath) == null)
                {
                    TaskDefinition taskDef = service.NewTask();
                    taskDef.Principal.RunLevel = TaskRunLevel.Highest;
                    taskDef.Actions.Add(new ExecAction(Application.ExecutablePath));
                    taskDef.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromSeconds(10) });

                    service.RootFolder.RegisterTaskDefinition(taskPath, taskDef);
                }
            }
            else
            {
                TaskService service = new TaskService();
                service.RootFolder.DeleteTask(taskPath, false);
            }
        }
        public void InitMonitor()
        {
            if (monitor != null)
                monitor.Dispose();
            monitor = new Monitor(ec, cfg.GpuCount, cfg.FanCount, TimeSpan.FromMilliseconds(cfg.PollSpan), TimeSpan.FromMinutes(5));
        }

        public bool InitNVSMI()
        {
            bool result;
            try
            {
                result = NV_Queries.nv_init();
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            ShowMainWindow();
        }

        public void ShowMainWindow()
        {
            if (window == null)
                window = new MainWindow();
            window.Show();
            window.Activate();
        }

        private void onExit(object sender, EventArgs e)
        {
            controller.Dispose();
            monitor.Dispose();
            tray.Dispose();
            NV_Queries.nv_shutdown();
            if (ols != null)
                ols.Dispose();
            ec.SetFanModeAuto();
        }

        public void Restart()
        {
            Application.Restart();//Fully release resources
        }
    }
}
