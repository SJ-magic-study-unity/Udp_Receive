/************************************************************
参考URL:UnityでUDPを受信してみる
	https://qiita.com/nenjiru/items/8fa8dfb27f55c0205651
	
string split
	string.Split
		https://www.sawalemontea.com/entry/2017/11/09/210000
		
	Regex.Split
		https://www.sawalemontea.com/entry/2018/02/16/193000
			var text = "AAｈりうｈAAｖほｇｈAAｈｒｈAAAAああｖｈｓ";
			var words = Regex.Split(text, "AA");
			
			foreach(var word in words)
			{
				Console.WriteLine("「{0}」",word);
			}
			
	sj note
		separatorが1文字の時は、Split、
		複数文字の時は、Regex.Split.
		
		だったら全部、Regex.Splitでsplitしてしまえばいいのでは？と思ったのだが、
		1文字に対してこれを
			var words = Regex.Split(text, "|");
		などとやると、上手く行かなかった.
************************************************************/
using UnityEngine;
using System.Collections;

// for udp.
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/************************************************************
************************************************************/

public class udp_receive : MonoBehaviour {
	/****************************************
	****************************************/
	[SerializeField] int IN_PORT = 12345;
	
	static UdpClient udp;
	Thread thread;
	
	static private string label = "saijo";

	/****************************************
	****************************************/
	
	/******************************
	******************************/
	void Start ()
	{
		udp = new UdpClient(IN_PORT);
		// udp.Client.ReceiveTimeout = 1000; // timeoutに引っかかってしまうことがあるので、設定しない.
		thread = new Thread(new ThreadStart(ThreadMethod));
		thread.Start(); 
	}

	/******************************
	******************************/
	void Update ()
	{
	}

	/******************************
	******************************/
	void OnApplicationQuit()
	{
		thread.Abort();
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
			// Debug.Log(text);
			
			var data = str_message.Split('|');
			// vector<string> data = ofSplitString(str_message,"|");
			
			label  = "size=";
			label += string.Format( "{0:0}", data.Length );
			label += ", (";
			for(int i = 0; i < data.Length; i++){
				if(data[i] == "")	label += "---";
				else				label += string.Format( "{0:000000.0}", (float)System.Convert.ToDouble(data[i]) );
				label += ", ";
			}
			label += ")";
		}
	}
	
	/******************************
	******************************/
	void OnGUI()
	{
		GUI.color = Color.black;
		
		GUI.Label(new Rect(15, 15, 1000, 30), label);
	}
}
