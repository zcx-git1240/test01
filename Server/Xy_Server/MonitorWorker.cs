using System;
using System.Data;

namespace Zp_Server
{
    class MonitorWorker
    {
        protected MonitorBaseData baseData;
        protected string [] INDEXS = "01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20 ch1 ch2 dch".Split(' ');

        public MonitorWorker(MonitorBaseData inBaseData)
        {
            baseData = inBaseData;
        }

        public void SaveTemp()
        {
            string WorkTag = "【监控：炉内温度】";
            bool res = true;
            for (int i = 0; i < 10; i++)
            {
                string insertCmd = "";
                string temp = "";
                try
                {
                    temp = baseData.opcValues[OPCHelper.KepTagsMonitor[i]];

                    //Logger.logwrite(temp);
                    int iTemp = (int)Convert.ToSingle(temp); 

                    insertCmd = "insert into fur_meas_value_tb(Rev_time, Fur_Area_id, Temp_act) values ('"
                        + baseData.dqsj + "'," + Convert.ToString(i+1) + "," + iTemp + ")";

                    //Logger.logwrite(insertCmd);

                    baseData.WriteDB(insertCmd);

                    //Logger.logwrite("插入数据库完成");
                }
                catch (Exception ex)
                {
                    res = false;
                    string errtxt = "插入错误！" + ex.Message;
                    Logger.Errlogwrite(errtxt, WorkTag, "");
                }
            }
            /*
            if (res)
            {
                string txt = "处理完成";
                Logger.logwrite(txt, WorkTag, "");
            }
            */
        }

        public void SaveMeas()
        {
            string WorkTag = "【监控：历史趋势】";
            bool res = true;
            try
            {
                string meas_value_tb_values = "(REV_TIME,TEMP_ACT1,TEMP_ACT2,TEMP_ACT3,TEMP_ACT4,TEMP_ACT5,TEMP_ACT6,TEMP_ACT7,TEMP_ACT8,TEMP_ACT9,TEMP_ACT10," +
                    "mqzgyl,mqzgll,zrkqyl,prere_kqll,prere_mqll,onere_kqll,onere_mqll,twore_kqll," +
                    "twore_mqll,junre_up_kqll,junre_up_mqll,junre_low_kqll,junre_low_mqll)";
                string insertCmd = "insert into meas_value_tb"+ meas_value_tb_values + "values ('"
                    + baseData.dqsj + "'" ;
                //KepTagsMonitor中前22个字段要插入meas_value_tb表
                for (int i = 0; i <= 22; i++)
                {
                       insertCmd += ",'" + baseData.opcValues[OPCHelper.KepTagsMonitor[i]] + "'";
                }
                insertCmd += ")";
                baseData.WriteDB(insertCmd);
            }
            catch (Exception ex)
            {
                res = false;
                string errtxt = "插入错误！" + ex.Message;
                Logger.Errlogwrite(errtxt, WorkTag, "");
            }

            /*
            if (res)
            {
                string txt = "处理完成";
                Logger.logwrite(txt, WorkTag, "");
            }
            */
        }

        static DateTime dtNextRecordTime = DateTime.Now;

