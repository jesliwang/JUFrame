using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JUFrame
{
    internal class Service
    {
        public readonly static Dictionary<int, Type> ReceiveServiceMap = new Dictionary<int, Type>()
        {
            { 3001 , typeof(Battle.GetBattleDataResponse) }
        };
        /**

        protected static List<Type> ServiceMapType = new List<Type>();

        protected static bool _isRunning;
        public static bool IsRunning
        {
            get
            {
                if (_isRunning) return true;
                var list = new List<Type>(Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => t != typeof(MuffinProtoBuf.IExtensible))
                .Where(t => typeof(MuffinProtoBuf.IExtensible).IsAssignableFrom(t)));

                ServiceMapType.Clear();
                ServiceMapType.AddRange(list);

                _isRunning = true;

                return true;
            }
        }
        ***/

        public static Type GetServiceType(int msgID)
        {
            if (ReceiveServiceMap.ContainsKey(msgID))
            {
                return ReceiveServiceMap[msgID];
            }
            else
            {
                return null;
            }
        }
    }
}
