using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Zp_Server
{
    class Logger
    {
        private static object lockObj = new object();
        public static void Taglogwrite(string txtstr, string tag = "", string action = "", string id = "")
        {
            lock (lockObj)
            {
                string path;
                string start = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                string str = start + "  ";
                str = str + id.PadRight(16);
                str = str + action.PadRight(20);
                str = str + txtstr;
                Console.WriteLine(str);

                path = Directory.GetCurrentDirectory();
                path = path + "\\log\\";
                path = path + tag + string.Format("{0:yyyyMMdd}", DateTime.Now);
                path = path + ".log";
           
                //StreamWriter sw = new StreamWriter(@"D:\home\202010082.log", true, Encoding.UTF8);
                StreamWriter sw;
                sw = File.AppendText(path);
                sw.WriteLine(str);
                sw.Close();//写入
            }
        }
        public static void logwrite(string txtstr, string action = "", string id = "")
        {
            Taglogwrite(txtstr, "", action, id);
        }

        public static void DBlogwrite(string txtstr, string action = "", string id = "")
        {
            Taglogwrite(txtstr, "DB_", action, id);
        }

        public static void OPClogwrite(string txtstr, string action = "", string id = "")
        {
            Taglogwrite(txtstr, "OPC_", action, id);
        }

        public static void Errlogwrite(string txtstr, string action = "", string id = "", bool erronly=false)
        {
            Taglogwrite(txtstr, "Err_", action, id);
            Taglogwrite(txtstr, "", action, id);
            //if (!erronly)
            //{
            //    Taglogwrite(txtstr, "", action, id);
            //}
        }
    }
}
