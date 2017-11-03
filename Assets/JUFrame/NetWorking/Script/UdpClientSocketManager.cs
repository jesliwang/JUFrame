using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace JUFrame
{
    public class UdpClientSocketManager : SocketManager
    {
        protected UdpClient client; 

        internal UdpClientSocketManager(String ip, Int32 port):base(ip,port)
        {
            client = new UdpClient(ip, port);

            clientSocket = client.Client;

            kcpManager.Add(1, new KCP(1, SendData));
            usingConv = 1;

            kcpUpdateThread = new Thread(UpdateKcp);
            kcpUpdateThread.IsBackground = true;
            kcpUpdateThread.Start();
        }

        internal override SocketError Connect()
        {
            connected = true;
            client.BeginReceive(OnReceive, null);
            string str = "connect_req";
            Send(Encoding.ASCII.GetBytes(str.ToCharArray()));

            return SocketError.Success;
        }

        protected override void SendData(byte[] data, int len)
        {
            client.BeginSend(data, len, OnSend, null);

            Log.Error("1111111111");
        }

        void OnSend(IAsyncResult ar)
        {
            client.EndSend(ar);
        }

        void OnReceive(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    IPEndPoint ipEndpoint = null;
                    byte[] data = client.EndReceive(ar, ref ipEndpoint);

                    if (!(data.Length < KCP.IKCP_OVERHEAD))
                    {
                        lock (kcpManager)
                        {
                            uint new_conv = 0;
                            KCP.ikcp_decode32u(data, 0, ref new_conv);

                            if (kcpManager.ContainsKey(new_conv))
                            {
                                kcpManager[new_conv].Input(data);
                            }

                        }
                    }
                }
                
            }
            catch (SocketException e)
            {
                // This happens when a client disconnects, as we fail to send to that port.
            }
            client.BeginReceive(OnReceive, null);
        }


        internal override int Send(byte[] sendBuffer)
        {
            lock (kcpManager)
            {
                int ret = kcpManager[usingConv].Send(sendBuffer);
                return ret;
            }
        }

    }
}