        public void Consume()
        {
            string WorkTag = "【记录煤气消耗】";
            try
            {
                if (Convert.ToDateTime(DateTime.Now) > Convert.ToDateTime(dtNextRecordTime)) {
                    int iCurrentShift = 2;  // 当前班次

                    //20.30 - 8.30
                    if ((dtNextRecordTime.Hour > 20 || dtNextRecordTime.Hour < 8)
                           || (dtNextRecordTime.Hour == 20 && dtNextRecordTime.Minute >= 30)
                           || (dtNextRecordTime.Hour == 8 && dtNextRecordTime.Minute < 30))
                    {
                        iCurrentShift = 1;
                        if(dtNextRecordTime.Hour >= 20)
                        dtNextRecordTime = dtNextRecordTime.AddDays(1);
                        dtNextRecordTime = new DateTime(dtNextRecordTime.Year, dtNextRecordTime.Month,
                            dtNextRecordTime.Day, 8, 30, 0);
                    }

                    else if ((dtNextRecordTime.Hour > 8 || dtNextRecordTime.Hour < 20)
                            || (dtNextRecordTime.Hour == 8 && dtNextRecordTime.Minute >= 30)
                            || (dtNextRecordTime.Hour == 20 && dtNextRecordTime.Minute < 30))
                    {
                        iCurrentShift = 2;
                        dtNextRecordTime = new DateTime(dtNextRecordTime.Year, dtNextRecordTime.Month,
                            dtNextRecordTime.Day, 20, 30, 0);
                    }

                    Logger.logwrite("下一次记录时间为：", dtNextRecordTime.ToString());

                    string strSql = "select * from consume_tb where record_date = '";
                    strSql += string.Format("{0:yyyy-MM-dd}", DateTime.Now);
                    strSql += "' and shift = '" + iCurrentShift.ToString() + "'";
                    DataTable dtShift = baseData.ReadDB(strSql);

                    if (dtShift.Rows.Count == 0)
                    {
                        string strInsert = "insert into consume_tb values ('";
                        strInsert += string.Format("{0:yyyy-MM-dd}", DateTime.Now);
                        strInsert += "', '" + iCurrentShift.ToString() + "', '" + baseData.opcValues["L2Com.IP1.mqlj"] + "')";
                        baseData.WriteDB(strInsert);
                    }
                }
            }
            catch (Exception ex)
            {
                string errtxt = "插入错误！" + ex.Message;
                Logger.Errlogwrite(errtxt, WorkTag, "");
            }
        }

        private void SaveTrackItem(string index, string sheet_no)
        {
            string Splate_length, Splate_width, Splate_thk, fur_splate_location, fur_splate_speed, fur_splate_time, fur_splate_id, fur_splate_temp;
            string Splate_location;
            Splate_length = baseData.opcValues["L2Com.EP1.Track.Plate_Length_" + index];
            Splate_width = baseData.opcValues["L2Com.EP1.Track.Plate_Width_" + index];
            Splate_thk = baseData.opcValues["L2Com.EP1.Track.Plate_Thickness_" + index];
            fur_splate_location = baseData.opcValues["L2Com.EP1.Track.Plate_HeadPosition_" + index];
            fur_splate_speed = baseData.opcValues["L2Com.EP1.Track.Plate_ActualSpeed_" + index];
            fur_splate_time = baseData.opcValues["L2Com.EP1.Track.Plate_ActualStayingTime_" + index];

            fur_splate_id = "0";
            switch (index)
            {
                case "ch1":
                    Splate_location = "1";
                    break;
                case "ch2":
                    Splate_location = "2";
                    break;
                case "dch":
                    Splate_location = "4";
                    break;
                default:
                    Splate_location = "3";
                    fur_splate_id = Convert.ToString(Convert.ToInt32(index));
                    break;
            }

            fur_splate_temp = baseData.opcValues["L2Com.IP1.wdact06"];

            string insertCmd = "insert into HTF_PLATE_TRACK(Rev_time, sheet_no, Splate_length, Splate_width, Splate_thk, Splate_location, Fur_splate_id, "
                + "  Fur_splate_location, Fur_splate_speed, Fur_splate_time, Fur_splate_temp ) values ('"
                + baseData.dqsj + "'" + ",'" + sheet_no + "','" + Splate_length + "','" + Splate_width + "','" + Splate_thk
                + "','" + Splate_location + "','" + fur_splate_id + "','" + fur_splate_location + "','" + fur_splate_speed + "','" + fur_splate_time + "','" + fur_splate_temp
                + "')";
            baseData.WriteDB(insertCmd);
        }

