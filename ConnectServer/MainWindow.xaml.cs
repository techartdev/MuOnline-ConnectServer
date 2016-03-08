using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConnectServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CSSocket connectServer;

        public CSSocket supp1;
        public CSSocket supp2;

        public MainWindow()
        {
            InitializeComponent();
            connectServer = new CSSocket(IPAddress.Parse("192.168.1.50"), 44405, 20);
            connectServer.SendHello = true;
            connectServer.WriteLogs = true;
            connectServer.WriteDebugLogs = true;
            connectServer.ProtocolType = System.Net.Sockets.ProtocolType.Tcp;

        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            connectServer.Start();
            serverStatus.Background = Brushes.Green;
        }

        private void stopServer_Click(object sender, RoutedEventArgs e)
        {
            connectServer.Stop();
            serverStatus.Background = Brushes.Red;
        }

        private void killConnections_Click(object sender, RoutedEventArgs e)
        {
            connectServer.KillAllConnections();
        }
    }
}
