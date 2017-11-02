using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using VoltageMeterReader.Helper;
using VoltageMeterReader.Models;

namespace VoltageMeterReader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Application mApplication;
        RTUSerialPort[] mPorts;

        public MainWindow()
        {
            InitializeComponent();
            if (null == Application.Current)
            {
                new System.Windows.Application();
            }
            mApplication = Application.Current;
            XDocument xml = XDocument.Load(Environment.CurrentDirectory + @"\configuration.xml");
            mPorts = (from Port in xml.Descendants("Port") 
                      select new RTUSerialPort((from Slave in Port.Descendants("Slave") 
                                                select new RTUSlave(byte.Parse(Slave.Attribute("SlaveId").Value), (from Parameter in Slave.Descendants("Parameter") 
                                                                                                                   select new Parameter(Parameter.Attribute("Type").Value, ushort.Parse(Parameter.Attribute("Address").Value), Parameter.Attribute("Name").Value)).ToArray<Parameter>(), Slave.Attribute("SlaveName").Value)).ToArray<RTUSlave>(), Port.Attribute("PortName").Value, Port.Attribute("DisplayName").Value)).ToArray<RTUSerialPort>();
            for (int i = 0; i < mPorts.Count(); i++)
            {
                for (int j = 0; j < mPorts[i].mSlaves.Count(); j++)
                {
                    VoltageMeterReader.View.VoltageMeter meter = new View.VoltageMeter();
                    mVoltageGrid.Children.Add(new VoltageMeterReader.View.VoltageMeter());
                    SetDataBindings(meter, mPorts[i].mSlaves[j]);
                }
            }
            RtuHelper helper = new RtuHelper(mPorts,null);
        }

        private void SetDataBindings(View.VoltageMeter meter, RTUSlave rtuSlave)
        {
            meter.setName(rtuSlave.mDisplayName);
            for (int i = 0; i < rtuSlave.mParameters.Count(); i++)
            {
                View.VoltageReader reader = new View.VoltageReader();
                reader.ReaderName.Content = rtuSlave.mParameters[i].mName;
                Binding binding = new Binding();
                binding.Source = rtuSlave.mParameters[i].mValue;
                reader.ReaderValue.SetBinding(Label.ContentProperty, binding);
            }
        }
    }
}
