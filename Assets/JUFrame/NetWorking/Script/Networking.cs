using Battle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;
using UnityEngine;

namespace JUFrame
{
    public class Networking
    {
        protected SocketManager smanager = null;

        /// <summary>  
        /// 判断是否已连接  
        /// </summary>  
        public bool Connected
        {
            get { return smanager != null && smanager.Connected; }
        }

        protected SocketError Connect(string ip, int port)
        {
            if (Connected) return SocketError.Success;

            if (string.IsNullOrEmpty(ip) || port < 1000) return SocketError.Fault;

            SocketError error;
            //smanager = new SocketManager(ip, port);
            smanager = new UdpClientSocketManager(ip, port);
            error = smanager.Connect();

            if(error == SocketError.Success)
            {
                // 连接成功后,就注册事件.最好在成功后再注册.
                smanager.ServerDataHandler += OnReceivedServerData;
                smanager.ServerStopEvent += OnServerStopEvent;
            }

            return error;
        }

        protected string hostIp;
        protected int hostPort;

        public void Connect(string ip, string port)
        {
            hostIp = ip;
            if(!int.TryParse(port, out hostPort))
                hostPort = 51005;

            SocketError ot = Connect(ip, hostPort);

        }

        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name="buff"></param>  
        protected void OnReceivedServerData(CommonPackHead packHead, byte[] buff)
        {
            Log.Debug("OnReceivedServerData.dataLength=" + packHead.msg_id);
            Type type = Service.GetServiceType(packHead.msg_id);
            var genericType = typeof(NetMessage<>).MakeGenericType(type);
            Log.Assert(null != type, string.Format("msg_id({0}) receive not setup", packHead.msg_id));
            var p = Activator.CreateInstance(genericType, buff);

        }

        /// <summary>  
        /// 服务器已断开  
        /// </summary>  
        protected void OnServerStopEvent()
        {
            Log.Debug("OnServerStopEvent");
            Connect(hostIp, hostPort);
        }

        // <summary>  
        /// 发送字节流  
        /// </summary>  
        /// <param name="buff"></param>  
        /// <returns></returns>  
        protected bool Send(byte[] buff)
        {
            if (!Connected) return false;
            return 0 == smanager.Send(buff);
        }

        public bool Send<T>(int _msgID, long uid, T _data) where T : InfibrProtoBuf.IExtensible
        {
            if (!Connected) return false;
            return 0 == smanager.Send(new NetMessage<T>(_msgID, uid, _data).Serialize());
        }

        public bool Send<T>(NetMessage<T> data) where T : InfibrProtoBuf.IExtensible
        {
            if (!Connected) return false;
            return 0 == smanager.Send(data.Serialize());
        }

        public void Disconnect()
        {
            smanager.Dispose();
        }
    }
}