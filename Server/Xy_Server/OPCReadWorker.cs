using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using OPCAutomation;


namespace Zp_Server
{
    class OPCReadWorker
    {
        private static string db_str = "";
        private static string[] ItemValue;
        private static bool[] Bzw = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };  //15

        public OPCReadWorker(string in_db_str, params string[] Values)
        {
            db_str = in_db_str;
            ItemValue = Values;
        }
    }
}
