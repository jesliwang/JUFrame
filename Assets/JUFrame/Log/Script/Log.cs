using UnityEngine;
using System.Collections;
using System;
using System.IO;

namespace JUFrame
{

    public class Log
    {
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            string filePath = Application.persistentDataPath + "/log.ini";

            if (File.Exists(filePath))
            {
                UnityEngine.Debug.unityLogger.logHandler = new LocalLogHandler();
            }
            else
            {
                if (!IsLogOpen)
                {
                    UnityEngine.Debug.unityLogger.logHandler = new NullLogHandler();
                }
            }
        }

        private static bool IsLogOpen
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void Debug(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        public static void Warn(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void Assert(bool condition, string message)
        {
            UnityEngine.Debug.Assert(condition, message);
        }
    }

}



