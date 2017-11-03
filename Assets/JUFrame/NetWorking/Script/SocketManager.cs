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
    public class ObjectPool<TObject>
    {
        private int maxPoolSize;
        private Dictionary<Type, Stack<TObject>> poolCache;
        private Func<TObject> factory;

        public ObjectPool(int poolSize)
        {
            this.maxPoolSize = poolSize;
            this.poolCache = new Dictionary<Type, Stack<TObject>>();
        }

        public ObjectPool(int poolSize, Func<TObject> factory) : this(poolSize)
        {
            this.factory = factory;
        }

        public T Rent<T>() where T : TObject
        {
            return (T)this.Rent(typeof(T));
        }

        public TObject Rent(Type type)
        {
            
            Stack<TObject> cachedCollection;
            lock (this.poolCache)
            {
                if (!this.poolCache.TryGetValue(type, out cachedCollection))
                {
                    cachedCollection = new Stack<TObject>();
                    this.poolCache.Add(type, cachedCollection);
                }
            }

            

            if (cachedCollection.Count > 0)
            {
                TObject instance = cachedCollection.Pop();
                if (instance != null)
                    return instance;
            }

            // New instances don't need to be prepared for re-use, so we just return it.
            if (this.factory == null)
            {
                return (TObject)Activator.CreateInstance(type);
            }
            else
            {
                return this.factory();
            }
        }

        public void Return(TObject instanceObject)
        {
            Stack<TObject> cachedCollection = null;
            Type type = typeof(TObject);

            lock (this.poolCache)
            {
                if (!this.poolCache.TryGetValue(type, out cachedCollection))
                {
                    cachedCollection = new Stack<TObject>();
                    this.poolCache.Add(type, cachedCollection);
                }

                if (cachedCollection.Count >= this.maxPoolSize)
                {
                    return;
                }

                cachedCollection.Push(instanceObject);
            }
            
        }
    }

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
        protected Socket clientSocket;

        // Flag for connected socket.  
        protected bool connected
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

        private IPEndPoint localEndPoint;

        // Signals a connection.  
        private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);

        BufferManager m_bufferManager;
        //定义接收数据的对象  
        List<byte> m_buffer;
        //发送与接收的MySocketEventArgs变量定义.  
        private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();
        int tagCount = 0;


        private ObjectPool<MySocketEventArgs> receiveEventArgsPool;
        /// <summary>  
        /// 当前连接状态  
        /// </summary>  
        public bool Connected { get {
                return clientSocket != null /*&& clientSocket.Connected*/ && usingConv > 1; } }


        //服务器主动发出数据受理委托及事件  
        public delegate void OnServerDataReceived(CommonPackHead packHead, byte[] receiveBuff);
        public event OnServerDataReceived ServerDataHandler;

        //服务器主动关闭连接委托及事件  
        public delegate void OnServerStop();
        public event OnServerStop ServerStopEvent;


        protected Dictionary<uint, KCP> kcpManager = new Dictionary<uint, KCP>();
        protected uint usingConv = 0;

        // Create an uninitialized client instance.  
        // To start the send/receive processing call the  
        // Connect method followed by SendReceive method.  
        internal SocketManager(String ip, Int32 port)
        {
            // Instantiates the endpoint and socket.  
            hostEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            clientSocket = new Socket(hostEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            Log.Error(" p= " + clientSocket.ReceiveTimeout );

            localEndPoint = new IPEndPoint(IPAddress.Any, 23232);
            clientSocket.Bind(localEndPoint);

            m_bufferManager = new BufferManager(BuffSize * 30, BuffSize);
            m_buffer = new List<byte>();

            m_bufferManager.InitBuffer();
            receiveEventArgsPool = new ObjectPool<MySocketEventArgs>(10, this.ConfigureSocketEventArgs);
        }
        
        private MySocketEventArgs ConfigureSocketEventArgs()
        {
            var eventArg = new MySocketEventArgs();
            eventArg.Completed += new EventHandler<SocketAsyncEventArgs>(this.IO_Completed);
            m_bufferManager.SetBuffer(eventArg);
            eventArg.RemoteEndPoint = localEndPoint;
            eventArg.ArgsTag = 0;

            return eventArg;
        }

        /// <summary>  
        /// 连接到主机  
        /// </summary>  
        /// <returns>0.连接成功, 其他值失败,参考SocketError的值列表</returns>  
        internal virtual SocketError Connect()
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.UserToken = clientSocket;
            connectArgs.RemoteEndPoint = hostEndPoint;
            connectArgs.AcceptSocket = clientSocket;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);
            Log.Error("Csssssssss");

            OnConnect(connectArgs);

            //clientSocket.ConnectAsync(connectArgs);
            //autoConnectEvent.WaitOne(5000, false); //阻塞. 让程序在这里等待,直到连接响应后再返回连接结果  
            Log.Error("23424323424");
            return SocketError.Success;
            //return connectArgs.SocketError;
        }

        protected virtual void SendData(byte[] data, int len)
        {
            if (connected)
            {
                uint new_conv = 0;
                KCP.ikcp_decode32u(data, 0, ref new_conv);
                Log.Error("send_cov=" + new_conv + ",time=" + GetCurrent());

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

                if (!clientSocket.SendToAsync(sendArgs))
                {
                    sendArgs.IsUsing = false;
                    ProcessSend(sendArgs);
                }

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

        private void OnConnect(SocketAsyncEventArgs e)
        {
            // Signals the end of connection.  
            //autoConnectEvent.Set(); //释放阻塞.  
            Log.Error("usingConv=sss" + usingConv);
            if (usingConv <= 0)
            {
                lock (kcpManager)
                {
                    if (!kcpManager.ContainsKey(1))
                    {
                        kcpManager.Add(1, new KCP(1, SendData));
                    }
                    usingConv = 1;
                }
                if (null == kcpUpdateThread)
                {
                    kcpUpdateThread = new Thread(UpdateKcp);
                    kcpUpdateThread.IsBackground = true;
                    kcpUpdateThread.Start();
                }
            }
            Log.Error("usingConv=sss" + usingConv);
            // Set the flag for socket connected.  
            connected = true;
            //如果连接成功,则初始化socketAsyncEventArgs  
            if (connected && (usingConv == 1))
            {
                Log.Error("usingConv=bbbbbbbbbbbb" + usingConv);
                initArgs(e);
                string str = "connect_req";
                Send(Encoding.ASCII.GetBytes(str.ToCharArray()));
            }
            else if (connected)
            {
                initArgs(e);
            }

        }


        // Calback for connect operation  
        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of connection.  
            //autoConnectEvent.Set(); //释放阻塞.  
            Log.Error("usingConv=sss" + usingConv);
            if (usingConv <= 0)
            {
                lock (kcpManager)
                {
                    if (!kcpManager.ContainsKey(1))
                    {
                        kcpManager.Add(1, new KCP(1, SendData));
                    }
                    usingConv = 1;
                }
                if (null == kcpUpdateThread)
                {
                    kcpUpdateThread = new Thread(UpdateKcp);
                    kcpUpdateThread.IsBackground = true;
                    kcpUpdateThread.Start();
                }
            }
            Log.Error("usingConv=sss" + usingConv);
            // Set the flag for socket connected.  
            connected = (e.SocketError == SocketError.Success);
            //如果连接成功,则初始化socketAsyncEventArgs  
            if (connected && (usingConv == 1))
            {
                Log.Error("usingConv=bbbbbbbbbbbb" + usingConv);
                initArgs(e);
                string str = "connect_req";
                Send(Encoding.ASCII.GetBytes(str.ToCharArray()));
            }
            else if(connected)
            {
                initArgs(e);
            }
            
        }

        protected long GetCurrent()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
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

                    List<uint> pp = new List<uint>(kcpManager.Keys);

                    for(var index = 0; index < pp.Count; index ++)
                    //foreach (KeyValuePair<uint, KCP> kv in kcpManager)
                    {
                        KeyValuePair<uint, KCP> kv = new KeyValuePair<uint, KCP>(pp[index], kcpManager[pp[index]]);
                        kv.Value.Update((uint)start);

                        int len = kv.Value.Recv(kcpDataCache);

                        if (len > 0)
                        {
                            byte[] data = new byte[len];
                            Array.Copy(kcpDataCache, data, len);
                            lock (m_buffer)
                            {
                                m_buffer.AddRange(data);
                            }

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

                                    if (packHead.msg_len <= m_buffer.Count)
                                    {
                                        //包够长时,则提取出来,交给后面的程序去处理  
                                        byte[] recv = m_buffer.GetRange(0, (int)packHead.msg_len).ToArray();

                                        lock (m_buffer)
                                        {
                                            m_buffer.RemoveRange(0, (int)packHead.msg_len);
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
                                            lock (m_buffer)
                                            {
                                                m_buffer.RemoveRange(0, 4);
                                            }
                                        }

                                        usingConv = new_conv;
                                        Log.Error("usingConv=" + new_conv);
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
            //发送参数  
            initSendArgs();

            MySocketEventArgs readArgs = this.receiveEventArgsPool.Rent<MySocketEventArgs>();
            this.clientSocket.ReceiveFromAsync(readArgs); 
             
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
            sendArg.DisconnectReuseSocket = true;
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
            Log.Error("111111111=" + e.LastOperation);
            // determine which type of operation just completed and call the associated handler  
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
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
                
                Log.Error("EEE=" + e.BytesTransferred + "," + e.SocketError + "," + e.Count);

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    MySocketEventArgs eventArgs = this.receiveEventArgsPool.Rent<MySocketEventArgs>();
                    this.clientSocket.ReceiveFromAsync(eventArgs);

                    //读取数据  
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);

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

                    this.receiveEventArgsPool.Return((MySocketEventArgs)e);
                    /*
                    if (!token.ReceiveFromAsync(e))
                        this.ProcessReceive(e);
                    */
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
            /*
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
            */
            connected = false;
            //这里一定要记得把事件移走,如果不移走,当断开服务器后再次连接上,会造成多次事件触发.  
            foreach (MySocketEventArgs arg in listArgs)
                arg.Completed -= IO_Completed;

            listArgs.Clear();

            //receiveEventArgs.Completed -= IO_Completed;

            if (ServerStopEvent != null)
                ServerStopEvent();
        }

        // Exchange a message with the host.  
        internal virtual int Send(byte[] sendBuffer)
        {
            if (connected)
            {
                lock (kcpManager)
                {
                    int ret = kcpManager[usingConv].Send(sendBuffer);
                    return ret;
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
            if(null != kcpUpdateThread)
            {
                kcpUpdateThread.Abort();
                kcpUpdateThread = null;
            }
            //autoConnectEvent.Close();
            if (clientSocket.Connected)
            {
                clientSocket.Close();
            }
        }

        #endregion
    }
}