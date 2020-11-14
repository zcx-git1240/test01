using System;
using System.Data;

namespace Zp_Server
{
    class PackageWorker : WorkerBase
    {

        public static int count = 0;

        public PackageWorker(BaseData inBaseData): base(inBaseData) {
            count++;
        }

        public void PacketEnd() {
            //1.2.11捆包打包完成
            //整理钢板实绩和捆包信息 
            try
            {
                string StrPack = " select * from package_to_mes  where flag=0 order by PACKAGE_NO desc ";//查找发送的捆包信息
                DataTable PackData = ReadDB(StrPack);
                if (PackData.Rows.Count >= 1)//有新的捆包信息需要处理
                {
                    string Packno = PackData.Rows[0]["PACKAGE_NO"].ToString();
                    try
                    {
                        int len = 0, wth = 0;
                        double thk = 0;
                        double weights = 0;
                        int llzl = 0;

                        StrPack = " select * from package_to_mes  where flag=0 and  PACKAGE_NO='" + Packno + "'";//查找发送的捆包信息
                        DataTable PackSheetData = ReadDB(StrPack);
                        if (PackSheetData.Rows.Count >= 1)//有钢板实绩需要处理
                        {
                            for (int k = 0; k < PackSheetData.Rows.Count; k++)
                            {
                                string Sheetno = PackSheetData.Rows[k]["SHEET_NO"].ToString().Trim();  //获取钢板号
                                string sheetinno = Sheetno.Substring(0, 13) + "1";

                                string Ids = "0";
                                string StrSql_id = "select SAVE_SEQ_NO.nextVal Vals from dual ";
                                DataTable Id302_Data = ReadDB(StrSql_id);
                                Ids = Id302_Data.Rows[0]["Vals"].ToString();
                                Id302_Data.Dispose();

                                string sendtime = string.Format("{0:yyyyMMddHHmmss}", DateTime.Now);  //发送时间
                                if (k == 0)
                                {
                                    string StrSql_Weight = "select OUT_MAT_LEN,OUT_MAT_WID,OUT_MAT_THICK from HTF_PLAN_AIM where SHEET_NO='" + Sheetno + "' ";
                                    DataTable Weight_Data = ReadDB(StrSql_Weight);
                                    if (Weight_Data.Rows.Count >= 1)
                                    {
                                        len = Convert.ToInt16(Weight_Data.Rows[0]["OUT_MAT_LEN"].ToString());
                                        wth = Convert.ToInt16(Weight_Data.Rows[0]["OUT_MAT_WID"].ToString());
                                        thk = Convert.ToDouble(Weight_Data.Rows[0]["OUT_MAT_THICK"].ToString());
                                        weights = len * wth * thk * 7.85 / 1000000;//理论重量
                                        llzl = Convert.ToInt16(weights);
                                    }
                                    Weight_Data.Dispose();
                                }

                                //将钢板实绩信息写入HLP302
                                string insertCmd = "insert into HPL302(ID,SEND_TIME,SHEET_NO,IN_MAT_NO,PLAN_NO,PACKAGE_NO,MAT_ACT_WIDTH,MAT_ACT_THICK,MAT_ACT_LEN,MAT_ACT_WT,"
                                    + " STEEL_GRADE,ST_NO,BETTER_SURFACE_CODE,SHOT_BLAST_FLAG,CREW_ID,SHIFT_NO,PROD_TIME,HEAT_MODE,"
                                    + " END_SHEET_FLAG,SAMPLE_FLAG,SAMPLE_TYPE,SAMPLE_ACT_WIDTH,SAMPLE_ACT_LEN,SAMPLE_TIME,ORDER_NO) "
                                    + " select '" + Ids + "','" + sendtime + "','" + sheetinno + "',SHEET_NO,PLAN_NO,'" + Packno + "',SPLATE_WIDTH,SPLATE_THK*1000,"
                                    + " SPLATE_LENGTH," + llzl + ",GRADE,ST_NO,BETTER_SURFACE_CODE,SHOT_BLAST_FLAG,CREW_ID,SHIFT_NO,to_char(FEEDING_TIME,'yyyymmddhh24miss'), "
                                    + " 'T','1','0','H',0,0,to_char(FEEDING_TIME,'yyyymmddhh24miss'),ORDER_NO from HTF_PLATE_DATA where SHEET_NO='" + Sheetno + "'";
                                WriteDB(insertCmd);

                                string updateCmd = "update package_to_mes set flag=1 where SHEET_NO='" + Sheetno + "' and flag=0";
                                WriteDB(updateCmd);

                                string txt = Sheetno + " 钢板实绩写入HLP302成功！";
                                Logger.logwrite(txt);

                                if (k == PackSheetData.Rows.Count - 1)//最后一块钢板，提取捆包信息
                                {
                                    string graden = "", stnon = "", planno = "", crew = "", shift = "", prouducetime = "";
                                    string StrSql_Qt = "select GRADE,ST_NO,PLAN_NO,CREW_ID,SHIFT_NO,FEEDING_TIME from HTF_PLATE_DATA where SHEET_NO='" + Sheetno + "' ";
                                    DataTable Qt_Data = ReadDB(StrSql_Qt);
                                    if (Qt_Data.Rows.Count >= 1)
                                    {
                                        graden = Qt_Data.Rows[0]["GRADE"].ToString().Trim();
                                        stnon = Qt_Data.Rows[0]["ST_NO"].ToString().Trim();
                                        planno = Qt_Data.Rows[0]["PLAN_NO"].ToString().Trim();
                                        crew = Qt_Data.Rows[0]["CREW_ID"].ToString().Trim();
                                        shift = Qt_Data.Rows[0]["SHIFT_NO"].ToString().Trim();
                                        prouducetime = Qt_Data.Rows[0]["FEEDING_TIME"].ToString().Trim();
                                    }
                                    Qt_Data.Dispose();
                                    int ks = k + 1;
                                    double weight_pack = weights * ks;
                                    try
                                    {
                                        StrSql_Qt = "select count(*) rowc from HTF_PACKAGE_DATA where PACKAGE_NO='" + Packno + "'";
                                        DataTable Qt_Data1 = ReadDB(StrSql_Qt);
                                        int rowid = Convert.ToInt16(Qt_Data1.Rows[0]["rowc"].ToString());
                                        if (rowid == 1)//有记录
                                        {
                                            StrPack = " select  count(*) rowc1 from package_to_mes  where   PACKAGE_NO='" + Packno + "'";//求该捆包总记录数
                                            DataTable Qt_Data2 = ReadDB(StrPack);
                                            int rowZsid = Convert.ToInt16(Qt_Data2.Rows[0]["rowc1"].ToString());
                                            Qt_Data2.Dispose();
                                            weight_pack = weights * rowZsid;

                                            updateCmd = "update HTF_PACKAGE_DATA set NUM_OF_SHEET= " + rowZsid + ",MAT_ACT_WT=" + weight_pack + " where PACKAGE_NO='" + Packno + "' ";
                                            WriteDB(updateCmd);

                                        }
                                        else
                                        {
                                            insertCmd = " insert into HTF_PACKAGE_DATA(PACKAGE_NO,PLAN_NO,MAT_ACT_WIDTH,MAT_ACT_THICK,MAT_ACT_LEN,"
                                                         + " NUM_OF_SHEET,MAT_ACT_WT,STEEL_GRADE,ST_NO,PRODUCT_TIME,CREW_ID,SHIFT_NO) "
                                                         + " values('" + Packno + "','" + planno + "'," + wth + "," + thk + "," + len + "," + ks + "," + weight_pack + ", "
                                                         + " '" + graden + "','" + stnon + "','" + prouducetime + "','" + crew + "','" + shift + "' )";
                                            WriteDB(insertCmd);

                                        }
                                        Qt_Data1.Dispose();
                                        baseData.i_count = 0;//计数开始
                                        baseData.tz = 1;//置位
                                    }
                                    catch (Exception ex)
                                    {
                                        string errtxt = Packno + " 捆包实绩写入错误！" + ex.Message;
                                        Logger.logwrite(errtxt);
                                    }
                                }
                            }
                        }
                        PackSheetData.Dispose();
                    }
                    catch (Exception ex1)
                    {
                        string errtxt = Packno + " 钢板实绩写入HLP302出错！" + ex1.Message;
                        Logger.logwrite(errtxt);
                    }
                }
                PackData.Dispose();
            }
            catch (Exception ex1)
            {
                string errtxt = "钢板实绩写入HLP302出错！" + ex1.Message;
                Logger.logwrite(errtxt);
            }
        }

