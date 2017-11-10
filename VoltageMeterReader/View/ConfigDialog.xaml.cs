using System;
using System.Collections.Generic;
using System.IO.Ports;
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
using System.Windows.Shapes;

namespace VoltageMeterReader.View
{
    /// <summary>
    /// CongidWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigDialog : Window
    {
        public int mBaudrate 
        {
            get { return int.Parse(((ComboBoxItem)mBaudrateComboBox.SelectedItem).Content.ToString()); }
        }

        public int mDataBits
        {
            get { return int.Parse(((ComboBoxItem)mDataBitsComboBox.SelectedItem).Content.ToString()); }
        }

        public Parity mParity
        {
            get 
            {
                if (mParityComboBox.SelectedIndex == 0)
                {
                    return Parity.None;
                }
                else if (mParityComboBox.SelectedIndex == 1)
                {
                    return Parity.Odd;
                }
                else
                {
                    return Parity.Even;
                }
            }
        }

        public StopBits mStopBit
        {
            get 
            {
                if (mStopBitComboBox.SelectedIndex == 0)
                {
                    return StopBits.One;
                }
                else
                {
                    return StopBits.Two;
                }
            }
        }

        public ConfigDialog(bool enable)
        {
            InitializeComponent();
            if (!enable)
            {
                btnCancel.IsEnabled = false;
                btnDialogOk.IsEnabled = false;
                mBaudrateComboBox.IsEnabled = false;
                mParityComboBox.IsEnabled = false;
                mStopBitComboBox.IsEnabled = false;
                mDataBitsComboBox.IsEnabled = false;
            }
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
