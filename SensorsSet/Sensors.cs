using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace SensorsSet
{
    public enum Messages : byte { ReadAllSensors = 0xDA, Hello = 0xE5 }

    public class Sensors : SerialPort
    {
        #region Конструкторы

        public Sensors() : base() { }

        public Sensors(string portName) : base(portName) { }

        public Sensors(IContainer container) : base(container) { }

        public Sensors(string portName, int baudRate = 19200) : base(portName, baudRate) { }

        public Sensors(string portName, int baudRate, Parity parity) : base(portName, baudRate, parity) { }

        public Sensors(string portName, int baudRate, Parity parity, int dataBits) : base(portName, baudRate, parity, dataBits) { }

        public Sensors(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : base(portName, baudRate, parity, dataBits, stopBits) { }

        #endregion


        private int autoconnectTimeout = 200;

        public int AutoconnectTimeout
        {
            get => autoconnectTimeout;
            set => autoconnectTimeout = value;
        }

        public SensorsData ReadSensorsData(int timeout_ms)
        {
            WriteTimeout = timeout_ms;
            ReadTimeout = timeout_ms;

            return ReadSensorsData();
        }

        public SensorsData ReadSensorsData()
        {
            if (!IsOpen)
                throw new Exception("Device is not opend!");

            DiscardInBuffer();

            byte[] outBuff = new byte[] { (byte)Messages.ReadAllSensors };
            Write(outBuff, 0, outBuff.Length);

            int size = SizeOfDataPackage();
            
            byte[] inputBuff = new byte[size];

            DateTime stTime = DateTime.Now;
            int receivedBytes = 0;

            while(true)
            {
                if (DateTime.Now.Subtract(stTime).TotalMilliseconds > 2*ReadTimeout)
                    throw new TimeoutException("Sensors receive deta timeout");

                if (BytesToRead < size)
                    continue;

                receivedBytes = Read(inputBuff, 0, inputBuff.Length);
                break;
            }

            DataPackage package;

            unsafe
            {
                fixed(byte* p = &inputBuff[0])
                {
                    IntPtr ptr = (IntPtr)p;
                    package = Marshal.PtrToStructure<DataPackage>(ptr);
                }
            }

            SensorsData sensorsData = new SensorsData()
            {
                Humidity = package.humidity * 1e-5d,
                Temperature = package.temperature * 1e-5d,
                Longitude = package.longitude*1e-7d,
                Latitude = package.latitude*1e-7d,
                Lux = package.light,
                TimeOfWeek = package.time_of_week,
                Weeks = package.weeks,
            };

            if (!checkHeader(package))
                throw new Exception("Invalid Package");

            return sensorsData;
        }

        #region Autoconnect

        public bool AutoConnect()
        {
            return AutoConnect(this.BaudRate, this.Parity, this.DataBits, this.StopBits);
        }
        
        public bool AutoConnect(int baudRate)
        {
            return AutoConnect(baudRate, this.Parity);
        }

        public bool AutoConnect(int baudRate, Parity parity)
        {
            return AutoConnect(baudRate, parity, this.DataBits, this.StopBits);
        }

        public bool AutoConnect(int baudRate, Parity parity, int dataBits)
        {
            return AutoConnect(baudRate, parity, dataBits, this.StopBits);
        }

        public bool AutoConnect(int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            if(IsOpen)
                Close();

            string[] ports = SerialPort.GetPortNames();

            if (ports.Length == 0)
                return false;

            string port = "";
            for(int i = 0; i < ports.Length; i++)
            {
                try
                {
                    SerialPort sp = new SerialPort(ports[i], baudRate, parity, dataBits, stopBits);
                    sp.Open();
                    sp.ReadTimeout = autoconnectTimeout;
                    sp.WriteTimeout = autoconnectTimeout;
                    sp.DiscardInBuffer();
                    sp.Write(new byte[] { (byte)Messages.Hello }, 0, 1);
                    byte[] buff = new byte[1];
                    sp.Read(buff, 0, buff.Length);
                    sp.Close();
                    sp.Dispose();
                    byte hl = (byte)Messages.Hello;
                    if (buff[0]+1 == (byte)(Messages.Hello));
                    {
                        port = ports[i];
                        break;
                    }
                }
                catch (Exception) { }
            }

            if (port != "")
            {
                PortName = port;
                BaudRate = baudRate;
                Parity = parity;
                DataBits = dataBits;
                StopBits = stopBits;
                this.Open();
            }

            return IsOpen;
        }

        #endregion

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
