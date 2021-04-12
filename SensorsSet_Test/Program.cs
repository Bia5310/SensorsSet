using System;
using System.Management;
using System.Net;
using SensorsSetNET;

namespace SensorsSet_Test
{
    class Program
    {
        static void MainEth(string[] args)
        {
            SensorsEthernet set = new SensorsEthernet();

            IPAddress ipAddress = IPAddress.Parse("192.168.0.201");
            set.Connect(ipAddress, 8234);

            Console.WriteLine("{0}, {1}", set.IPEndPoint.Address, set.IPEndPoint.Port);

            //Console.WriteLine(set.AutoConnect(19200));

            while (true)
            {
                try
                {
                    var sensorsData = set.ReadSensorsData(5000);
                    Console.WriteLine("----------------D-A-T-A---------------------");
                    Console.WriteLine(sensorsData.Humidity.ToString("F2") + "%");
                    Console.WriteLine(sensorsData.Temperature.ToString("F2") + "°C");
                    Console.WriteLine(sensorsData.Lux + " lux");
                    Console.WriteLine(sensorsData.Longitude + "°");
                    Console.WriteLine(sensorsData.Latitude + "°");
                    Console.WriteLine(sensorsData.TimeOfWeek + " ms");
                    Console.WriteLine(sensorsData.Weeks + " неделя с 5-6 января 1980");
                    Console.WriteLine("--------------------------------------------");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                var keyinfo = Console.ReadKey();
                if (keyinfo.Key == ConsoleKey.Escape)
                    return;
            }
        }

        static void Main(string[] args)
        {
            SensorsSerial set = new SensorsSerial();
            set.BaudRate = 19200;

            Console.WriteLine(set.AutoConnect(19200));

            Console.WriteLine("{0}, {1}, {2}, {3}", set.BaudRate, set.Parity, set.StopBits, set.DataBits);


            while (true)
            {
                try
                {
                    var sensorsData = set.ReadSensorsData(5000);
                    Console.WriteLine("----------------D-A-T-A---------------------");
                    Console.WriteLine(sensorsData.Humidity.ToString("F2") + "%");
                    Console.WriteLine(sensorsData.Temperature.ToString("F2") + "°C");
                    Console.WriteLine(sensorsData.Lux + " lux");
                    Console.WriteLine(sensorsData.Longitude + "°");
                    Console.WriteLine(sensorsData.Latitude + "°");
                    Console.WriteLine(sensorsData.TimeOfWeek + " ms");
                    Console.WriteLine(sensorsData.Weeks + " неделя с 5-6 января 1980");
                    Console.WriteLine("--------------------------------------------");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                var keyinfo = Console.ReadKey();
                if (keyinfo.Key == ConsoleKey.Escape)
                    return;
            }
        }
    }
}
