using UnityEngine;
using UnityEditor;
using System.Collections;

namespace JUFrame
{

    [CustomEditor(typeof(ExcelScriptObject))]
    public class ExcelLoaderInspector : Editor
    {
        protected int index;


        protected ExcelScriptObject Excel
        {
            get
            {
                return (ExcelScriptObject)target;
            }
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Excel"))
            {
                Application.OpenURL(Application.dataPath + "/../" + Excel.excelPath);
            }
            EditorGUILayout.LabelField("File Name", Excel.excelName);

            index = EditorGUILayout.Popup("SheetName", index, Excel.SheetNames);

            EditorGUILayout.BeginVertical();
            for (int i = 0; i < Excel.Rows[index]; i++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < Excel.Cols[index]; j++)
                {
                    EditorGUILayout.LabelField(Excel.Table[index][i, j]);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

    }

}
