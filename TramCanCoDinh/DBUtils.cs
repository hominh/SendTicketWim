using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TramCanCoDinh
{
    class DBUtils
    {
        public static MySqlConnection GetDBConnection()
        {

            IniFile myIni = new IniFile("config.ini");                                                                        

            string host = myIni.Read("DB_HOST", "Database Config");
            int port = Int32.Parse( myIni.Read("DB_PORT", "Database Config"));
            string database = myIni.Read("DB_SCHEMA", "Database Config");
            string username = ConfigAccess.GetDatabaseUsername();
            string password = ConfigAccess.GetDatabasePassword();
            //string username = myIni.Read("DB_USER", "Database Config");
            //string password = myIni.Read("DB_PASS", "Database Config");            

            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }

    }
}