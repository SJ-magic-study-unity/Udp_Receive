/************************************************************
参考URL
	UnityでUDPを受信してみる
		https://qiita.com/nenjiru/items/8fa8dfb27f55c0205651
	
string split
	https://www.sawalemontea.com/entry/2017/11/09/210000

Regex.Split
	https://www.sawalemontea.com/entry/2018/02/16/193000
************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// for udp.
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// for SkeltonManager.cs
using Golem;

using SCENE_DEMO;

using System.Text.RegularExpressions;

/************************************************************
************************************************************/

public class udp_receive : MonoBehaviour {
	/****************************************
	****************************************/
	/********************
	********************/
	[SerializeField]
	int IN_PORT = 12352;
	
	static UdpClient udp = null; // need to be "static" to be touched in thread.
	Thread thread;
	
	/********************
	********************/
	static FROM_GOLEM__FRAMEDATA_ALL FromGolem__FrameDataAll = new FROM_GOLEM__FRAMEDATA_ALL();
	
	static StateChart_main StateChart_main;
	
	
	/****************************************
	****************************************/

	void Start ()
	{
		/********************
		********************/
		udp = new UdpClient(IN_PORT);
		// udp.Client.ReceiveTimeout = 1000;
		thread = new Thread(new ThreadStart(ThreadMethod));
		thread.Start(); 
		
		/********************
		********************/
		StateChart_main = gameObject.GetComponent<StateChart_main>();
	}

	void Update ()
	{
	}

	void OnDestroy () {
		Debug.Log("OnDestroy:udp_receive");
		
		udp.Close();
		udp = null;
		
 		thread.Abort();
		thread = null;
	}
	
	void OnApplicationQuit()
	{
		// thread.Abort();
	}
	
	/******************************
	******************************/
	static void Set_SkeltonDefinition(ref string[] block)
	{
		/********************
		********************/
		if(block.Length < 28){
			Debug.Log("Skelton Defs : format not match");
			return;
		}
		
		/********************
		********************/
		Debug.Log("set skelton definition");
		
		List<BoneDefs> boneDefsList = new List<BoneDefs>();
		
		for(int i = 0; i < 27; i++){
			// string[] data = Regex.Split(block[i + 1], "|");
			string[] data = block[i + 1].Split('|');
			if(data.Length < 10){
				Debug.Log("Skelton Defs : format not match");
				return;
			}
			
			boneDefsList.Add(new BoneDefs(	System.Convert.ToInt32(data[0]),
											System.Convert.ToInt32(data[1]),
											System.Convert.ToString(data[2]),
											(float)(System.Convert.ToDouble(data[3])),
											(float)(System.Convert.ToDouble(data[4])),
											(float)(System.Convert.ToDouble(data[5])),
											(float)(System.Convert.ToDouble(data[6])),
											(float)(System.Convert.ToDouble(data[7])),
											(float)(System.Convert.ToDouble(data[8])),
											(float)(System.Convert.ToDouble(data[9]))
											));
		}
		StateChart_main.FromGolem__boneDefsList = boneDefsList;
		StateChart_main.b_set_BoneDefsList = true;
	}

	/******************************
	******************************/
	static void Set_FrameData_All(ref string[] block)
	{
		/********************
		********************/
		// string[] data = Regex.Split(block[1], "|");
		string[] data = block[1].Split('|');
		
		if( data.Length < 232){
			Debug.Log("Skelton data : format not match");
			return;
		}
		
		/********************
		********************/
		int ofs = 1;
		for(int i = 0; i < 6; i++){
			FromGolem__FrameDataAll.INS_pos[i] = new Vector3((float)System.Convert.ToDouble(data[i * 3 + ofs + 0]), (float)System.Convert.ToDouble(data[i * 3 + ofs + 1]), (float)System.Convert.ToDouble(data[i * 3 + ofs + 2]));
		}
		
		ofs = 19;
		for(int i = 0; i < 6; i++){
			FromGolem__FrameDataAll.AI1_pos[i] = new Vector3((float)System.Convert.ToDouble(data[i * 3 + ofs + 0]), (float)System.Convert.ToDouble(data[i * 3 + ofs + 1]), (float)System.Convert.ToDouble(data[i * 3 + ofs + 2]));
		}
		
		ofs = 37;
		for(int i = 0; i < 27; i++){
			FromGolem__FrameDataAll.AI2_pos[i] = new Vector3((float)System.Convert.ToDouble(data[i * 7 + ofs + 0]), (float)System.Convert.ToDouble(data[i * 7 + ofs + 1]), (float)System.Convert.ToDouble(data[i * 7 + ofs + 2]));
			FromGolem__FrameDataAll.AI2_quartenion[i] = new Quaternion(	(float)System.Convert.ToDouble(data[i * 7 + ofs + 3]), 
																		(float)System.Convert.ToDouble(data[i * 7 + ofs + 4]), 
																		(float)System.Convert.ToDouble(data[i * 7 + ofs + 5]), 
																		(float)System.Convert.ToDouble(data[i * 7 + ofs + 6]));
		}
		
		ofs = 226;
		FromGolem__FrameDataAll.ZUPT_L = (float)System.Convert.ToDouble(data[ofs]);
		
		ofs++;
		FromGolem__FrameDataAll.ZUPT_R = (float)System.Convert.ToDouble(data[ofs]);
		
		ofs++;
		FromGolem__FrameDataAll.Hand_VelNorm_L = (float)System.Convert.ToDouble(data[ofs]);
		
		ofs++;
		FromGolem__FrameDataAll.Hand_VelNorm_R = (float)System.Convert.ToDouble(data[ofs]);
		
		ofs++;
		FromGolem__FrameDataAll.Hand_Theta_L = (float)System.Convert.ToDouble(data[ofs]);
		
		ofs++;
		FromGolem__FrameDataAll.Hand_Theta_R = (float)System.Convert.ToDouble(data[ofs]);
		
		// FrameId is for debug.
		// so, Real Golem system will not send this item.
		ofs++;
		if(233 <= data.Length)	FromGolem__FrameDataAll.FrameId = System.Convert.ToInt32(data[ofs]);
		else					FromGolem__FrameDataAll.FrameId = 0;
		
		/********************
		********************/
		StateChart_main.FromGolem__FrameData_All.set(ref FromGolem__FrameDataAll);
	}
	
	/******************************
	******************************/
	private static void ThreadMethod()
	{
		while(true)
		{
			IPEndPoint remoteEP = null;
			byte[] message = udp.Receive(ref remoteEP);
			string str_message = Encoding.ASCII.GetString(message);
			
			string[] block = Regex.Split(str_message, "<p>");
			if(System.Convert.ToString(block[0]) == "/Golem/SkeletonDefinition"){
				Set_SkeltonDefinition(ref block);
			}else if(System.Convert.ToString(block[0]) == "/Golem/SkeltonData"){
				Set_FrameData_All(ref block);
			}
		}
	}
}
