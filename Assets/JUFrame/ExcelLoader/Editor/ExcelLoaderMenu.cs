using UnityEditor;
using UnityEngine;
using System.Collections;
using Excel;
using System.IO;

namespace JUFrame
{
    public class ExcelLoaderMenu
    {

        // 这里的scriptobject的文件名，必须用asset后缀
        [MenuItem("Assets/JUFrame/ExcelLoader/CompileExcel")]
        public static void CreateExcelScriptObject()
        {
            // 检查选中的是资源否
            if (Selection.assetGUIDs.Length > 0)
            {
                for (int i = 0; i < Selection.assetGUIDs.Length; i++)
                {
                    ConvertExcelToObject(Selection.assetGUIDs[i]);

                }
                AssetDatabase.SaveAssets();

            }
        }



        protected static void ConvertExcelToObject(string assetGUID)
        {
            string filePath = Application.dataPath + "/../" + AssetDatabase.GUIDToAssetPath(assetGUID);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {

                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                {
                    var excelData = reader.AsDataSet();
                    Debug.Assert(excelData.Tables.Count > 0, "Excel must have 1 sheet!!!");

                    var ExcelObject = ScriptableObject.CreateInstance<ExcelScriptObject>();
                    ExcelObject.excelName = Path.GetFileNameWithoutExtension(filePath);
                    ExcelObject.excelPath = AssetDatabase.GUIDToAssetPath(assetGUID);

                    ExcelObject.SheetNumber = excelData.Tables.Count;

                    ExcelObject.SheetNames = new string[ExcelObject.SheetNumber];

                    ExcelObject.Rows = new int[ExcelObject.SheetNumber];
                    ExcelObject.Cols = new int[ExcelObject.SheetNumber];

                    ExcelObject.ID = new string[ExcelObject.SheetNumber][,];
                    ExcelObject.Table = new string[ExcelObject.SheetNumber][,];

                    for (int tableIndex = 0; tableIndex < ExcelObject.SheetNumber; tableIndex ++)
                    {
                        Debug.Assert(excelData.Tables[tableIndex].Rows.Count > 3, "Excel must have more than 2 rows!!!");


                        ExcelObject.SheetNames[tableIndex] = excelData.Tables[tableIndex].TableName;

                        ExcelObject.Rows[tableIndex] = excelData.Tables[tableIndex].Rows.Count - 2;
                        ExcelObject.Cols[tableIndex] = excelData.Tables[tableIndex].Columns.Count;

                        ExcelObject.ID[tableIndex] = new string[2, ExcelObject.Cols[tableIndex]];
                        ExcelObject.Table[tableIndex] = new string[ExcelObject.Rows[tableIndex], ExcelObject.Cols[tableIndex]];

                        for (int i = 0; i < ExcelObject.Cols[tableIndex]; i++)
                        {
                            ExcelObject.ID[tableIndex][0, i] = excelData.Tables[tableIndex].Rows[0][i].ToString();
                            ExcelObject.ID[tableIndex][0, i] = excelData.Tables[tableIndex].Rows[1][i].ToString();

                            for (int j = 0; j < ExcelObject.Rows[tableIndex]; j++)
                            {
                                ExcelObject.Table[tableIndex][j, i] = excelData.Tables[tableIndex].Rows[2 + j][i].ToString();
                            }

                        }
                    }


                    AssetDatabase.CreateAsset(ExcelObject, "Assets/test.asset");

                }
            }
        }
    }

}

