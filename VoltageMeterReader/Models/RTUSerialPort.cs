using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltageMeterReader.Models
{
    class RTUSerialPort : SerialPort
    {
        public RTUSlave[] mSlaves;
        public String mPortName;
    }
}
