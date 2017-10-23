using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace JUFrame
{
    public class LuaMenu
    {

        [MenuItem("Assets/Create/Lua Script", false, 80)]
        public static void CreateNewFile()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<LuaScriptAsset>(),
                GetSelectedPathOrFallback() + "/New Lua.lua.txt",
                null,
                "Assets/JUFrame/LuaTools/Template/template.lua.txt");
        }

        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }
    }
}


