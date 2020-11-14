using System;
using System.Data;
using System.Text;

namespace Zp_Server
{
    class TemperingFurnace : WorkerBase
    {
        public static string upper_id_bef = "";
        public static string entry_id_bef = "";
        public static string charge_id_bef = "";
        public static string out_id_bef = "";
        public static string platelen_bef = "";
        public static string platewidth_bef = "";
        public static string platehtk_bef = "";

        public TemperingFurnace(BaseData inBaseData) : base(inBaseData)
        {
            /*
            if (baseData.opcValues["L2Com.EP1.R_Upper_Id"].Trim() != "")
                upper_id_bef = baseData.opcValues["L2Com.EP1.R_Upper_Id"].Trim();
            if (baseData.opcValues["L2Com.EP1.R_Charge_Id"].Trim() != "")
                charge_id_bef = baseData.opcValues["L2Com.EP1.R_Charge_Id"].Trim();
            if (baseData.opcValues["L2Com.EP1.R_Length"].Trim() != "" && baseData.opcValues["L2Com.EP1.R_Length"].Trim() != "0")
                platelen_bef = baseData.opcValues["L2Com.EP1.R_Length"].Trim();
            if (baseData.opcValues["L2Com.EP1.R_Width"].Trim() != "" && baseData.opcValues["L2Com.EP1.R_Width"].Trim() != "0")
                platewidth_bef = baseData.opcValues["L2Com.EP1.R_Width"].Trim();
            if (baseData.opcValues["L2Com.EP1.R_Thickness"].Trim() != "" && baseData.opcValues["L2Com.EP1.R_Thickness"].Trim() != "0")
                platehtk_bef = baseData.opcValues["L2Com.EP1.R_Thickness"].Trim();
                */
        }

        static bool m_bNewEnter = false;  // 信号从0跳到1，才表示新的钢坯装炉完成操作。
        //static bool m_bNewOut = false;
        static bool m_bNewReject = false;
        static string m_strEnterId = "0";       // 到达炉内第一钢位的
        static string m_strOnereId = "0";  // 用于记录3段钢坯号的变化
        static string m_strTworeId = "0";
        static string m_strJunreId = "0";
        static string m_strOutId = "0";

        static string m_strPlanNo = "-1"; // 用于记录当前计划号
        static string m_strSheetNo = "";  //用于记录当前钢坯号
        static int m_iWeight = 0;   // 实测重量
        static int m_iEnterWeight = 0;   // 记录给到达第一钢位的重量
        static bool bReWeight = true;   // 重量重置

        static bool m_bReStart = true;    // 用于重启时读取当前的点位值
        static int m_iWPlanNo = 0;    // 每根钢，将批次编号写给1级。（0~5）

