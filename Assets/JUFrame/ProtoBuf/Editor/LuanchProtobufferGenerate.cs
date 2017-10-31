using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using AssetBundles.Manager;

namespace JUFrame
{
    internal class LuanchProtobufferGenerate : ScriptableSingleton<LuanchProtobufferGenerate>
    {
        [SerializeField]
        protected string ProtoFilesPath;

        [SerializeField]
        protected string ExportPath;

        [MenuItem("JUFrame/GeneratePB")]
        public static void Generate()
        {
            string pathToProtogen = "Assets/JUFrame/ProtoBuf/Editor/ProtoGen/protogen.exe";

#if UNITY_2017_1_OR_NEWER
			string monoProfile = "4.5";
#elif UNITY_5_5_OR_NEWER
            string monoProfile = "v4.0.30319";
#else
			string monoProfile = "4.0";
#endif
            pathToProtogen = pathToProtogen.Replace("Assets", Application.dataPath);
            UnityEngine.Debug.LogError(pathToProtogen);
            var args = string.Format("-i:{0} -o:{1}", Application.dataPath.Replace("Assets", "test.proto"), Application.dataPath.Replace("Assets", "test.cs"));
            UnityEngine.Debug.LogError(args);
            ProcessStartInfo startInfo = ExecuteInternalMono.GetProfileStartInfoForMono(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), monoProfile, pathToProtogen, args, true);

            startInfo.WorkingDirectory = "Assets/JUFrame/ProtoBuf/Editor/ProtoGen/".Replace("Assets", Application.dataPath);
            startInfo.UseShellExecute = false;
            
            Process launchProcess = Process.Start(startInfo);
            /*
            if (launchProcess == null || launchProcess.HasExited == true || launchProcess.Id == 0)
            {
                //Unable to start process
                UnityEngine.Debug.LogError("Unable Start AssetBundleServer process");
                instance.m_args = "Not running.";
                instance.m_launchedSetting = null;
            }
            else
            {
                UnityEngine.Debug.LogFormat("Local Server started with arg:{0}", args);
                //We seem to have launched, let's save the PID
                instance.m_ServerPID = launchProcess.Id;
                instance.m_args = args;
                instance.m_launchedSetting = serverSetting;
            }
            */
        }

    }
}


