using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConnectServer
{
    public struct PBMSG_HEAD
    {
        public void Set(byte headCode, byte size)
        {
            c = 0xC1;
            HeadCode = headCode;
            Size = size;
        }

        public void SetE(byte headCode, byte size)
        {
            c = 0xC3;
            HeadCode = headCode;
            Size = size;
        }

        public byte c;
        public byte Size;
        public byte HeadCode;
    };

    struct PWMSG_HEAD
    {
        public void Set(byte headCode, byte size)
        {
            c = 0xC2;
            HeadCode = headCode;
            SizeH = Helpers.SET_NUMBERH(size);
            SizeL = Helpers.SET_NUMBERL(size);
        }

        public void SetE(byte headCode, byte size)
        {
            c = 0xC4;
            HeadCode = headCode;
            SizeH = Helpers.SET_NUMBERH(size);
            SizeL = Helpers.SET_NUMBERL(size);
        }

        byte c;
        byte SizeH;
        byte SizeL;
        byte HeadCode;
    };

    public struct PMSG_HELLO
    {
        public PBMSG_HEAD Head;
        public byte Result;
    };

    public struct GS_CONNECT_INFO
    {
        public PBMSG_HEAD Head;
        public byte SubHead;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string IP;

        public ushort Port;
    };

    public struct _SERVER
    {
        public ushort ServerCode;
        public bool Status;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string IP;
        public ushort Port;
    };

    public struct _SERVERS
    {
        public ushort Count;
        public ushort Group;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public _SERVER[] Server;
    };

    struct ServerInfo
    {
        ushort ServerCode;
        byte Percent;
        byte UNK;
    };

    struct ServerList
    {
        PWMSG_HEAD h;
        byte SubHead;
        byte ServersCountH;
        byte ServersCountL;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 361)]
        ServerInfo[] Servers;
    };
}
