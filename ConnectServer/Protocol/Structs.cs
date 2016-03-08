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

    public struct PMSG_SERVERINFO
    {
        public PBMSG_HEAD Head;   // C1:01
        public short ServerCode;   // 4
        public byte Percent;   // 6
        public short UserCount;    // 8
        public short AccountCount; // A
        public short PCbangCount;  // C
        public short MaxUserCount; // E
    };

    public struct JoinServerUDPMSG
    {
        public PBMSG_HEAD Head;
        public byte Unk1;
        public byte Unk2;
        public byte Unk3;
        public byte Unk4;
        public byte Unk5;
    }
}
