using System;

namespace ftdiSharp
{
    public enum FTStatus
    {
        Ok,
        InvalidHandle,
        DeviceNotFound,
        DeviceNotOpened,
        IOError,
        InsufficientResources,
        InvalidParameter,
        InvalidBaudRate,
        NotOpenForErase,
        NotOpenForWrite,
        WriteFailed,
        EEPROMReadFailed,
        EEPROMWriteFailed,
        EEPROMEraseFailed,
        EEPROMNotPresent,
        EEPROMNotProgrammed,
        InvalidArgs,
        NotSupported,
        OtherError,
        DeviceListNotReady
    }

    [Flags]
    public enum FTEvent
    {
        RxChar = 1,
        ModemStatus = 2,
        LineStatus = 4
    }

    public enum FTDeviceType
    {
        // lots of unfriendly names starting with numbers here
// ReSharper disable InconsistentNaming
        FT_BM,
        FT_AM,
        FT_100AX,
        Unknown,
        FT_2232C,
        FT_232R,
        FT_2232H,
        FT_4232H,
        FT_232H,
        XSeries
// ReSharper restore InconsistentNaming
    }

    public enum FTBitMode : byte
    {
        Reset = 0,
        AsyncBitBang = 1,
        MPSSE = 1 << 1,
        SyncBitBang = 1 << 2,
        MCUHost = 1 << 3,
        FastOptoIsolated = 1 << 4,
        CBUSBitBang = 1 << 5,
        Sync245FIFO = 1 << 6
    }

    [Flags]
    public enum FTDeviceFlag
    {
        Open = 1,
        HighSpeedUSB = 2,
    }

    [Flags]
    public enum FTPurge
    {
        Rx = 1,
        Tx = 2
    }

    [Flags]
    public enum FTFlowControl : ushort
    {
        None = 0,
        RTSCTS = 0x100,
        DTRDSR = 0x200,
        XonXoff = 0x400
    }

    public enum FTParity : byte
    {
        None = 0,
        Odd = 1,
        Even = 2,
        Mark = 3,
        Space = 4
    }
}