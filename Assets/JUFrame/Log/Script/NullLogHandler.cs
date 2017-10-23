using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUFrame
{
    public class NullLogHandler : ILogHandler
    {
        public void LogException(Exception exception, UnityEngine.Object context)
        {

        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {

        }
    }
}