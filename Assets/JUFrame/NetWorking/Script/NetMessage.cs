using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JUFrame
{
    public class NetMessage<T> where T : MuffinProtoBuf.IExtensible
    {
        protected CommonPackHead MessageHead;

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
            Data = MuffinProtoBuf.Serializer.DeepClone<T>(_data);
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


            Data = MuffinProtoBuf.Serializer.Deserialize<T>(new MemoryStream(rawData, headLength, rawData.Length - headLength));
        }

        public byte[] Serialize()
        {
            MessageHead.msg_id = (UInt16) MsgID;
            MessageHead.uid = (UInt64)UID;

            byte[] sendData = SerializeData();

            MessageHead.msg_len = (uint)sendData.Length;

            int headSize = Marshal.SizeOf(typeof(CommonPackHead));

            byte[] bufferArray = new byte[headSize + MessageHead.msg_len];

            IntPtr buffPtr = Marshal.AllocHGlobal(headSize);
            Marshal.StructureToPtr(MessageHead, buffPtr, false);

            Marshal.Copy(buffPtr, bufferArray, 0, headSize);
            Marshal.FreeHGlobal(buffPtr);

            Array.Copy(sendData, 0, bufferArray, headSize, MessageHead.msg_len);


            return bufferArray;
        }

        protected byte[] SerializeData()
        {
            
            MemoryStream memStream = new MemoryStream();
            MuffinProtoBuf.Serializer.Serialize<T>(memStream, Data);
            byte[] x = memStream.ToArray();
            memStream.Close();
            return x;
        }



    }

}

