using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorsSetNET
{
    public class SensorsViaAOF : ISensors
    {
        public SensorsViaAOF() { }

        private SensorsEthernet sensorsEthernet = null;
        public SensorsViaAOF(SensorsEthernet sensorsEthernet)
        {
            this.sensorsEthernet = sensorsEthernet;
        }

        public SensorsData ReadSensorsData(int timeout_ms)
        {
            return ReadSensorsData();
        }

        public bool Connected => sensorsEthernet != null && sensorsEthernet.Connected;

        public SensorsConnectionType SensorsConnectionType => SensorsConnectionType.ViaAOF;

        public void Connect(IPAddress ipAddress, int port, int timeout_ms = 16000)
        {
            sensorsEthernet = new SensorsEthernet();
            sensorsEthernet.Connect(ipAddress, port, timeout_ms);
        }

        public void Connect(IPEndPoint ipEndPoint, int timeout_ms = 16000)
        {
            sensorsEthernet = new SensorsEthernet();
            sensorsEthernet.Connect(ipEndPoint, timeout_ms);
        }

        public SensorsData ReadSensorsData()
        {
            if (!Connected)
                throw new Exception("Device is not connected!");

            Stream stream = new MemoryStream(new byte[]);

            return dataReader.ReadSensorsData();
        }

        public void Close()
        {
            if(netStream != null)
                netStream.Close();
            netStream = null;
            if (tcpClient != null)
            {
                tcpClient.Close();
            }
            tcpClient = null;
        }
    }
}
