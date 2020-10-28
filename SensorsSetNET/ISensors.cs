using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorsSetNET
{
    public interface ISensors
    {
        bool Connected { get; }

        SensorsConnectionType SensorsConnectionType { get; }

        SensorsData ReadSensorsData(int timeout_ms);

        SensorsData ReadSensorsData();

        void Close();
    }
}
