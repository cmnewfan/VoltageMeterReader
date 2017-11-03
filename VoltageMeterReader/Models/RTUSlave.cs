using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltageMeterReader.Models
{
    public class RTUSlave
    {
        public byte mSlaveId;
        public Parameter[] mParameters;
        public Parameter[] mBoolParameters;
        public Parameter[] mSingleParameters;
        public String mDisplayName;
        public List<ReadingList> mBoolReadingList;
        public List<ReadingList> mSingleReadingList;

        public RTUSlave(byte slaveId, Parameter[] parameters, String name)
        {
            mSlaveId = slaveId;
            mParameters = parameters;
            if (parameters.FirstOrDefault(item => item.mType.Equals("Bool")) != null)
            {
                mBoolParameters = parameters.Select(item =>
                {
                    if (item.mType.Equals("Bool"))
                        return item;
                    else
                        return null;
                }).OrderBy(item => item.mAddress).ToArray();
            }
            if (parameters.FirstOrDefault(item => item.mType.Equals("Single")) != null)
            {
                mSingleParameters = parameters.Select(item =>
                {
                    if (item.mType.Equals("Single"))
                        return item;
                    else
                        return null;
                }).OrderBy(item => item.mAddress).ToArray();
            }
            mDisplayName = name;
            mBoolReadingList = new List<ReadingList>();
            mSingleReadingList = new List<ReadingList>();
            AnalyzeReadingList(parameters);
        }

        private void AnalyzeReadingList(Parameter[] parameters)
        {
            if (mBoolParameters != null)
            {
                if (mBoolParameters.Count() > 1)
                {
                    int boolCount = 1;
                    ushort boolAddress = mBoolParameters.ElementAt(0).mAddress;
                    for (int i = 1; i < mBoolParameters.Count(); i++)
                    {
                        if (mBoolParameters[i].mAddress - boolAddress == 1)
                        {
                            boolCount++;
                            continue;
                        }
                        else if (mBoolParameters[i].mAddress - boolAddress == 0)
                        {
                            throw new ArgumentException("地址重复");
                        }
                        else
                        {
                            mBoolReadingList.Add(new ReadingList(boolAddress, boolCount, "Bool"));
                            boolCount = 1;
                            boolAddress = mBoolParameters[i].mAddress;
                        }
                        if (i == mBoolParameters.Count() - 1)
                        {
                            mBoolReadingList.Add(new ReadingList(boolAddress, boolCount, "Bool"));
                        }
                    }
                }
                else if (mBoolParameters.Count() == 1)
                {
                    mBoolReadingList.Add(new ReadingList(mBoolParameters.ElementAt(0).mAddress, 1, "Bool"));
                }
            }
            if (mSingleParameters != null)
            {
                if (mSingleParameters.Count() > 1)
                {
                    int singleCount = 1;
                    ushort singleAddress = mSingleParameters.ElementAt(0).mAddress;
                    ushort lastAddress = mSingleParameters.ElementAt(0).mAddress;
                    for (int i = 1; i < mSingleParameters.Count(); i++)
                    {
                        if (mSingleParameters[i].mAddress - lastAddress == 2)
                        {
                            lastAddress = mSingleParameters[i].mAddress;
                            singleCount++;
                        }
                        else if (mSingleParameters[i].mAddress - singleAddress == 0)
                        {
                            throw new ArgumentException("地址重复");
                        }
                        else
                        {
                            mSingleReadingList.Add(new ReadingList(singleAddress, singleCount, "Single"));
                            singleCount = 1;
                            singleAddress = mSingleParameters[i].mAddress;
                        }
                        if (i == mSingleParameters.Count() - 1)
                        {
                            mSingleReadingList.Add(new ReadingList(singleAddress, singleCount, "Single"));
                        }
                    }
                }
                else if (mSingleParameters.Count() == 1)
                {
                    mSingleReadingList.Add(new ReadingList(mSingleParameters.ElementAt(0).mAddress, 1, "Single"));
                }
            }
        }
    }

    public class ReadingList
    {
        public ushort mStartAddress;
        public int mNum;
        String mType;
        public ReadingList(ushort address, int num, String Type)
        {
            mStartAddress = address;
            mNum = num;
        }
    }
}
