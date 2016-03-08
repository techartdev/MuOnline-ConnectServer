using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConnectServer
{

    public class CSConnection
    {

        public ConnectServer ParentServer;
        public string IpAddress;
        public int Index;
        public Socket CSConnectionSocket;
        public byte[] DataBuffer;

        public CSConnection(Socket Socket, ConnectServer Server)
        {
            DataBuffer = new byte[1024 * 512];
            CSConnectionSocket = Socket;
            ParentServer = Server;
            IpAddress = Regex.Match(CSConnectionSocket.RemoteEndPoint.ToString(), "([0-9]+).([0-9]+).([0-9]+).([0-9]+)").Value;
        }

        public void StartReceiveData()
        {
            if (CSConnectionSocket.Connected)
            {
                CSConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveData), CSConnectionSocket);
            }
        }

        public void EndReceive(IAsyncResult IAr)
        {
            if (CSConnectionSocket.Connected)
            {
                CSConnectionSocket.EndReceive(IAr);
            }
        }

        private void ReceiveData(IAsyncResult IAr)
        {
            if (CSConnectionSocket.Connected)
            {
                try
                {
                    if (CSConnectionSocket.EndReceive(IAr) > 0)
                    {
                        DataBuffer = MiscFunctions.TrimNullByteData(DataBuffer);
                        Log.WriteLog(String.Format("Recived Data From[{0}][{1}][{2}]", Index, IpAddress, MiscFunctions.ByteArrayToString(DataBuffer)));
                        ProtocolCore.ProcessCSPacket(this, DataBuffer);
                        /////////////////////////////////////////////////////////////////////////////////
                        Array.Clear(DataBuffer, 0, DataBuffer.Length);
                    }
                    CSConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveData), CSConnectionSocket);
                }
                catch (Exception Ex)
                {
                    Log.WriteLog(String.Format("Lost CSConnection Index[{0}]. Closing CSConnection..", Index));
                    Log.WriteLog(Ex.Message);
                    this.Close();
                }
            }
        }

        public void SendData(byte[] Data)
        {
            try
            {
                CSConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
                Log.WriteLog(String.Format("Send Data To[{0}][{1}]", Index, IpAddress));
                Array.Clear(DataBuffer, 0, DataBuffer.Length);
            }
            catch (Exception Ex)
            {
                 Log.WriteLog(Ex.Message);
            }
        }

        public void SendData()
        {
            DataBuffer = MiscFunctions.TrimNullByteData(DataBuffer);
            CSConnectionSocket.Send(DataBuffer, 0, DataBuffer.Length, SocketFlags.None);
            Log.WriteLog(String.Format("Send Data To Index[{0}][{1}]", Index, IpAddress));
            Array.Clear(DataBuffer, 0, DataBuffer.Length);
        }

        public void Close()
        {
            try
            {

                CSConnectionSocket.Shutdown(SocketShutdown.Both);
                CSConnectionSocket.Close();
                Log.WriteLog(String.Format("CSConnection Closed Index[{0}]", Index));
                ParentServer.CloseCSConnection(this);


            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }

    }

    public class ConnectServer : IDisposable
    {
        private Socket SocketServer;
        private List<string> BlackList = new List<string>();
        int MaxCSConnections;
        int Port;
        IPAddress IPAddress;
        public Dictionary<int, CSConnection> CSConnectionsList = new Dictionary<int, CSConnection>();
        private bool IsListening = false;

        public ConnectServer(IPAddress IPAddress, int Port, int MaxCSConnections)
        {
            this.IPAddress = IPAddress;
            this.Port = Port;
            this.MaxCSConnections = MaxCSConnections;
        }

        public bool Start()
        {
            try
            {
                Log.WriteLog("Starting Socket Listener");
                SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketServer.Bind(new IPEndPoint(IPAddress, Port));

                SocketServer.Listen(10);
                CSConnectionsList = new Dictionary<int, CSConnection>();
                //FormControlUpdater.UpdateServerStatus(1);
                //FormControlUpdater.UpdateCSConnectionCount(CSConnectionsList.Count);
                SocketServer.BeginAccept(new AsyncCallback(CSConnectionRequest), SocketServer);
                Log.WriteLog(String.Format("Socket Listener Succesfully Running On Port[{0}]", Port));
                IsListening = true;
                return true;
            }
            catch (Exception Ex)
            {
                Log.WriteLog(String.Format("Failed To Set Up CSConnection Listener On Port [{0}]", Port));
                Log.WriteLog(Ex.Message);
                return false;
            }
        }

        public void Stop()
        {

            Log.WriteLog("Stoping Socket Listener");

            if (SocketServer != null)
            {
                SocketServer.Close();
                IsListening = false;
            }

            Log.WriteLog("Close All CSConnections");
            CloseAllCSConnections();
            SocketServer = null;
            //FormControlUpdater.UpdateServerStatus(0);
            //FormControlUpdater.UpdateCSConnectionCount(0);
        }

        public virtual void Shutdown()
        {
            if (IsListening)
            {
                Stop();
            }

            SocketServer = null;
        }

        public void Dispose()
        {
            try
            {
                GC.SuppressFinalize(this);
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }

        private void CSConnectionRequest(IAsyncResult IAr)
        {
            if (IsListening)
            {
                try
                {

                    Socket NewConectionSocket = SocketServer.EndAccept(IAr);
                    string NewCSConnectionIP = Regex.Match(NewConectionSocket.RemoteEndPoint.ToString(), "([0-9]+).([0-9]+).([0-9]+).([0-9]+)").Value;

                    if (CSConnectionsList.Count > MaxCSConnections)
                    {
                        NewConectionSocket.Close();
                        Log.WriteLog(String.Format("Refused CSConnection Request From [{0}] Max Amount Of CSConnections [{1}] Has Been Reached.", NewCSConnectionIP, MaxCSConnections));
                    }
                    else if (BlackList.Contains(NewCSConnectionIP))
                    {
                        NewConectionSocket.Close();
                        Log.WriteLog(String.Format("Refused CSConnection Request From [{0}] IP Address Is On The CSConnection BlackList.", NewCSConnectionIP));
                    }
                    else
                    {
                        CreateCSConnection(NewConectionSocket);
                    }

                }
                catch (Exception Ex)
                {
                    Log.WriteLog(Ex.Message);
                }
                SocketServer.BeginAccept(new AsyncCallback(CSConnectionRequest), SocketServer);
            }
        }

        private void CreateCSConnection(Socket NewCSConnectionSocket)
        {
            try
            {
                CSConnection CSConnection = new CSConnection(NewCSConnectionSocket, this);
                int Index = FindEmptyIndex();
                CSConnection.Index = Index;
                CSConnectionsList.Add(Index, CSConnection);
                CSConnection.StartReceiveData();
                //FormControlUpdater.UpdateCSConnectionCount(CSConnectionsList.Count);
                Log.WriteLog(String.Format("Created CSConnection Index [{0}] For [{1}]", CSConnection.Index, CSConnection.IpAddress));

                ProtocolCore.SendHelloMessage(Index);
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }

        public CSConnection GetInstance(int Index)
        {
            foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
            {
                if (Item.Key.Equals(Index))
                {
                    return Item.Value;
                }
            }
            Log.WriteLog(String.Format("CSConnection Index: [{0}] Doesn't Exist.", Index));
            return null;
        }

        private int FindEmptyIndex()
        {
            for (int i = 0; ; i++)
            {
                if (!CSConnectionsList.ContainsKey(i))
                {
                    return i;
                }
            }
        }

        public void SendDataToGroupCSConnections(byte[] Data, int[] Index)
        {
            if (CSConnectionsList.Count > 0)
            {
                if (Index.Length > 0)
                {
                    if (Data.Length > 0)
                    {
                        foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
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
            if (CSConnectionsList.Count > 0)
            {
                if (Data.Length > 0)
                {
                    foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
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
            if (CSConnectionsList.Count > 0)
            {
                if (Data.Length > 0)
                {
                    foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
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
                foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
                {
                    if (Item.Value.CSConnectionSocket.Connected)
                    {
                        Item.Value.Close();
                    }
                }
                CSConnectionsList.Clear();
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
                foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
                {
                    if (Item.Key.Equals(Index))
                    {
                        if (Item.Value.CSConnectionSocket.Connected)
                        {
                            Item.Value.Close();
                        }
                    }
                }
                CSConnectionsList.Remove(Index);
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
                if (CSConnectionsList.Count > 0)
                {
                    foreach (KeyValuePair<int, CSConnection> Item in CSConnectionsList)
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
                    CSConnectionsList.Remove(TakeIndex);
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
