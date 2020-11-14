using System;
using OPCAutomation;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Zp_Server
{
    class OPCHelper
    {
        private OPCServer KepServer;
        private OPCGroups KepGroups;
        private OPCGroup KepGroup;
        private OPCItems KepItems;
        public bool connected;
        public Dictionary<string, OPCItem> opcItem = new Dictionary<string, OPCItem>();
        public Dictionary<string, string> ItemValue = new Dictionary<string, string>();

        public Dictionary<string, OPCItem> opcItemMonitor = new Dictionary<string, OPCItem>();
        public Dictionary<string, string> ItemValueMonitor = new Dictionary<string, string>();

        public Dictionary<string, OPCItem> opcItemTrack = new Dictionary<string, OPCItem>();

        public int ClientHandle = 2;
        public static string OPC_ERROR_FLAG = "READ_OPC_ERROR";
        public static string[] KepTags = {

            "L2Com.EP1.Arrive_Enter",//到达入炉位信号
            "L2Com.EP1.Enter_Done",//装炉完成信号
            "L2Com.EP1.Enter",//入炉信号
            "L2Com.EP1.W_Id",//写钢坯号（0）
            "L2Com.EP1.W_Plan_No",//写计划号
            "L2Com.EP1.Enter_Id",//装炉完成钢坯号（0）
            "L2Com.EP1.Enter_Length",//入炉钢坯长度
            "L2Com.EP1.Enter_Width",//入炉钢坯宽度
            "L2Com.EP1.Enter_Thick",//入炉钢坯厚度
            "L2Com.EP1.Enter_Weight",//入炉钢坯重量
            "L2Com.EP1.Onere_Id",//到达一加段钢坯号（47）
            "L2Com.EP1.Twore_Id",//到达二加段钢坯号（76）
            "L2Com.EP1.Junre_Id",//到达均热段钢坯号（103）
            "L2Com.EP1.Out_Id",//出炉位钢坯号（131）
            "L2Com.EP1.Out",//出炉信号
            "L2Com.EP1.Out_Temp",//钢板出炉温度(目前没有，以后有)
            "L2Com.EP1.Out_Length",//出炉钢坯长度
            "L2Com.EP1.Out_Width",//出炉钢坯宽度
            "L2Com.EP1.Out_Thick",//出炉钢坯厚度
            "L2Com.EP1.Out_Weight"//出炉钢坯重量

            /*
            "L2Com.EP1.Enter", // 装炉完成(定位完成)
            "L2Com.EP1.W_Id", // 写钢坯号
            "L2Com.EP1.W_Plan_No", // 写计划号编号
		    "L2Com.EP1.Weight", // 钢坯实测重
            "L2Com.EP1.Enter_Id", // 到达第1炉位的钢坯号
            "L2Com.EP1.Onere_Id", // 到达一加段钢坯号
            "L2Com.EP1.Twore_Id", // 到达二加段钢坯号
            "L2Com.EP1.Junre_Id", // 到达均热段钢坯号
		    "L2Com.EP1.Out", // 钢坯出炉信号(出料悬臂辊有钢)
		    "L2Com.EP1.Out_Id", //出炉位钢坯号
		    "L2Com.EP1.Reject", //进入剔废辊道(测长称重辊道激光检)
            */
        };
        public static string[] KepTagsMonitor = {
            "L2Com.IP1.junre_up_A_temp",//均热上层a侧温度
            "L2Com.IP1.junre_up_B_temp",//均热上层b侧温度
            "L2Com.IP1.junre_low_A_temp",//均热下层a侧温度
            "L2Com.IP1.junre_low_B_temp",//均热下层b侧温度
            "L2Com.IP1.twore_up_A_temp",//二加段上层a侧温度
            "L2Com.IP1.twore_up_B_temp",//二加段上层b侧温度
            "L2Com.IP1.onere_up_A_temp",//一加段上层a侧温度
            "L2Com.IP1.onere_up_B_temp",//一加段上层b侧温度
            "L2Com.IP1.prere_up_A_temp",//预热段上层a侧温度
            "L2Com.IP1.prere_up_B_temp",//预热段上层b侧温度

            //煤气&空气
            "L2Com.IP1.mqzgyl",//煤气总管压力
            "L2Com.IP1.mqzgll",//煤气总管流量
            "L2Com.IP1.zrkqyl",//助燃空气压力
            "L2Com.IP1.prere_kqll",//预热段空气流量
            "L2Com.IP1.prere_mqll",//预热段煤气流量
            "L2Com.IP1.onere_kqll",//加一段空气流量
            "L2Com.IP1.onere_mqll",//加一段煤气流量
            "L2Com.IP1.twore_kqll",//加二段空气流量
            "L2Com.IP1.twore_mqll",//加二段煤气流量
            "L2Com.IP1.junre_up_kqll",//均热段上部空气流量
            "L2Com.IP1.junre_up_mqll",//均热段上部煤气流量
            "L2Com.IP1.junre_low_kqll",//均热段下部空气流量
            "L2Com.IP1.junre_low_mqll",//均热段下部煤气流量

            "L2Com.IP1.Enter_Temp",//入炉钢坯温度
            "L2Com.IP1.yskqyl",//压缩空气压力
            "L2Com.IP1.jhsll",//净环水流量
            "L2Com.IP1.rsll",//软水流量
            "L2Com.IP1.wszqll",//外送蒸汽流量
            "L2Com.IP1.wszqyl",//外送蒸汽压力
            "L2Com.IP1.dqyl",//氮气压力
            "L2Com.IP1.mqlj"//煤气累计


            /*
            "L2Com.IP1.mqll", //煤气流量
		    "L2Com.IP1.mqlj", //煤气消耗量累计

		    "L2Com.IP1.junre_up_A_temp",//均热上层a侧温度
		    "L2Com.IP1.junre_up_B_temp", //均热上层b侧温度
		    "L2Com.IP1.junre_low_A_temp", //均热下层a侧温度
            "L2Com.IP1.junre_low_B_temp", //均热下层b侧温度
		    "L2Com.IP1.twore_up_A_temp", //二加段上层a侧温度
		    "L2Com.IP1.twore_up_B_temp", //二加段上层b侧温度
		    "L2Com.IP1.onere_up_A_temp", //一加段上层a侧温度
		    "L2Com.IP1.onere_up_B_temp", //一加段上层b侧温度
		    "L2Com.IP1.prere_up_A_temp", //预热段上层a侧温度
		    "L2Com.IP1.prere_up_B_temp", //预热段上层b侧温度

            //煤气&空气
		    "L2Com.IP1.mqzgyl", // 煤气总管压力
		    "L2Com.IP1.mqzgll", // 煤气总管流量
            "L2Com.IP1.zrkqyl", // 助燃空气压力(助燃风机总管压力)
            "L2Com.IP1.prere_kqll", // 预热段空气流量
            "L2Com.IP1.prere_mqll", // 预热段煤气流量
		    "L2Com.IP1.onere_kqll", // 加一段空气流量
		    "L2Com.IP1.onere_mqll", // 加一段煤气流量
            "L2Com.IP1.twore_kqll", // 加二段空气流量
            "L2Com.IP1.twore_mqll", // 加二段煤气流量
		    "L2Com.IP1.junre_up_kqll", // 均热段上部空气流量
		    "L2Com.IP1.junre_up_mqll", // 均热段上部煤气流量
            "L2Com.IP1.junre_low_kqll", // 均热段下部空气流量
		    "L2Com.IP1.junre_low_mqll", // 均热段下部煤气流量
        */            
        };
        public static string[] KepTagsTrack = {
            /*
            "L2Com.EP1.Track.Plate_ID_01", //
            "L2Com.EP1.Track.Plate_ID_02", //
            "L2Com.EP1.Track.Plate_ID_03", //
            "L2Com.EP1.Track.Plate_ID_04", //
            "L2Com.EP1.Track.Plate_ID_05", //
            "L2Com.EP1.Track.Plate_ID_06", //
            "L2Com.EP1.Track.Plate_ID_07", //
            "L2Com.EP1.Track.Plate_ID_08", //
            "L2Com.EP1.Track.Plate_ID_09", //
            "L2Com.EP1.Track.Plate_ID_10", //
            "L2Com.EP1.Track.Plate_ID_11", //
            "L2Com.EP1.Track.Plate_ID_12", //
            "L2Com.EP1.Track.Plate_ID_13", //
            "L2Com.EP1.Track.Plate_ID_14", //
            "L2Com.EP1.Track.Plate_ID_15", //
            "L2Com.EP1.Track.Plate_ID_16", //
            "L2Com.EP1.Track.Plate_ID_17", //
            "L2Com.EP1.Track.Plate_ID_18", //
            "L2Com.EP1.Track.Plate_ID_19", //
            "L2Com.EP1.Track.Plate_ID_20", //
            "L2Com.EP1.Track.Plate_ID_ch1", //
            "L2Com.EP1.Track.Plate_ID_ch1_SP", //
            "L2Com.EP1.Track.Plate_ID_ch2", //
            "L2Com.EP1.Track.Plate_ID_ch2_SP", //
            "L2Com.EP1.Track.Plate_ID_dch", //
            "L2Com.EP1.Track.Plate_Length_01", //
            "L2Com.EP1.Track.Plate_Length_02", //
            "L2Com.EP1.Track.Plate_Length_03", //
            "L2Com.EP1.Track.Plate_Length_04", //
            "L2Com.EP1.Track.Plate_Length_05", //
            "L2Com.EP1.Track.Plate_Length_06", //
            "L2Com.EP1.Track.Plate_Length_07", //
            "L2Com.EP1.Track.Plate_Length_08", //
            "L2Com.EP1.Track.Plate_Length_09", //
            "L2Com.EP1.Track.Plate_Length_10", //
            "L2Com.EP1.Track.Plate_Length_11", //
            "L2Com.EP1.Track.Plate_Length_12", //
            "L2Com.EP1.Track.Plate_Length_13", //
            "L2Com.EP1.Track.Plate_Length_14", //
            "L2Com.EP1.Track.Plate_Length_15", //
            "L2Com.EP1.Track.Plate_Length_16", //
            "L2Com.EP1.Track.Plate_Length_17", //
            "L2Com.EP1.Track.Plate_Length_18", //
            "L2Com.EP1.Track.Plate_Length_19", //
            "L2Com.EP1.Track.Plate_Length_20", //
            "L2Com.EP1.Track.Plate_Length_ch1", //
            "L2Com.EP1.Track.Plate_Length_ch1_SP", //
            "L2Com.EP1.Track.Plate_Length_ch2", //
            "L2Com.EP1.Track.Plate_Length_ch2_SP", //
            "L2Com.EP1.Track.Plate_Length_dch", //
            "L2Com.EP1.Track.Plate_Width_01", //
            "L2Com.EP1.Track.Plate_Width_02", //
            "L2Com.EP1.Track.Plate_Width_03", //
            "L2Com.EP1.Track.Plate_Width_04", //
            "L2Com.EP1.Track.Plate_Width_05", //
            "L2Com.EP1.Track.Plate_Width_06", //
            "L2Com.EP1.Track.Plate_Width_07", //
            "L2Com.EP1.Track.Plate_Width_08", //
            "L2Com.EP1.Track.Plate_Width_09", //
            "L2Com.EP1.Track.Plate_Width_10", //
            "L2Com.EP1.Track.Plate_Width_11", //
            "L2Com.EP1.Track.Plate_Width_12", //
            "L2Com.EP1.Track.Plate_Width_13", //
            "L2Com.EP1.Track.Plate_Width_14", //
            "L2Com.EP1.Track.Plate_Width_15", //
            "L2Com.EP1.Track.Plate_Width_16", //
            "L2Com.EP1.Track.Plate_Width_17", //
            "L2Com.EP1.Track.Plate_Width_18", //
            "L2Com.EP1.Track.Plate_Width_19", //
            "L2Com.EP1.Track.Plate_Width_20", //
            "L2Com.EP1.Track.Plate_Width_ch1", //
            "L2Com.EP1.Track.Plate_Width_ch1_SP", //
            "L2Com.EP1.Track.Plate_Width_ch2", //
            "L2Com.EP1.Track.Plate_Width_ch2_SP", //
            "L2Com.EP1.Track.Plate_Width_dch", //
            "L2Com.EP1.Track.Plate_Thickness_01", //
            "L2Com.EP1.Track.Plate_Thickness_02", //
            "L2Com.EP1.Track.Plate_Thickness_03", //
            "L2Com.EP1.Track.Plate_Thickness_04", //
            "L2Com.EP1.Track.Plate_Thickness_05", //
            "L2Com.EP1.Track.Plate_Thickness_06", //
            "L2Com.EP1.Track.Plate_Thickness_07", //
            "L2Com.EP1.Track.Plate_Thickness_08", //
            "L2Com.EP1.Track.Plate_Thickness_09", //
            "L2Com.EP1.Track.Plate_Thickness_10", //
            "L2Com.EP1.Track.Plate_Thickness_11", //
            "L2Com.EP1.Track.Plate_Thickness_12", //
            "L2Com.EP1.Track.Plate_Thickness_13", //
            "L2Com.EP1.Track.Plate_Thickness_14", //
            "L2Com.EP1.Track.Plate_Thickness_15", //
            "L2Com.EP1.Track.Plate_Thickness_16", //
            "L2Com.EP1.Track.Plate_Thickness_17", //
            "L2Com.EP1.Track.Plate_Thickness_18", //
            "L2Com.EP1.Track.Plate_Thickness_19", //
            "L2Com.EP1.Track.Plate_Thickness_20", //
            "L2Com.EP1.Track.Plate_Thickness_ch1", //
            "L2Com.EP1.Track.Plate_Thickness_ch1_SP", //
            "L2Com.EP1.Track.Plate_Thickness_ch2", //
            "L2Com.EP1.Track.Plate_Thickness_ch2_SP", //
            "L2Com.EP1.Track.Plate_Thickness_dch", //
            "L2Com.EP1.Track.Plate_HeadPosition_01", //
            "L2Com.EP1.Track.Plate_HeadPosition_02", //
            "L2Com.EP1.Track.Plate_HeadPosition_03", //
            "L2Com.EP1.Track.Plate_HeadPosition_04", //
            "L2Com.EP1.Track.Plate_HeadPosition_05", //
            "L2Com.EP1.Track.Plate_HeadPosition_06", //
            "L2Com.EP1.Track.Plate_HeadPosition_07", //
            "L2Com.EP1.Track.Plate_HeadPosition_08", //
            "L2Com.EP1.Track.Plate_HeadPosition_09", //
            "L2Com.EP1.Track.Plate_HeadPosition_10", //
            "L2Com.EP1.Track.Plate_HeadPosition_11", //
            "L2Com.EP1.Track.Plate_HeadPosition_12", //
            "L2Com.EP1.Track.Plate_HeadPosition_13", //
            "L2Com.EP1.Track.Plate_HeadPosition_14", //
            "L2Com.EP1.Track.Plate_HeadPosition_15", //
            "L2Com.EP1.Track.Plate_HeadPosition_16", //
            "L2Com.EP1.Track.Plate_HeadPosition_17", //
            "L2Com.EP1.Track.Plate_HeadPosition_18", //
            "L2Com.EP1.Track.Plate_HeadPosition_19", //
            "L2Com.EP1.Track.Plate_HeadPosition_20", //
            "L2Com.EP1.Track.Plate_HeadPosition_ch1", //
            "L2Com.EP1.Track.Plate_HeadPosition_ch1_SP", //
            "L2Com.EP1.Track.Plate_HeadPosition_ch2", //
            "L2Com.EP1.Track.Plate_HeadPosition_ch2_SP", //
            "L2Com.EP1.Track.Plate_HeadPosition_dch", //
            "L2Com.EP1.Track.Plate_ActualSpeed_01", //
            "L2Com.EP1.Track.Plate_ActualSpeed_02", //
            "L2Com.EP1.Track.Plate_ActualSpeed_03", //
            "L2Com.EP1.Track.Plate_ActualSpeed_04", //
            "L2Com.EP1.Track.Plate_ActualSpeed_05", //
            "L2Com.EP1.Track.Plate_ActualSpeed_06", //
            "L2Com.EP1.Track.Plate_ActualSpeed_07", //
            "L2Com.EP1.Track.Plate_ActualSpeed_08", //
            "L2Com.EP1.Track.Plate_ActualSpeed_09", //
            "L2Com.EP1.Track.Plate_ActualSpeed_10", //
            "L2Com.EP1.Track.Plate_ActualSpeed_11", //
            "L2Com.EP1.Track.Plate_ActualSpeed_12", //
            "L2Com.EP1.Track.Plate_ActualSpeed_13", //
            "L2Com.EP1.Track.Plate_ActualSpeed_14", //
            "L2Com.EP1.Track.Plate_ActualSpeed_15", //
            "L2Com.EP1.Track.Plate_ActualSpeed_16", //
            "L2Com.EP1.Track.Plate_ActualSpeed_17", //
            "L2Com.EP1.Track.Plate_ActualSpeed_18", //
            "L2Com.EP1.Track.Plate_ActualSpeed_19", //
            "L2Com.EP1.Track.Plate_ActualSpeed_20", //
            "L2Com.EP1.Track.Plate_ActualSpeed_ch1", //
            "L2Com.EP1.Track.Plate_ActualSpeed_ch1_SP", //
            "L2Com.EP1.Track.Plate_ActualSpeed_ch2", //
            "L2Com.EP1.Track.Plate_ActualSpeed_ch2_SP", //
            "L2Com.EP1.Track.Plate_ActualSpeed_dch", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_01", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_02", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_03", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_04", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_05", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_06", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_07", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_08", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_09", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_10", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_11", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_12", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_13", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_14", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_15", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_16", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_17", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_18", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_19", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_20", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_ch1", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_ch1_SP", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_ch2", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_ch2_SP", //
            "L2Com.EP1.Track.Plate_ActualStayingTime_dch", //
            */
        };
        public OPCHelper()
        {
            connected = false;
            bool inited = false;
            while (!inited)
            {
                try
                {
                    Logger.logwrite("初始化OPC");
                    initOPC();
                    Logger.logwrite("OPC初始化完毕");
                    inited = true;
                }
                catch (Exception err)
                {
                    Logger.logwrite("10秒后重试");
                    Thread.Sleep(10000);
                }

            }
        }

        public void initOPC()
        {
            KepServer = new OPCServer();
            try
            {
                KepServer.Connect("KEPware.KEPServerEx.V6", "");
            }
            catch (Exception err1)
            {
                KepServer.Connect("KEPware.KEPServerEx.V4", "");
            }

            connected = true;
            try
            {
                KepGroups = KepServer.OPCGroups;
                KepGroup = KepGroups.Add("OPCDOTNETGROUP");
                SetGroupProperty();
                KepItems = KepGroup.OPCItems;
            }
            catch (Exception err)
            {
                string errtxt = "创建组出现错误：" + err.Message;
                Logger.logwrite(errtxt);
                Console.ReadKey();
                Environment.Exit(0);
            }

            InitOPCItem();
        }

        public void SetGroupProperty()
        {
            KepServer.OPCGroups.DefaultGroupIsActive = Convert.ToBoolean("true");
            KepServer.OPCGroups.DefaultGroupDeadband = Convert.ToInt32("0");
            KepGroup.UpdateRate = Convert.ToInt32("250");
            KepGroup.IsActive = Convert.ToBoolean("true");
            KepGroup.IsSubscribed = Convert.ToBoolean("true");
        }

        public void InitOPCItem()
        {
            string tag = "";
            try
            {
                for (int i = 0; i < KepTags.Length; i++)
                {
                    tag = KepTags[i];
                    //Logger.logwrite("AddItem KepTags" + i);
                    OPCItem item = KepItems.AddItem(tag, ClientHandle);
                    opcItem.Add(tag, item);
                    ItemValue.Add(tag, "");
                }
                for (int i = 0; i < KepTagsMonitor.Length; i++)
                {
                    tag = KepTagsMonitor[i];
                    //Logger.logwrite("AddItem KepTagsMonitor" + i);
                    OPCItem item = KepItems.AddItem(tag, ClientHandle);
                    opcItemMonitor.Add(tag, item);
                    ItemValueMonitor.Add(tag, "");
                }
                /*
                for (int i = 0; i < KepTagsTrack.Length; i++)
                {
                    tag = KepTagsTrack[i];
                    Logger.logwrite("AddItem KepTagsTrack" + i);
                    OPCItem item = KepItems.AddItem(tag, ClientHandle);
                    opcItemTrack.Add(tag, item);
                    ItemValueMonitor.Add(tag, "");
                }
                */

                //opcItem["L2Com.EP1.W_Weight1"].Write(1);
                //object PValue;
                //object PQuality;
                //object PTimeStamp;
                //opcItem["L2Com.EP1.R_Out_Id"].Read(1, out PValue, out PQuality, out PTimeStamp);
                //Int16[] myArray = { 56, 48, 48, 65, 66, 67, 68, 69, 70, 48, 48, 48, 48, 68, 69, 70, 48, 48, 48, 48 };
                //char[] myArray2 = "8001234567890       ".ToCharArray();
                //opcItem["L2Com.EP1.W_Upper_Id2"].Write(PValue);
                //opcItem["L2Com.EP1.W_Upper_Id2"].Write((object)(myArray));

            }
            catch (Exception err)
            {
                string errtxt = "初始化OPCItem出现错误，请检查OPC服务器！" + tag;
                Logger.logwrite(errtxt);
                Logger.logwrite(errtxt, "OPC");
                Logger.logwrite(err.Message, "OPC");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        /// 将一个object对象序列化，返回一个string
        public static string ObjectToString(object obj)
        {
            Int16[] array = (Int16[])obj;
            int len = array.Length;
            char[] c_array = new char[len];
            for (int i = 0; i < len; i++)
            {
                c_array[i] = (char)(array[i]);
            }
            return new string(c_array);
        }

        public bool ReadData(Dictionary<string, OPCItem> opcItemTemp, Dictionary<string, string> ItemValueTemp, string[] KepTagsTemp)
        {

            for (int i = 0; i < KepTagsTemp.Length; i++)
            {
                string tag = KepTagsTemp[i];
                OPCItem item = opcItemTemp[tag];
                string res = ReadDataByTag(item, tag);
                if (res == OPC_ERROR_FLAG)
                    return false;
                ItemValueTemp[tag] = res;
            }
            return true;
        }

        public string ReadDataByTag(OPCItem item, string tag)
        {
            object PValue;
            object PQuality;
            object PTimeStamp;
            string res = "";
            try
            {
                item.Read(1, out PValue, out PQuality, out PTimeStamp);
                if (Convert.ToInt32(PQuality) != 0)
                {
                    if (Convert.ToString(PValue) == "True")
                    {
                        res = "1";
                    }
                    else if (Convert.ToString(PValue) == "False")
                    {
                        res = "0";
                    }
                    else
                        res = Convert.ToString(PValue).Trim();

                    if (res == "System.Int16[]")
                    {
                        res = ObjectToString(PValue).Trim();
                    }
                }
                else if (Convert.ToInt32(PQuality) == 0)
                {
                    res = null;
                }
            }
            catch (Exception err)
            {
                string errtxt = string.Format("读取一级数据{0}出现错误：{1}", tag, err.Message);
                Logger.logwrite(errtxt);
                Logger.logwrite(errtxt, "OPC");
                initOPC();
                res = OPC_ERROR_FLAG;
            }
            return res;
        }


        public bool ReadDataFromOPC(bool monitor = false)
        {
            Dictionary<string, OPCItem> opcItemTemp;
            Dictionary<string, string> ItemValueTemp;
            string[] KepTagsTemp;
            if (!monitor)
            {
                opcItemTemp = opcItem;
                ItemValueTemp = ItemValue;
                KepTagsTemp = KepTags;
                return ReadData(opcItem, ItemValue, KepTags);
            }
            else
            {
                bool res;
                opcItemTemp = opcItemMonitor;
                ItemValueTemp = ItemValueMonitor;
                KepTagsTemp = KepTagsMonitor;
                res = ReadData(opcItemMonitor, ItemValueMonitor, KepTagsMonitor);
                if (res)
                {
                    res = ReadData(opcItemTrack, ItemValueMonitor, KepTagsTrack);
                }
                return res;
            }

        }

        public void HeartBeat()
        {
            try
            {
                int r_heart = Convert.ToInt16(ItemValue["L2Com.EP1.R_HEART"].Trim());

                opcItem["L2Com.EP1.W_HEART"].Write(r_heart);

            }
            catch (Exception ex)
            {
                string errtxt = "心跳电文出错！" + ex.Message;
                Logger.logwrite(errtxt);
                Logger.logwrite(errtxt, "OPC");
            }
        }
    }
}