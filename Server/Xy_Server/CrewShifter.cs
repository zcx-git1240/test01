using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zp_Server
{
    class CrewShifter
    {
        public static void CrewShif(out int crewid, out int shiftid)
        {
            DateTime kssj = DateTime.Parse("2017-12-01 00:00:00");
            string dqsj = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
            if (string.Compare(dqsj.Substring(11, 8), "00:00:00") >= 0 && string.Compare(dqsj.Substring(11, 8), "08:00:00") < 0)
            {
                shiftid = 1;//晚班
            }
            else if (string.Compare(dqsj.Substring(11, 8), "08:00:00") >= 0 && string.Compare(dqsj.Substring(11, 8), "16:00:00") < 0)
            {
                shiftid = 2;//白班
            }
            else
            {
                shiftid = 3;//中班
            }

            DateTime dt = DateTime.Now;
            TimeSpan ts = dt - kssj;
            Int32 day_jg = ts.Days;
            int ys = day_jg % 9;
            switch (ys)
            {
                case 0:
                case 1:
                case 2:
                    if (shiftid == 1)
                    {
                        crewid = 2;  //乙
                    }
                    else if (shiftid == 2)
                    {
                        crewid = 3;  //丙
                    }
                    else
                    {
                        crewid = 1;  //甲
                    }
                    break;
                case 3:
                case 4:
                case 5:
                    if (shiftid == 1)
                    {
                        crewid = 3;  //丙
                    }
                    else if (shiftid == 2)
                    {
                        crewid = 1;  //甲
                    }
                    else
                    {
                        crewid = 2;  //乙
                    }
                    break;
                case 6:
                case 7:
                case 8:
                    if (shiftid == 1)
                    {
                        crewid = 1;  //甲
                    }
                    else if (shiftid == 2)
                    {
                        crewid = 2;  //乙
                    }
                    else
                    {
                        crewid = 3;  //丙
                    }
                    break;
                default:
                    crewid = 0;
                    shiftid = 0;
                    break;
            }
        }
    }
}
