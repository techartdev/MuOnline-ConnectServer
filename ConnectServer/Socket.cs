using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConnectServer
{
    public class CSSocket
    {
        #region Public Properties
        [DefaultValue(44405)]
        public int Port { get; set; }

        [DefaultValue(20)]
        public int MaxConnections { get; set; }

        public Dictionary<int, Connection> ConnectionList { get; set; }

        public IPAddress IPAddress { get; set; }
        #endregion

        #region Private Properties
        private bool IsAlive = false;

        private Socket SocketServer;
        #endregion

        public CSSocket(IPAddress IPAddress, int Port, int MaxConnections)
        {
            this.IPAddress = IPAddress;
            this.Port = Port;
            this.MaxConnections = MaxConnections;
            ConnectionList = new Dictionary<int, Connection>();
        }

        public void Start()
        {
            try
            {
                Log.WriteLog("Starting Server");
                SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketServer.Bind(new IPEndPoint(IPAddress, Port));

                SocketServer.Listen(10);

                //FormControlUpdater.UpdateServerStatus(1);
                //FormControlUpdater.UpdateConnectionCount(ConnectionsList.Count);
                SocketServer.BeginAccept(new AsyncCallback(OnConnect), SocketServer);
                Log.WriteLog(String.Format("Socket Listener Succesfully Running On Port[{0}]", "green", Port));
                IsAlive = true;
            }
            catch (Exception Ex)
            {
                Log.WriteLog(String.Format("Failed To Set Up Connection Listener On Port [{0}]", "red", Port));
                Log.WriteLog(Ex.Message, "red");
            }
        }

        public void Stop()
        {
            Log.WriteLog("Stoping Socket Listener");

            if (SocketServer != null)
            {
                SocketServer.Close();
                IsAlive = false;
            }

            KillAllConnections();
            SocketServer = null;

            Log.WriteLog("All Connections Killed");
        }

        private void OnConnect(IAsyncResult ar)
        {
            if (IsAlive)
            {
                try
                {
                    Socket socket = SocketServer.EndAccept(ar);
                    string connectionIP = socket.RemoteEndPoint.ToString();

                    if (ConnectionList.Count == MaxConnections)
                    {
                        socket.Close();
                        Log.WriteLog(String.Format("Refused Connection Request From [{0}] Max Amount Of Connections [{1}] Has Been Reached.", connectionIP, MaxConnections));
                    }
                    else
                    {
                        CreateConnection(socket);
                    }

                }
                catch (Exception Ex)
                {
                    Log.WriteLog(Ex.Message);
                }

                SocketServer.BeginAccept(new AsyncCallback(OnConnect), SocketServer);
            }
        }

        private void CreateConnection(Socket socket)
        {
            try
            {
                Connection conn = new Connection(socket, this);
                int index = MiscFunctions.GetFirstIndexFromList(ConnectionList);
                conn.Index = index;
                ConnectionList.Add(index, conn);
                conn.StartReceiveData();
                //FormControlUpdater.UpdateConnectionCount(ConnectionsList.Count);
                Log.WriteLog("Created connection for IP [{0}]", "green", conn.IpAddress);

                ProtocolCore.SendHelloMessage(index);
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }

        public Connection GetInstance(int Index)
        {
            foreach (KeyValuePair<int, Connection> Item in ConnectionList)
            {
                if (Item.Key.Equals(Index))
                {
                    return Item.Value;
                }
            }
            Log.WriteLog(String.Format("Connection with index [{0}] is not existing.", "red", Index));
            return null;
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
                Log.WriteLog(Ex.Message, "red");
            }
        }

        public void CloseConnection(int cIndex)
        {
            try
            {
                ConnectionList[cIndex].Close();
                ConnectionList.Remove(cIndex);

                Log.WriteLog(string.Format("Closed Connection with index [{0}]", cIndex));
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message, "red");
            }
        }

        public void CloseConnection(Connection Connection)
        {
            try
            {
                if (ConnectionList.Any(wr => wr.Value == Connection))
                {
                    Connection.Close();
                    ConnectionList.Remove(Connection.Index);

                    Log.WriteLog(string.Format("Closed Connection with index [{0}] ", Connection.Index));
                }
                else
                {
                    Log.WriteLog("The connection can't be closed because is not existing.");
                }
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message, "red");
            }
        }
    }
}
