using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ftdiSharp;

namespace ftdiSharpTest.Examples
{

    /// <summary>
    /// This is a programmer that talks to an emulated AVR ISP bootloader on the other side of the FT232R
    /// Uses CBUS bitbang to reset the device and the Write/Read functions to program the data 
    /// </summary>
    [TestFixture]
    public class Bootloader
    {
        private FTDevice _device;
        private FileStream _fs;
        private byte[] _image;
        
        [Test]
        public void TestBootload()
        {
            using (_device = FTDevice.Open())
            {
                // reset device
                _device.SetBitMode(FTBitMode.CBUSBitBang, 0xF0);
                Thread.Sleep(10);
                _device.SetBitMode(FTBitMode.CBUSBitBang, 0xFF);
                Thread.Sleep(100);

                // empty any possible leftover data in the rx/tx queue
                // this is necessary because the serialtest program is blasting the usart with data
                // and some of it lives on after the reset in the ft queues
                _device.Purge();

                _device.SetFlowControl(FTFlowControl.None);
                _device.SetDataCharacteristics(8, 1, FTParity.None);
                _device.SetBaudRate(19200);

                SendHELO();
                ReceiveEHLO();
                EnterProgrammingMode();
                ParseHEX("serialtest.hex");
                WriteImage();
                VerifyImage();
                LeaveProgrammingMode();

                _device.SetBaudRate(38400);
                _device.WaitFor(FTEvent.RxChar);
                var msg = new byte[10];
                _device.ReadFully(msg, 0, 10);
                var text = Encoding.ASCII.GetString(msg);
                Assert.AreEqual("serialtest", text);
            }
        }

        public void WriteImage()
        {
            const int pageSize = 256;

            int pages = _image.Length/256;
            // we assume the image is a continuos block starting at page 0
            for (int i = 0; i < pages; i++)
                WriteData(pageSize*i, _image, pageSize*i, pageSize);
            if ((_image.Length%pageSize) > 0)
                WriteData(pageSize*pages, _image, pageSize*pages, _image.Length % pageSize);
        }

        public void VerifyImage()
        {
            const int pageSize = 256;

            int pages = _image.Length/256;
            for (int i = 0; i < pages; i++)
                VerifyData(pageSize*i, _image, pageSize*i, pageSize);
            if ((_image.Length%pageSize) > 0)
                VerifyData(pageSize*pages, _image, pageSize*pages, _image.Length % pageSize);
        }
        
        public void WriteData(int addr, byte[] data, int offset, int count)
        {
            SetAddress(addr);

            var prelude = new byte[] {(byte) 'd', 0, 0, 0};
            var lenbytes = BitConverter.GetBytes((ushort) count);
            prelude[1] = lenbytes[1];
            prelude[2] = lenbytes[0];
            _device.WriteFully(prelude, 0, 4);
            _device.WriteFully(data, offset, count);
            _device.WriteFully(new[]{(byte)' '}, 0, 1);
            ReceiveNothing();
        }

        public void VerifyData(int addr, byte[] data, int offset, int count)
        {
            SetAddress(addr);

            var cmd = new byte[] { (byte)'t', 0, 0, 0, (byte)' ' };
            var lenbytes = BitConverter.GetBytes((ushort) count);
            cmd[1] = lenbytes[1];
            cmd[2] = lenbytes[0];
            _device.WriteFully(cmd, 0, 5);

            _device.ReadFully(cmd, 0, 1);
            if (cmd[0] != 0x14)
                throw new InvalidDataException("Expected start of data byte");

            var state = new byte[count];
            _device.ReadFully(state, 0, count);
            for (int i = 0; i < count; i++)
                if (state[i] != data[offset+i])
                    throw new InvalidDataException("Invalid data on flash at offset 0x" + addr.ToString("X") + "+" + i.ToString("X") + ": is: 0x" + state[i].ToString("X") + " should be: 0x" + data[offset+i].ToString("X"));

            _device.ReadFully(cmd, 0, 1);
            if (cmd[0] != 0x10)
                throw new InvalidDataException("Expected end of data byte");
        }

