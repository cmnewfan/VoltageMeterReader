using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// LogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LogWindow : Window
    {
        private ObservableCollection<TextBlock> logs = new ObservableCollection<TextBlock>();
        public LogWindow(ObservableCollection<TextBlock> Logs)
        {
            InitializeComponent();
            for (int i = 0; i < Logs.Count; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = Logs[i].Text;
                tb.Foreground = Logs[i].Foreground;
                logs.Add(tb);
            }
            mListBox.ItemsSource = logs;
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = System.Windows.Visibility.Hidden;
        }

        public void Update(ObservableCollection<TextBlock> Logs)
        {
            logs.Clear();
            for (int i = 0; i < Logs.Count; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = Logs[i].Text;
                tb.Foreground = Logs[i].Foreground;
                logs.Add(tb);
            }
            mListBox.ItemsSource = logs;
        }
    }
}
