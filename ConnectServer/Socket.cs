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

        public Dictionary<int, CSConnection> ConnectionList { get; set; }

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
            ConnectionList = new Dictionary<int, CSConnection>();
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
                //FormControlUpdater.UpdateCSConnectionCount(CSConnectionsList.Count);
                SocketServer.BeginAccept(new AsyncCallback(OnConnect), SocketServer);
                Log.WriteLog(String.Format("Socket Listener Succesfully Running On Port[{0}]", Port));
                IsAlive = true;
            }
            catch (Exception Ex)
            {
                Log.WriteLog(String.Format("Failed To Set Up CSConnection Listener On Port [{0}]", Port));
                Log.WriteLog(Ex.Message);
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

            Log.WriteLog("Close All CSConnections");
            CloseAllCSConnections();
            SocketServer = null;
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
                        Log.WriteLog(String.Format("Refused CSConnection Request From [{0}] Max Amount Of CSConnections [{1}] Has Been Reached.", NewCSConnectionIP, MaxCSConnections));
                    }
                    else
                    {
                        CreateCSConnection(socket);
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
                CSConnection conn = new CSConnection(socket, this);
                int index = MiscFunctions.GetFirstIndexFromList(ConnectionList);
                conn.Index = index;
                ConnectionList.Add(index, conn);
                conn.StartReceiveData();
                //FormControlUpdater.UpdateCSConnectionCount(CSConnectionsList.Count);
                Log.WriteLog("Created connection for IP [{0}]", "green", conn.IpAddress);

                ProtocolCore.SendHelloMessage(index);
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }

        public CSConnection GetInstance(int Index)
        {
            foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
            {
                if (Item.Key.Equals(Index))
                {
                    return Item.Value;
                }
            }
            Log.WriteLog(String.Format("CSConnection Index: [{0}] Doesn't Exist.", Index));
            return null;
        }

        public void SendDataToGroupCSConnections(byte[] Data, int[] Index)
        {
            if (ConnectionList.Count > 0)
            {
                if (Index.Length > 0)
                {
                    if (Data.Length > 0)
                    {
                        foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
                        {
                            foreach (int Elements in Index)
                            {
                                if (Item.Key.Equals(Elements))
                                {
                                    Item.Value.CSConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);

                                }
                            }
                        }
                    }
                }
            }
        }

        public bool SendDataToOneCSConnection(byte[] Data, int Index)
        {
            if (ConnectionList.Count > 0)
            {
                if (Data.Length > 0)
                {
                    foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
                    {
                        if (Item.Key.Equals(Index))
                        {
                            Item.Value.CSConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
                            Log.WriteLog(String.Format("Sent Data to connection [{0}][{1}]", Index, MiscFunctions.ByteArrayToString(Data)));
                            return true;
                        }
                    }
                }
            }

            Log.WriteLog(String.Format("Can't Find Connection [{0}] Send Data To One CSConnection Fail", Index));
            return false;
        }

        public void SendDataToAllCSConnections(byte[] Data)
        {
            if (ConnectionList.Count > 0)
            {
                if (Data.Length > 0)
                {
                    foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
                    {
                        Item.Value.CSConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
                    }
                }
            }
        }

        public void CloseAllCSConnections()
        {
            try
            {
                Log.WriteLog(String.Format("Preparing To Dispose All CSConnection On Server Port[{0}]", Port));
                foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
                {
                    if (Item.Value.CSConnectionSocket.Connected)
                    {
                        Item.Value.Close();
                    }
                }
                ConnectionList.Clear();
                Log.WriteLog("All CSConnections Disposed Sucessfully");
                // FormControlUpdater.UpdateCSConnectionCount(CSConnectionsList.Count);
            }
            catch (Exception Ex)
            {

                Log.WriteLog(Ex.Message);
            }
        }

        public void CloseCSConnection(int Index)
        {
            try
            {
                foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
                {
                    if (Item.Key.Equals(Index))
                    {
                        if (Item.Value.CSConnectionSocket.Connected)
                        {
                            Item.Value.Close();
                        }
                    }
                }
                ConnectionList.Remove(Index);
                Log.WriteLog(string.Format("Closed CSConnection Index[{0}]", Index));
            }
            catch (Exception Ex)
            {

                Log.WriteLog(Ex.Message);
            }
        }

        public void CloseCSConnection(CSConnection CSConnection)
        {
            try
            {
                int TakeIndex = -1;
                if (ConnectionList.Count > 0)
                {
                    foreach (KeyValuePair<int, CSConnection> Item in ConnectionList)
                    {
                        if (Item.Value.Equals(CSConnection))
                        {
                            if (Item.Value.CSConnectionSocket.Connected)
                            {

                                Item.Value.Close();
                            }
                            TakeIndex = Item.Key;
                        }
                    }
                    ConnectionList.Remove(TakeIndex);
                    Log.WriteLog(string.Format("Closed CSConnection Index[{0}] ", TakeIndex));
                    //FormControlUpdater.UpdateCSConnectionCount(CSConnectionsList.Count);
                }
                else
                {
                    Log.WriteLog("Trying To Close Not Existing CSConnection");
                }
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }
    }
}
