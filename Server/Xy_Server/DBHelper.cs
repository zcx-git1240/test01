using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;

namespace Zp_Server
{
    class DBHelper
    {
        private OleDbConnection conn;

        public DBHelper(string inDBStr)
        {
            conn = new OleDbConnection(inDBStr);
        }

        public DBHelper(OleDbConnection inConn)
        {
            conn = inConn;
        }

        public DataTable ReadDatatable_OraDB(string commandText)
        {

            DataTable dt = new DataTable();   
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                OleDbDataAdapter Da = new OleDbDataAdapter(commandText, conn);
                Da.Fill(dt);

                Da.Dispose();
                //conn.Close();
                //conn.Dispose();

                return dt;
            }
            catch (Exception ex)
            {
                string errtxt = "数据库读取错误！" + ex.Message;
                Logger.logwrite(errtxt);
                Logger.logwrite(commandText, "DB");
                Logger.logwrite(errtxt, "DB");
                return dt;
            }
        }

        public int ExecuteNonQuery(string commandText)
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                OleDbCommand command = new OleDbCommand();
                command.Connection = conn;
                command.CommandText = commandText;
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                string errtxt = "数据库写入错误！" + ex.Message;
                Logger.Errlogwrite(commandText, errtxt);
                return -1;
            }
        }
    }
}
