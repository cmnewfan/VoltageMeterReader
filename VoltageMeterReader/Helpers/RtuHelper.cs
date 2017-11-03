using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modbus.Device;
using VoltageMeterReader.Models;
using System.Timers;
using VoltageMeterReader.Helpers;

namespace VoltageMeterReader.Helper
{
    public class RtuHelper
    {
        private RTUSerialPort[] mPorts;
        private ModbusSerialMaster[] masters;
        private SerialPort[] clients;
        private bool[] RtuConnected;
        private int mBaudrate;
        private Parity mParity;
        private int mDataBits;
        private StopBits mStopBits;
        public delegate void ChangedEventHandler(object o, ProgressChangedEventArgs e);
        private event ChangedEventHandler ValueUpdatedRequest;
        //private Dictionary<ushort, bool> UnfinishedWork = new Dictionary<ushort, bool>();
        //private object UnfinishedWorkLock = new object();

        public RtuHelper(RTUSerialPort[] ports, ChangedEventHandler handler, int baudrate=9600, Parity parity=Parity.Even, int dataBits=8, StopBits stopBits=StopBits.One)
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
                RtuConnected = new bool[mPorts.Count()];
                mBaudrate = baudrate;
                mDataBits = dataBits;
                mParity = parity;
                mStopBits = stopBits;
                Connect();
                ValueUpdatedRequest += handler;
                
            }
            
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            /*BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();*/
            int PortID = ((ExTimer)sender).TimerID;
            for (int i=0;i<mPorts[PortID].mSlaves.Count();i++)
            {
                RTUSlave slave = mPorts[PortID].mSlaves[i];
                ExTimer timer = new ExTimer();
                timer.TimerID = i;
                timer.AutoReset = false;
                timer.Interval = 10;
                timer.Elapsed += delegate(object slave_sender, ElapsedEventArgs slave_e)
                {
                    int slaveID = ((ExTimer)slave_sender).TimerID;
                    int num = 0;
                    foreach (ReadingList list in slave.mSingleReadingList)
                    {
                        var c = masters[PortID].ReadHoldingRegisters(mPorts[PortID].mSlaves[slaveID].mSlaveId, list.mStartAddress, (ushort)(2*list.mNum));
                        for (int j=0; j < list.mNum; j++)
                        {
                            Parameter param = mPorts[PortID].mSlaves[slaveID].mSingleParameters[num];
                            int low = c[j * 2];
                            int high = c[j * 2 + 1];
                            param.mValue = modbusToFloat(high, low);
                            num++;
                        }
                    }
                    num = 0;
                    foreach (ReadingList list in slave.mBoolReadingList)
                    {
                        var c = masters[PortID].ReadCoils(mPorts[PortID].mSlaves[slaveID].mSlaveId, list.mStartAddress, (ushort)(2 * list.mNum));
                        for (int j = 0; j < list.mNum; j++)
                        {
                            Parameter param = mPorts[PortID].mSlaves[slaveID].mBoolParameters[num];
                            param.mValue = c[j];
                            num++;
                        }
                    }
                    /*var c = masters[PortID].ReadHoldingRegisters(mPorts[PortID].mSlaves[slaveID].mSlaveId, 0, 48);
                    for (int j = 0; j < mPorts[PortID].mSlaves[slaveID].mParameters.Count(); j++)
                    {
                        Parameter param = mPorts[PortID].mSlaves[slaveID].mParameters[j];
                        int low = c[j*2];
                        int high = c[j*2+1];
                        param.mValue = modbusToFloat(high, low);
                    }*/
                };
                timer.Start();
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
                        clients[i] = new SerialPort(mPorts[i].mPortName, mBaudrate, mParity, mDataBits, mStopBits);
                        clients[i].Open();
                        masters[i] = ModbusSerialMaster.CreateRtu(clients[i]);
                        ExTimer timer = new ExTimer();
                        timer.AutoReset = true;
                        timer.Interval = 1000;
                        timer.Elapsed += timer_Elapsed;
                        timer.TimerID = i;
                        timer.Start();
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
                    clients[index] = new SerialPort(mPorts[index].mPortName,mBaudrate,mParity,mDataBits,mStopBits);
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
            //while (true)
            //{
                for (int i = 0; i < mPorts.Count(); i++)
                {
                    if (ServerConnected[i])
                    {
                        foreach(RTUSlave slave in mPorts[i].mSlaves)
                        {
                            foreach (Parameter parameter in slave.mParameters)
                            {
                                try
                                {
                                    if (parameter.mType.Equals("Single"))
                                    {
                                        var c = masters[i].ReadHoldingRegisters(slave.mSlaveId, parameter.mAddress, 2);
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
            //}
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
