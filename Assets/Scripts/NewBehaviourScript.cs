using UnityEngine;
using System.Collections;

public class NewBehaviourScript : MonoBehaviour {

	public string name = "朱涛RUN";

   

#if UNITY_EDITOR
	void Reset()
	{
		Debug.Log ("脚本添加事件");
	}

	void OnValidate()
	{
		Debug.Log ("脚本对象数据发生改变事件");
	}
#endif
}
