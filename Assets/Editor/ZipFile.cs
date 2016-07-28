using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography;


public class ZipFile
{
	
    public class md5
    {
        //32bitMD5
        public static string encrypt(string str)
        {
            string cl = str;
            string pwd = "";

            MD5 md5 = MD5.Create();

            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            for (int i = 0; i < s.Length; i++)
            {
                pwd = pwd + s[i].ToString("X");
            }
            return pwd;
        }

        public static string getMd5HashFromFile(string fn)
        {
            System.IO.FileStream fs = new FileStream(fn,System.IO.FileMode.Open,System.IO.FileAccess.Read);
            byte[] bs = new Byte[fs.Length];
            fs.Read(bs, 0, bs.Length);
            fs.Close();
            return getMd5Hash(bs);
        }

        public static string getMd5Hash(byte[] bytes)
        {
            string pwd = "";
            MD5 md5 = MD5.Create();

            byte[] s = md5.ComputeHash(bytes);
            for (int i = 0; i < s.Length; i++)
            {
                pwd = pwd + s[i].ToString("X");
            }
            return pwd;
        }
    }
}
