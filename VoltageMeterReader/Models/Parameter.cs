using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltageMeterReader.Models
{
    public class Parameter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private object _mValue;
        public object mValue
        {
            get { return _mValue;}
            set 
            { 
                if(!value.Equals(_mValue))
                {
                    _mValue = value;
                    if (PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("mValue"));
                    }
                }
            }
        }
        public String mType;
        public ushort mAddress;
        public String mName;
        public Parameter(String type, ushort address, String name)
        {
            mType = type;
            mAddress = address;
            mName = name;
        }
    }
}