        public void EnterFurnace()
        {
            // 1级称重不断变化，记录最大值。当为0时，重置下一次。
            if (baseData.opcValues["L2Com.EP1.Enter_Weight"] == "0")
            {
                bReWeight = true;
            }
            else if (bReWeight || Convert.ToInt32(baseData.opcValues["L2Com.EP1.Enter_Weight"]) > m_iWeight)
            {
                m_iWeight = Convert.ToInt32(baseData.opcValues["L2Com.EP1.Enter_Weight"]);
                bReWeight = false;
            }

            string strPlanNo = "";
            //0号炉位上还没有坯，此时炉外或许已经有坯了，也可能没坯
            if ((baseData.opcValues["L2Com.EP1.Enter_Done"]) == "0")
            {
                m_bNewEnter = true;
            }
            else if (m_bNewEnter)  //钢坯装炉完成
            {
                m_bNewEnter = false;
                string WorkTag = "【装炉完成】";
                Logger.logwrite(WorkTag);

                try
                {
                    string info_txt = "钢坯装炉完成";
                    Logger.logwrite(info_txt, WorkTag, info_txt);

                    // 获得当前计划表未关联计划的第1个计划号，钢坯号
                    string strSql = "select top 1 PLAN_NO, SHEET_NO ,ID,STEEL_GRADE from plan_tb where is_completed = 0 order by id";
                    DataTable dtPlanNo = ReadDB(strSql);

                    // 如果和当前计划号不同，则向1级写新的编号（0~5）
                    if (dtPlanNo.Rows.Count > 0)
                    {
                        //保证dtPlanNo中的数据唯一性
                        //dtPlanNo = dealRepeatPlan(dtPlanNo.Rows[0]["PLAN_NO"].ToString(), dtPlanNo.Rows[0]["SHEET_NO"].ToString());


                        //新的计划号
                        strPlanNo = dtPlanNo.Rows[0][0].ToString();
                        //m_strSheetNo = dtPlanNo.Rows[0]["SHEET_NO"].ToString();
                        baseData.opcItems["L2Com.EP1.W_Plan_No"].Write(dtPlanNo.Rows[0]["STEEL_GRADE"].ToString());//对应了钢种信息，检查一下
                    }

                    // 程序重启，读取计划号编号，及计划号
                    //if (m_bReStart)
                    //{
                    //    string strWPlanNo = baseData.opcValues["L2Com.EP1.W_Plan_No"];
                    //    if (strWPlanNo.Length > 0)
                    //    {
                    //        m_iWPlanNo = Convert.ToInt32(strWPlanNo);
                    //    }
                    //    m_bReStart = false;
                    //}
                    //else if (m_strPlanNo != strPlanNo)//当前计划号  不等于  计划表里的计划号
                    //{
                    //    Logger.logwrite("准备将新的批次编码写入1级");

                    //    if (++m_iWPlanNo == 2)
                    //        m_iWPlanNo = 0;
                    //}

                    m_strPlanNo = strPlanNo;
                    
                    Logger.logwrite("写入批次编码" + m_iWPlanNo.ToString());

                    if (baseData.opcValues["L2Com.EP1.W_Id"].Length == 0)
                    {
                        // 将下一个钢坯ID写入PLC给1级
                        strSql = "select Max(id) from furnace_billet_data_tb";
                        DataTable dtId = ReadDB(strSql);
                        int iCurrentId = Convert.ToInt32(dtId.Rows[0][0]) + 1;
                        //int iCurrentId = Convert.ToInt32(dtPlanNo.Rows[0]["ID"]);
                        baseData.opcItems["L2Com.EP1.W_Id"].Write(iCurrentId.ToString());
                        Logger.logwrite("给1级写入入炉钢坯ID" + iCurrentId.ToString());

                        //设置接收时间
                        //DateTime dtNow = DateTime.Now;
                        //string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dtNow);
                        //string sql = "update plan_tb set rev_time = '" + sj + "' where id = '" + iCurrentId.ToString() + "'";
                        //WriteDB(sql);
                    }

                    m_iEnterWeight = m_iWeight;
                }
                catch (Exception ex)
                {
                    string errtxt = "处理错误" + ex.Message;
                    Logger.Errlogwrite(errtxt, WorkTag);
                }
                //baseData.opcItems["L2Com.EP1.R_Upper"].Write("0");
            }

            // 到达第1炉位表示入炉
            if ((baseData.opcValues["L2Com.EP1.Enter_Id"].Length > 0)
                && Convert.ToInt32(baseData.opcValues["L2Com.EP1.Enter_Id"]) > Convert.ToInt32(m_strEnterId)
                && (baseData.opcValues["L2Com.EP1.Enter_Id"] != "0"))
            {
                string info_txt = "【钢坯入炉（到达第1炉位）】";
                Logger.logwrite(info_txt);

                try
                {
                    m_strEnterId = baseData.opcValues["L2Com.EP1.Enter_Id"];

                    Logger.logwrite("入炉钢坯（到达第1炉位）：", m_strEnterId);

                    // 如果已有此ID，则不再添加钢坯   （如果钢坯号不能唯一标识钢坯，需要通过计划号 + 钢坯号 的方式进行判断）
                    string strSql = "select id from furnace_billet_data_tb where id = '" + m_strEnterId + "'";
                    DataTable dtBillet = ReadDB(strSql);

                    if (dtBillet.Rows.Count > 0)
                    {
                        Logger.logwrite("已有该ID，不再添加钢坯", m_strEnterId);
                        return;
                    }

                    // 获得当前计划表第1个计划号
                    strSql = "select top 1 PLAN_NO, SHEET_NO ,ID from plan_tb where is_completed = 0 order by id";
                    //strSql = "select top 1 PLAN_NO, SHEET_NO ,ID from plan_tb where is_completed = 0 and ID = '" + m_strEnterId + "'";
                    DataTable dtPlanNo = ReadDB(strSql);
                    string strSheetNo = "";

                    if (dtPlanNo.Rows.Count > 0)
                    {
                        strPlanNo = dtPlanNo.Rows[0][0].ToString();
                        strSheetNo = dtPlanNo.Rows[0]["SHEET_NO"].ToString();
                    }
                    DateTime dtNow = DateTime.Now;
                    // 插入1条钢坯数据
                    string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dtNow);

                    if (m_iEnterWeight > 9999)
                        m_iEnterWeight = 9999;
                    string insertCmd = "insert into furnace_billet_data_tb(id, loading_time, actual_weight)"
                                       + "values('" + m_strEnterId + "', '" + sj + "', '" + m_iEnterWeight + "');";
                    Logger.logwrite(insertCmd);
                    WriteDB(insertCmd);

              //      string insert_billet_in_out_info_sql = "insert into billet_in_out_info_tb( PLAN_TB_ID,IN_FURNACE_START_TIME,PLAN_NO,IN_FURNACE_SHIFT_NO,IN_SHEET_NO,SHEET_WEIGHT_T)  " +
              //"values('" + m_strEnterId + "','" + sj + "','" + strPlanNo + "','" + Currentshift(dtNow).ToString() + "','" + strSheetNo + "','" + m_iEnterWeight + "')";


                    string insert_billet_in_out_info_sql = "insert into billet_in_out_info_tb( PLAN_TB_ID,IN_FURNACE_START_TIME,IN_FURNACE_SHIFT_NO,SHEET_WEIGHT_T)  " +
              "values('" + m_strEnterId + "','" + sj + "','"  + Currentshift(dtNow).ToString() + "','"   + m_iEnterWeight + "')";
                    Logger.logwrite("向进出炉信息表中写入【入炉信息】 " + insert_billet_in_out_info_sql);
                    WriteDB(insert_billet_in_out_info_sql);

                    // 如果钢坯匹配标记为0，则将坯料号置为强制   
                    string StrSqlMatch = "select * from match_plan_flag_tb";
                    DataTable dtMatch = ReadDB(StrSqlMatch);
                    if (dtMatch.Rows[0][0].ToString() == "0")
                    {
                        string strCmd = "update furnace_billet_data_tb set PLAN_NO = 'PASS' where id = " + m_strEnterId;
                        WriteDB(strCmd);
                        Logger.logwrite(strCmd);

                        string insert_billet_in_out_info_sql_pass = "update  billet_in_out_info_tb set PLAN_NO = 'PASS' where PLAN_TB_ID=" + m_strEnterId;
                        WriteDB(insert_billet_in_out_info_sql_pass);
                        Logger.logwrite("向进出炉信息表中写入【入炉信息没有计划号】 " + insert_billet_in_out_info_sql_pass);
                        return;
                    }

            

                    // 发送3级电文
                    //if (!Program.socketClient.Connected)
                    //{
                    //    Logger.logwrite("3级接收服务器已断开，尝试重新连接");
                    //    Program.ConnectToMes();
                    //}

                    //string strJ1m1 = "0079J1M101";
                    //strJ1m1 += dtNow.ToString("yyyyMMddHHmmss");
                    //strJ1m1 += "J1M1D";

                    //// 电文补空格
                    //strPlanNo = strPlanNo.PadLeft(10, ' ');
                    //strJ1m1 += strPlanNo;
                    //strJ1m1 += dtNow.ToString("yyyyMMddHHmmss");

                    //strJ1m1 += Currentshift(dtNow).ToString();

                    //strSheetNo = strSheetNo.PadLeft(20, ' ');
                    //strJ1m1 += strSheetNo;

                    //string strWeight = m_iEnterWeight.ToString().PadLeft(4, '0');
                    //strJ1m1 += strWeight;
                    //strJ1m1 += "\n";
                    //Logger.logwrite("入炉电文组装完毕：" + strJ1m1);

                    //if (Program.socketClient.Connected)
                    //{
                    //    Logger.logwrite("向3级发送入炉电文：" + strJ1m1);
                    //    var buffter = Encoding.UTF8.GetBytes(strJ1m1);
                    //    Program.socketClient.Send(buffter);
                    //}
                }
                catch (Exception ex)
                {
                    string errtxt = "入炉处理错误" + ex.Message;
                    Logger.Errlogwrite(errtxt);
                }
            }

        }

