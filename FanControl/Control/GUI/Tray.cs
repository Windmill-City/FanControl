using FanControl.Control.Resources;
using System;
using System.Windows.Forms;

namespace FanControl
{
    public class Tray : IDisposable
    {
        NotifyIcon trayIcon;
        public void AddTrayIcon()
        {
            if (trayIcon != null)
            {
                return;
            }
            trayIcon = new NotifyIcon
            {
                Icon = Resource.Fan,
                Text = "FanControl"
            };
            trayIcon.Visible = true;
            trayIcon.MouseDoubleClick += onTrayClick;

            #region ContextMenu
            ContextMenu menu = new ContextMenu();

            MenuItem exit = new MenuItem("Exit", onExitClick);
            menu.MenuItems.Add(exit);

            trayIcon.ContextMenu = menu;
            #endregion
        }

        private void onTrayClick(object sender, MouseEventArgs e)
        {
            SingleInstanceManager.Instance.ShowMainWindow();
        }

        private void onExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void RemoveTrayIcon()
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
        }

        public void Dispose()
        {
            RemoveTrayIcon();
        }
    }
}
