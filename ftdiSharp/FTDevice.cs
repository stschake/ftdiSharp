using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ftdiSharp
{
    public class FTDevice : IDisposable
    {
        private FTDeviceInfo _info;

        public int Index { get; private set; }
        public IntPtr Handle { get; private set; }

        internal FTDevice(int index, IntPtr handle)
        {
            Index = index;
            Handle = handle;
        }

        public FTDeviceInfo Info
        {
            get
            {
                if (_info == null)
                {
                    int count;
                    Native.FT_CreateDeviceInfoList(out count);
                    _info = new FTDeviceInfo(Index);
                }
                return _info;
            }
        }

        public int RxQueue
        {
            get
            {
                int ret, ignore;
                GetStatus(out ret, out ignore, out ignore);
                return ret;
            }
        }

        public int TxQueue
        {
            get
            {
                int ret, ignore;
                GetStatus(out ignore, out ret, out ignore);
                return ret;
            }
        }

        public int EventStatus
        {
            get
            {
                int ret, ignore;
                GetStatus(out ignore, out ignore, out ret);
                return ret;
            }
        }

        public void SetDataCharacteristics(byte dataBits, byte stopBits, FTParity parity)
        {
            if (!(dataBits == 8 || dataBits == 7))
                throw new ArgumentException("Must be 7 or 8", "dataBits");
            if (!(stopBits == 1 || stopBits == 2))
                throw new ArgumentException("Must be 1 or 2", "stopBits");

            // one stop bit is a 0 for the FT api
            if (stopBits == 1)
                stopBits = 0;

            FTStatus status;
            if ((status = Native.FT_SetDataCharacteristics(Handle, dataBits, stopBits, (byte)parity)) != FTStatus.Ok)
                throw new FTException("FT_SetDataCharacteristics", status);
        }

        public void SetFlowControl(FTFlowControl mask)
        {
            SetFlowControl(mask, 0x11, 0x13);
        }

        public void SetFlowControl(FTFlowControl mask, byte xOn, byte xOff)
        {
            FTStatus status;
            if ((status = Native.FT_SetFlowControl(Handle, (ushort)mask, xOn, xOff)) != FTStatus.Ok)
                throw new FTException("FT_SetFlowControl", status);
        }

        public void Purge()
        {
            Purge(FTPurge.Rx | FTPurge.Tx);
        }

        public void Purge(FTPurge mode)
        {
            FTStatus status;
            if ((status = Native.FT_Purge(Handle, (int)mode)) != FTStatus.Ok)
                throw new FTException("FT_Purge", status);
        }

        public void Close()
        {
            if (Handle == IntPtr.Zero)
                return;

            try
            {
                FTStatus status;
                if ((status = Native.FT_Close(Handle)) != FTStatus.Ok)
                    throw new FTException("FT_Close", status);
            }
            finally
            {
                Handle = IntPtr.Zero;
            }
        }

        public int ReadFully(byte[] buffer, int offset, int bytesToRead)
        {
            // per documentation, FT_Read blocks until the request has been satisfied completely
            // so keep this function here for completes sakeness
            return Read(buffer, offset, bytesToRead);
        }

        public void WriteFully(byte[] buffer, int offset, int bytesToWrite)
        {
            // documentation doesn't mention any guarantees that FT_Write will transfer completely

            var buf = Marshal.AllocHGlobal(bytesToWrite);
            if (buf == IntPtr.Zero)
                throw new OutOfMemoryException();

            Marshal.Copy(buffer, offset, buf, bytesToWrite);

            try
            {
                int left = bytesToWrite;
                while (left > 0)
                {
                    FTStatus status;
                    int written;

                    if ((status = Native.FT_Write(Handle, new IntPtr(buf.ToInt64() + (bytesToWrite-left)), left, out written)) != FTStatus.Ok)
                        throw new FTException("FT_Write", status);
                    left -= written;
                }
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
        }

        public int Write(byte[] buffer, int offset, int bytesToWrite)
        {
            var buf = Marshal.AllocHGlobal(bytesToWrite);
            if (buf == IntPtr.Zero)
                throw new OutOfMemoryException();

            Marshal.Copy(buffer, offset, buf, bytesToWrite);

            try
            {
                FTStatus status;
                int written;
                
                if ((status = Native.FT_Write(Handle, buf, bytesToWrite, out written)) != FTStatus.Ok)
                    throw new FTException("FT_Write", status);
                return written;
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
        }

        public int Read(byte[] buffer, int offset, int bytesToRead)
        {
            var buf = Marshal.AllocHGlobal(bytesToRead);
            if (buf == IntPtr.Zero)
                throw new OutOfMemoryException();

            try
            {
                FTStatus status;
                int read;
                if ((status = Native.FT_Read(Handle, buf, bytesToRead, out read)) != FTStatus.Ok)
                    throw new FTException("FT_Read", status);
                Marshal.Copy(buf, buffer, offset, read);
                return read;
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
        }

        public void GetStatus(out int rxQueue, out int txQueue, out int eventStatus)
        {
            FTStatus status;
            if ((status = Native.FT_GetStatus(Handle, out rxQueue, out txQueue, out eventStatus)) != FTStatus.Ok)
                throw new FTException("FT_GetStatus", status);
        }

        public void SetBaudRate(int baud)
        {
            FTStatus status;
            if ((status = Native.FT_SetBaudRate(Handle, baud)) != FTStatus.Ok)
                throw new FTException("FT_SetBaudRate", status);
        }

        public void SetBitMode(FTBitMode mode, byte mask)
        {
            FTStatus status;
            if ((status = Native.FT_SetBitMode(Handle, mask, (byte)mode)) != FTStatus.Ok)
                throw new FTException("FT_SetBitMode", status);
        }

        public byte GetBitMode()
        {
            FTStatus status;
            byte mode;
            if ((status = Native.FT_GetBitMode(Handle, out mode)) != FTStatus.Ok)
                throw new FTException("FT_GetBitMode", status);
            return mode;
        }

        public void SetEventNotification(FTEvent evMask, EventWaitHandle waitHandle)
        {
            FTStatus status;
            if ((status = Native.FT_SetEventNotification(Handle, (int)evMask, waitHandle.SafeWaitHandle.DangerousGetHandle())) != FTStatus.Ok)
                throw new FTException("FT_SetEventNotification", status);
        }

        public void WaitFor(FTEvent ev)
        {
            var native = new EventWaitHandle(false, EventResetMode.AutoReset);
            SetEventNotification(ev, native);
            native.WaitOne();
        }

        ~FTDevice()
        {
            Close();
        }

        public void Dispose()
        {
            Close();
        }

        internal void SetInfoHint(FTDeviceInfo info)
        {
            _info = info;
        }

        public static FTDevice Open()
        {
            return Open(0);
        }

        public static FTDevice Open(int deviceIndex)
        {
            IntPtr handle;
            FTStatus status;
            if ((status = Native.FT_Open(deviceIndex, out handle)) != FTStatus.Ok)
                throw new FTException("FT_Open", status);
            return new FTDevice(deviceIndex, handle);
        }

        public static List<FTDeviceInfo> List()
        {
            FTStatus status;
            int count;

            if ((status = Native.FT_CreateDeviceInfoList(out count)) != FTStatus.Ok)
                throw new FTException("FT_CreateDeviceInfoList", status);

            if (count <= 0)
                return new List<FTDeviceInfo>();

            var ret = new List<FTDeviceInfo>(count);
            for (int i = 0; i < count; i++)
                ret.Add(new FTDeviceInfo(i));

            return ret;
        }
    }

}