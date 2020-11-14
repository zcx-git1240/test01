using OPCAutomation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;

namespace Zp_Server
{
    class Program
    {
        [DllImport("User32.dll ", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll ", EntryPoint = "GetSystemMenu")]
        extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);

        [DllImport("user32.dll ", EntryPoint = "RemoveMenu")]
        extern static int RemoveMenu(IntPtr hMenu, int nPos, int flags);

        #region 私有变量
        private static int inTimer1 = 0, inTimerPlc = 0, inTimerReal=0; //定时

        //private static string ZP_DB = ConfigReader.getDBString();

        private static System.Timers.Timer aTimer = new System.Timers.Timer();  //调度专用   
        private static System.Timers.Timer aTimerPlc = new System.Timers.Timer();
        private static System.Timers.Timer aTimerMonitor = new System.Timers.Timer(); //跟踪数据采集
        
        private static OPCHelper opc = new OPCHelper();
        //private static OracleConnection db_Conn;
        private static string strDB = "provider=SQLOLEDB;Data Source=192.168.49.253;Initial Catalog=xgl2;User Id=sa;Password=Ztgt123";
        private static OleDbConnection db_Conn;

        public static Socket socketClient { get; set; }

        public static void ConnectToMes() {
            try
            {
                //创建实例
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //IPAddress ip = IPAddress.Parse("172.16.22.120");
                IPAddress ip = IPAddress.Parse("172.16.4.106");
                IPEndPoint point = new IPEndPoint(ip, 8087);
                //进行连接
                socketClient.Connect(point);
                Logger.logwrite("连接3级MES服务器成功");
            }
            catch (Exception e)
            {
                Logger.logwrite("连接3级MES服务器失败");
            }
        }

        #endregion

        static void Main(string[] args)
        {
            string fullPath = System.Environment.CurrentDirectory + "\\Zp_Server.exe";
            int WINDOW_HANDLER = FindWindow(null, fullPath);
            IntPtr CLOSE_MENU = GetSystemMenu((IntPtr)WINDOW_HANDLER, IntPtr.Zero);
            int SC_CLOSE = 0xF060;
            RemoveMenu(CLOSE_MENU, SC_CLOSE, 0x0);
            Console.WindowHeight = 25;
            Console.WindowWidth = 120;

            try
            {
                db_Conn = new OleDbConnection(strDB);
                db_Conn.Open();
            }
            catch (Exception err)
            {
                string errtxt = "连接数据库出现错误：" + err.Message;
                //Logger.logwrite(ZP_DB);
                Logger.logwrite(errtxt);
                Console.ReadKey();
                Environment.Exit(0);
            }

            //ConnectToMes();


            #region 定时器事件
            aTimer.Interval = 1 * 1000;
            aTimer.Elapsed += new ElapsedEventHandler(TimedEvent);
            aTimer.AutoReset = true;
            aTimer.Enabled = false;  //  默认为false

            aTimerMonitor.Interval = 30 * 1000 * 1;  //实时数据采集，30秒即可，以免数据过多。
            aTimerMonitor.Elapsed += new ElapsedEventHandler(TimedMonitorEvent);
            aTimerMonitor.AutoReset = true;
            aTimerMonitor.Enabled = true;

            aTimerPlc.Interval = 1 * 1000;
            aTimerPlc.Elapsed += new ElapsedEventHandler(TimedEventPlc);
            aTimerPlc.AutoReset = true;
            aTimerPlc.Enabled = true;

            #endregion

            string strLine;
            string start = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
            Console.WriteLine("启动时间：" + start);
            Console.WriteLine("====================================================================================================================");
            Console.WriteLine("                                           加热炉2级服务器端程序启动！");
            Console.WriteLine("                                           请输入“exit”+回车，退出程序");
            Console.WriteLine("====================================================================================================================");

            do
            {
                strLine = Console.ReadLine();
            } while (strLine != null && strLine != "exit");
        }

        private static void Dowork(string dqsj)
        {
            int crewID;
            int shiftID;
            CrewShifter.CrewShif(out crewID, out shiftID);

            BaseData bd = new BaseData(opc.opcItem, opc.ItemValue, db_Conn, crewID, shiftID, dqsj);
            try
            {
                TemperingFurnace tf = new TemperingFurnace(bd);
                PlateData pd = new PlateData(bd);
                //取消计划
                pd.CanclePlateData();

                //Logger.logwrite("EnterFurnace");
                // 钢板装炉完成
                tf.EnterFurnace();

                // 炉内各区段的信号响应（预热、加热、均热）
                tf.OnSection();

                //Logger.logwrite("OutFurnaceFinish");
                // 钢板出炉完成
                tf.OutFurnaceFinish();

                // 炉前剔除
                //tf.OnReject();

                // 关联计划信息
                pd.AddPlateData();

            }
            catch (Exception ex2)
            {
                string errtxt = "处理出错！+++++" + ex2.Message;
                Logger.logwrite(errtxt);
            }
        }

        private static void DoMonitorwork(Dictionary<string, string> values, string dqsj)
        {
            int crewID;
            int shiftID;
            CrewShifter.CrewShif(out crewID, out shiftID);
            MonitorBaseData mbd = new MonitorBaseData(opc.ItemValueMonitor, db_Conn, crewID, shiftID, dqsj);

            try
            {
                MonitorWorker mw = new MonitorWorker(mbd);
                mw.SaveTemp();
                mw.SaveMeas();
                mw.Consume();   // 煤气消耗
                //mw.SaveTrack();
                //mw.DealRepeateCancle();
            }
            catch (Exception ex2)
            {
                string errtxt = "数据库连接出错！" + ex2.Message;
                Logger.Errlogwrite(errtxt);
            }
        }

        private static void TimedEvent(object source, ElapsedEventArgs e)  //调度程序处理
        {
            if (!opc.connected)
            {
                opc.initOPC();
            }
            if (Interlocked.Exchange(ref inTimer1, 1) == 0)  //防止重入
            {
                string dqsj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                Dowork(dqsj);
                Interlocked.Exchange(ref inTimer1, 0);    //防止重入
            }
        }      
                   
        private static void TimedMonitorEvent(object source, ElapsedEventArgs e)  //跟踪数据采集
        {
            if (Interlocked.Exchange(ref inTimerReal, 1) == 0)  //防止重入
            {
                string dqsj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                if (!opc.ReadDataFromOPC(true))  //读取一级数据
                {
                    string txt = "4#热处理炉PLC数据读取错误！ ";
                    Logger.logwrite(txt);
                    // return;
                }
                else
                {
                    DoMonitorwork(opc.ItemValueMonitor, dqsj);
                }
                Interlocked.Exchange(ref inTimerReal, 0);    //防止重入
            }
        }

        private static void TimedEventPlc(object source, ElapsedEventArgs e)  //PLC数据、定时器启停
        {
            if (Interlocked.Exchange(ref inTimerPlc, 1) == 0)  //防止重入
            {
                try
                {
                    string dqsj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    if (!opc.ReadDataFromOPC())  //读取一级数据
                    {
                        string txt = "4#热处理炉PLC数据读取错误！ ";
                        Logger.logwrite(txt);
                    }
                    else
                    {
                        Dowork(dqsj);
                    }
                }
                catch (Exception ex)
                {
                    string errtxt = "4#热处理炉PLC数据读取错误！ " + ex.Message;
                    Logger.logwrite(errtxt);
                }             
                              
                Interlocked.Exchange(ref inTimerPlc, 0);    //防止重入
            }
        }
    }
}
