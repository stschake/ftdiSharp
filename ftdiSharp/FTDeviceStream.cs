using System;
using System.IO;

namespace ftdiSharp
{

    public class FTDeviceStream : Stream
    {
        public FTDevice Device { get; private set; }

        public FTDeviceStream(FTDevice device)
        {
            Device = device;
        }

        public override void Flush()
        {
            Device.Purge(FTPurge.Tx);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;

            long immediate = Math.Min(count, Device.RxQueue);

            // this means the rx queue is empty - at least wait for one character
            // the FT_Read function blocks; lets hope it does so smartly
            if (immediate == 0)
                immediate = 1;

            return Device.Read(buffer, offset, (int)immediate);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Device.WriteFully(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }

}