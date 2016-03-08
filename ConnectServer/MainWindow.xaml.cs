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

        public MainWindow()
        {
            InitializeComponent();
            connectServer = new CSSocket(IPAddress.Parse("192.168.1.50"), 44405, 20);
        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void stopServer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void killConnections_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
