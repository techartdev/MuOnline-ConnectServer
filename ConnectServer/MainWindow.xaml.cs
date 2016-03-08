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

        private bool editingServer = false;

        public MainWindow()
        {
            InitializeComponent();

            GSList = new List<GameServerItem>();
            LoadSavedData();
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
                serverList.ItemsSource = null;
                serverList.ItemsSource = GSList;
            }
        }

        private List<GameServerItem> LoadGSList(string path)
        {
            var stream = File.OpenRead(path);
            var xml = new XmlSerializer(typeof(List<GSSaveList>));
            List<GSSaveList> saveList = (List<GSSaveList>)xml.Deserialize(stream);
            stream.Close();

            List<GameServerItem> gsList = new List<GameServerItem>();
            foreach (GSSaveList slItem in saveList)
            {
                GameServerItem gsi = new GameServerItem
                {
                    ServerName = slItem.ServerName,
                    ServerCode = slItem.ServerCode,
                    IPAddress = IPAddress.Parse(slItem.IPAddress),
                    Port = slItem.Port,
                    IsHidden = slItem.Show
                };

                gsList.Add(gsi);
            }

            return gsList;
        }

        private void SaveGSList(string filePath)
        {
            List<GSSaveList> saveList = new List<GSSaveList>();

            foreach (GameServerItem gsi in GSList)
            {
                GSSaveList gssl = new GSSaveList
                {
                    ServerCode = gsi.ServerCode,
                    ServerName = gsi.ServerName,
                    IPAddress = gsi.IPAddress.ToString(),
                    Port = gsi.Port,
                    Show = gsi.IsHidden
                };

                saveList.Add(gssl);
            }

            var stream = File.Create(filePath);
            var xml = new XmlSerializer(saveList.GetType());
            xml.Serialize(stream, saveList);
            stream.Close();
        }

        private void ServerTimer_Tick(object sender, EventArgs e)
        {

        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            if (GSList.Count == 0)
            {
                MessageBox.Show("Please, add at least 1 GameServer !", "Start ConnectServer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            serverIP = IPAddress.Parse(csIpAddress.Text);
            MaxConnections = Convert.ToInt32(maxConnections.Text);

            string txt = "CSIPAddress=" + serverIP.ToString() + Environment.NewLine;
            txt += "MaxConnections=" + MaxConnections;

            File.WriteAllText("CSSettings.dat", txt);

            MessageBox.Show("ConnectServer configuration saved !", "Save Changes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            if (!editingServer)
            {
                GameServerItem gsitem = new GameServerItem
                {
                    ServerName = serverName.Text,
                    ServerCode = Convert.ToInt32(serverCode.Text),
                    IPAddress = IPAddress.Parse(serverIp.Text),
                    Port = Convert.ToInt32(serverPort.Text),
                    IsHidden = showServer.IsChecked.Value
                };

                GSList.Add(gsitem);

                serverList.ItemsSource = null;
                serverList.ItemsSource = GSList;

                SaveGSList("ServerList.xml");
            }
            else
            {
                int srvCode = Convert.ToInt32(serverCode.Text);
                GameServerItem gsitem = GSList.FirstOrDefault(wr => wr.ServerCode == srvCode);

                if (gsitem == null)
                {
                    addServer.Content = "Add Server";
                    editingServer = false;
                    cancelEdit.Visibility = Visibility.Hidden;
                    return;
                }

                int index = GSList.IndexOf(gsitem);


                gsitem.ServerName = serverName.Text;
                gsitem.ServerCode = Convert.ToInt32(serverCode.Text);
                gsitem.IPAddress = IPAddress.Parse(serverIp.Text);
                gsitem.Port = Convert.ToInt32(serverPort.Text);
                gsitem.IsHidden = showServer.IsChecked.Value;

                GSList.RemoveAt(index);
                GSList.Insert(index, gsitem);

                serverList.ItemsSource = null;
                serverList.ItemsSource = GSList;

                SaveGSList("ServerList.xml");

                addServer.Content = "Add Server";
                editingServer = false;
                cancelEdit.Visibility = Visibility.Hidden;
            }

            ClearConfigControls();
        }

        private void editConfigMI_Click(object sender, RoutedEventArgs e)
        {
            if (serverList.SelectedItems.Count == 0)
                return;

            GameServerItem gsi = serverList.SelectedItem as GameServerItem;

            tabControl.SelectedIndex = 1;

            serverName.Text = gsi.ServerName;
            serverCode.Text = gsi.ServerCode.ToString();
            serverIp.Text = gsi.IPAddress.ToString();
            serverPort.Text = gsi.Port.ToString();
            showServer.IsChecked = gsi.IsHidden;
            hideServer.IsChecked = !gsi.IsHidden;

            addServer.Content = "Save Changes";
            editingServer = true;
            cancelEdit.Visibility = Visibility.Visible;
        }

        private void serverList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (serverList.SelectedItems.Count == 0)
                return;

            GameServerItem gsi = serverList.SelectedItem as GameServerItem;

            tabControl.SelectedIndex = 1;

            serverName.Text = gsi.ServerName;
            serverCode.Text = gsi.ServerCode.ToString();
            serverIp.Text = gsi.IPAddress.ToString();
            serverPort.Text = gsi.Port.ToString();
            showServer.IsChecked = gsi.IsHidden;
            hideServer.IsChecked = !gsi.IsHidden;

            addServer.Content = "Save Changes";
            editingServer = true;
            cancelEdit.Visibility = Visibility.Visible;
        }

        private void ClearConfigControls()
        {
            serverName.Clear();
            serverCode.Clear();
            serverIp.Clear();
            serverPort.Clear();
            showServer.IsChecked = true;
        }

        private void removeServerMI_Click(object sender, RoutedEventArgs e)
        {
            if (serverList.SelectedItems.Count == 0)
                return;

            GameServerItem gsi = serverList.SelectedItem as GameServerItem;

            if (MessageBox.Show(string.Format("Server [{0}] with Code [{1}] will be removed. Continue ?",
                gsi.ServerName, gsi.ServerCode), "Delete GameServer", MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            GSList.Remove(gsi);

            serverList.ItemsSource = null;
            serverList.ItemsSource = GSList;

            SaveGSList("ServerList.xml");
        }

        private void cancelEdit_Click(object sender, RoutedEventArgs e)
        {
            addServer.Content = "Add Server";
            editingServer = false;
            cancelEdit.Visibility = Visibility.Hidden;
            ClearConfigControls();
        }
    }
}
