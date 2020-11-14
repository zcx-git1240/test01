using System;
using System.Data;

namespace Zp_Server
{
    class WorkerBase
    {
        protected BaseData baseData;

        public WorkerBase(BaseData inBaseData)
        {
            baseData = inBaseData;
        }

        public DataTable ReadDB(string sql)
        {
            return (new DBHelper(baseData.conn)).ReadDatatable_OraDB(sql);
        }

        public void WriteDB(string sql)
        {
            (new DBHelper(baseData.conn)).ExecuteNonQuery(sql);
        }

        public int UpdateDB(string sql)
        {
            return (new DBHelper(baseData.conn)).ExecuteNonQuery(sql);
        }
    }
}
