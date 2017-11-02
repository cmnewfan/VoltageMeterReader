using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltageMeterReader.Models
{
    public class RTUSlave
    {
        public byte mSlaveId;
        public Parameter[] mParameters;
        public String mDisplayName;
        public RTUSlave(byte slaveId, Parameter[] parameters, String name)
        {
            mSlaveId = slaveId;
            mParameters = parameters;
            mDisplayName = name;
        }
    }
}
