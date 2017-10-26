using System;  
using System.Collections.Generic;  
using System.Linq;  
using System.Net.Sockets;  
using System.Security.Cryptography;  
using System.Text;  
using System.Threading;  
using System.Timers;  
  
namespace JUFrame
{
    public class Networking : MonoSingleton<Networking>
    {
        protected SocketManager smanager;

        /// <summary>  
        /// 判断是否已连接  
        /// </summary>  
        protected bool Connected
        {
            get { return smanager != null && smanager.Connected; }
        }

        protected SocketError Connect(string ip, int port)
        {
            if (Connected) return SocketError.Success;

            if (string.IsNullOrEmpty(ip) || port < 1000) return SocketError.Fault;

            smanager = new SocketManager(ip, port);
            SocketError error = smanager.Connect();

            if(error == SocketError.Success)
            {
                // 连接成功后,就注册事件.最好在成功后再注册.
                smanager.ServerDataHandler += OnReceivedServerData;
                smanager.ServerStopEvent += OnServerStopEvent;
            }

            return error;
        }

        public void Connect(string ip, string port)
        {
            SocketError ot = Connect(ip, 2333);

        }

        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name="buff"></param>  
        protected void OnReceivedServerData(byte[] buff)
        {
            Log.Debug("OnReceivedServerData.dataLength=" + buff);
        }

        /// <summary>  
        /// 服务器已断开  
        /// </summary>  
        protected void OnServerStopEvent()
        {

        }

        // <summary>  
        /// 发送字节流  
        /// </summary>  
        /// <param name="buff"></param>  
        /// <returns></returns>  
        public bool Send(byte[] buff)
        {
            if (!Connected) return false;
            smanager.Send(buff);
            return true;
        }
    }
}