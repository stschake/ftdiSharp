using System;
using System.Runtime.InteropServices;

namespace ftdiSharp
{
    public class FTDeviceInfo
    {
        public FTDeviceFlag Flags { get; private set; }
        public FTDeviceType Type { get; private set; }
        public uint Id { get; private set; }
        public uint LocationId { get; private set; }
        public string Serial { get; private set; }
        public string Description { get; private set; }
        public int Index { get; private set; }

        public bool IsOpen
        {
            get { return Flags.HasFlag(FTDeviceFlag.Open); }
        }

        public FTDevice Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("Can't open already opened device");

            var ret = FTDevice.Open(Index);
            ret.SetInfoHint(this);
            return ret;
        }

        internal FTDeviceInfo(int deviceIndex)
        {
            IntPtr serial = Marshal.AllocHGlobal(16);
            IntPtr desc = Marshal.AllocHGlobal(64);

            try
            {
                if (serial == IntPtr.Zero || desc == IntPtr.Zero)
                    throw new OutOfMemoryException();

                // this is only populated when device is already open
                IntPtr handle;

                uint flags;
                uint type;
                uint location;
                uint id;

                FTStatus status;
                if (
                    (status =
                     Native.FT_GetDeviceInfoDetail(deviceIndex, out flags, out type, out id, out location, serial, desc,
                                                   out handle)) != FTStatus.Ok)
                    throw new FTException("FT_GetDeviceInfoDetail", status);

                Flags = (FTDeviceFlag) flags;
                Type = (FTDeviceType) type;
                Id = id;
                LocationId = location;
                Serial = Marshal.PtrToStringAnsi(serial);
                Description = Marshal.PtrToStringAnsi(desc);
                Index = deviceIndex;
            }
            finally
            {
                if (serial != IntPtr.Zero)
                    Marshal.FreeHGlobal(serial);
                if (desc != IntPtr.Zero)
                    Marshal.FreeHGlobal(desc);
            }
        }
    }
}