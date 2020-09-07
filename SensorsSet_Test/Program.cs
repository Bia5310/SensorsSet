using System;
using System.Management;
using SensorsSetNET;

namespace SensorsSet_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Sensors set = new Sensors();
            Console.WriteLine(set.AutoConnect(19200));
            
            while(true)
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
