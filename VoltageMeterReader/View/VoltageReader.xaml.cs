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
using VoltageMeterReader.Models;

namespace VoltageMeterReader.View
{
    /// <summary>
    /// VoltageReader.xaml 的交互逻辑
    /// </summary>
    /// 
    
    public partial class VoltageReader : UserControl
    {
        public VoltageReader()
        {
            InitializeComponent();
            Binding.AddTargetUpdatedHandler(ReaderValue, ReaderHandler);
        }

        void ReaderHandler(object sender, DataTransferEventArgs e)
        {
            String value = ((TextBlock)sender).Text;
            if (!value.Equals(""))
            {
                double mValue = double.Parse(((TextBlock)sender).Text);
                if (mValue >= 10000.0d)
                {
                    ReaderValue.Foreground = Brushes.Red;
                }
                else
                {
                    ReaderValue.Foreground = Brushes.Black;
                }
            }
        }
    }
}
