using System;
using System.Data;
using OPCAutomation;
using System.Collections.Generic;
using System.Data.OleDb;

namespace Zp_Server
{
    class MonitorBaseData
    {
        public Dictionary<string, string> opcValues;
        public OleDbConnection conn;
        public int crewID;
        public int shiftID;
        public string dqsj;

        public MonitorBaseData(Dictionary<string, string> inValues, OleDbConnection inconn, int increwID, int inshiftID, string indqsj)
        {
            opcValues = inValues;
            conn = inconn;
            crewID = increwID;
            shiftID = inshiftID;
            dqsj = indqsj;
        }

        public DataTable ReadDB(string sql)
        {
            return (new DBHelper(conn)).ReadDatatable_OraDB(sql);
        }

        public void WriteDB(string sql)
        {
            (new DBHelper(conn)).ExecuteNonQuery(sql);
        }
    }
}