        public void SavePacketData()
        {
            //Logger.logwrite(baseData.tz.ToString());
            //将捆包信息写入HLP303
            if (count > 300)
            {
                count = 0;
                //Logger.logwrite(baseData.i_count.ToString());
                if (baseData.tz == 1 || 1 == 1)
                {
                    Logger.logwrite("call SavePacketData");
                    //Logger.logwrite(baseData.tz.ToString());
                    baseData.tz = 0;
                    try
                    {
                        string StrPackno = " select * from htf_package_data  where flag=0 order by PACKAGE_NO desc ";//查找发送的捆包信息
                        DataTable PacknoData = ReadDB(StrPackno);
                        if (PacknoData.Rows.Count >= 1)//有新的捆包信息需要处理
                        {
                            for (int k = 0; k < PacknoData.Rows.Count; k++)
                            {
                                string packnos = PacknoData.Rows[k]["PACKAGE_NO"].ToString().Trim();
                                try
                                {
                                    string Ids = "0";
                                    string StrSql_id = "select SAVE_SEQ_NO.nextVal Vals from dual ";
                                    DataTable Id303_Data = ReadDB(StrSql_id);
                                    Ids = Id303_Data.Rows[0]["Vals"].ToString();
                                    Id303_Data.Dispose();
                                    string sendtime = string.Format("{0:yyyyMMddHHmmss}", DateTime.Now);  //发送时间
                                    string insertCmd = " insert into HPL303(ID,SEND_TIME,PACKAGE_NO,PLAN_NO,MAT_ACT_WIDTH,MAT_ACT_THICK,"
                                                      + " MAT_ACT_LEN,NUM_OF_SHEET,MAT_ACT_WT,STEEL_GRADE,ST_NO,PRODUCT_TIME,CREW_ID,SHIFT_NO)"
                                                      + " select '" + Ids + "','" + sendtime + "',PACKAGE_NO,PLAN_NO,MAT_ACT_WIDTH,MAT_ACT_THICK*1000,"
                                                      + " MAT_ACT_LEN,NUM_OF_SHEET,MAT_ACT_WT,STEEL_GRADE,ST_NO,to_char(PRODUCT_TIME,'yyyymmddhh24miss'),CREW_ID,SHIFT_NO "
                                                      + " from HTF_PACKAGE_DATA where PACKAGE_NO='" + packnos + "'";
                                    WriteDB(insertCmd);

                                    string updateStr = "update HTF_PACKAGE_DATA set flag=1 where  PACKAGE_NO='" + packnos + "'";
                                    WriteDB(updateStr);
                                    string txt = packnos + " 捆包信息写入HLP303完成！";
                                    Logger.logwrite(txt);
                                }
                                catch (Exception er)
                                {
                                    string errtxt = packnos + " 捆包信息写入HLP303出错！" + er.Message;
                                    Logger.logwrite(errtxt);
                                }

                            }
                        }
                        PacknoData.Dispose();
                    }
                    catch (Exception er)
                    {
                        string errtxt = "捆包信息写入HLP303出错！" + er.Message;
                        Logger.logwrite(errtxt);
                    }
                }
            }
        }
    }
}