        public void SaveTrack()
        {
            string WorkTag = "【监控：跟踪】";

            bool res = true;
            string plate_id = "";
            try
            {
                for (int i = 0; i < INDEXS.Length; i++)
                {
                    string tag = "L2Com.EP1.Track.Plate_ID_" + INDEXS[i];
                    plate_id = baseData.opcValues[tag];
                    if (plate_id != "")
                        SaveTrackItem(INDEXS[i], plate_id);
                }
            }
            catch (Exception ex)
            {
                res = false;
                string errtxt = "插入错误！" + ex.Message;
                Logger.Errlogwrite(errtxt, WorkTag, "");
            }

            if (res)
            {
                string txt = "处理完成";
                Logger.logwrite(txt, WorkTag, "");
            }
        }

        public void DealRepeateCancle()
        {
            //查看有没有重复撤销的
            string sql = "select top 20 PLAN_NO from plan_tb  group by PLAN_NO having count(SHEET_NO) > count(distinct SHEET_NO) ";
            DataTable dt = baseData.ReadDB(sql);
            if (dt.Rows.Count == 0) return;
            /**如果对于同一个 计划号 + 钢坯号 出现重复，则说明是
             * 1- 重复撤销，但最后【又确认回来了】此时需要把撤销信息【全部】删除，只保留待生产的计划信息。
             *     例如对于计划A（有10支坯），我确认，再撤销，再确认，此时plan_tb中会有20条记录（10条是需要生产的，10条是撤销的，其实这10条撤销的应该删除）
             *     如果多次撤销，错误数据会更多
             * 2- 重复撤销，且最后【没有确认回来】此时需要把撤销信息删除【一部分】，只保留最后一次撤销的信息
             * 
             * 这两种情况的区别就是计划表中有没有与重复撤销的相同的  计划号 + 钢坯号  处于待生产状态
             */
           
            
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //对于重复撤销的，查看有没有又确认回来
                //sql = string.Format("select top 1 PLAN_NO,SHEET_NO  from plan_tb where PLAN_NO = '{0}' and SHEET_NO = '{1} and is_completed = 0", dt.Rows[i]["PLAN_NO"], dt.Rows[i]["SHEET_NO"]);
                sql = "select " +
                    "sum(case when is_completed = 0 then 1 else 0 end) as uncompletedNum," +//未入炉
                    "sum(case when is_completed = 1 then 1 else 0 end) as completedNum," +//已入炉
                    "sum(case when is_completed = 2 then 1 else 0 end) as cancledNum," +//撤销
                    "sum(case when is_completed = 4 then 1 else 0 end) as passdNum," +//强制完成
                    "count(1) as sumNum " +
                    "from plan_tb where PLAN_NO = '" + dt.Rows[i]["PLAN_NO"] + "'";
                DataTable dtRePlan = baseData.ReadDB(sql);
                if(Convert.ToInt32(dtRePlan.Rows[0]["completedNum"]) > 0)//撤销之后又回来的，要删除全部撤销信息
                {
                    Logger.DBlogwrite("重复撤销了计划号" + dt.Rows[i]["PLAN_NO"]  + "的钢坯。且又重新确认生产");
                    sql = string.Format("delete from plan_tb where PLAN_NO = '{0}'  and is_completed = 2", dt.Rows[i]["PLAN_NO"]);
                    baseData.WriteDB(sql);
                    Logger.DBlogwrite("【全部删除撤销】删除了重复计划号" + dt.Rows[i]["PLAN_NO"]  + "的钢坯的全部撤销信息。" + sql);
                }
                else//撤销之后没回来的，要删除一部分撤销信息
                {
                    Logger.DBlogwrite("重复撤销了计划号" + dt.Rows[i]["PLAN_NO"] + "的钢坯。");
                    sql = string.Format("delete from plan_tb where PLAN_NO = '{0}' and  ID not in (select MAX(ID) as ID from plan_tb group by SHEET_NO)", dt.Rows[i]["PLAN_NO"]);
                    baseData.WriteDB(sql);
                    Logger.DBlogwrite("【部分删除撤销】删除了重复计划号" + dt.Rows[i]["PLAN_NO"]  + "的钢坯的部分撤销信息。" + sql);
                }
                
            }
        }
    }
}
