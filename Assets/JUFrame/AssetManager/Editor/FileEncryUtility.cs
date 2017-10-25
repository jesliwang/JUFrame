using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace JUFrame
{
    public class FileEncryUtility 
    {
        [MenuItem("Test/Load")]
        public static void TestLoad()
        {
            string tmpPath = "GraphBundle/GraphBundle";
            StringBuilder sb = new StringBuilder();
            sb.Append("TestLoad---Start");
            using (var stream = File.OpenRead(tmpPath))
            {
                List<byte> ans = new List<byte>();
                int k = stream.ReadByte();
                while(-1 != k)
                {
                    sb.AppendFormat("{0},{1}", k, (byte)(((byte)k) ^ 77));
                    ans.Add((byte)(((byte)k) ^ 77));
                    k = stream.ReadByte();
                }
                
                var p = AssetBundle.LoadFromMemory(ans.ToArray());
                if(null != p)
                {
                    var ps = p.GetAllAssetNames();
                    Debug.LogError("3333333333=" + ps.Length);
                }
                else
                {
                    Debug.LogError("222222222222");
                }

            }
            sb.Append("TesLoad---Start");
            Debug.Log(sb.ToString());

            Debug.LogError("111111111");
        }

        [MenuItem("Test/Decry")]
        public static void TestDecry()
        {
            string tmpPath = "GraphBundle/GraphBundle";
            string target = "GraphBundle/GraphBundle.bak.txt";
            if (File.Exists(target))
            {
                File.Delete(target);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("TestDecry---Start");
            using (var stream = File.OpenRead(tmpPath))
            {
                using (var writeStream = File.Open(target, FileMode.OpenOrCreate))
                {
                    int mByte = 0;
                    mByte = stream.ReadByte();
                    while (mByte != -1)
                    {
                        sb.AppendFormat("{0},{1}", mByte, (byte)((byte)mByte ^ 77));
                        writeStream.WriteByte((byte)((byte)mByte ^ 77));
                        mByte = stream.ReadByte();
                    }
                }


            }
            sb.Append("TestDecry---End");
            Debug.Log(sb.ToString());
           
        }

        [MenuItem("Test/Encry")]
        public static void TestEncry()
        {
            string tmpPath = "GraphBundle/GraphBundle.bak";
            string target = "GraphBundle/GraphBundle.test.txt";
            if (File.Exists(target))
            {
                File.Delete(target);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("TestEncry---Start");
            using (var stream = File.OpenRead(tmpPath))
            {
                using (var writeStream = File.Open(target, FileMode.OpenOrCreate))
                {
                    int mByte = 0;
                    mByte = stream.ReadByte();
                    while (mByte != -1)
                    {
                        sb.AppendFormat("{0},{1}\n", mByte, (byte)((byte)mByte ^ 77));
                        writeStream.WriteByte((byte)((byte)mByte ^ 77));
                        mByte = stream.ReadByte();
                    }
                }

            }
            sb.Append("TestEncry---End");
            Debug.Log(sb.ToString());
        }

        public static void EncryFile(string absolutePath)
        {
            string tmpPath = absolutePath + ".bak";
            if (File.Exists(tmpPath))
            {
                File.Delete(tmpPath);
            }
            File.Copy(absolutePath, tmpPath, true);
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(tmpPath))
                {
                    using (var writeStream = File.Open(absolutePath, FileMode.OpenOrCreate))
                    {
                        int mByte = 0;
                        mByte = stream.ReadByte();
                        while(mByte != -1)
                        {
                            writeStream.WriteByte((byte)((byte)mByte ^ 77));
                            mByte = stream.ReadByte();
                        }
                    }
                    
                    
                }
            }
            File.Delete(tmpPath);
        }
    }
}


