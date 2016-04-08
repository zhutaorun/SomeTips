using UnityEngine;
using System.Collections;

public class FtpInfo : MonoBehaviour {

	public enum TransProtocol{
		scp,
		sftp
	}

	public enum ZipLevel{
		level_0=0,
		level_1,
		level_2,
		level_3,
		level_4,
		level_5,
		level_6,
		level_7,
		level_8,
		level_9
	}

	public string host;
	public string username;
	public string pswd;
	public TransProtocol trabsProtocol = TransProtocol.sftp;
	public string localRoot = "_Prefab/GameRoot";
	public string remoteRoot = "/home/lihouran/SmartFoxServre_2X/SFS2X/www/root/_UnityGameRoot";
	public ZipLevel ziplevel = ZipLevel.level_6;
	public string zipPassword="RanRanIsSmartBeautitifulGirl";
}
