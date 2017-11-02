using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modbus.Device;
using VoltageMeterReader.Models;
using System.Threading;

namespace VoltageMeterReader.Helper
{
    public class RtuHelper
    {
        private RTUSerialPort[] mPorts;
        private ModbusSerialMaster[] masters;
        private SerialPort[] clients;
        private int mBaudrate;
        private Parity mParity;
        private int mDataBits;
        private StopBits mStopBits;
        public delegate void ChangedEventHandler(object o, ProgressChangedEventArgs e);
        private event ChangedEventHandler ValueUpdatedRequest;
        //private Dictionary<ushort, bool> UnfinishedWork = new Dictionary<ushort, bool>();
        //private object UnfinishedWorkLock = new object();

        public RtuHelper(RTUSerialPort[] ports, ChangedEventHandler handler, int baudrate=9600, Parity parity=Parity.Even, int dataBits=1, StopBits stopBits=StopBits.One)
        {
            mPorts = ports;
            if (mPorts.Count() <= 0)
            {
                throw new ArgumentOutOfRangeException("串口为空");
            }
            else
            {
                clients = new SerialPort[mPorts.Count()];
                masters = new ModbusSerialMaster[mPorts.Count()];
                mBaudrate = baudrate;
                mDataBits = dataBits;
                mParity = parity;
                mStopBits = stopBits;
                ValueUpdatedRequest += handler;
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += worker_DoWork;
                worker.WorkerReportsProgress = true;
                worker.ProgressChanged += worker_ProgressChanged;
                worker.RunWorkerAsync();
            }
            
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

        public bool Connect(int index=-1)
        {
            if (index == -1)
            {
                try
                {
                    for (int i = 0; i < mPorts.Count(); i++)
                    {
                        clients[i].Open();
                        masters[i] = ModbusSerialMaster.CreateRtu(clients[i]);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Log.LogException(ex);
                    return false;
                }
            }
            else
            {
                if (clients[index] != null)
                {
                    clients[index].Close();
                }
                if (masters[index] != null)
                {
                    masters[index].Dispose();
                }
                try
                {
                    clients[index].Open();
                    masters[index] = ModbusSerialMaster.CreateRtu(clients[index]);
                }
                catch (Exception ex)
                {
                    Log.LogException(ex);
                    return false;
                }
                return true;
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ValueUpdatedRequest(e.UserState, e);
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
            bool[] ServerConnected = new bool[mPorts.Count()];
            while (true)
            {
                for (int i = 0; i < mPorts.Count(); i++)
                {
                    if (ServerConnected[i])
                    {
                        foreach(RTUSlave slave in mPorts[i].mSlaves)
                        {
                            foreach(Parameter parameter in slave.mParameters)
                            {
                                try 
                                {	        
		                            if(parameter.mType.Equals("Single"))
                                    { 
                                        var c = masters[i].ReadHoldingRegisters(slave.mSlaveId,parameter.mAddress,2);
                                        int low = c[0];
                                        int high = c[1];
                                        parameter.mValue = modbusToFloat(high, low);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.LogException(ex);
                                    ServerConnected[i] = Connect(i);
                                }
                            }
                        }
                    }
                    else
                    {
                        ServerConnected[i] = Connect(i);
                    }
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
