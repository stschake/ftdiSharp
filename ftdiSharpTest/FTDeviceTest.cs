using System;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ftdiSharp;

namespace ftdiSharpTest
{

    [TestFixture]
    public class FTDeviceTest
    {

        [Test]
        public void TestList()
        {
            var devs = FTDevice.List();
            Assert.IsNotNull(devs);
            Assert.IsNotEmpty(devs);

            Assert.IsFalse(devs[0].IsOpen);
            Assert.IsNotNullOrEmpty(devs[0].Serial);
            Assert.IsNotNullOrEmpty(devs[0].Description);
        }

        [Test]
        public void TestOpen()
        {
            using (var dev = FTDevice.Open())
            {
                Assert.IsNotNull(dev);
                Assert.IsNotNull(dev.Info);
                Assert.AreEqual(dev.Index, dev.Info.Index);
                Assert.IsTrue(dev.Info.IsOpen);
            }
        }

        [Test]
        public void TestBitMode()
        {
            using (var dev = FTDevice.Open())
            {
                dev.SetBitMode(FTBitMode.CBUSBitBang, 0xF0);
                var val = dev.GetBitMode();
                Assert.AreEqual(0xF0, val);
            }
        }

        [Test]
        public void TestDataCharacteristics()
        {
            using (var dev = FTDevice.Open(0))
            {
                // throws on invalid data bits value
                Assert.Throws<ArgumentException>(() => dev.SetDataCharacteristics(1, 1, FTParity.None));

                // throws on invalid stop bits value
                Assert.Throws<ArgumentException>(() => dev.SetDataCharacteristics(8, 5, FTParity.None));

                Assert.DoesNotThrow(() => dev.SetDataCharacteristics(8, 1, FTParity.None));
            }
        }

        [Test]
        public void TestResetWriteRead()
        {
            using (FTDevice dev = FTDevice.Open(0))
            {
                // reset
                dev.SetBitMode(FTBitMode.CBUSBitBang, 0xF0);
                Thread.Sleep(10);
                dev.SetBitMode(FTBitMode.CBUSBitBang, 0xFF);
                // allow some time for startup
                Thread.Sleep(100);

                // send hello to bootloader
                dev.SetBaudRate(19200);
                var data = new[]{(byte)'1', (byte)' '};
                dev.WriteFully(data, 0, 2);

                // read ehlo
                var buf = new byte[9];
                int r = dev.ReadFully(buf, 0, 9);
                Assert.AreEqual(9, r);
                var text = Encoding.ASCII.GetString(buf);
                Assert.IsTrue(text.Contains("AVR ISP"));
            }
        }

    }
}
