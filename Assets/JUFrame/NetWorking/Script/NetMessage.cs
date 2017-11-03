using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace JUFrame
{
    public class NetMessage<T> where T : InfibrProtoBuf.IExtensible
    {
        protected CommonPackHead MessageHead;

        protected byte[] appendData = new byte[0];

        public void AddAppendData(byte[] data, long len)
        {
            appendData = new byte[len];
            Array.Copy(data, appendData, len);
        }

        public T Data {
            get;protected set;
        }

        public int MsgID
        {
            get; protected set;
        }

        public long UID
        {
            get; protected set;
        }

        public NetMessage(int _msgID, long uid, T _data)
        {
            MessageHead = new CommonPackHead();
            MsgID = _msgID;
            UID = uid;
            Data = InfibrProtoBuf.Serializer.DeepClone<T>(_data);
        }

        public NetMessage(byte[] rawData)
        {

            int headLength = Marshal.SizeOf(typeof(CommonPackHead));
            //分配结构体内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(headLength);
            //将byte数组拷贝到分配好的内存空间
            Marshal.Copy(rawData, 0, structPtr, headLength);
            //将内存空间转换为目标结构体
            MessageHead = (CommonPackHead)Marshal.PtrToStructure(structPtr, typeof(CommonPackHead));
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);


            ///
            MsgID = MessageHead.msg_id;
            UID = (long)MessageHead.uid;

            byte[] tmpAppend = new byte[MessageHead.option_len];
            Array.Copy(rawData, headLength, tmpAppend, 0, MessageHead.option_len);
            AddAppendData(tmpAppend, MessageHead.option_len);


            byte[] encryData = new byte[(int)MessageHead.msg_len - headLength - MessageHead.option_len];
            Array.Copy(rawData, headLength + MessageHead.option_len, encryData, 0, encryData.Length);
            encryData = RC4.Decrypt(Encoding.ASCII.GetBytes(encryKey.ToCharArray()), encryData);
            encryData = Snappy.Sharp.Snappy.Uncompress(encryData);


            Data = InfibrProtoBuf.Serializer.Deserialize<T>(new MemoryStream(encryData, 0, encryData.Length));
        }

        protected string encryKey = "123456";

        public byte[] Serialize()
        {
            MessageHead.msg_id = (UInt16) MsgID;

            MessageHead.uid = (UInt64)UID;
            MessageHead.option_ver = 0;

            byte[] sendData = SerializeData();
            sendData = Snappy.Sharp.Snappy.Compress(sendData); // snappy 压缩pb
            // 加密
            sendData = RC4.Encrypt(Encoding.ASCII.GetBytes(encryKey.ToCharArray()), sendData);

            /*
            //////////////////////////////////////
            byte[] testData = new byte[sendData.Length];
            Array.Copy(sendData, 0, testData, 0, sendData.Length);
            testData = RC4.Decrypt(Encoding.ASCII.GetBytes(encryKey.ToCharArray()), testData);
            testData = Snappy.Sharp.Snappy.Uncompress(testData);
            var test = InfibrProtoBuf.Serializer.Deserialize<Battle.GetBattleDataRequest>(new MemoryStream(testData, 0, testData.Length));
            Log.Error("dddddd=" + Encoding.UTF8.GetString(test.room_id));

            /////////////////////////////////////////
            */

            MessageHead.msg_len = (uint)sendData.Length;

            int headSize = Marshal.SizeOf(typeof(CommonPackHead));
           
            int optLen = appendData.Length;
            MessageHead.option_len = (byte)optLen;
            MessageHead.msg_len = MessageHead.msg_len + (uint)headSize + MessageHead.option_len;

            byte[] bufferArray = new byte[MessageHead.msg_len];

            IntPtr buffPtr = Marshal.AllocHGlobal(headSize);
            Marshal.StructureToPtr(MessageHead, buffPtr, false);

            Marshal.Copy(buffPtr, bufferArray, 0, headSize);
            Marshal.FreeHGlobal(buffPtr);

            Array.Copy(appendData, 0, bufferArray, headSize, MessageHead.option_len);

            Array.Copy(sendData, 0, bufferArray, headSize + MessageHead.option_len, sendData.Length);

            //var hex = BitConverter.ToString(bufferArray, 0).ToLower();


            return bufferArray;
        }

        protected byte[] SerializeData()
        {
            
            MemoryStream memStream = new MemoryStream();
            InfibrProtoBuf.Serializer.Serialize<T>(memStream, Data);
            byte[] x = memStream.ToArray();
            memStream.Close();
            return x;
        }



    }

}

