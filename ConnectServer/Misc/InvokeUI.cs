using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ConnectServer
{
    public static class InvokeUI
    {
        public static MainWindow MainWindowInstance
        {
            get
            {
                MainWindow win = null;
                Application.Current.Dispatcher.Invoke(delegate
                {
                    win = (MainWindow)Application.Current.MainWindow;
                });

                return win;
            }
        }

        public static void UpdateConnectionsCount(int connections)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                MainWindowInstance.connectedUsers.Text = connections.ToString();
            });
        }
    }
}
