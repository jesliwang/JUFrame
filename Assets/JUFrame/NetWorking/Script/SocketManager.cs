using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace JUFrame
{
    class MySocketEventArgs : SocketAsyncEventArgs
    {

        /// <summary>  
        /// 标识，只是一个编号而已  
        /// </summary>  
        public int ArgsTag { get; set; }

        /// <summary>  
        /// 设置/获取使用状态  
        /// </summary>  
        public bool IsUsing { get; set; }

    }

    /*
    // option_ver:
    // 1：用于外网通信，option_len为0
    // 2：用于内网通信，option_len大于0，附加数据是pb的名字

    #pragma pack(push, 1)
    typedef struct{
        uint16_t msg_id; // 消息id
        uint32_t msg_len; // 消息长度
        uint64_t uid; // 发送该包的玩家
        uint8_t option_ver; // 附加数据版本
        uint8_t option_len; // 附加数据长度
    }CommonPackHead;
    #pragma pack(pop)
    */
    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct CommonPackHead
    {
        public UInt16 msg_id; // 消息id
        public UInt32 msg_len; // 消息长度
        public UInt64 uid; // 发送该包的玩家
        public byte option_ver; // 附加数据版本
        public byte option_len; // 附加数据版本
    }

    public class SocketManager : IDisposable
    {
        private const Int32 BuffSize = 1024;

        // The socket used to send/receive messages.  
        private Socket clientSocket;

        // Flag for connected socket.  
        private bool connected
        {
            get
            {
                int val = 1;
                Interlocked.CompareExchange(ref val, 0, _connected);
                if (val == 0)
                    return true;
                return false;
            }
            set
            {
                _connected = value ? 1 : 0;
            }
        }
        private int _connected;

        // Listener endpoint.  
        private IPEndPoint hostEndPoint;

        // Signals a connection.  
        private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);

        BufferManager m_bufferManager;
        //定义接收数据的对象  
        List<byte> m_buffer;
        //发送与接收的MySocketEventArgs变量定义.  
        private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();
        private MySocketEventArgs receiveEventArgs = new MySocketEventArgs();
        int tagCount = 0;

        /// <summary>  
        /// 当前连接状态  
        /// </summary>  
        public bool Connected { get { return clientSocket != null && clientSocket.Connected; } }

        //服务器主动发出数据受理委托及事件  
        public delegate void OnServerDataReceived(CommonPackHead packHead, byte[] receiveBuff);
        public event OnServerDataReceived ServerDataHandler;

        //服务器主动关闭连接委托及事件  
        public delegate void OnServerStop();
        public event OnServerStop ServerStopEvent;


        protected Dictionary<uint, KCP> kcpManager = new Dictionary<uint, KCP>();
        protected uint usingConv;

        // Create an uninitialized client instance.  
        // To start the send/receive processing call the  
        // Connect method followed by SendReceive method.  
        internal SocketManager(String ip, Int32 port)
        {
            // Instantiates the endpoint and socket.  
            hostEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            clientSocket = new Socket(hostEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_bufferManager = new BufferManager(BuffSize * 2, BuffSize);
            m_buffer = new List<byte>();
        }

        /// <summary>  
        /// 连接到主机  
        /// </summary>  
        /// <returns>0.连接成功, 其他值失败,参考SocketError的值列表</returns>  
        internal SocketError Connect()
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.UserToken = clientSocket;
            connectArgs.RemoteEndPoint = hostEndPoint;
            connectArgs.AcceptSocket = clientSocket;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

            clientSocket.ConnectAsync(connectArgs);
            autoConnectEvent.WaitOne(5000, false); //阻塞. 让程序在这里等待,直到连接响应后再返回连接结果  

            return connectArgs.SocketError;
        }

        protected void SendData(byte[] data, int len)
        {
            if (connected)
            {
                //查找有没有空闲的发送MySocketEventArgs,有就直接拿来用,没有就创建新的.So easy!  
                MySocketEventArgs sendArgs = listArgs.Find(a => a.IsUsing == false);
                if (sendArgs == null)
                {
                    sendArgs = initSendArgs();
                }
                lock (sendArgs) //要锁定,不锁定让别的线程抢走了就不妙了.  
                {
                    sendArgs.IsUsing = true;
                    sendArgs.SetBuffer(data, 0, len);
                }

                clientSocket.SendAsync(sendArgs); 
            }
            else
            {
                throw new SocketException((Int32)SocketError.NotConnected);
            }
        }

        /// Disconnect from the host.  
        internal void Disconnect()
        {
            clientSocket.Disconnect(false);
        }

        protected Thread kcpUpdateThread;

        // Calback for connect operation  
        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of connection.  
            autoConnectEvent.Set(); //释放阻塞.  
            lock (kcpManager)
            {
                if (!kcpManager.ContainsKey(1))
                {
                    kcpManager.Add(1, new KCP(1, SendData));
                }
                usingConv = 1;
            }
            kcpUpdateThread = new Thread(UpdateKcp);
            kcpUpdateThread.IsBackground = true;
            kcpUpdateThread.Start();

            // Set the flag for socket connected.  
            connected = (e.SocketError == SocketError.Success);
            //如果连接成功,则初始化socketAsyncEventArgs  
            if (connected)
            {
                initArgs(e);
                string str = "connect_req";
                Send(Encoding.ASCII.GetBytes(str.ToCharArray()));
            }

        }

        protected long GetCurrent()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.Milliseconds);
        }

        // kcp线程更新频率
        protected long ThreadGap = 50;

        protected byte[] kcpDataCache = new byte[1024 * 10];

        protected void UpdateKcp()
        {
            while (connected)
            {
                long start = GetCurrent();
                lock (kcpManager)
                {
                    foreach (KeyValuePair<uint, KCP> kv in kcpManager)
                    {
                        kv.Value.Update((uint)start);

                        int len = kv.Value.Recv(kcpDataCache);

                        if (len > 0)
                        {

                            byte[] data = new byte[len];
                            Array.Copy(kcpDataCache, data, len);
                            m_buffer.AddRange(data);

                            //DoReceiveEvent()
                            do
                            {

                                int headLength = Marshal.SizeOf(typeof(CommonPackHead));
                                // 判断包头是否满足
                                if (headLength <= m_buffer.Count)
                                {
                                    byte[] lenBytes = m_buffer.GetRange(0, headLength).ToArray();
                                    //分配结构体内存空间
                                    IntPtr structPtr = Marshal.AllocHGlobal(headLength);
                                    //将byte数组拷贝到分配好的内存空间
                                    Marshal.Copy(lenBytes, 0, structPtr, headLength);
                                    //将内存空间转换为目标结构体
                                    CommonPackHead packHead = (CommonPackHead)Marshal.PtrToStructure(structPtr, typeof(CommonPackHead));
                                    //释放内存空间
                                    Marshal.FreeHGlobal(structPtr);

                                    if (packHead.msg_len <= m_buffer.Count - headLength)
                                    {
                                        //包够长时,则提取出来,交给后面的程序去处理  
                                        byte[] recv = m_buffer.GetRange(0, headLength + (int)packHead.msg_len).ToArray();

                                        lock (m_buffer)
                                        {
                                            m_buffer.RemoveRange(0, headLength + (int)packHead.msg_len);
                                        }
                                        //将数据包交给前台去处理  
                                        DoReceiveEvent(packHead, recv);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                
                                    // 
                                    if( 4 == len)
                                    {
                                        uint new_conv = 0;
                                        KCP.ikcp_decode32u(data, 0, ref new_conv);
                                        
                                        if(!kcpManager.ContainsKey(new_conv))
                                        {
                                            kcpManager.Add(new_conv, new KCP(new_conv, SendData));
                                            Log.Error("new_conv=" + new_conv);
                                            lock (m_buffer)
                                            {
                                                m_buffer.RemoveRange(0, 4);
                                            }
                                        }

                                        usingConv = new_conv;

                                    }

                                    //长度不够,还得继续接收,需要跳出循环  
                                    break;
                                }

                            } while (m_buffer.Count > 4);
                        }

                    }

                }
                long after = GetCurrent();
                if(after - start < ThreadGap)
                {
                    Thread.Sleep((int)(ThreadGap + start - after));
                }

            }

        }


        #region args  

        /// <summary>  
        /// 初始化收发参数  
        /// </summary>  
        /// <param name="e"></param>  
        private void initArgs(SocketAsyncEventArgs e)
        {
            m_bufferManager.InitBuffer(); 
            //发送参数  
            initSendArgs(); 
            //接收参数  
            receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed); 
            receiveEventArgs.UserToken = e.UserToken; 
            receiveEventArgs.ArgsTag = 0; 
            m_bufferManager.SetBuffer(receiveEventArgs); 

            //启动接收,不管有没有,一定得启动.否则有数据来了也不知道.  
            if (!e.AcceptSocket.ReceiveAsync(receiveEventArgs))
                ProcessReceive(receiveEventArgs);

        }

        /// <summary>  
        /// 初始化发送参数MySocketEventArgs  
        /// </summary>  
        /// <returns></returns>  
        MySocketEventArgs initSendArgs()
        {
            MySocketEventArgs sendArg = new MySocketEventArgs();
            sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            sendArg.UserToken = clientSocket;
            sendArg.RemoteEndPoint = hostEndPoint;
            sendArg.IsUsing = false;
            Interlocked.Increment(ref tagCount);
            sendArg.ArgsTag = tagCount;
            lock (listArgs)
            {
                listArgs.Add(sendArg);
            }
            return sendArg;
        }



        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            MySocketEventArgs mys = (MySocketEventArgs)e;
            // determine which type of operation just completed and call the associated handler  
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    mys.IsUsing = false; //数据发送已完成.状态设为False  
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        // This method is invoked when an asynchronous receive operation completes.   
        // If the remote host closed the connection, then the socket is closed.    
        // If data was received then the data is echoed back to the client.  
        //  
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                // check if the remote host closed the connection  
                Socket token = (Socket)e.UserToken;
                Log.Error("esdf=" + e.BytesTransferred + "," + e.SocketError);
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //读取数据  
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);

                    if (!(e.BytesTransferred < KCP.IKCP_OVERHEAD))
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
                   
                    if (!token.ReceiveAsync(e))
                        this.ProcessReceive(e);
                }
                else
                {
                    ProcessError(e);
                }
            }
            catch (Exception xe)
            {
                Console.WriteLine(xe.Message);
            }
        }

        // This method is invoked when an asynchronous send operation completes.    
        // The method issues another receive on the socket to read any additional   
        // data sent from the client  
        //  
        // <param name="e"></param>  
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                ProcessError(e);
            }
        }

        #endregion

        #region read write  

        // Close socket in case of failure and throws  
        // a SockeException according to the SocketError.  
        private void ProcessError(SocketAsyncEventArgs e)
        {
             
            Socket s = (Socket)e.UserToken;
            if (s.Connected)
            {
                // close the socket associated with the client  
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    // throws if client process has already closed  
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                    connected = false;
                }
            }
            //这里一定要记得把事件移走,如果不移走,当断开服务器后再次连接上,会造成多次事件触发.  
            foreach (MySocketEventArgs arg in listArgs)
                arg.Completed -= IO_Completed;
            receiveEventArgs.Completed -= IO_Completed;

            if (ServerStopEvent != null)
                ServerStopEvent();
        }

        // Exchange a message with the host.  
        internal int Send(byte[] sendBuffer)
        {
            if (connected)
            {
                lock (kcpManager)
                {
                    return kcpManager[usingConv].Send(sendBuffer);
                }
                    
            }
            else
            {
                throw new SocketException((Int32)SocketError.NotConnected);
            }
            
        }

        /// <summary>  
        /// 使用新进程通知事件回调  
        /// </summary>  
        /// <param name="buff"></param>  
        private void DoReceiveEvent(CommonPackHead packHead, byte[] buff)
        {
            if (ServerDataHandler == null) return;
            //ServerDataHandler(buff); //可直接调用.  
            //但我更喜欢用新的线程,这样不拖延接收新数据.  
            Thread thread = new Thread(new ParameterizedThreadStart((obj) =>
            {
                ServerDataHandler(packHead, (byte[])obj);
            }));
            thread.IsBackground = true;
            thread.Start(buff);
        }

        #endregion

        #region IDisposable Members  

        // Disposes the instance of SocketClient.  
        public void Dispose()
        {
            autoConnectEvent.Close();
            if (clientSocket.Connected)
            {
                clientSocket.Close();
            }
        }

        #endregion
    }
}