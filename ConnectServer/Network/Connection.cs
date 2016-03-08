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

    public class Connection
    {
        public IPAddress IpAddress { get; set; }

        public int cIndex { get; set; }

        public byte[] DataBuffer { get; set; }

        public CSSocket ParentServer { get; set; }

        public Socket ConnectionSocket { get; set; }

        public Connection(Socket Socket, CSSocket Server)
        {
            DataBuffer = new byte[1024 * 512];
            ConnectionSocket = Socket;
            ParentServer = Server;
            IpAddress = (ConnectionSocket.RemoteEndPoint as IPEndPoint).Address;
        }

        public void StartReceiveData()
        {
            if (ConnectionSocket.Connected)
            {
                ConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveData), ConnectionSocket);
            }
        }

        public void EndReceive(IAsyncResult ar)
        {
            if (ConnectionSocket.Connected)
            {
                ConnectionSocket.EndReceive(ar);
            }
        }

        private void ReceiveData(IAsyncResult ar)
        {
            if (ConnectionSocket.Connected)
            {
                try
                {
                    if (ConnectionSocket.EndReceive(ar) > 0)
                    {
                        DataBuffer = Helpers.TrimNullByteData(DataBuffer);

                        if (ParentServer.WriteDebugLogs)
                            Logs.WriteLog("black", "Recived Data From Index [{0}] IP [{1}] Data : [{2}]", cIndex, IpAddress, Helpers.ByteArrayToString(DataBuffer));

                        ProtocolCore.ProcessCSPacket(this, DataBuffer);

                        Array.Clear(DataBuffer, 0, DataBuffer.Length);
                    }

                    ConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveData), ConnectionSocket);
                }
                catch (Exception Ex)
                {
                    if (ParentServer.WriteDebugLogs)
                    {
                        Logs.WriteLog("red", "Lost Connection with index [{0}]. Closing Connection..", cIndex);
                        Logs.WriteLog("red", Ex.Message);
                    }
                    this.Close();
                }
            }
        }

        public void SendData(byte[] Data)
        {
            try
            {
                ConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);

                if (ParentServer.WriteDebugLogs)
                    Logs.WriteLog("black", "Send Data To[{0}][{1}]", cIndex, IpAddress);

                Array.Clear(DataBuffer, 0, DataBuffer.Length);
            }
            catch (Exception Ex)
            {
                if (ParentServer.WriteDebugLogs)
                    Logs.WriteLog("red", Ex.Message);
            }
        }

        public void SendData()
        {
            DataBuffer = Helpers.TrimNullByteData(DataBuffer);
            ConnectionSocket.Send(DataBuffer, 0, DataBuffer.Length, SocketFlags.None);

            if (ParentServer.WriteDebugLogs)
                Logs.WriteLog("black", "Send Data To Index[{0}][{1}]", cIndex, IpAddress);

            Array.Clear(DataBuffer, 0, DataBuffer.Length);
        }

        public void Close()
        {
            try
            {
                ConnectionSocket.Shutdown(SocketShutdown.Both);
                ConnectionSocket.Close();

                if (ParentServer.WriteDebugLogs)
                    Logs.WriteLog("black", "Connection with index [{0}] closed.", cIndex);

                ParentServer.CloseConnection(this);
            }
            catch (Exception Ex)
            {
                if (ParentServer.WriteDebugLogs)
                    Logs.WriteLog("red", Ex.Message);
            }
        }

    }
}
