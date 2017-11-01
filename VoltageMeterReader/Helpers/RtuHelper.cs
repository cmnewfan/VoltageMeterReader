using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modbus.Device;
using VoltageMeterReader.Models;

namespace VoltageMeterReader.Helper
{
    class RtuHelper
    {
        private String[] mPortsNames;
        private ModbusSerialMaster[] masters;
        private SerialPort[] clients;
        private ModbusSerialMaster master;
        public delegate void ChangedEventHandler(object o, ProgressChangedEventArgs e);
        private event ChangedEventHandler ValueUpdatedRequest;
        //private Dictionary<ushort, bool> UnfinishedWork = new Dictionary<ushort, bool>();
        //private object UnfinishedWorkLock = new object();

        public RtuHelper(String[] ports_names, Parameter[] parameters, ChangedEventHandler handler, int baudrate=9600, Parity parity=Parity.Even, int dataBits=1, StopBits stopBits=StopBits.One)
        {
            mPortsNames = ports_names;
            if (mPortsNames.Count() <= 0)
            {
                throw new ArgumentOutOfRangeException("串口为空");
            }
            else
            {
                masters = new ModbusSerialMaster[mPortsNames.Count()];
            }
            port = Port;
            FtpDownloadRequest += handler;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync(Addresses);
        }

        /*public void AddUnfinishedWork(ushort address, bool value)
        {
            lock (UnfinishedWorkLock)
            {
                if (!UnfinishedWork.ContainsKey(address))
                {
                    UnfinishedWork.Add(address, value);
                }
            }
        }

        private void RemoveUnfinishedWork(ushort address)
        {
            lock (UnfinishedWorkLock)
            {
                if (UnfinishedWork.ContainsKey(address))
                {
                    UnfinishedWork.Remove(address);
                }
            }
        }*/

        /*public bool setValue(ushort address, bool value)
        {
            if (master != null)
            {
                try
                {
                    //master.WriteSingleCoil(address, value);
                    return true;
                }
                catch (Exception ex)
                {
                    AddUnfinishedWork(address, value);
                    Log.LogException(ex);
                    return false;
                }
            }
            else
            {
                AddUnfinishedWork(address, value);
                return false;
            }
        }*/

        public bool Connect()
        {
            if (client != null)
            {
                client.Close();
            }
            if (master != null)
            {
                master.Dispose();
            }
            try
            {
                client = new SerialPort("COM4", 9600, Parity.Even, 8, StopBits.One);
                client.Open();
                //client = new TcpClient(host, int.Parse(port));
                master = ModbusSerialMaster.CreateRtu(client);
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
                return false;
            }
            return true;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FtpDownloadRequest(e.ProgressPercentage, e);
        }

        float modbusToFloat(int x1, int x2)
        {
            int fuhao, fuhaoRest, exponent, exponentRest;
            float value, weishu;
            fuhao = x1 / 32768;
            fuhaoRest = x1 % 32768;
            exponent = fuhaoRest / 128;
            exponentRest = fuhaoRest % 128;
            weishu = (float)(exponentRest * 65536 + x2) / 8388608;
            value = (float)Math.Pow(-1, fuhao) * (float)Math.Pow(2, exponent - 127) * (weishu + 1);
            return value;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            ushort[] addresses = e.Argument as ushort[];
            bool ServerConnected = true;
            Dictionary<ushort, bool> results = new Dictionary<ushort, bool>(addresses.Count());
            foreach (ushort address in addresses)
            {
                results.Add(address, false);
            }
            while (true)
            {
                if (ServerConnected)
                {
                    try
                    {
                        bool SetResult = true;
                        if (UnfinishedWork.Count > 0)
                        {
                            for (int i = 0; i < UnfinishedWork.Count(); i++)
                            {
                                if (!UnfinishedWork.ElementAt(i).Value)
                                {
                                    lock (UnfinishedWorkLock)
                                    {
                                        results.Remove(UnfinishedWork.ElementAt(i).Key);
                                        results.Add(UnfinishedWork.ElementAt(i).Key, UnfinishedWork.ElementAt(i).Value); 
                                    }
                                }
                                else
                                {
                                    SetResult = setValue(results.ElementAt(i).Key, results.ElementAt(i).Value);
                                }
                            }
                        }
                        if (SetResult)
                        {
                            UnfinishedWork.Clear();
                        }
                        foreach (ushort address in addresses)
                        {
                            bool Value;
                            results.TryGetValue(address, out Value);
                            var c = master.ReadHoldingRegisters(1, 0, 2);
                            int low = c[0];
                            int high = c[1];
                            var value = modbusToFloat(high, low);
                            Log.LogEvent(value.ToString());
                            /*if (c.ElementAt(0) != Value)
                            {
                                results.Remove(address);
                                results.Add(address, c.ElementAt(0));
                                if (c.ElementAt(0))
                                {
                                    bgWorker.ReportProgress(address);
                                }
                            }*/
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogException(ex);
                        ServerConnected = ReConnect(bgWorker, ServerConnected);
                        Thread.Sleep(3000);
                    }
                }
                else
                {
                    ServerConnected = ReConnect(bgWorker, ServerConnected);
                    Thread.Sleep(3000);
                }
                Thread.Sleep(3000); 
            }
        }

        private bool ReConnect(BackgroundWorker bgWorker, bool ServerConnected)
        {
            bgWorker.ReportProgress(-1, "读取Modbus服务器数据失败,正在重连");
            ServerConnected = Connect();
            if (ServerConnected)
            {
                bgWorker.ReportProgress(-1, "连接成功");
            }
            else
            {
                bgWorker.ReportProgress(-1, "连接失败");
            }
            return ServerConnected;
        }
    }
}
