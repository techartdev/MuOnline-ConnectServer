using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ConnectServer
{
    enum CSHeaders
    {
        CS_SERVER_SELECT = 0x03,
        CS_CLIENT_CONNECT = 0x06
    };

    public static class ProtocolCore
    {
        public static void ProcessCSPacket(Connection conn, byte[] DataBuffer)
        {
            if (DataBuffer.Length < 3)
                return;

            switch (DataBuffer[2])
            {
                case 0x01:
                    //sub_409F50(a2, a4);
                    break;
                case 0x05:
                    //sub_409FB0(a2, a4);
                    break;
                case 0x06:
                    //sub_40A080(a2, a4);
                    break;
                case 0x07:
                    //sub_40A150(a2, a4);
                    break;
                case 0xF4:
                    switch (DataBuffer[3])
                    {
                        case (byte)CSHeaders.CS_SERVER_SELECT:
                            SendServerInfo(conn.cIndex, DataBuffer);
                            break;
                        case (byte)CSHeaders.CS_CLIENT_CONNECT:
                            SendServerList(conn.cIndex, DataBuffer);
                            break;
                        case 0x07:
                            //sub_40B160(a4);
                            break;
                        default:
                            break;
                    }
                    break;
                case 0xF5:
                    //if (!*(_BYTE*)(a2 + 3))
                    //    nullsub_3(a2, a4);
                    break;
                case 0x10:
                    //sub_409E20(a4);
                    break;
                case 0x11:
                    //sub_409E40(a4);
                    break;
                case 0x12:
                    //nullsub_2(a2, a4);
                    break;
                default:
                    return;
            }
        }

        public static void ProcessUDPPacket(byte[] DataBuffer)
        {
            //Recived Data : [0xc1 0x10 0x01 0x08 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x64 0x00 ]
            //Recived Data : [0xc1 0x08 0x02 0x00 0x00 0x00 0x00 0x00 ] //JoinServer

            switch (DataBuffer[2])
            {
                case 0x01:
                    ParseGameServerInfo(DataBuffer);
                    break;
                case 0x02:
                    ParseJoinServerPacket(DataBuffer);
                    break;
                default:
                    break;
            }
        }

        public static void ParseJoinServerPacket(byte[] Data)
        {
            JoinServerUDPMSG jsMsg = Helpers.GetStructFromArray<JoinServerUDPMSG>(Data);

            InvokeUI.MainWindowInstance.JoinServerAlive = true;
        }

        public static void ParseGameServerInfo(byte[] Data)
        {
            PMSG_SERVERINFO serverInfo = Helpers.GetStructFromArray<PMSG_SERVERINFO>(Data);

            MainWindow window = InvokeUI.MainWindowInstance;
            if (window.GSList.Any(wr => wr.ServerCode == serverInfo.ServerCode))
            {
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).IsAlive = true;
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).IsOnline = true;
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).Percent = serverInfo.Percent;
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).AccountCount = serverInfo.AccountCount;
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).PCbangCount = serverInfo.PCbangCount;
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).MaxUserCount = serverInfo.MaxUserCount;
                window.GSList.First(wr => wr.ServerCode == serverInfo.ServerCode).UserCount = serverInfo.UserCount;
            }
        }

        public static void SendServerList(int cIndex, byte[] data)
        {
            MainWindow mainWindow = InvokeUI.MainWindowInstance;

            List<GameServerItem> gsList = mainWindow.GSList;


            byte[] ServerList = new byte[gsList.Count * 4 + 7]; // server count * 4 + 7

            ServerList[0] = 0xC2;
            ServerList[1] = 0x00;
            ServerList[2] = 0x0B;
            ServerList[3] = 0xF4;
            ServerList[4] = 0x06;
            ServerList[5] = 0x00;

            _SERVERS servers = new _SERVERS();
            servers.Count = (ushort)gsList.Count;
            servers.Group = 1;
            servers.Server = new _SERVER[5];

            int cnt = 0;
            foreach (GameServerItem gsi in gsList)
            {
                if (gsi.IsOnline && !gsi.IsHidden)
                {
                    _SERVER lpServer = new _SERVER();
                    lpServer.ServerCode = (ushort)gsi.ServerCode;
                    lpServer.Status = gsi.IsOnline;
                    lpServer.IP = gsi.IPAddress.ToString();
                    lpServer.Port = (ushort)gsi.Port;

                    ServerList[cnt * 4 + 9] = (byte)gsi.Percent;
                    ServerList[6]++;

                    servers.Server[cnt] = lpServer;
                    cnt++;
                }
                else
                {
                    ServerList = Helpers.SubByteArray(ServerList, ServerList.Length - 4);
                }
            }

            if (ServerList[6] > 0)
            {
                ServerList[1] = Helpers.HIBYTE((ushort)ServerList.Length);
                ServerList[2] = Helpers.LOBYTE((ushort)ServerList.Length);
                Application.Current.Dispatcher.Invoke(delegate
                {
                    InvokeUI.MainWindowInstance.connectServer.DataSend(ServerList, cIndex);
                });
            }
            else
            {
                Logs.WriteLog("black", "[ConnectServer] :: No active server!");

                Application.Current.Dispatcher.Invoke(delegate
                {
                    InvokeUI.MainWindowInstance.connectServer.CloseConnection(cIndex);
                });
            }

        }

        public static void SendHelloMessage(int cIndex)
        {
            PMSG_HELLO hello = new PMSG_HELLO();
            hello.Head.c = 0xC1;
            hello.Head.Size = (byte)Marshal.SizeOf(hello);
            hello.Head.HeadCode = 0x00;
            hello.Result = 0x01;

            Application.Current.Dispatcher.Invoke(delegate
            {
                InvokeUI.MainWindowInstance.connectServer.DataSend(Helpers.GetStructBytes(hello), cIndex);
            });
        }

        public static void SendServerInfo(int cIndex, byte[] Data)
        {
            GS_CONNECT_INFO conInfo = new GS_CONNECT_INFO();
            conInfo.Head.c = 0xC1;
            conInfo.Head.Size = (byte)Marshal.SizeOf(conInfo);
            conInfo.Head.HeadCode = 0xF4;
            conInfo.SubHead = 0x03;

            conInfo.IP = "192.168.1.50";
            conInfo.Port = 55901;

            Application.Current.Dispatcher.Invoke(delegate
            {
                InvokeUI.MainWindowInstance.connectServer.DataSend(Helpers.GetStructBytes(conInfo), cIndex);
            });
        }
    }
}
