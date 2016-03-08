using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ConnectServer
{
    public class CSSocket
    {
        #region Public Properties
        public int Port { get; set; }
        
        public int MaxConnections { get; set; }

        public Dictionary<int, Connection> ConnectionList { get; set; }

        public IPAddress IPAddress { get; set; }

        public bool SendHello { get; set; }
        
        public bool WriteLogs { get; set; }
        
        public bool WriteDebugLogs { get; set; }

        public ProtocolType ProtocolType { get; set; }
        #endregion

        #region Private Properties
        private bool IsAlive = false;

        private Socket SocketServer;

        private MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        #endregion

        public CSSocket(IPAddress IPAddress, int Port, int MaxConnections)
        {
            this.IPAddress = IPAddress;
            this.Port = Port;
            this.MaxConnections = MaxConnections;
            ConnectionList = new Dictionary<int, Connection>();
        }

        /// <summary>
        /// Starts the socket listener
        /// </summary>
        public void Start()
        {
            try
            {
                if (WriteLogs)
                    Logs.WriteLog("black", "Starting Server");

                SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType);
                SocketServer.Bind(new IPEndPoint(IPAddress, Port));

                SocketServer.Listen(10);

                //FormControlUpdater.UpdateServerStatus(1);
                //FormControlUpdater.UpdateConnectionCount(ConnectionsList.Count);


                SocketServer.BeginAccept(new AsyncCallback(OnConnect), SocketServer);
                if (WriteLogs)
                    Logs.WriteLog("green", "Socket Listener Succesfully Running On Port [{0}]", Port);

                IsAlive = true;
            }
            catch (Exception Ex)
            {
                if (WriteDebugLogs)
                {
                    Logs.WriteLog("red", "Failed To Set Up Connection Listener On Port [{0}]", Port);
                    Logs.WriteLog("red", Ex.Message);
                }
            }
        }

        /// <summary>
        /// Stops the socket listener
        /// </summary>
        public void Stop()
        {
            if (WriteLogs)
                Logs.WriteLog("black", "Stoping Socket Listener");

            if (SocketServer != null)
            {
                SocketServer.Close();
                IsAlive = false;
            }

            KillAllConnections();
            SocketServer = null;

            if (WriteLogs)
                Logs.WriteLog("black", "All Connections Killed");
        }

        private void OnConnect(IAsyncResult ar)
        {
            if (IsAlive)
            {
                try
                {
                    Socket socket = SocketServer.EndAccept(ar);
                    IPAddress connectionIP = (socket.RemoteEndPoint as IPEndPoint).Address;

                    if (ConnectionList.Count == MaxConnections)
                    {
                        socket.Close();
                        if (WriteLogs)
                            Logs.WriteLog("black", "Refused Connection from IP [{0}] because the maximum connections count [{1}] is reached.", connectionIP, MaxConnections);
                    }
                    else
                    {
                        CreateConnection(socket);
                    }

                }
                catch (Exception Ex)
                {
                    if (WriteDebugLogs)
                        Logs.WriteLog("red", Ex.Message);
                }

                SocketServer.BeginAccept(new AsyncCallback(OnConnect), SocketServer);
            }
        }

        private void CreateConnection(Socket socket)
        {
            try
            {
                Connection conn = new Connection(socket, this);
                int index = Helpers.GetFirstIndexFromList(ConnectionList);
                conn.cIndex = index;
                ConnectionList.Add(index, conn);
                conn.StartReceiveData();
                //FormControlUpdater.UpdateConnectionCount(ConnectionsList.Count);
                if (WriteLogs)
                    Logs.WriteLog("green", "Created connection for IP [{0}]", conn.IpAddress);

                if (SendHello)
                {
                    ProtocolCore.SendHelloMessage(index);
                }
            }
            catch (Exception Ex)
            {
                if (WriteDebugLogs)
                    Logs.WriteLog("red", Ex.Message);
            }
        }

        /// <summary>
        /// Send data to multiple connections
        /// </summary>
        public void DataSend(byte[] Data, int[] cIndexes)
        {
            if (ConnectionList.Count == 0 || cIndexes.Length == 0 || Data.Length == 0)
                return;

            foreach (var conn in ConnectionList.Where(wr => cIndexes.Contains(wr.Key)))
                conn.Value.ConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
        }

        /// <summary>
        /// Send data to specific connection
        /// </summary>
        /// <param name="cIndex">Connection index</param>
        public void DataSend(byte[] Data, int cIndex)
        {
            if (ConnectionList.Count == 0 || Data.Length == 0)
                return;

            ConnectionList[cIndex].ConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
        }

        /// <summary>
        /// Send data to all alive connections
        /// </summary>
        public void DataSendAll(byte[] Data)
        {
            if (ConnectionList.Count == 0 || Data.Length == 0)
                return;

            foreach (var conn in ConnectionList)
            {
                conn.Value.ConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
            }
        }

        /// <summary>
        /// Kills all alive connections
        /// </summary>
        public void KillAllConnections()
        {
            try
            {
                foreach (var conn in ConnectionList)
                {
                    if (conn.Value.ConnectionSocket.Connected)
                    {
                        conn.Value.Close();
                    }
                }
                ConnectionList.Clear();
                // FormControlUpdater.UpdateConnectionCount(ConnectionsList.Count);
            }
            catch (Exception Ex)
            {
                if (WriteDebugLogs)
                    Logs.WriteLog("red", Ex.Message);
            }
        }

        /// <summary>
        /// Kill connection with specific index
        /// </summary>
        /// <param name="cIndex"></param>
        public void CloseConnection(int cIndex)
        {
            try
            {
                ConnectionList[cIndex].Close();
                ConnectionList.Remove(cIndex);
                if (WriteLogs)
                    Logs.WriteLog("black", "Closed Connection with index [{0}]", cIndex);
            }
            catch (Exception Ex)
            {
                if (WriteDebugLogs)
                    Logs.WriteLog("red", Ex.Message);
            }
        }

        /// <summary>
        /// Kill specific connection
        /// </summary>
        /// <param name="Connection"></param>
        public void CloseConnection(Connection Connection)
        {
            try
            {
                if (ConnectionList.Any(wr => wr.Value == Connection))
                {
                    Connection.Close();
                    ConnectionList.Remove(Connection.cIndex);

                    if (WriteLogs)
                        Logs.WriteLog("black", "Closed Connection with index [{0}] ", Connection.cIndex);
                }
                else
                {
                    if (WriteLogs)
                        Logs.WriteLog("black", "The connection can't be closed because is not existing.");
                }
            }
            catch (Exception Ex)
            {
                if (WriteDebugLogs)
                    Logs.WriteLog("red", Ex.Message);
            }
        }
    }
}
