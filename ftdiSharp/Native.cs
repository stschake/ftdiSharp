using System;
using System.Runtime.InteropServices;

namespace ftdiSharp
{

    internal static class Native
    {
        public const string Library = "ftd2xx.dll";

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_Open(int deviceIndex, out IntPtr handle);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_Close(IntPtr handle);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_CreateDeviceInfoList(out int deviceCount);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_GetDeviceInfoDetail(int index, out uint flags, out uint type, out uint deviceId,
            out uint deviceLocationId, IntPtr serialNumberBuf, IntPtr descBuf, out IntPtr handle);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_SetBitMode(IntPtr handle, byte mask, byte mode);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_GetBitMode(IntPtr handle, out byte mode);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_GetStatus(IntPtr handle, out int rxQueueAmount, out int txQueueAmount, out int eventStatus);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_Read(IntPtr handle, IntPtr buffer, int bytesToRead, out int read);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_Write(IntPtr handle, IntPtr buffer, int bytesToWrite, out int written);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_SetBaudRate(IntPtr handle, int baudRate);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_SetEventNotification(IntPtr handle, int eventMask, IntPtr eventHandle);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_Purge(IntPtr handle, int mask);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_SetFlowControl(IntPtr handle, ushort flowControl, byte Xon, byte Xoff);

        [DllImport(Library, CallingConvention = CallingConvention.Winapi)]
        public static extern FTStatus FT_SetDataCharacteristics(IntPtr handle, byte dataBits, byte stopBits, byte parity);
    }

}