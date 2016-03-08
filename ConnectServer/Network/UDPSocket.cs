using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectServer
{
    public class UDPSocket
    {
        public int Port { get; set; }

        public bool IsAlive { get; set; }

        public bool WriteLogs { get; set; }

        public bool WriteDebugLogs { get; set; }

        private UdpClient udpServer;

        private IPEndPoint serverEndPoint;

        public UDPSocket(int Port)
        {
            this.Port = Port;
            serverEndPoint = new IPEndPoint(IPAddress.Any, Port);
        }

        public void Start()
        {
            IsAlive = true;
            udpServer = new UdpClient(serverEndPoint);
            
            udpServer.BeginReceive(new AsyncCallback(ReceiveData), udpServer);

            if (WriteLogs)
                Logs.WriteLog("black", "UDP Socket initialized on Port [{0}]", Port);
        }

        private void ReceiveData(IAsyncResult ar)
        {
            if (IsAlive)
            {
                try
                {
                    UdpClient socket = (UdpClient)ar.AsyncState;
                
                    byte[] data = socket.EndReceive(ar, ref serverEndPoint);

                    ProtocolCore.ProcessUDPPacket(data);

                    if (WriteDebugLogs)
                        Logs.WriteLog("black", "Recived Data : [{0}]", Helpers.ByteArrayToString(data));

                    udpServer.BeginReceive(new AsyncCallback(ReceiveData), socket);
                }
                catch (Exception Ex)
                {
                    if (WriteDebugLogs)
                        Logs.WriteLog("red", Ex.Message);
                }
            }
        }

        public void Stop()
        {
            IsAlive = false;

            if (WriteLogs)
                Logs.WriteLog("black", "UDP Socket on Port [{0}] stopped", Port);
        }
    }
}
