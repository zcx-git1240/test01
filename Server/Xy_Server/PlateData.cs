using System;
using System.Data;

namespace Zp_Server
{
    class PlateData : WorkerBase
    {
        public PlateData(BaseData inBaseData) : base(inBaseData) { }

        //计划取消
        public void CanclePlateData()
        {
            string StrSqlCancel = "select PLAN_NO,SHEET_NO from reject_from_mes_tb Where is_canceled = 0 order by reject_from_mes_tb_id";//取消任务还未执行的
            DataTable dtCancel = ReadDB(StrSqlCancel);
            try
            {
                if (dtCancel.Rows.Count > 0)//还有未执行的取消计划
                {
                    //对plan_tb中还未入炉的钢坯进行撤销
                    for (int i = 0; i < dtCancel.Rows.Count; i++)
                    {

                        DateTime dtNow = DateTime.Now;
                        string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dtNow);

                        //没有对撤销的钢坯进行检查，而是直接更新，若影响的数量大于0，说明撤销成功，如果等于0，说明钢坯已入炉
                        //string sqlCancel = "update plan_tb SET is_completed = 2 Where  PLAN_NO = '" + dtCancel.Rows[i]["PLAN_NO"] + "' AND SHEET_NO = '" + dtCancel.Rows[i]["SHEET_NO"]
                        //    + "' AND is_completed = 0";
                        string sqlCancel = "delete from plan_tb where PLAN_NO = '" + dtCancel.Rows[i]["PLAN_NO"] + "' AND SHEET_NO = '" + dtCancel.Rows[i]["SHEET_NO"] + "'";
                        int CancelNum = UpdateDB(sqlCancel);
                        Logger.logwrite("对计划进行撤销" + sqlCancel);

                        //在实绩表中对炉内钢坯进行撤销（如果有的话）
                        string strSql = "update furnace_billet_data_tb set PLAN_NO = null where out_time is null and PLAN_NO = '"
                            + dtCancel.Rows[i]["PLAN_NO"] + "' and SHEET_NO = '" + dtCancel.Rows[i]["SHEET_NO"] + "'";
                        WriteDB(strSql);
                        Logger.logwrite("在实绩表中对炉内钢坯进行撤销" + strSql);
                        //在进出炉表中对炉内钢坯进行撤销
                        strSql = "update billet_in_out_info_tb set PLAN_NO = null where OUT_FURNACE_TIME is null and PLAN_NO  = '"
                            + dtCancel.Rows[i]["PLAN_NO"] + "' and IN_SHEET_NO = '" + dtCancel.Rows[i]["SHEET_NO"] + "'";
                        WriteDB(strSql);
                        Logger.logwrite("在进出炉表中对炉内钢坯进行撤销" + strSql);

                        string sqlCancelDone = "update reject_from_mes_tb SET IS_CANCELED = 1,CANCELED_TIME = '" + sj + "' Where  PLAN_NO = '" + dtCancel.Rows[i]["PLAN_NO"] + "' AND SHEET_NO = '" + dtCancel.Rows[i]["SHEET_NO"] + "'";
                        WriteDB(sqlCancelDone);
                        Logger.logwrite("撤销了计划号：" + dtCancel.Rows[i]["PLAN_NO"] + "  钢坯号：" + dtCancel.Rows[i]["SHEET_NO"] + "的钢坯");
                    }

                }
            }
            catch (Exception e)
            {
                string errtxt = "钢板撤销计划出错！" + e.Message;
                Logger.Errlogwrite(errtxt);
            }
        }
        // 关联计划信息到钢坯
        public void AddPlateData()
        {
            try
            {
                string StrSqlPlan = "select * from plan_tb Where is_completed = 0 order by ID";
                DataTable dtPlans = ReadDB(StrSqlPlan);
                if (dtPlans.Rows.Count > 0)  // 有计划
                {
                    // 炉内钢坯信息
                    string strSqlBillets = "select * from furnace_billet_data_tb Where out_time is null and PLAN_NO is null order by id";
                    DataTable dtBillets = ReadDB(strSqlBillets);

                    // 入炉出炉信息关联
                    string in_out_tb = "select * from billet_in_out_info_tb Where OUT_FURNACE_TIME is null and PLAN_NO is null order by PLAN_TB_ID";
                    DataTable dtIn_out_tb = ReadDB(in_out_tb);

                    int iCounts = dtPlans.Rows.Count < dtBillets.Rows.Count ? dtPlans.Rows.Count : dtBillets.Rows.Count;
                    iCounts = iCounts < dtIn_out_tb.Rows.Count ? iCounts : dtIn_out_tb.Rows.Count;
                    for (int k = 0; k < iCounts; k++)
                    {
                        string strCmd = "update furnace_billet_data_tb set PLAN_NO = '" + dtPlans.Rows[k]["PLAN_NO"]
                                        + "', SHEET_NO = '" + dtPlans.Rows[k]["SHEET_NO"]
                                        + "', GRADE = '" + dtPlans.Rows[k]["STEEL_GRADE"]
                                        + "', billet_lenght = '" + dtPlans.Rows[k]["SHEET_ACTUAL_LENGTH"]
                                        + "', billet_width = '" + dtPlans.Rows[k]["SHEET_ACTUAL_WIDTH"]
                                        + "', billet_thickness = '" + dtPlans.Rows[k]["SHEET_ACTUAL_THICKNESS"]
                                        + "', billet_weight = '" + dtPlans.Rows[k]["SHEET_WEIGHT_T"]
                                        + "' where id ='" + dtBillets.Rows[k]["id"] + "'";
                        WriteDB(strCmd);
                        Logger.logwrite(strCmd);

                        string strCmd2 = "update billet_in_out_info_tb set PLAN_NO = '" + dtPlans.Rows[k]["PLAN_NO"]
                                       + "', IN_SHEET_NO = '" + dtPlans.Rows[k]["SHEET_NO"]
                                       + "' where PLAN_TB_ID ='" + dtIn_out_tb.Rows[k]["PLAN_TB_ID"] + "'";
                        WriteDB(strCmd2);
                        Logger.logwrite(strCmd2);

                        strCmd = "update plan_tb set is_completed = 1 where ID ='" + dtPlans.Rows[k]["ID"] + "'";//置完成标志
                        WriteDB(strCmd);

                        Logger.logwrite("关联计划号到钢坯");
                    }
                    dtBillets.Dispose();
                }
                dtPlans.Dispose();
            }

            catch (Exception ex)
            {
                string errtxt = "钢板关联计划出错！" + ex.Message;
                Logger.Errlogwrite(errtxt);
            }
        }

