using System;
using System.Xml;

namespace Zp_Server
{
    public class ConfigReader
    {
        public static String getDBString()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load("config.xml");
                }
                catch (Exception)
                {
                    try
                    {
                        xmlDoc.Load("D:/config.xml");
                    }
                    catch (Exception)
                    {
                        xmlDoc.Load("E:/config.xml");
                    }
                }
                XmlNode portConfigure = xmlDoc.SelectSingleNode("Configure/dbConfigure");
                string host = portConfigure.SelectSingleNode("Host").InnerText;
                string port = portConfigure.SelectSingleNode("Port").InnerText;
                string service_name = portConfigure.SelectSingleNode("ServiceName").InnerText;
                string user_id = portConfigure.SelectSingleNode("UserId").InnerText;
                string password = portConfigure.SelectSingleNode("Password").InnerText;
                string connStr = String.Format("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={2})));Persist Security Info=True;User ID={3};Password={4};",
                                                host, port, service_name, user_id, password);
                //Console.WriteLine(connStr);
                return connStr;
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
                return null;
            }
        }
    }
}
