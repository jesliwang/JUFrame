using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;

namespace JUFrame.Tests
{
    public class NetworkingTest
    {

        [Test]
        public void TestNetworking()
        {
            // 设置服务器
            //Thread p = new Thread(Server);
            //p.Start();

            var net = new Networking();

            // 连接
            net.Connect("192.168.0.13", "51005");

            Assert.AreEqual(true, net.Connected);

            

            //p.Abort();
        }

        void Server()
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 6015);
            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            newsock.Bind(ip);

            byte[] data = new byte[1024];
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)(sender);
            int recv = newsock.ReceiveFrom(data, ref Remote);
            Debug.LogError("recv=" + recv);
        }
    }
}

