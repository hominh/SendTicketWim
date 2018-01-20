using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace TramCanCoDinh
{
    class Common
    {       
        public static string FileToBase64(string fileName)
        {
            using (FileStream reader = new FileStream(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[reader.Length];
                reader.Read(buffer, 0, (int)reader.Length);
                return Convert.ToBase64String(buffer);
            }
        }
        public static void Base64ToFile(string toFileName, string base64String)
        {
            using (System.IO.FileStream reader = System.IO.File.Create(toFileName))
            {
                byte[] buffer = Convert.FromBase64String(base64String);
                reader.Write(buffer, 0, buffer.Length);
            }
        }

        public static string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits
                //for each byte
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

    }
}
