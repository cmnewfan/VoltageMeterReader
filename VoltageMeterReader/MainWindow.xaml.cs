using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using VoltageMeterReader.Helper;
using VoltageMeterReader.Models;
using VoltageMeterReader.View;

namespace VoltageMeterReader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Application mApplication;
        RTUSerialPort[] mPorts;
        ObservableCollection<TextBlock> logs = new ObservableCollection<TextBlock>();
        LogWindow log;
        private int PageCount = 0;
        private int rowCount = 0;
        private int columnCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            if (null == Application.Current)
            {
                new System.Windows.Application();
            }
            mListBox.ItemsSource = logs;
            mOpenMenuButton.Click += mOpenMenuButton_Click;
            mApplication = Application.Current;
            XDocument xml = XDocument.Load(Environment.CurrentDirectory + @"\configuration.xml");
            mPorts = (from Port in xml.Descendants("Port") 
                      select new RTUSerialPort((from Slave in Port.Descendants("Slave") 
                                                select new RTUSlave(byte.Parse(Slave.Attribute("SlaveId").Value), (from Parameter in Slave.Descendants("Parameter") 
                                                                                                                   select new Parameter(Parameter.Attribute("Type").Value, ushort.Parse(Parameter.Attribute("Address").Value), Parameter.Attribute("Name").Value)).ToArray<Parameter>(), Slave.Attribute("SlaveName").Value)).ToArray<RTUSlave>(), Port.Attribute("PortName").Value, Port.Attribute("DisplayName").Value)).ToArray<RTUSerialPort>();
            Loaded += delegate
            {
                rowCount = (int)Math.Floor(mVoltageGrid.ActualHeight / 320);
                columnCount = (int)Math.Floor(mVoltageGrid.ActualWidth / 320);
                PageCount = 0;
                for (int i = 0; i < mPorts.Count(); i++)
                {
                    for (int j = 0; j < mPorts[i].mSlaves.Count(); j++)
                    {
                        PageCount++;
                    }
                }
                var x = PageCount / (rowCount * columnCount);
                var y = PageCount % (rowCount * columnCount); ;
                PageCount = y > 0 ? x + 1 : x;
                for (int i = 0; i < rowCount; i++)
                {
                    mVoltageGrid.RowDefinitions.Add(new RowDefinition());
                }
                for (int i = 0; i < columnCount; i++)
                {
                    mVoltageGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
                JumpToPage(1);
            };
            RtuHelper helper = new RtuHelper(mPorts, OnMessage);
        }

        public void OnMessage(object o, LogLevel level)
        {
            if (logs.Count > 999)
            {
                logs.Clear();
            }
            mApplication.Dispatcher.Invoke(new Action(() =>
                {
                    TextBlock tb = new TextBlock();
                    tb.Text = new StringBuilder(DateTime.Now.ToString()).Append(@":").Append(o.ToString()).ToString();
                    if (level == LogLevel.Error)
                    {
                        tb.Foreground = Brushes.Red;
                    }
                    else
                    {
                        tb.Foreground = Brushes.Black;
                    }
                    logs.Add(tb);
                }));
        }

        void unBindData()
        {
            for (int i = 0; i < mVoltageGrid.Children.Count; i++)
            {
                VoltageMeter meter = mVoltageGrid.Children[i] as VoltageMeter;
                for (int j = 0; j < meter.mMeterGrid.Children.Count; j++)
                {
                    VoltageReader reader = meter.mMeterGrid.Children[j] as VoltageReader;
                    //BindingOperations.ClearAllBindings(reader);
                    reader.DataContext = null;
                    reader.ReaderName.Content = null;
                }
                meter.mMeterGrid.Children.Clear();
            }
            mVoltageGrid.Children.Clear();
        }

        void JumpToPage(int PageIndex)
        {
            unBindData();
            int column = 0;
            int row = 0;
            int startIndex = rowCount * columnCount * (PageIndex - 1);
            int voltageCount = 0;
            int num = 0;
            if (PageIndex != 0)
            {
                for (int i = 0; i < mPorts.Count(); i++)
                {
                    for (int j = 0; j < mPorts[i].mSlaves.Count(); j++)
                    {
                        if (num < startIndex)
                        {
                            num++;
                            continue;
                        }
                        num++;
                        if (voltageCount >= rowCount * columnCount)
                        {
                            break;
                        }
                        VoltageMeterReader.View.VoltageMeter meter = new View.VoltageMeter();
                        mVoltageGrid.Children.Add(meter);
                        Grid.SetColumn(meter, column);
                        Grid.SetRow(meter, row);
                        column++;
                        if (column == columnCount)
                        {
                            column = 0;
                            row++;
                        }
                        voltageCount++;
                        SetDataBindings(meter, mPorts[i].mSlaves[j]);
                    }
                }
            }
        }

        void mOpenMenuButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchDialog dialog = new SwitchDialog(PageCount);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                JumpToPage(dialog.inputIndex);
            }
        }

        private void SetDataBindings(View.VoltageMeter meter, RTUSlave rtuSlave)
        {
            meter.setName(rtuSlave.mDisplayName);
            //List<View.VoltageReader> readers = new List<View.VoltageReader>();
            //meter.mMeterGrid.Children. = readers;
            int column=0;
            int row=0;
            meter.mMeterGrid.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < rtuSlave.mParameters.Count(); i++)
            {
                View.VoltageReader reader = new View.VoltageReader();
                meter.mMeterGrid.Children.Add(reader);
                Grid.SetColumn(reader, column);
                Grid.SetRow(reader, row);
                column++;
                if (column == meter.mMeterGrid.ColumnDefinitions.Count())
                {
                    meter.mMeterGrid.RowDefinitions.Add(new RowDefinition());
                    column = 0;
                    row++;
                }
                reader.DataContext = rtuSlave.mParameters[i];
                //readers.Add(reader);
                reader.ReaderName.Content = rtuSlave.mParameters[i].mName;
            }
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (sender==mLogButton)
            {
                if (log == null)
                {
                    log = new LogWindow(logs);
                    log.Show();
                }
                else
                {
                    log.Update(logs);
                    log.Visibility = System.Windows.Visibility.Visible;
                    log.Focus();
                }
            }
        }

        private void mLogButton_MouseEnter_1(object sender, MouseEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(0.2, 1.0, new Duration(TimeSpan.FromSeconds(1.0)));
            mLogButton.BeginAnimation(Button.OpacityProperty, animation, HandoffBehavior.Compose);
            mLogButton.FontSize = 20;
            mLogButton.Content = "查看日志";
        }

        private void mLogButton_MouseLeave_1(object sender, MouseEventArgs e)
        {
            mLogButton.Content = "";
        }
    }
}
