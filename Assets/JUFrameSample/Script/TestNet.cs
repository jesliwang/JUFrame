using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using JUFrame;
using Battle;
using System.Text;
using System;

public class TestNet : MonoBehaviour {

    protected Networking networker;
    
    // Use this for initialization
    void Start () {
        networker = new Networking();
        networker.Connect("192.168.0.13", "51005");

        StartCoroutine(TestData());
    }

    IEnumerator TestData()
    {
        while( null == networker || (!networker.Connected))
        {
            yield return null;
        }

        yield return new WaitForSeconds(5);

        GetBattleDataRequest data = new GetBattleDataRequest();
        string str = "20171102";

        data.room_id = Encoding.ASCII.GetBytes(str.ToCharArray());

        var message = new NetMessage<GetBattleDataRequest>(3001, 100001, data);

        byte[] checkCode = BitConverter.GetBytes((ushort)666);
        string battleId = "1001";
        byte[] battleIdByte = Encoding.ASCII.GetBytes(battleId);

        byte[] optData = new byte[checkCode.Length + battleIdByte.Length];
        Array.Copy(checkCode, 0, optData, 0, checkCode.Length);

        Array.Copy(battleIdByte, 0, optData, checkCode.Length, battleIdByte.Length);

        message.AddAppendData(optData, optData.Length);

        Log.Error("setNetStatus=" + networker.Send(message));

        StartCoroutine(TestData());
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnDestroy()
    {
        if(null != networker)
        {
            networker.Disconnect();
        }
    }
}
