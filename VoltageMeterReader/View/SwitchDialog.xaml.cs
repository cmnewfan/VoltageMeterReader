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
using System.Windows.Shapes;

namespace VoltageMeterReader.View
{
    /// <summary>
    /// SwitchDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SwitchDialog : Window
    {
        public int inputIndex
        {
            get 
            {
                if (txtIndex.Text.Equals(""))
                {
                    return 0;
                }
                else
                {
                    return int.Parse(txtIndex.Text);
                }
            }
        }

        public int maxIndex;

        public SwitchDialog(int index)
        {
            InitializeComponent();
            maxIndex = index;
            lblIndex.Content = new StringBuilder(@"/").Append(index);
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        protected void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                e.Key == Key.Back ||
                e.Key == Key.Left || e.Key == Key.Right)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!txtIndex.Text.Equals("") && int.Parse(txtIndex.Text) >= maxIndex)
            {
                txtIndex.Text = maxIndex.ToString();
            }
        }
    }
}