        public void SetAddress(int addr)
        {
            var data = new byte[] {(byte) 'U', 0, 0, (byte) ' '};
            // words address, so divide by 2
            var addrbytes = BitConverter.GetBytes((ushort) (addr/2));
            addrbytes.CopyTo(data, 1);
            _device.WriteFully(data, 0, 4);
            ReceiveNothing();
        }

        public void ParseHEX(string path)
        {
            var image = new List<byte>(500);

            using (_fs = File.OpenRead(path))
            {
                try
                {
                    int lastAddr = 0;
                    int lastSize = 0;

                    while (_fs.Position < _fs.Length - 1)
                    {
                        bool reachedEnd = false;

                        if (_fs.ReadByte() != ':')
                            throw new InvalidDataException("Expected ':'");

                        var len = ReadInt8();
                        var addr = ReadInt16();
                        var type = ReadInt8();
                        if (type == 0)
                        {
                            if ((lastAddr + lastSize) < (addr))
                            {
                                for (int i = 0; i < (addr - lastAddr + lastSize); i++)
                                    image.Add(0xFF);
                            }
                            if (addr < (lastAddr + lastSize))
                            {
                                for (int i = 0; i < (lastAddr + lastSize - addr); i++)
                                    image.RemoveAt(image.Count - 1);
                            }
                        }

                        lastAddr = addr;
                        lastSize = len;

                        switch (type)
                        {
                            case 0:
                                {
                                    // data record
                                    var data = new byte[len];
                                    for (int i = 0; i < len; i++)
                                        data[i] = (byte)ReadInt8();
                                    // checksum, don't care
                                    ReadInt8();
                                    image.AddRange(data);
                                    break;
                                } 
                            case 1:
                                {
                                    // end of file record
                                    reachedEnd = true;
                                    // just checksum, don't care
                                    ReadInt8();
                                    break;
                                }
                        }

                        if (reachedEnd)
                            break;

                        if (_fs.ReadByte() != '\r')
                            throw new InvalidDataException("Expected CR");
                        if (_fs.ReadByte() != '\n')
                            throw new InvalidDataException("Expected LF");
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidDataException("Failed to parse program data at offset " + _fs.Position, e);
                }
            }

            _image = image.ToArray();
        }

        public int ReadInt8()
        {
            return (FromASCII(_fs.ReadByte()) << 4) | FromASCII(_fs.ReadByte());
        }

        public int ReadInt16()
        {
            return (FromASCII(_fs.ReadByte()) << 12) | (FromASCII(_fs.ReadByte()) << 8) |
                   (FromASCII(_fs.ReadByte()) << 4) | FromASCII(_fs.ReadByte());
        }

        public void LeaveProgrammingMode()
        {
            _device.WriteFully(new[]{(byte)'Q', (byte)' '}, 0, 2);
            ReceiveNothing();
        }

        public void EnterProgrammingMode()
        {
            _device.WriteFully(new[]{(byte)'P', (byte)' '}, 0, 2);
            ReceiveNothing();
        }
        
        public void ReceiveNothing()
        {
            var buf = new byte[2];
            _device.ReadFully(buf, 0, 2);
            if (!(buf[0] == 0x14 && buf[1] == 0x10))
                throw new InvalidOperationException("Expected nothing response");
        }

        public void ReceiveEHLO()
        {
            var buf = new byte[9];
            _device.ReadFully(buf, 0, 9);
            var text = Encoding.ASCII.GetString(buf);
            if (!text.Contains("AVR ISP"))
                throw new InvalidOperationException("Bootloader didn't respond to HELO");
        }
        
        public void SendHELO()
        {
            _device.WriteFully(new[]{(byte)'1', (byte)' '}, 0, 2);
        }

        private static byte FromASCII(int c)
        {
            if (c <= (byte)'9')
                return (byte)(c - (byte) '0');
            return (byte) (c - 55);
        }

    }

}