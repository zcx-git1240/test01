using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zp_Server;
using System.Data;
namespace XyL2_Server
{
    class DealRepeatCanceled : WorkerBase
    {
        public DealRepeatCanceled(BaseData inBaseData) : base(inBaseData) { }

        public void DealRepeateCancle()
        {
            string sql = "select top 20 PLAN_NO,SHEET_NO from plan_tb where is_completed = 2 group by PLAN_NO,SHEET_NO having count(SHEET_NO) > count(distinct SHEET_NO) ";
            DataTable dt = ReadDB(sql);
            if (dt.Rows.Count == 0) return;
            for(int i = 0;i < dt.Rows.Count; i++)
            {
                Logger.DBlogwrite("重复撤销了计划号" + dt.Rows[i]["PLAN_NO"] + "钢坯号" + dt.Rows[i]["SHEET_NO"] + "的钢坯。");
                sql = string.Format("select top 1 SEND_TIME from plan_tb where PLAN_NO = '{0}' and SHEET_NO = '{1}' order by SEND_TIME desc",dt.Rows[i]["PLAN_NO"],dt.Rows[i]["SHEET_NO"]);
                DataTable dtReptItems = ReadDB(sql);
                sql = string.Format("delete from plan_tb where PLAN_NO = '{0}' and SHEET_NO = '{1}' and SEND_TIME < '{2}'", dt.Rows[i]["PLAN_NO"], dt.Rows[i]["SHEET_NO"], dtReptItems.Rows[0]["SEND_TIME"]);
                WriteDB(sql);
                Logger.DBlogwrite("删除了重复计划号" + dt.Rows[i]["PLAN_NO"] + "钢坯号" + dt.Rows[i]["SHEET_NO"] + "的钢坯。" + sql);
            }
        }
    }
}