        int Currentshift(DateTime dt)
        {
            int iShift = 0;
            // 班次
            if (dt.Hour > 8 && dt.Hour < 20)
            {
                iShift = 2;//早班
            }
            else 
            {
                iShift = 1;//晚班
            }

            return iShift;
        }

        public void OutFurnaceFinish()
        {
            string WorkTag = "【钢坯出炉】 ";

            // 出炉ID变化，则出炉
            if ((baseData.opcValues["L2Com.EP1.Out_Id"].Length > 0)
                && Convert.ToInt32(baseData.opcValues["L2Com.EP1.Out_Id"]) > Convert.ToInt32(m_strOutId)
                && (baseData.opcValues["L2Com.EP1.Out_Id"] != "0"))
            {
                try
                {
                    string info_txt = "钢坯出炉";
                    Logger.logwrite(info_txt, WorkTag);

                    // 如果重启，则置出炉ID为当前ID。
                    if (m_strOutId == "0")
                        m_strOutId = baseData.opcValues["L2Com.EP1.Out_Id"];

                    DateTime dtNow = DateTime.Now;
                    string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dtNow);
                    string strBilletId = m_strOutId;

                    string strSql = "UPDATE furnace_billet_data_tb SET out_time = '";
                    strSql += sj;
                    strSql += "' WHERE id = '" + strBilletId + "'";
                    Logger.logwrite(strSql);
                    WriteDB(strSql);

                    m_strOutId = baseData.opcValues["L2Com.EP1.Out_Id"];

                    //向进炉出炉表中写入出炉信息
                    strSql = "select * from furnace_billet_data_tb where id = '" + strBilletId + "'";
                    DataTable dtBilletsData = ReadDB(strSql);
                    Logger.logwrite(strSql);

                    DateTime dtEnterTime = Convert.ToDateTime(dtBilletsData.Rows[0]["loading_time"]);
                    DateTime dtOnereTime = Convert.ToDateTime(dtBilletsData.Rows[0]["onere_time"]);
                    DateTime dtJunreTime = Convert.ToDateTime(dtBilletsData.Rows[0]["junre_time"]);
                    Logger.logwrite("读取时间成功");
                    // 在炉时间
                    TimeSpan tsInFce = dtNow - dtEnterTime;

                    // 预热段时间
                    TimeSpan tsYure = dtOnereTime - dtEnterTime;

                    // 加热段时间
                    TimeSpan tsJiare = dtJunreTime - dtOnereTime;

                    // 均热段时间
                    TimeSpan tsJunre = dtNow - dtJunreTime;
                    Logger.logwrite("计算时间成功");
                    // 温度信息
                    // 预热段
                    strSql = "select sum(TEMP_ACT9), count(TEMP_ACT9) from meas_value_tb where REV_TIME >= '"
                            + dtBilletsData.Rows[0]["loading_time"] + "' and REV_TIME <= '"
                            + dtBilletsData.Rows[0]["onere_time"] + "'";
                    DataTable dtMeasData = ReadDB(strSql);
                    int YuReTemp = (int)Convert.ToSingle(dtMeasData.Rows[0][0].ToString())
                                    / Convert.ToInt32(dtMeasData.Rows[0][1].ToString());
                    Logger.logwrite("预热成功");
                    // 加热段
                    strSql = "select sum(TEMP_ACT5), count(TEMP_ACT5) from meas_value_tb where REV_TIME >= '"
                            + dtBilletsData.Rows[0]["onere_time"] + "' and REV_TIME <= '"
                            + dtBilletsData.Rows[0]["junre_time"] + "'";
                    dtMeasData = ReadDB(strSql);
                    int JiaReiTemp = (int)Convert.ToSingle(dtMeasData.Rows[0][0].ToString())
                                    / Convert.ToInt32(dtMeasData.Rows[0][1].ToString());
                    Logger.logwrite("加热成功");
                    // 均热段
                    strSql = "select sum(TEMP_ACT3), count(TEMP_ACT3) from meas_value_tb where REV_TIME >= '"
                            + dtBilletsData.Rows[0]["junre_time"] + "' and REV_TIME <= '"
                            + dtBilletsData.Rows[0]["out_time"] + "'";
                    dtMeasData = ReadDB(strSql);
                    int JunReTemp = (int)Convert.ToSingle(dtMeasData.Rows[0][0].ToString())
                                    / Convert.ToInt32(dtMeasData.Rows[0][1].ToString());
                    Logger.logwrite("均热成功");
                    //string insert_billet_in_out_info_sql = "update  billet_in_out_info_tb(OUT_FURNACE_TIME,OUT_FURNACE_SHIFT_NO,RF_IN_FCE_TIME,RF_PRHT_IN_FCE_TIME,RF_HT_Z_IN_FCE_TIME,RF_NOR_Z_IN_FCE_TIME,RF_PRHT_TM,RF_HT_Z_TM,RF_NOR_Z_TM) values('" + sj + "','" + Currentshift(dtNow).ToString() + "','" +
                    //    tsInFce.TotalSeconds.ToString() + "','" + tsYure.TotalSeconds.ToString() + "','" + tsJiare.TotalSeconds.ToString() + "','" + tsJunre.TotalSeconds.ToString() + "','" +
                    //    YuReTemp.ToString() + "','" + JiaReiTemp.ToString() + "','" + JunReTemp.ToString() + "')" + "where PLAN_TB_ID = '" + strBilletId + "'";

                    string insert_billet_in_out_info_sql = string.Format("update  billet_in_out_info_tb set OUT_FURNACE_TIME='{0}',OUT_FURNACE_SHIFT_NO = {1},RF_IN_FCE_TIME = {2},RF_PRHT_IN_FCE_TIME = {3},RF_HT_Z_IN_FCE_TIME = {4},RF_NOR_Z_IN_FCE_TIME = {5},RF_PRHT_TM = '{6}',RF_HT_Z_TM = '{7}',RF_NOR_Z_TM = '{8}'  where PLAN_TB_ID ='{9}'",
                        sj, Currentshift(dtNow).ToString(),
                        tsInFce.TotalSeconds, tsYure.TotalSeconds, tsJiare.TotalSeconds, tsJunre.TotalSeconds,
                        YuReTemp.ToString(), JiaReiTemp.ToString(), JunReTemp.ToString(),
                        strBilletId);
                    Logger.logwrite("更新成功");
                    Logger.logwrite("向进炉出炉信息表中写入【出炉信息】 " + insert_billet_in_out_info_sql);
                    WriteDB(insert_billet_in_out_info_sql);

                    // 向3级发送出炉电文
                    // 发送3级电文
                    //if (!Program.socketClient.Connected)
                    //{
                    //    Logger.logwrite("3级接收服务器已断开，尝试重新连接");
                    //    Program.ConnectToMes();
                    //}

                    //string strJ1m102 = "0111J1M102";
                    //strJ1m102 += dtNow.ToString("yyyyMMddHHmmss");
                    //strJ1m102 += "J1M1D";

                    //strJ1m102 += dtNow.ToString("yyyyMMddHHmmss");

                    //strJ1m102 += Currentshift(dtNow).ToString();

                    //strSql = "select * from furnace_billet_data_tb where id = '" + strBilletId + "'";
                    //DataTable dtBilletsData = ReadDB(strSql);

                    //DateTime dtEnterTime = Convert.ToDateTime(dtBilletsData.Rows[0]["loading_time"]);
                    //DateTime dtOnereTime = Convert.ToDateTime(dtBilletsData.Rows[0]["onere_time"]);
                    //DateTime dtJunreTime = Convert.ToDateTime(dtBilletsData.Rows[0]["junre_time"]);

                    //// 在炉时间
                    //TimeSpan tsInFce = dtNow - dtEnterTime;
                    //int iSeconds = (int)tsInFce.TotalSeconds;
                    //string strTimeSpan = iSeconds.ToString().PadLeft(6, '0');
                    ////Logger.logwrite("在炉时间：" + strTimeSpan);
                    //strJ1m102 += strTimeSpan;

                    //// 预热段时间
                    //TimeSpan tsYure = dtOnereTime - dtEnterTime;
                    //strTimeSpan = tsYure.TotalSeconds.ToString().PadLeft(6, '0');
                    ////Logger.logwrite("预热段时间：" + strTimeSpan);
                    //strJ1m102 += strTimeSpan;

                    //// 加热段时间
                    //TimeSpan tsJiare = dtJunreTime - dtOnereTime;
                    //strTimeSpan = tsJiare.TotalSeconds.ToString().PadLeft(6, '0');
                    ////Logger.logwrite("加热段时间：" + strTimeSpan);
                    //strJ1m102 += strTimeSpan;

                    //// 均热段时间
                    //TimeSpan tsJunre = dtNow - dtJunreTime;
                    //iSeconds = (int)tsJunre.TotalSeconds;
                    //strTimeSpan = (iSeconds).ToString().PadLeft(6, '0');
                    ////Logger.logwrite("均热段时间：" + strTimeSpan);
                    //strJ1m102 += strTimeSpan;

                    //// 温度信息
                    //// 预热段
                    //strSql = "select sum(TEMP_ACT9), count(TEMP_ACT9) from meas_value_tb where REV_TIME >= '"
                    //        + dtBilletsData.Rows[0]["loading_time"] + "' and REV_TIME <= '"
                    //        + dtBilletsData.Rows[0]["onere_time"] + "'";
                    //DataTable dtMeasData = ReadDB(strSql);
                    //int iTemp = (int)Convert.ToSingle(dtMeasData.Rows[0][0].ToString())
                    //                / Convert.ToInt32(dtMeasData.Rows[0][1].ToString());
                    //strJ1m102 += iTemp.ToString().PadLeft(4, '0');

                    //// 加热段
                    //strSql = "select sum(TEMP_ACT5), count(TEMP_ACT5) from meas_value_tb where REV_TIME >= '"
                    //        + dtBilletsData.Rows[0]["onere_time"] + "' and REV_TIME <= '"
                    //        + dtBilletsData.Rows[0]["junre_time"] + "'";
                    //dtMeasData = ReadDB(strSql);
                    //iTemp = (int)Convert.ToSingle(dtMeasData.Rows[0][0].ToString())
                    //                / Convert.ToInt32(dtMeasData.Rows[0][1].ToString());
                    //strJ1m102 += iTemp.ToString().PadLeft(4, '0');

                    //// 均热段
                    //strSql = "select sum(TEMP_ACT3), count(TEMP_ACT3) from meas_value_tb where REV_TIME >= '"
                    //        + dtBilletsData.Rows[0]["junre_time"] + "' and REV_TIME <= '"
                    //        + dtBilletsData.Rows[0]["out_time"] + "'";
                    //dtMeasData = ReadDB(strSql);
                    //iTemp = (int)Convert.ToSingle(dtMeasData.Rows[0][0].ToString())
                    //                / Convert.ToInt32(dtMeasData.Rows[0][1].ToString());
                    //strJ1m102 += iTemp.ToString().PadLeft(4, '0');

                    //strJ1m102 += dtBilletsData.Rows[0]["PLAN_NO"].ToString().PadLeft(10, ' ');
                    //strJ1m102 += dtBilletsData.Rows[0]["Sheet_NO"].ToString().PadLeft(20, ' ');

                    //strJ1m102 += "\n";
                    //Logger.logwrite("出炉电文组装完毕：" + strJ1m102);

                    //if (Program.socketClient.Connected)
                    //{
                    //    Logger.logwrite("向3级发送出炉电文：" + strJ1m102);
                    //    var buffter = Encoding.UTF8.GetBytes(strJ1m102);
                    //    Program.socketClient.Send(buffter);
                    //}
                }

                catch (Exception ex)
                {
                    string errtxt = "处理错误" + ex.Message;
                    Logger.Errlogwrite(errtxt, WorkTag);
                }

            }
        }

        // 一加、二加、均热段的钢坯号变化
        public void OnSection()
        {
            string WorkTag = "";
            try
            {

                // 一加段钢坯序号变化
                if ((baseData.opcValues["L2Com.EP1.Onere_Id"].Length > 0)
                    && (baseData.opcValues["L2Com.EP1.Onere_Id"]) != m_strOnereId
                    && (baseData.opcValues["L2Com.EP1.Onere_Id"] != "0"))
                {
                    m_strOnereId = baseData.opcValues["L2Com.EP1.Onere_Id"];

                    string info_txt = "钢坯到达一加段：" + m_strOnereId;
                    Logger.logwrite(info_txt);
                    string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    string strSql = "UPDATE furnace_billet_data_tb SET onere_time = '";
                    strSql += sj;
                    strSql += "' WHERE id = '" + m_strOnereId + "'";
                    Logger.logwrite(strSql);
                    WriteDB(strSql);
                }

                // 二加段钢坯序号变化
                if ((baseData.opcValues["L2Com.EP1.Twore_Id"].Length > 0)
                    && (baseData.opcValues["L2Com.EP1.Twore_Id"]) != m_strTworeId
                    && (baseData.opcValues["L2Com.EP1.Twore_Id"] != "0"))
                {
                    m_strTworeId = baseData.opcValues["L2Com.EP1.Twore_Id"];

                    string info_txt = "钢坯到达二加段：" + m_strTworeId;
                    Logger.logwrite(info_txt);
                    string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    string strSql = "UPDATE furnace_billet_data_tb SET twore_time = '";
                    strSql += sj;
                    strSql += "' WHERE id = '" + m_strTworeId + "'";
                    Logger.logwrite(strSql);
                    WriteDB(strSql);
                }

                // 均热段钢坯序号变化
                if ((baseData.opcValues["L2Com.EP1.Junre_Id"].Length > 0)
                    && (baseData.opcValues["L2Com.EP1.Junre_Id"]) != m_strJunreId
                    && (baseData.opcValues["L2Com.EP1.Junre_Id"] != "0"))
                {
                    m_strJunreId = baseData.opcValues["L2Com.EP1.Junre_Id"];

                    string info_txt = "钢坯到达均热段：" + m_strJunreId;
                    Logger.logwrite(info_txt);
                    string sj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    string strSql = "UPDATE furnace_billet_data_tb SET junre_time = '";
                    strSql += sj;
                    strSql += "' WHERE id = '" + m_strJunreId + "'";
                    Logger.logwrite(strSql);
                    WriteDB(strSql);
                }
            }

            catch (Exception ex)
            {
                string errtxt = "处理错误" + ex.Message;
                Logger.Errlogwrite(errtxt, WorkTag);
            }
        }

        public void OnReject()
        {
            // 信号由0变为1才出
            if ((baseData.opcValues["L2Com.EP1.Reject"]) == "0")
            {
                m_bNewReject = true;
            }

            // 剔除
            else if (m_bNewReject)
            {
                m_bNewReject = false;

                try
                {
                    string info_txt = "剔除" + m_strPlanNo;
                    Logger.logwrite(info_txt);

                    // 将当前计划批次的最后一根改为is_completed = 3
                    // 将当前ID写入PLC给1级
                    string strSql = "select top 1 sheet_no, sheet_weight_t from plan_tb where is_completed = 0 and plan_no = '"
                                    + m_strPlanNo + "' order by id desc";
                    DataTable dtRejectInfo = ReadDB(strSql);
                    string strRejectSheetNo = dtRejectInfo.Rows[0][0].ToString();
                    Logger.logwrite("剔除坯料号" + strRejectSheetNo);

                    strSql = "update plan_tb set is_completed = 3 where sheet_no = '" + strRejectSheetNo + "'";
                    Logger.logwrite(strSql);
                    WriteDB(strSql);

                    // 发送3级电文
                    if (!Program.socketClient.Connected)
                    {
                        Logger.logwrite("3级接收服务器已断开，尝试重新连接");
                        Program.ConnectToMes();
                    }

                    DateTime dtNow = DateTime.Now;
                    string strJ1m1 = "0079J1M103";
                    strJ1m1 += dtNow.ToString("yyyyMMddHHmmss");
                    strJ1m1 += "J1M1D";

                    // 电文补空格
                    strJ1m1 += dtNow.ToString("yyyyMMddHHmmss");

                    strJ1m1 += Currentshift(dtNow).ToString();

                    string strPlanNo = m_strPlanNo;
                    strPlanNo = strPlanNo.PadLeft(10, ' ');
                    strJ1m1 += strPlanNo;

                    strRejectSheetNo = strRejectSheetNo.PadLeft(20, ' ');
                    strJ1m1 += strRejectSheetNo;

                    string strWeight = dtRejectInfo.Rows[0][1].ToString();
                    strWeight = strWeight.ToString().PadLeft(4, '0');
                    strJ1m1 += strWeight;
                    strJ1m1 += "\n";
                    Logger.logwrite("剔除组装完毕：" + strJ1m1);

                    if (Program.socketClient.Connected)
                    {
                        Logger.logwrite("向3级发送剔除电文：" + strJ1m1);
                        var buffter = Encoding.UTF8.GetBytes(strJ1m1);
                        Program.socketClient.Send(buffter);
                    }
                }

                catch (Exception ex)
                {
                    string errtxt = "处理错误" + ex.Message;
                    Logger.Errlogwrite(errtxt);
                }
            }
        }
        //处理重复写入的计划
        public DataTable dealRepeatPlan(string plan_no, string sheet_no)
        {
            string SearchSql = string.Format("select ID,PLAN_NO,SHEET_NO,STEEL_GRADE,SEND_TIME from plan_tb where PLAN_NO = '{0}' and SHEET_NO = '{1}' and is_completed = 0 order by SEND_TIME asc", plan_no, sheet_no);
            DataTable dt = ReadDB(SearchSql);
            try
            {
                if (dt.Rows.Count > 1)//有重复的计划
                {

                    string DeleteSql = string.Format("delete from plan_tb where PLAN_NO = '{0}' and SHEET_NO = '{1}' and is_completed = 0 and ID > '{2}'", plan_no, sheet_no, dt.Rows[0]["ID"]);
                    Logger.logwrite(DeleteSql);
                    WriteDB(DeleteSql);
                    for (int i = dt.Rows.Count - 1; i > 0; i--)//只保留时间最晚的那一条
                    {
                        Logger.logwrite("删除了ID" + dt.Rows[i]["ID"] + "发送时间" + dt.Rows[i]["SEND_TIME"] + "计划号" + plan_no + "钢坯号" + sheet_no + "的钢坯");
                        dt.Rows.RemoveAt(i);
                    }
                    Logger.logwrite("保留了ID" + dt.Rows[0]["ID"] + "发送时间" + dt.Rows[0]["SEND_TIME"] + "计划号" + dt.Rows[0]["PLAN_NO"] + "钢坯号" + dt.Rows[0]["SHEET_NO"] + "的钢坯");

                }
                return dt;

            }
            catch (Exception e)
            {
                string errtxt = "重复计划处理错误，返回重复数据下发时间最晚的一条" + e.Message;
                Logger.Errlogwrite(errtxt);
                //DataTable ErrTable = new DataTable();
                //ErrTable.Rows.Add(dt.Rows[0]);
                //return ErrTable;
            }
            return dt;
        }
    }
}

