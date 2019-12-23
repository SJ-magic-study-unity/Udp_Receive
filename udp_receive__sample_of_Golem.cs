/************************************************************
参考URL
	UnityでUDPを受信してみる
		https://qiita.com/nenjiru/items/8fa8dfb27f55c0205651
	
string split
	https://www.sawalemontea.com/entry/2017/11/09/210000

Regex.Split
	https://www.sawalemontea.com/entry/2018/02/16/193000
	
thread:lock
	http://tsubakit1.hateblo.jp/entry/20121110/1352555965
	

Thread.Abort メソッドを利用してスレッドを終了させる際の注意点について
	https://blogs.msdn.microsoft.com/japan_platform_sdkwindows_sdk_support_team_blog/2018/04/13/thread-abort-method/
		
		contents
			Abort メソッドはスレッドを強制終了させるメソッドです。
			Abort メソッドを利用してスレッドを強制終了させると、以下の様な現象が発生する可能性があります。
			・ オブジェクト ハンドルやメモリ等のリーク
			・ プロセスの強制終了
			・ プロセスのデッドロック
	
スレッド終了 - [ マルチスレッド / C# ]
	http://bicycle.life.coocan.jp/takamints/index.php/snippets/snippet/mt/endthread/cs
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
	static Object sync = new Object();

	/********************
	********************/
	[SerializeField] int IN_PORT = 12351;
	
	static UdpClient udp = null; // need to be "static" to be touched in thread.
	static Thread thread;
	static bool b_ThreadRunning = false;
	
	/********************
	********************/
	static List<BoneDefs> boneDefsList = new List<BoneDefs>();
	static bool b_set_BoneDefsList = false;
	
	static FROM_GOLEM__FRAMEDATA_ALL FromGolem__FrameDataAll = new FROM_GOLEM__FRAMEDATA_ALL();
	
	
	StateChart_main StateChart_main = null;
	
	
	/****************************************
	****************************************/
	/******************************
	******************************/
	void Start ()
	{
		/********************
		********************/
		udp = new UdpClient(IN_PORT);
		udp.Client.ReceiveTimeout = 1000;
		
		thread = new Thread(new ThreadStart(ThreadMethod));
		b_ThreadRunning = true;
		thread.Start(); 
		
		/********************
		********************/
		StateChart_main = gameObject.GetComponent<StateChart_main>();
	}

	/******************************
	******************************/
	void Update ()
	{
		lock(sync){
			/********************
			********************/
			if(b_set_BoneDefsList){
				if(StateChart_main){
					GlobalParam.FromGolem__boneDefsList = boneDefsList;
					StateChart_main.b_set_BoneDefsList = true;
				}
				
				b_set_BoneDefsList = false;
			}
			
			/********************
			********************/
			if(FromGolem__FrameDataAll.b_set){
				if(StateChart_main)		StateChart_main.FromGolem__FrameData_All.set(ref FromGolem__FrameDataAll);
				
				FromGolem__FrameDataAll.b_set = false;
			}
		}
	}

	/******************************
	******************************/
	void OnDestroy () {
		if(thread != null) {
			lock(sync){
				b_ThreadRunning = false;//終了要求
			}
			if(thread.Join(2000/*ms*/))	{/*success*/}
			else						{thread.Abort(); /*timeout -> 強制終了.*/}
            thread = null;
			
			// 注) udpは、thread stopの後
			if(udp != null){
				udp.Close();
				udp = null;
			}
        }
	}
	
	/******************************
	******************************/
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
		
		boneDefsList.Clear();
		
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
		
		b_set_BoneDefsList = true;
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
		if(233 <= data.Length){
			int FrameId;
			
			bool b_Converted = int.TryParse(data[ofs], out FrameId);
			if(b_Converted)	FromGolem__FrameDataAll.FrameId = System.Convert.ToInt32(data[ofs]);
			else			FromGolem__FrameDataAll.FrameId = 0;
			
		}else{
			FromGolem__FrameDataAll.FrameId = 0;
		}
		
		/********************
		********************/
		FromGolem__FrameDataAll.b_set = true;
	}
	
	/******************************
	******************************/
	private static void ThreadMethod()
	{
		while(true)
		{
			/********************
			********************/
			IPEndPoint remoteEP = null;
			
			/********************
			Start()で、下記をコメントアウトすると、
				udp.Client.ReceiveTimeout = 1000;
				
			以下で、message来るまで、ずっと待機
				message = udp.Receive(ref remoteEP);
				
			もし、udpの相手がいない場合、threadを抜けられなくなる(Abortすればいいのだけど)ので、
			timeoutを設けた上で、try/catchを実装。catchしてやらないと、例外が漏れて、落ちてしまう。
			********************/
			byte[] message;
			try{
				message = udp.Receive(ref remoteEP);
			}catch (SocketException){
				// Debug.Log("udp:timeout");
				message = System.Text.Encoding.GetEncoding("shift_jis").GetBytes("");
			}
			
			/********************
			********************/
			string str_message = Encoding.ASCII.GetString(message);
			
			string[] block = Regex.Split(str_message, "<p>");
			if(System.Convert.ToString(block[0]) == "/Golem/SkeletonDefinition"){
				lock(sync){
					Set_SkeltonDefinition(ref block);
				}
			}else if(System.Convert.ToString(block[0]) == "/Golem/SkeletonData"){
				lock(sync){
					Set_FrameData_All(ref block);
				}
			}
			
			/********************
			********************/
			bool _b_ThreadRunning;
			lock(sync) { _b_ThreadRunning = b_ThreadRunning; }
			if(!_b_ThreadRunning) break;
			
			/********************
			********************/
			Thread.Sleep(1);
		}
	}
}
