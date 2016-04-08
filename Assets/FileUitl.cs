using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
public class FileUitl {

	public static byte[] GetFileData(string fn){
		if (!File.Exists (fn)) 
			return null;
		FileStream fs = new FileStream (fn, FileMode.Open);
		try{
			if(fs.Length>0){
				byte[] data = new byte[(int)fs.Length];
				fs.Read(data,0,(int)fs.Length);
				return data;
			}else{
				return null;
			}
		}finally{
			fs.Close();
		}
	}

	public static Dictionary<string,string> GetDictionaryFromFile(string fn){
		byte[] data = GetFileData (fn);
		if (data != null) {
			ByteReader br = new ByteReader(data);
			return br.ReadDictionary();
		}
		return null;
	}

	public static void SaveDictionary(string fn,Dictionary<string,string> dic){
		System.Text.StringBuilder sb = new System.Text.StringBuilder ();
		foreach (string k in dic.Keys) {
			string v= dic[k];
			sb.Append(string.Format("{0}={1}\r\n",k,v));
		}
		byte[] data = System.Text.ASCIIEncoding.ASCII.GetBytes (sb.ToString());
		saveFileData(fn,data);
	}

	public static void saveFileData(string fn,byte[] date){
		string dir = Path.GetDirectoryName (fn);
		DirectoryInfo dirinfo = new DirectoryInfo(dir);
		if (!dirinfo.Exists) 
			dirinfo.Create();
		FileStream fs = new FileStream (fn, FileMode.Create);
		try{
			fs.Write(date,0,date.Length);
		}finally{
			fs.Close();		
		}

	}
}
