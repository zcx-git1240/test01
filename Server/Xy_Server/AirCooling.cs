using System;
using System.Data;

namespace Zp_Server
{
    class AirCooling : WorkerBase
    {
        public AirCooling(BaseData inBaseData): base(inBaseData) { }

        public void CoolingBegin()
        {
            //钢板强制风冷开始
            if ((baseData.opcValues["L2Com.EP1.R_Cool_Start"]) == "1")  //风冷开始信号
            {
                if (!baseData.flags[4])
                {
                    baseData.flags[4] = true;
                    string CoolStart_id = "";//风冷开始钢板号   
                    try
                    {
                        CoolStart_id = baseData.opcValues["L2Com.EP1.R_Cool_Start_Id"].Trim();//风冷开始钢板号   
                        if (CoolStart_id!= "")
                        {
                            string StrSql_Id = "select count(*) row_count from HTF_PLATE_DATA Where sheet_no='" + CoolStart_id + "' ";
                            DataTable Change_Data = ReadDB(StrSql_Id);
                            int rowid = Convert.ToInt16(Change_Data.Rows[0]["row_count"].ToString());
                            if (rowid == 1)//有记录
                            {
                                string updateStr = "update HTF_PLATE_DATA set COOL_START_TIME='" 
                                    + baseData.dqsj + "' where sheet_no='" 
                                    + CoolStart_id + "' ";
                                WriteDB(updateStr);

                                string txt = CoolStart_id + " 风冷开始！";
                                Logger.logwrite(txt);
                            }
                            else
                            {
                                string txt = "数据库中不存在该风冷钢板号！";
                                Logger.logwrite(txt);
                            }
                            Change_Data.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        string errtxt = CoolStart_id + " 风冷开始处理出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }

                }
            }
            else
            {
                baseData.flags[4] = false;
            }
        }

        public void CoolingEnd()
        {
            //1.2.7 钢板强制风冷结束
            if ((baseData.opcValues["L2Com.EP1.R_Cool_End"]) == "1")  //风冷结束信号
            {
                if (!baseData.flags[5])
                {
                    baseData.flags[5] = true;

                    string CoolEnd_id = "";
                    try
                    {
                        CoolEnd_id = baseData.opcValues["L2Com.EP1.R_Cool_End_Id"].Trim();//风冷结束钢板号   
                                                          //string CoolEnd_Temp = baseData.opcValues[19].Trim();//风冷结束温度   
                        double CoolEnd_Temp = Convert.ToDouble(baseData.opcValues["L2Com.EP1.R_Cool_End_Temp"].Trim());//风冷结束温度       
                        int Cool_Temp = Convert.ToInt16(CoolEnd_Temp);

                        if (CoolEnd_id!= "")
                        {
                            string StrSql_Id = "select count(*) row_count from HTF_PLATE_DATA Where sheet_no='" + CoolEnd_id + "' ";
                            DataTable Change_Data = ReadDB(StrSql_Id);
                            int rowid = Convert.ToInt16(Change_Data.Rows[0]["row_count"].ToString());
                            if (rowid == 1)//有记录
                            {
                                string updateStr = "update HTF_PLATE_DATA set COOL_END_TIME = '" 
                                    + baseData.dqsj + "', COOL_END_TEMP=" + Cool_Temp 
                                    + "  where sheet_no='" + CoolEnd_id + "' ";
                                WriteDB(updateStr);

                                string txt = CoolEnd_id + " 风冷结束！";
                                Logger.logwrite(txt);
                            }
                            else
                            {
                                string txt = "数据库中不存在该风冷结束钢板号！";
                                Logger.logwrite(txt);
                            }
                            Change_Data.Dispose();

                            //发送给喷印机内容
                            StrSql_Id = "select PACKAGE_NO_SET,GRADE,PRINT_LENGTH,PRINT_WIDTH,PRINT_THK,PRINT_PACKAGE_ID_SET from HTF_PRINT_DATA Where sheet_no='" + CoolEnd_id + "' ";
                            DataTable PrintText_Data = ReadDB(StrSql_Id);
                            if (PrintText_Data.Rows.Count > 0)
                            {
                                string Print_Packno = Utils.Fill_Space(PrintText_Data.Rows[0]["PACKAGE_NO_SET"].ToString(), 15);  //出口捆包号 
                                string printsend = Print_Packno.Trim();
                                StrSql_Id = "select max(PRINT_PACKAGE_ID_SET) IDMAX from HTF_PRINT_DATA Where PACKAGE_NO_SET='" + printsend + "' ";
                                DataTable PrintMax_Data = ReadDB(StrSql_Id);
                                string Print_max = PrintMax_Data.Rows[0]["IDMAX"].ToString().Trim();
                                PrintMax_Data.Dispose();

                                string Print_Sheetno = Utils.Fill_Space(CoolEnd_id.ToString(), 15);//喷印钢板号
                                                                                             //string Print_Packno = Fill_Space(PrintText_Data.Rows[0]["PACKAGE_NO_SET"].ToString(), 15);  //出口捆包号 
                                string Print_Grade = Utils.Fill_Space(PrintText_Data.Rows[0]["GRADE"].ToString(), 20);  //钢种
                                string Print_Len = PrintText_Data.Rows[0]["PRINT_LENGTH"].ToString().Trim();
                                string Print_Wth = PrintText_Data.Rows[0]["PRINT_WIDTH"].ToString().Trim();
                                string Print_Thk = PrintText_Data.Rows[0]["PRINT_THK"].ToString().Trim();
                                string Print_Id = PrintText_Data.Rows[0]["PRINT_PACKAGE_ID_SET"].ToString().Trim();

                                bool W_Completed = true;
                                try
                                {
                                    baseData.opcItems["L2Com.EP1.Print.W_SHEET_NO"].Write(Print_Sheetno);
                                    baseData.opcItems["L2Com.EP1.Print.W_GRADE"].Write(Print_Grade);
                                    baseData.opcItems["L2Com.EP1.Print.W_LEN"].Write(Print_Len);
                                    baseData.opcItems["L2Com.EP1.Print.W_PACKAGE"].Write(Print_Packno);
                                    baseData.opcItems["L2Com.EP1.Print.W_THK"].Write(Print_Thk);
                                    baseData.opcItems["L2Com.EP1.Print.W_WTH"].Write(Print_Wth);
                                    baseData.opcItems["L2Com.EP1.Print.W_KZS"].Write(Print_max);
                                    baseData.opcItems["L2Com.EP1.Print.W_KXH"].Write(Print_Id);

                                    baseData.opcItems["L2Com.EP1.Print.W_SEND_COMPLETED"].Write(W_Completed);

                                    string txt = CoolEnd_id + " 写计划喷印数据到PLC完成！";
                                    Logger.logwrite(txt);

                                    string updateStr = "update htf_print_data set flag=1  where sheet_no='" 
                                        + CoolEnd_id + "' ";
                                    WriteDB(updateStr);

                                }
                                catch (Exception ex)
                                {
                                    string errtxt = CoolEnd_id + " 写喷印数据到PLC出错！" + ex.Message;
                                    Logger.logwrite(errtxt);
                                }
                            }
                            else//只写钢板号到PLC
                            {
                                try
                                {
                                    StrSql_Id = "select STEEL_GRADE,OUT_MAT_LEN,OUT_MAT_WID,OUT_MAT_THICK from HTF_PLAN_PRODUCE Where sheet_no='" + CoolEnd_id + "' ";//取计划
                                    DataTable PrintText1_Data = ReadDB(StrSql_Id);

                                    string Print_Sheetno = Utils.Fill_Space(CoolEnd_id.ToString(), 15);//喷印钢板号                                        
                                    string Print_Grade = Utils.Fill_Space(PrintText1_Data.Rows[0]["STEEL_GRADE"].ToString().Trim(), 20);
                                    string Print_Packno = Utils.Fill_Space(" ", 15);
                                    string Print_Len = PrintText1_Data.Rows[0]["PRINT_LENGTH"].ToString().Trim();
                                    //string Print_max="0";
                                    string Print_Wth = PrintText1_Data.Rows[0]["OUT_MAT_WID"].ToString().Trim();
                                    string Print_Thk = PrintText1_Data.Rows[0]["OUT_MAT_THICK"].ToString().Trim(); ;
                                    // string Print_Id = "0";
                                    PrintText1_Data.Dispose();
                                    bool W_Completed = true;

                                    baseData.opcItems["L2Com.EP1.Print.W_SHEET_NO"].Write(Print_Sheetno);
                                    baseData.opcItems["L2Com.EP1.Print.W_GRADE"].Write(Print_Grade);
                                    baseData.opcItems["L2Com.EP1.Print.W_LEN"].Write(Print_Len);
                                    //baseData.opcItems["L2Com.EP1.Print.W_PACKAGE"].Write(Print_Packno);
                                    baseData.opcItems["L2Com.EP1.Print.W_THK"].Write(Print_Thk);
                                    baseData.opcItems["L2Com.EP1.Print.W_WTH"].Write(Print_Wth);
                                    //baseData.opcItems["L2Com.EP1.Print.W_KZS"].Write(Print_max);
                                    //baseData.opcItems["L2Com.EP1.Print.W_KXH"].Write(Print_Id);

                                    baseData.opcItems["L2Com.EP1.Print.W_SEND_COMPLETED"].Write(W_Completed);
                                    string txt = CoolEnd_id + " 只写钢板号到喷印PLC完成！";
                                    Logger.logwrite(txt);
                                }
                                catch (Exception ex)
                                {
                                    string errtxt = CoolEnd_id + " 只写钢板号到喷印PLC出错！" + ex.Message;
                                    Logger.logwrite(errtxt);
                                }

                            }
                            PrintText_Data.Dispose();
                        }
                        else
                        {
                            string txt = CoolEnd_id + " 风冷结束钢板号错误";
                            Logger.logwrite(txt);
                        }
                    }
                    catch (Exception ex)
                    {
                        string errtxt = CoolEnd_id + " 风冷结束处理出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }
                }
            }
            else
            {
                baseData.flags[5] = false;
            }
        }
    }
}
