using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;
using System.Xml.Serialization;

namespace ConnectServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CSSocket connectServer;
        public CSSocket TCPRecv;
        public UDPSocket udpServer;

        private DispatcherTimer serverTimer;

        public List<GameServerItem> GSList;
        public bool JoinServerAlive = false;
        public int MaxConnections = 20;
        public IPAddress serverIP = IPAddress.Parse("127.0.0.1");

        public int CSPort = 44405;
        public int TCPRecvPort = 55558;
        public int UDPPort = 55557;

        public MainWindow()
        {
            InitializeComponent();

            GSList = new List<GameServerItem>();

            connectServer = new CSSocket(serverIP, CSPort, MaxConnections);
            connectServer.SendHello = true;
            connectServer.WriteLogs = true;
            connectServer.WriteDebugLogs = false;
            connectServer.ProtocolType = System.Net.Sockets.ProtocolType.Tcp;

            TCPRecv = new CSSocket(serverIP, TCPRecvPort, MaxConnections);
            TCPRecv.SendHello = false;
            TCPRecv.WriteLogs = true;
            TCPRecv.WriteDebugLogs = false;
            TCPRecv.ProtocolType = System.Net.Sockets.ProtocolType.Tcp;

            udpServer = new UDPSocket(UDPPort);
            udpServer.WriteLogs = true;
            udpServer.WriteDebugLogs = false;

            serverTimer = new DispatcherTimer();
            serverTimer.Tick += ServerTimer_Tick;
            serverTimer.Interval = new TimeSpan(0, 0, 3); // tick every 3 seconds to check are servers alive   
        }

        private void LoadSavedData()
        {
            if (File.Exists("CSSettings.dat"))
            {
                string[] lines = File.ReadAllLines("CSSettings.dat");

                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string prop = line.Split('=')[0].Trim();
                    string val = line.Split('=')[1].Trim();

                    switch (prop)
                    {
                        case "CSIPAddress":
                            serverIP = IPAddress.Parse(val);
                            csIpAddress.Text = val;
                            break;
                        case "MaxConnections":
                            MaxConnections = Convert.ToInt32(val);
                            maxConnections.Text = val;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (File.Exists("ServerList.xml"))
            {
                GSList = LoadGSList("ServerList.xml");
                serverList.ItemsSource = GSList;
            }
        }

        private List<GameServerItem> LoadGSList(string path)
        {
            var stream = File.OpenRead(path);
            var xml = new XmlSerializer(typeof(List<GameServerItem>));
            List<GameServerItem> campaign = (List<GameServerItem>)xml.Deserialize(stream);
            stream.Close();
            return campaign;
        }

        private void SaveGSList(string filePath)
        {
            var stream = File.Create(filePath);
            var xml = new XmlSerializer(GSList.GetType());
            xml.Serialize(stream, GSList);
            stream.Close();
        }

        private void ServerTimer_Tick(object sender, EventArgs e)
        {

        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            connectServer.Start();
            TCPRecv.Start();
            udpServer.Start();
            serverStatus.Background = Brushes.Green;
        }

        private void stopServer_Click(object sender, RoutedEventArgs e)
        {
            connectServer.Stop();
            TCPRecv.Stop();
            udpServer.Stop();
            serverStatus.Background = Brushes.Red;
        }

        private void killConnections_Click(object sender, RoutedEventArgs e)
        {
            connectServer.KillAllConnections();
        }

        private void saveChanges_Click(object sender, RoutedEventArgs e)
        {
            File.Create("CSSettings.dat").Close();

            string txt = "CSIPAddress=" + serverIP.ToString() + Environment.NewLine;
            txt += "MaxConnections=" + MaxConnections;

            File.WriteAllText("CSSettings.dat", txt);
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            GameServerItem gsitem = new GameServerItem
            {
                ServerName = serverName.Text,
                ServerCode = Convert.ToInt32(serverCode.Text),
                IPAddress = IPAddress.Parse(serverIp.Text),
                Port = Convert.ToInt32(serverPort.Text),
                Percent = 1,
                MaxUserCount = 100,
                UserCount = 1,
                IsHidden = showServer.IsChecked.Value,
                IsAlive = true
            };

            GSList.Add(gsitem);
            serverList.ItemsSource = GSList;

            //SaveGSList("ServerList.xml");
        }
    }
}
