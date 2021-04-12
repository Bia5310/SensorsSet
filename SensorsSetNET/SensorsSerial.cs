using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;

namespace SensorsSetNET
{
    public enum Messages : byte { ReadAllSensors = 0xDA, Hello = 0xE5 }

    public enum SensorsConnectionType { SerialPort, Ethernet, ViaAOF }

    public class SensorsSerial : SerialPort, ISensors
    {
        #region Конструкторы

        public SensorsSerial() : base() { }

        public SensorsSerial(string portName) : base(portName) { }

        public SensorsSerial(IContainer container) : base(container) { }

        public SensorsSerial(string portName, int baudRate = 19200) : base(portName, baudRate) { }

        public SensorsSerial(string portName, int baudRate, Parity parity) : base(portName, baudRate, parity) { }

        public SensorsSerial(string portName, int baudRate, Parity parity, int dataBits) : base(portName, baudRate, parity, dataBits) { }

        public SensorsSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : base(portName, baudRate, parity, dataBits, stopBits) { }

        #endregion

        public SensorsConnectionType SensorsConnectionType => SensorsConnectionType.SerialPort;

        private int autoconnectTimeout = 200;

        public int AutoconnectTimeout
        {
            get => autoconnectTimeout;
            set => autoconnectTimeout = value;
        }

        public bool Connected => IsOpen;

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

            DataReader dataReader = new DataReader(BaseStream);

            return dataReader.ReadSensorsData();
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
            if (IsOpen)
                Close();

            string[] ports = SerialPort.GetPortNames();

            if (ports.Length == 0)
                return false;

            string port = "";
            for (int i = 0; i < ports.Length; i++)
            {
                try
                {
                    using (SerialPort sp = new SerialPort(ports[i], baudRate, parity, dataBits, stopBits))
                    {
                        sp.Open();
                        sp.ReadTimeout = autoconnectTimeout;
                        sp.WriteTimeout = autoconnectTimeout;
                        //sp.DiscardInBuffer();
                        sp.Write(new byte[] { (byte)Messages.Hello }, 0, 1);
                        byte[] buff = new byte[1];
                        sp.Read(buff, 0, buff.Length);
                        sp.Close();
                        //sp.Dispose();
                        byte hl = (byte)Messages.Hello;
                        if (buff[0] + 1 == (byte)(Messages.Hello))
                        {
                            port = ports[i];
                            break;
                        }
                    }
                }
                catch (Exception ex) {
                
                }
            }

            if (port != "")
            {
                PortName = port;
                BaudRate = baudRate;
                Parity = parity;
                DataBits = dataBits;
                StopBits = stopBits;
                Thread.Sleep(300);
                this.Open();
            }

            return IsOpen;
        }

        #endregion
    }
}