        //重新生产
        public void ReProduce()
        {
            string StrSql_re = "select * from sheet_to_reinfur Where FLAG=0";
            DataTable Re_Data = ReadDB(StrSql_re);
            if (Re_Data.Rows.Count > 0)//有记录
            {
                for (int k = 0; k < Re_Data.Rows.Count; k++)
                {
                    string entry_id = Re_Data.Rows[k]["SHEET_NO"].ToString().Trim();
                    try
                    {
                        string insertCmd = "update htf_plan_produce set UPPER_FLAG=0 where sheet_no='" + entry_id + "'";//重置上料标志
                        WriteDB(insertCmd);

                        insertCmd = "update sheet_to_reinfur set FLAG=1 where sheet_no='" + entry_id + "'";//置完成标志
                        WriteDB(insertCmd);

                        string txt = entry_id + " 重新生产。";
                        Logger.logwrite(txt);
                    }
                    catch (Exception ex)
                    {
                        string errtxt = entry_id + " 重置上料标志出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                        string insertCmd = "update sheet_to_reinfur set FLAG=1 where sheet_no='" + entry_id + "'";//置完成标志
                        WriteDB(insertCmd);
                    }
                }
            }
            Re_Data.Dispose();
        }

        //记录班消耗
        public void HtfConsume()
        {
            if ((string.Compare(baseData.dqsj.Substring(11, 8), "00:00:30") <= 0) || ((string.Compare(baseData.dqsj.Substring(11, 8), "08:00:30") <= 0) && (string.Compare(baseData.dqsj.Substring(11, 8), "08:00:00") >= 0)) || ((string.Compare(baseData.dqsj.Substring(11, 8), "16:00:30") <= 0) && (string.Compare(baseData.dqsj.Substring(11, 8), "16:00:00") >= 0)))
            {
                if (!baseData.flags[14])
                {
                    baseData.flags[14] = true;
                    try
                    {
                        string procdate = baseData.dqsj.Substring(0, 10);
                        long mqlj = Convert.ToInt32(baseData.opcValues["L2Com.IP1.mqlj"]);
                        string insertCmd = "insert into consume_tb(PROD_DATE,SHIFT_ID,CREW_ID,GASSTART) values('" + procdate + "','" + baseData.shiftID + "','" + baseData.crewID + "'," + mqlj + ")";
                        WriteDB(insertCmd);
                        string txt = "班消耗开始记录完成！";
                        Logger.logwrite(txt);
                    }
                    catch (Exception ex)
                    {
                        string errtxt = "记录班消耗开始出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }
                }

            }
            else
            {
                baseData.flags[14] = false;
            }

            if (((string.Compare(baseData.dqsj.Substring(11, 8), "07:59:58") >= 0) && (string.Compare(baseData.dqsj.Substring(11, 8), "08:00:00") < 0)) || ((string.Compare(baseData.dqsj.Substring(11, 8), "15:59:58") >= 0) && (string.Compare(baseData.dqsj.Substring(11, 8), "16:00:00") < 0)) || ((string.Compare(baseData.dqsj.Substring(11, 8), "23:59:58") >= 0) && (string.Compare(baseData.dqsj.Substring(11, 8), "23:59:59") <= 0)))
            {
                if (!baseData.flags[13])
                {
                    baseData.flags[13] = true;
                    string procdate = baseData.dqsj.Substring(0, 10);
                    try
                    {
                        string StrSql_id = "select GASSTART from  consume_tb where to_char(PROD_DATE,'YYYY-MM-DD')='" + procdate + "' and SHIFT_ID= '" + baseData.shiftID + "'";
                        DataTable IdMqxh_Data = ReadDB(StrSql_id);
                        if (IdMqxh_Data.Rows[0]["GASSTART"].ToString().Trim() != "")
                        {
                            long gasstart = Convert.ToInt32(IdMqxh_Data.Rows[0]["GASSTART"].ToString().Trim());
                            long mqend = Convert.ToInt32(baseData.opcValues["L2Com.IP1.mqlj"]);
                            long bxh = 0;
                            if (mqend > gasstart)
                            {
                                bxh = mqend - gasstart;
                            }
                            else { bxh = mqend; }

                            if ((bxh > 8000) && (gasstart == 0 || mqend == 0))
                            {
                                bxh = 3200;
                            }

                            string updateCmd = "update consume_tb set GASEND = " + mqend + ",GAS_CONSUME=" + bxh + " where  to_char(PROD_DATE,'YYYY-MM-DD')='" + procdate + "' and SHIFT_ID= '" + baseData.shiftID + "'";
                            WriteDB(updateCmd);
                            string txt = "班消耗结束记录完成！";
                            Logger.logwrite(txt);
                        }
                        IdMqxh_Data.Dispose();
                    }
                    catch (Exception ex)
                    {
                        string errtxt = "记录班消耗总量出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }
                }
            }
            else
            {
                baseData.flags[13] = false;
            }
        }
    }
}
