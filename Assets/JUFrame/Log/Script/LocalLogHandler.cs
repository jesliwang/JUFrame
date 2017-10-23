using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace JUFrame
{
    public class LocalLogHandler : ILogHandler
    {
        protected FileStream m_FileStream;
        protected StreamWriter m_StreamWriter;

        protected ILogHandler m_DefaultLogHandler;

        public LocalLogHandler()
        {
            string filePath = Application.persistentDataPath + "/Logs. " + System.DateTime.UtcNow.ToFileTimeUtc().ToString() + ".txt";

            m_FileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            m_StreamWriter = new StreamWriter(m_FileStream);

            m_DefaultLogHandler = Debug.unityLogger.logHandler;
        }

        ~LocalLogHandler()
        {
            m_StreamWriter.Dispose();
            m_FileStream.Close();
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            m_StreamWriter.WriteLine(exception.ToString());
            m_StreamWriter.Flush();
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            m_StreamWriter.WriteLine(String.Format(format, args));
            m_StreamWriter.Flush();
        }

    }
}
