using JUFrame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testeeeee : MonoBehaviour {

    protected Networking net;
	// Use this for initialization
	void Start () {
        net = new Networking();

        // 连接
        net.Connect("192.168.0.13", "51005");
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
