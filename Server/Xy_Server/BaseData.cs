using System;
using OPCAutomation;
using System.Collections.Generic;
using System.Data.OleDb;

namespace Zp_Server
{
    class BaseData
    {
        public Dictionary<string, OPCItem> opcItems;
        public Dictionary<string, string> opcValues;
        public OleDbConnection conn;
        public int crewID;
        public int shiftID;
        public string dqsj;
        public int i_count = 0, tz = 0; //计数
        public bool[] flags = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };  //15


        public BaseData(Dictionary<string, OPCItem> inOpcItem, Dictionary<string, string> inValues, OleDbConnection inconn, int increwID, int inshiftID, string indqsj)
        {
            opcItems = inOpcItem;
            opcValues = inValues;
            conn = inconn;
            crewID = increwID;
            shiftID = inshiftID;
            dqsj = indqsj;
        }
    }
}
