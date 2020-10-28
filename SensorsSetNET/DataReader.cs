using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SensorsSetNET
{
    public class DataReader
    {
        Stream stream = null;

        public DataReader(Stream stream)
        {
            this.stream = stream;
        }

        public SensorsData ReadSensorsData()
        {
            stream.Flush();
            //while (stream.ReadByte() != -1) { }

            byte[] outBuff = new byte[] { (byte)Messages.ReadAllSensors };
            stream.Write(outBuff, 0, outBuff.Length);

            int size = SizeOfDataPackage();

            byte[] inputBuff = new byte[size];

            DateTime stTime = DateTime.Now;
            //int receivedBytes = 0;

            int c = -1;
            int c_prev = -1;
            while (true)
            {
                if (stream.ReadTimeout != 0)
                    if (DateTime.Now.Subtract(stTime).TotalMilliseconds > 2 * stream.ReadTimeout)
                        throw new TimeoutException("Sensors receive data timeout");

                c = stream.ReadByte();
                if (c == -1)
                    break;

                if(c_prev == (HEAD & 0xff) && c == ((HEAD >> 8) & 0xff))
                {
                    inputBuff[0] = (byte)c_prev;
                    inputBuff[1] = (byte)c;
                    stream.Read(inputBuff, 2, size - 2);
                    break;
                }

                c_prev = c;

            }
            /*while (true)
            {
                if (stream.ReadTimeout != 0)
                    if (DateTime.Now.Subtract(stTime).TotalMilliseconds > 2 * stream.ReadTimeout)
                        throw new TimeoutException("Sensors receive data timeout");

                if(stream.CanSeek)
                    if (stream.Length - stream.Position < size)
                        continue;

                receivedBytes += stream.Read(inputBuff, receivedBytes, inputBuff.Length-receivedBytes);

                if (receivedBytes < size)
                    continue;

                break;
            }*/

            DataPackage package;

            unsafe
            {
                fixed (byte* p = &inputBuff[0])
                {
                    IntPtr ptr = (IntPtr)p;
                    package = Marshal.PtrToStructure<DataPackage>(ptr);
                }
            }

            SensorsData sensorsData = new SensorsData()
            {
                Humidity = package.humidity * 1e-5d,
                Temperature = package.temperature * 1e-5d,
                Longitude = package.longitude * 1e-7d,
                Latitude = package.latitude * 1e-7d,
                Lux = package.light,
                TimeOfWeek = package.time_of_week,
                Weeks = package.weeks,
            };

            if (!checkHeader(package))
            {
                while(stream.ReadByte() != -1) { }
                throw new Exception("Invalid Package");
            }

            return sensorsData;
        }

        private unsafe int SizeOfDataPackage()
        {
            return sizeof(DataPackage);
        }

        private bool checkHeader(DataPackage pack)
        {
            return pack.head == HEAD;
        }

        private const ushort HEAD = 0xE1D6;

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        public unsafe struct DataPackage
        {
            [FieldOffset(0)]
            public ushort head;

            [FieldOffset(2)]
            public ushort packsize;

            [FieldOffset(4)]
            public ushort light;

            [FieldOffset(6)]
            public int humidity;

            [FieldOffset(10)]
            public int temperature;

            [FieldOffset(14)]
            public int longitude;

            [FieldOffset(18)]
            public int latitude;

            [FieldOffset(22)]
            public uint time_of_week;

            [FieldOffset(26)]
            public short weeks;
        }
    }
}
