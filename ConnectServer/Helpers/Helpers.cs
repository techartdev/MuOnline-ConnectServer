using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConnectServer
{
    public static class Helpers
    {
        public static int GetFirstIndexFromList<Z>(Dictionary<int, Z> list)
        {
            int index = 0;
            while (true)
            {
                if (!list.ContainsKey(index))
                {
                    return index;
                }

                index++;
            }
        }

        public static byte[] TrimNullByteData(byte[] Data)
        {
            bool DataFound = false;
            byte[] NewData = Data.Reverse().SkipWhile(point =>
            {
                if (DataFound) return false;
                if (point == 0x00) return true; else { DataFound = true; return false; }
            }).Reverse().ToArray();
            return NewData;
        }

        public static byte[] SubByteArray(this byte[] byteArray, int len)
        {
            byte[] tmp = new byte[len];
            Array.Copy(byteArray, tmp, len);

            return tmp;
        }

        public static byte[] GetStructBytes<T>(T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static T GetStructFromArray<T>(byte[] arr)
        {
            T str = (T)Activator.CreateInstance(typeof(T));

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public static string ByteArrayToString(byte[] Array)
        {
            StringBuilder HexString = new StringBuilder(Array.Length * 2);
            foreach (byte Item in Array)
                HexString.AppendFormat("0x{0:x2} ", Item);
            return HexString.ToString();
        }

        public static byte SET_NUMBERH(int x)
        {
            return (byte)((x) >> 8);
        }

        public static byte SET_NUMBERL(int x)
        {
            return (byte)((x) & 0xFF);
        }

        public static byte LOBYTE(UInt16 a)
        {
            return (byte)(a & 0xff);
        }

        public static byte HIBYTE(UInt16 a)
        {
            return (byte)(a >> 8);
        }
    }
}
