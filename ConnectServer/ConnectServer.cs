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

        public CSSocket ParentServer;
        public string IpAddress;
        public int Index;
        public Socket ConnectionSocket;
        public byte[] DataBuffer;

        public Connection(Socket Socket, CSSocket Server)
        {
            DataBuffer = new byte[1024 * 512];
            ConnectionSocket = Socket;
            ParentServer = Server;
            IpAddress = Regex.Match(ConnectionSocket.RemoteEndPoint.ToString(), "([0-9]+).([0-9]+).([0-9]+).([0-9]+)").Value;
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
                        DataBuffer = MiscFunctions.TrimNullByteData(DataBuffer);
                        Log.WriteLog(String.Format("Recived Data From[{0}][{1}][{2}]", Index, IpAddress, MiscFunctions.ByteArrayToString(DataBuffer)));
                        ProtocolCore.ProcessCSPacket(this, DataBuffer);

                        Array.Clear(DataBuffer, 0, DataBuffer.Length);
                    }

                    ConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveData), ConnectionSocket);
                }
                catch (Exception Ex)
                {
                    Log.WriteLog(String.Format("Lost Connection with index [{0}]. Closing Connection..", Index));
                    Log.WriteLog(Ex.Message);
                    this.Close();
                }
            }
        }

        public void SendData(byte[] Data)
        {
            try
            {
                ConnectionSocket.Send(Data, 0, Data.Length, SocketFlags.None);
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
            ConnectionSocket.Send(DataBuffer, 0, DataBuffer.Length, SocketFlags.None);
            Log.WriteLog(String.Format("Send Data To Index[{0}][{1}]", Index, IpAddress));
            Array.Clear(DataBuffer, 0, DataBuffer.Length);
        }

        public void Close()
        {
            try
            {
                ConnectionSocket.Shutdown(SocketShutdown.Both);
                ConnectionSocket.Close();
                Log.WriteLog(String.Format("Connection Closed Index[{0}]", Index));
                ParentServer.CloseConnection(this);
            }
            catch (Exception Ex)
            {
                Log.WriteLog(Ex.Message);
            }
        }

    }
}
