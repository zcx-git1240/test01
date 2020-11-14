using System;
using System.Data;

namespace Zp_Server
{
    class SprayPrinting : WorkerBase
    {
        public SprayPrinting(BaseData inBaseData): base(inBaseData) { }

        public void SprayPrintingGet()
        {
            //手动喷印数据发送到喷印机
            try
            {
                string StrSql_m = "select  * from htf_print_manual Where flag=0 order by Send_time desc ";
                DataTable PrintManual_Data = ReadDB(StrSql_m);
                if (PrintManual_Data.Rows.Count > 0)
                {
                    string Print_Packno = Utils.Fill_Space(PrintManual_Data.Rows[0]["PACKAGE_NO_SET"].ToString(), 15);  //出口捆包号 
                    string printsend = Print_Packno.Trim();
                    StrSql_m = "select max(PRINT_PACKAGE_ID_SET) IDMAX from HTF_PRINT_DATA Where PACKAGE_NO_SET='" + printsend + "' ";
                    DataTable PrintMax_Data = ReadDB(StrSql_m);
                    string Print_max = PrintMax_Data.Rows[0]["IDMAX"].ToString().Trim();
                    PrintMax_Data.Dispose();

                    string Print_Sheetno = Utils.Fill_Space(PrintManual_Data.Rows[0]["SHEET_NO"].ToString(), 15);//喷印钢板号
                                                                                                           //string Print_Packno = Fill_Space(PrintText_Data.Rows[0]["PACKAGE_NO_SET"].ToString(), 15);  //出口捆包号 
                    string Print_Grade = Utils.Fill_Space(PrintManual_Data.Rows[0]["GRADE"].ToString(), 20);  //钢种
                    string Print_Len = PrintManual_Data.Rows[0]["PRINT_LENGTH"].ToString().Trim();
                    string Print_Wth = PrintManual_Data.Rows[0]["PRINT_WIDTH"].ToString().Trim();
                    string Print_Thk = PrintManual_Data.Rows[0]["PRINT_THK"].ToString().Trim();
                    string Print_Id = PrintManual_Data.Rows[0]["PRINT_PACKAGE_ID_SET"].ToString().Trim();

                    bool W_Completed = true;
                    try
                    {
                        baseData.opcItems["L2Com.EP1.Print.W_SHEET_NO"].Write(Print_Sheetno);
                        baseData.opcItems["L2Com.EP1.Print.W_GRADE"].Write(Print_Grade);
                        baseData.opcItems["L2Com.EP1.Print.W_LEN"].Write(Print_Len);
                        baseData.opcItems["L2Com.EP1.Print.W_PACKAGE"].Write(Print_Packno);
                        baseData.opcItems["L2Com.EP1.Print.W_THK"].Write(Print_Thk);
                        baseData.opcItems["L2Com.EP1.Print.W_WTH"].Write(Print_Wth);
                        baseData.opcItems["L2Com.EP1.Print.W_WTH"].Write(Print_max);
                        baseData.opcItems["L2Com.EP1.Print.W_KXH"].Write(Print_Id);

                        baseData.opcItems["L2Com.EP1.Print.W_SEND_COMPLETED"].Write(W_Completed);

                        string txt = Print_Sheetno + " 写喷印数据到PLC完成！";
                        Logger.logwrite(txt);

                        string updateStr = "update htf_print_manual set flag=1  where flag=0 ";
                        WriteDB(updateStr);

                    }
                    catch (Exception ex)
                    {
                        string errtxt = "手动写喷印数据到PLC出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }
                }
                PrintManual_Data.Dispose();
            }
            catch (Exception ex1)
            {
                string errtxt = "手动写喷印数据到PLC出错！" + ex1.Message;
                Logger.logwrite(errtxt);
            }

        }

        public void SprayPrintingEnd()
        {
            //喷印完成
            if ((baseData.opcValues["L2Com.EP1.Print.R_PRINT_COMPLETED"]) == "1")  //喷印完成
            {
                if (!baseData.flags[8])
                {
                    baseData.flags[8] = true;
                    string Printed_ID = "";//喷印完成钢板号
                    string Print_ID_fl = "";//风冷写入钢板号
                    try
                    {
                        Print_ID_fl = baseData.opcValues["L2Com.EP1.Print.W_SHEET_NO"].Trim();//风冷结束钢板号
                        Printed_ID = baseData.opcValues["L2Com.EP1.Print.R_SHEET_NO"].Trim();//喷印完成钢板号
                        string Printed_Packno = baseData.opcValues["L2Com.EP1.Print.R_PACKAGE"].Trim();//喷印完成捆包号
                        string Printed_Xh = baseData.opcValues["L2Com.EP1.Print.R_KXH"].Trim();//喷印完成捆内序号


                        if (Printed_Packno != "")
                        {
                            string StrSql_Id = "select count(*) row_count from htf_package_index Where PACKAGE_OUT='" + Printed_Packno + "' ";
                            DataTable Print_Data = ReadDB(StrSql_Id);
                            int rowid = Convert.ToInt16(Print_Data.Rows[0]["row_count"].ToString());
                            if (rowid == 0)//有记录
                            {
                                //增加捆索引信息
                                string years = string.Format("{0:yyyy}", DateTime.Now) + "-" + Printed_Packno.Substring(2, 2) + "-" + Printed_Packno.Substring(4, 2);  //获取日期                                                                     
                                int xh = Convert.ToInt16(Printed_Packno.Substring(6, 2));
                                string insertStr = "insert into htf_package_index(PACKAGE_OUT,PROD_DATE,ID) values('" + Printed_Packno + "','" + years + "'," + xh + ")";
                                WriteDB(insertStr);

                            }
                        }

                        if (Print_ID_fl!= "")
                        {
                            if (Printed_ID == Print_ID_fl)//喷印的钢板号与风冷结束钢板号相同，直接修改
                            {
                                string StrSql_Id = "select count(*) row_count from HTF_PLATE_DATA Where sheet_no='" + Printed_ID + "' ";
                                DataTable Print_Data = ReadDB(StrSql_Id);
                                int rowid = Convert.ToInt16(Print_Data.Rows[0]["row_count"].ToString());
                                if (rowid == 1)//有记录
                                {
                                    //修改喷印时间和捆包号（先用设定值）
                                    string updateStr = "update HTF_PLATE_DATA set PRINT_TIME = '" + baseData.dqsj 
                                        + "',PRINT_PACKAGE_NO='" + Printed_Packno 
                                        + "',PRINT_PACKAGE_ID=" + Printed_Xh + " where sheet_no='" + Printed_ID 
                                        + "' ";
                                    WriteDB(updateStr);

                                    updateStr = "update HTF_PRINT_DATA set flag=2 where flag=3 ";
                                    WriteDB(updateStr);

                                    updateStr = "update HTF_PRINT_DATA set PACKAGE_NO_ACT='" + Printed_Packno + "',PRINT_PACKAGE_ID=" + Printed_Xh + ",flag=3 where sheet_no='" + Printed_ID + "' ";
                                    WriteDB(updateStr);

                                    updateStr = "update HTF_PRINT_DATA set flag=0 where flag=1 ";
                                    WriteDB(updateStr);
                                    string txt = Print_ID_fl + " 钢板号一致使用喷印的钢板号为喷印号";
                                    Logger.logwrite(txt);

                                }
                                else
                                {
                                    string txt = "数据库中不存在喷印钢板号！";
                                    Logger.logwrite(txt);
                                }
                                Print_Data.Dispose();
                            }
                            else
                            {
                                try
                                {
                                    string StrSql_Id = "select count(*) row_count from HTF_PLATE_DATA Where sheet_no='" + Print_ID_fl + "' ";
                                    DataTable Print_Data = ReadDB(StrSql_Id);
                                    int rowid = Convert.ToInt16(Print_Data.Rows[0]["row_count"].ToString());
                                    Print_Data.Dispose();
                                    if (rowid == 1)//有记录
                                    {
                                        StrSql_Id = "select  PRINT_PACKAGE_NO from HTF_PLATE_DATA Where sheet_no='" + Print_ID_fl + "' ";
                                        DataTable Printtext_Data = ReadDB(StrSql_Id);
                                        string printpack = Printtext_Data.Rows[0]["PRINT_PACKAGE_NO"].ToString().Trim();
                                        Printtext_Data.Dispose();
                                        if (printpack == "")  //如果写入钢板号为空，就将捆包号写入
                                        {
                                            string updateStr = "update HTF_PLATE_DATA set PRINT_TIME = '" 
                                                + baseData.dqsj + "',PRINT_PACKAGE_NO='" 
                                                + Printed_Packno + "',PRINT_PACKAGE_ID=" + Printed_Xh 
                                                + " where sheet_no='" + Print_ID_fl + "' ";
                                            WriteDB(updateStr);

                                            updateStr = "update HTF_PRINT_DATA set flag=2 where flag=3 ";
                                            WriteDB(updateStr);

                                            updateStr = "update HTF_PRINT_DATA set PACKAGE_NO_ACT='" + Printed_Packno + "',PRINT_PACKAGE_ID=" + Printed_Xh + ",flag=3 where sheet_no='" + Print_ID_fl + "' ";
                                            WriteDB(updateStr);

                                            updateStr = "update HTF_PRINT_DATA set flag=0 where flag=1 ";
                                            WriteDB(updateStr);
                                            string txt = Print_ID_fl + " 使用写入钢板号为喷印号";
                                            Logger.logwrite(txt);
                                        }
                                        else
                                        {
                                            StrSql_Id = "select count(*) row_count from HTF_PLATE_DATA Where sheet_no='" + Printed_ID + "' ";
                                            DataTable Print1_Data = ReadDB(StrSql_Id);
                                            int rowid1 = Convert.ToInt16(Print1_Data.Rows[0]["row_count"].ToString());
                                            Print1_Data.Dispose();
                                            if (rowid1 == 1)//有记录
                                            {
                                                StrSql_Id = "select  PRINT_PACKAGE_NO from HTF_PLATE_DATA Where sheet_no='" + Printed_ID + "' ";
                                                DataTable Printtext1_Data = ReadDB(StrSql_Id);
                                                string printpack1 = Printtext1_Data.Rows[0]["PRINT_PACKAGE_NO"].ToString().Trim();
                                                Printtext1_Data.Dispose();
                                                if (printpack1 == "")  //如果喷印钢板号为空，就将捆包号写入
                                                {
                                                    string updateStr = "update HTF_PLATE_DATA set PRINT_TIME = '" + 
                                                        baseData.dqsj + "',PRINT_PACKAGE_NO='" + Printed_Packno 
                                                        + "',PRINT_PACKAGE_ID=" + Printed_Xh + " where sheet_no='" + Printed_ID + "' ";
                                                    WriteDB(updateStr);

                                                    updateStr = "update HTF_PRINT_DATA set flag=2 where flag=3 ";
                                                    WriteDB(updateStr);

                                                    updateStr = "update HTF_PRINT_DATA set PACKAGE_NO_ACT='" + Printed_Packno 
                                                        + "',PRINT_PACKAGE_ID=" + Printed_Xh + ",flag=3 where sheet_no='" 
                                                        + Printed_ID + "' ";
                                                    WriteDB(updateStr);

                                                    updateStr = "update HTF_PRINT_DATA set flag=0 where flag=1 ";
                                                    WriteDB(updateStr);
                                                    string txt = Print_ID_fl + " 使用喷印的钢板号为喷印号";
                                                    Logger.logwrite(txt);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errtxt = Print_ID_fl + " 喷印完成更新数据出错！" + ex.Message;
                                    Logger.logwrite(errtxt);
                                }

                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        string errtxt = Printed_ID + " 喷印完成处理出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }

                    //不管处理成功与否，都写读完成标志
                    try
                    {
                        bool R_Completed = true;
                        baseData.opcItems["L2Com.EP1.Print.W_PRINT_COMPLETED"].Write(R_Completed);
                        string txt = Printed_ID + " 读喷印完成，写标志位成功！";
                        Logger.logwrite(txt);
                    }
                    catch (Exception ex)
                    {
                        string errtxt = Printed_ID + " 喷印完成标志位写入出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }

                }
            }
            else
            {
                baseData.flags[8] = false;
            }
        }

        public void SprayPrintingReset()
        {

            if ((baseData.opcValues["L2Com.EP1.Print.R_PRINT_COMPLETED"]) == "0")  //读喷印完成复位
            {
                if (!baseData.flags[9])
                {
                    baseData.flags[9] = true;
                    try
                    {
                        bool R_Completed = false;
                        baseData.opcItems["L2Com.EP1.Print.W_PRINT_COMPLETED"].Write(R_Completed);
                        //string txt1 = "读喷印完成标志位复位成功";
                        //Logger.logwrite(txt1);
                    }
                    catch (Exception ex)
                    {
                        string errtxt = "读喷印完成标志位复位出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }
                }
            }
            else
            {
                baseData.flags[9] = false;
            }

        }

        public void SprayPrintingFineshReset()
        {
            if ((baseData.opcValues["L2Com.EP1.Print.R_SEND_COMPLETED"]) == "1")  //L1接收喷印信息完成复位
            {
                if (!baseData.flags[10])
                {
                    baseData.flags[10] = true;
                    try
                    {
                        bool R_Completed = false;
                        baseData.opcItems["L2Com.EP1.Print.W_SEND_COMPLETED"].Write(R_Completed);
                        string txt1 = "L1接收喷印信息完成标志位复位成功";
                        Logger.logwrite(txt1);
                    }
                    catch (Exception ex)
                    {
                        string errtxt = "L1接收喷印信息完成标志位复位出错！" + ex.Message;
                        Logger.logwrite(errtxt);
                    }
                }
            }
            else
            {
                baseData.flags[10] = false;
            }
        }
    }
}
