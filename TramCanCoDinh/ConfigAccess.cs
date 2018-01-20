using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace TramCanCoDinh
{
    class ConfigAccess
    {
        public static string GetTramCanID()
        {
            return ConfigurationSettings.AppSettings["TramCanID"].Trim();
        }

        public static string GetIpServer()
        {
            return ConfigurationSettings.AppSettings["SERVER_IP"].Trim();
        }
        public static int GetPortServer()
        {
            return Convert.ToInt32(ConfigurationSettings.AppSettings["SERVER_PORT"].Trim());
        }

        public static string GetVirtualHost()
        {
            return ConfigurationSettings.AppSettings["VIRTUALHOST"].Trim();
        }
        public static string GetExchange()
        {
            return ConfigurationSettings.AppSettings["EXCHANGE"].Trim();
        }

        public static string GetRoutingKey()
        {
            return ConfigurationSettings.AppSettings["ROUTINGKEY"].Trim();
        }
       
        public static string GetUserName()
        {
            return ConfigurationSettings.AppSettings["USERNAME_CONNECTSERVER"].Trim();
        }

        public static string GetPassword()
        {
            return ConfigurationSettings.AppSettings["PASSWORD_CONNECTSERVER"].Trim();
        }

        public static string GetDatabaseUsername()
        {
            return ConfigurationSettings.AppSettings["DB_USERNAME"].Trim();
        }

        public static string GetDatabasePassword()
        {
            return ConfigurationSettings.AppSettings["DB_PASSWD"].Trim();
        }

    }
}
