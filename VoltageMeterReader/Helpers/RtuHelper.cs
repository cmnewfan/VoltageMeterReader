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
        public delegate void ChangedEventHandler(object o, LogLevel level);
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
                ValueUpdatedRequest += handler;
                Connect();
            }
            
        }

        void slave_timer_elapsed(object slave_sender, ElapsedEventArgs slave_e)
        {
            int slaveID = ((ExTimer)slave_sender).TimerID;
            int num = 0;
            int PortID = ((int[])((ExTimer)slave_sender).Tag)[0];
            if (RtuConnected[PortID])
            {
                foreach (ReadingList list in mPorts[PortID].mSlaves[slaveID].mSingleReadingList)
                {
                    try
                    {
                        var c = masters[PortID].ReadHoldingRegisters(mPorts[PortID].mSlaves[slaveID].mSlaveId, list.mStartAddress, (ushort)(2 * list.mNum));
                        for (int j = 0; j < list.mNum; j++)
                        {
                            Parameter param = mPorts[PortID].mSlaves[slaveID].mSingleParameters[num];
                            int low = c[j * 2];
                            int high = c[j * 2 + 1];
                            param.mValue = modbusToFloat(high, low);
                            num++;
                        }
                        mPorts[PortID].mSlaves[slaveID].mErrorCount = 0;
                    }
                    catch (Exception)
                    {
                        num += list.mNum;
                        mPorts[PortID].mSlaves[slaveID].mErrorCount++;
                        if (mPorts[PortID].mSlaves[slaveID].mErrorCount >= 60)
                        {
                            mPorts[PortID].mSlaves[slaveID].mErrorCount = 0;
                            ValueUpdatedRequest(mPorts[PortID].mPortName + "的下属" + mPorts[PortID].mSlaves[slaveID].mDisplayName + "读取错误",LogLevel.Error);
                        }
                        continue;
                    }
                    
                }
                num = 0;
                foreach (ReadingList list in mPorts[PortID].mSlaves[slaveID].mBoolReadingList)
                {
                    try
                    {
                        using (ModbusSerialMaster master = masters[PortID])
                        {
                            var c = masters[PortID].ReadCoils(mPorts[PortID].mSlaves[slaveID].mSlaveId, list.mStartAddress, (ushort)(2 * list.mNum));
                            for (int j = 0; j < list.mNum; j++)
                            {
                                Parameter param = mPorts[PortID].mSlaves[slaveID].mBoolParameters[num];
                                param.mValue = c[j];
                                num++;
                            }
                        }
                        mPorts[PortID].mSlaves[slaveID].mErrorCount = 0;
                    }
                    catch (Exception)
                    {
                        num += list.mNum;
                        mPorts[PortID].mSlaves[slaveID].mErrorCount++;
                        if (mPorts[PortID].mSlaves[slaveID].mErrorCount >= 60)
                        {
                            mPorts[PortID].mSlaves[slaveID].mErrorCount = 0;
                            ValueUpdatedRequest(mPorts[PortID].mPortName + "的下属" + mPorts[PortID].mSlaves[slaveID].mDisplayName + "读取错误", LogLevel.Error);
                        }
                        continue;
                    }
                }
            }
            ((ExTimer)slave_sender).Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            int PortID = ((ExTimer)sender).TimerID;
            for (int i=0;i<mPorts[PortID].mSlaves.Count();i++)
            {
                RTUSlave slave = mPorts[PortID].mSlaves[i];
                ExTimer timer = new ExTimer();
                timer.TimerID = i;
                timer.AutoReset = false;
                timer.Interval = 1000;
                timer.Tag = new int[] { PortID, i };
                timer.Elapsed += slave_timer_elapsed;
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
                for (int i = 0; i < mPorts.Count(); i++)
                {
                    try
                    {
                        clients[i] = new SerialPort(mPorts[i].mPortName, mBaudrate, mParity, mDataBits, mStopBits);
                        clients[i].ErrorReceived += delegate(object sender, SerialErrorReceivedEventArgs e)
                        {
                            RtuConnected[i] = false;
                            ExTimer reconnect_timer = new ExTimer();
                            reconnect_timer.AutoReset = true;
                            reconnect_timer.Interval = 10 * 1000;
                            reconnect_timer.Elapsed += delegate(object t_sender, ElapsedEventArgs t_e)
                            {
                                if (!RtuConnected[i])
                                {
                                    Connect(i);
                                }
                            };
                        };
                        clients[i].Open();
                        masters[i] = ModbusSerialMaster.CreateRtu(clients[i]);
                        masters[i].Transport.ReadTimeout = 300;
                        RtuConnected[i] = true;
                        ValueUpdatedRequest(mPorts[i].mPortName + "连接成功", LogLevel.Event);
                    }
                    catch (Exception ex)
                    {
                        RtuConnected[i] = false;
                        Log.LogException(ex);
                        ValueUpdatedRequest(mPorts[i].mPortName+"连接失败",LogLevel.Error);
                    }
                    ExTimer timer = new ExTimer();
                    timer.AutoReset = false;
                    timer.Interval = 1000;
                    timer.Elapsed += timer_Elapsed;
                    timer.TimerID = i;
                    timer.Start();
                }
                return true;
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
                    masters[index].Transport.ReadTimeout = 300;
                    RtuConnected[index] = true;
                    ValueUpdatedRequest(mPorts[index].mPortName + "连接成功", LogLevel.Event);
                }
                catch (Exception ex)
                {
                    Log.LogException(ex);
                    ValueUpdatedRequest(mPorts[index].mPortName + "连接失败", LogLevel.Error);
                    return false;
                }
                return true;
            }
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
    }
}
