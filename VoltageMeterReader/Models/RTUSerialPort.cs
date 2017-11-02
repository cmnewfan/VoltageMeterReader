using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltageMeterReader.Models
{
    public class RTUSerialPort : SerialPort
    {
        public RTUSlave[] mSlaves;
        public String mPortName;
        public String mDisplayName;
        public RTUSerialPort(RTUSlave[] slaves, String portName, String displayName)
        {
            mSlaves = slaves;
            mPortName = portName;
            mDisplayName = displayName; 
        }
    }
}
