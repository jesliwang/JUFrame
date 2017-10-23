using UnityEngine;
using System.Collections;

namespace JUFrame
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {

        private static T mInstance = null;

        public static bool IsValid
        {
            get
            {
                return null != mInstance;
            }
        }

        public static T Instance
        {
            get
            {
                if (null == mInstance)
                {
                    mInstance = GameObject.FindObjectOfType(typeof(T)) as T;
                    if (null == mInstance)
                    {
                        mInstance = new GameObject(typeof(T).ToString(), typeof(T)).GetComponent<T>();
                        mInstance.Init();
                    }
                }
                return mInstance;
            }
        }

        // Use this for initialization
        void Awake()
        {
            if (null == mInstance)
            {
                DontDestroyOnLoad(gameObject);
                mInstance = this as T;
                mInstance.Init();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化单例信息
        /// </summary>
        protected virtual void Init()
        {
        }

        protected virtual void UnInit()
        {
        }

        void OnApplicationQuit()
        {
            if (null != mInstance)
            {
                mInstance.UnInit();
            }
            mInstance = null;
        }
    }

}