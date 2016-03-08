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
        private static MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        
        public static void ProcessCSPacket(CSConnection Connection, byte[] DataBuffer)
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
                            //JoinGame.SendServerInfo(Connection.Index, DataBuffer);
                            break;
                        case (byte)CSHeaders.CS_CLIENT_CONNECT:
                            //JoinGame.SendServerList(Connection.Index, DataBuffer);
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

        public static void SendServerList(int cIndex, byte[] data)
        {
            _SERVER lpServer = new _SERVER();
            lpServer.ServerCode = 1;
            lpServer.Status = true;
            lpServer.IP = "192.168.1.50";
            lpServer.Port = 55901;

            _SERVERS servers = new _SERVERS();
            servers.Count = 1;
            servers.Group = 1;
            servers.Server = new _SERVER[5];
            servers.Server[0] = lpServer;

            byte[] ServerList = new byte[servers.Count * 4 + 7]; // server count * 4 + 7
            //int Percent = 0;
            //ushort Count = 0;

            ServerList[0] = 0xC2;
            ServerList[1] = 0x00;
            ServerList[2] = 0x0B;
            ServerList[3] = 0xF4;
            ServerList[4] = 0x06;
            ServerList[5] = 0x00;
            ServerList[6] = 0x01;
            ServerList[9] = 0x01;
            //for (int n = 0; n < servers.Group; n++)
            //{
            //    for (int i = 0; i < servers.Count; i++)
            //    {
            //        _SERVER serv = servers.Server[n];
            //        if ((serv.Status) && (Percent != -1))
            //        {
            //            Buffer.BlockCopy(BitConverter.GetBytes(serv.ServerCode), 0, ServerList, Count * 4 + 7, 2);
            //            ServerList[Count * 4 + 9] = (byte)Percent;
            //            ServerList[6]++;
            //            Count++;
            //        }
            //        else
            //        {
            //            ServerList = MiscFunctions.SubByteArray(ServerList, ServerList.Length - 4);
            //        }
            //    }
            //}

            if (ServerList[6] > 0)
            {
                ///ServerList[1] = Macro.HIBYTE((ushort)ServerList.Length);
                //ServerList[2] = Macro.LOBYTE((ushort)ServerList.Length);
                Application.Current.Dispatcher.Invoke(delegate
                {
                    mainWindow.connectServer.SendDataToOneCSConnection(ServerList, cIndex);
                });
            }
            else
            {
                Log.WriteLog("[ConnectServer] :: No active server!");
                mainWindow.connectServer.CloseCSConnection(cIndex);
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
                mainWindow.connectServer.SendDataToOneCSConnection(MiscFunctions.GetStructBytes(hello), cIndex);
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
                mainWindow.connectServer.SendDataToOneCSConnection(MiscFunctions.GetStructBytes(conInfo), cIndex);
            });
        }
    }
}
