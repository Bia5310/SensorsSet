using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorsSetNET
{
    public class SensorsEthernet : ISensors
    {
        private TcpClient tcpClient = null;
        public TcpClient TcpClient
        {
            get => tcpClient;
        }

        public SensorsConnectionType SensorsConnectionType => SensorsConnectionType.Ethernet;

        public SensorsEthernet() { }

        public SensorsEthernet(TcpClient tcpClient)
        {
            if (!tcpClient.Connected)
                throw new InvalidOperationException("TcpClient не подключен к удаленному узлу");
            this.tcpClient = tcpClient;
            netStream = tcpClient.GetStream();
        }

        public bool Connected => tcpClient == null ? false : tcpClient.Connected;

        private NetworkStream netStream = null;
        public NetworkStream NetworkStream => netStream;

        public SensorsData ReadSensorsData(int timeout_ms)
        {
            netStream.WriteTimeout = timeout_ms;
            netStream.ReadTimeout = timeout_ms;

            return ReadSensorsData();
        }

        private IPEndPoint ipEndPoint = null;// = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5310);

        private IPAddress IP => ipEndPoint.Address;

        private int Port => ipEndPoint.Port;

        public IPEndPoint IPEndPoint => ipEndPoint;

        public void Connect(IPAddress ipAddress, int port, int timeout_ms = 16000)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);
            Connect(ipEndPoint, timeout_ms);
        }

        public void Connect(IPEndPoint ipEndPoint, int timeout_ms = 16000)
        {
            Close();
            TcpListener listener = new TcpListener(ipEndPoint);
            //tcpClient = new TcpClient(ipEndPoint);
            listener.Start();
            
            DateTime dateTime = DateTime.Now;
            while(true)
            {
                if (listener.Pending())
                {
                    tcpClient = listener.AcceptTcpClient();
                    listener.Stop();
                    break;
                }
                if (DateTime.Now.Subtract(dateTime).TotalMilliseconds > timeout_ms)
                {
                    listener.Stop();
                    throw new TimeoutException();
                }
                Thread.Sleep(20);
            }
            listener.Stop();

            if (null == tcpClient || !tcpClient.Connected)
                throw new Exception("Not connected");
            netStream = tcpClient.GetStream();
            this.ipEndPoint = ipEndPoint;
        }

        public SensorsData ReadSensorsData()
        {
            if (!Connected)
                throw new Exception("Device is not connected!");

            DataReader dataReader = new DataReader(netStream);

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
