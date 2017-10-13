using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Excel;
using System.IO;

namespace JUFrame
{
    public class ExcelLoaderMenu
    {

        [MenuItem("Assets/JUFrame/ExcelLoader/Config", false, 102)]
        public static void ConfigExcel()
        {

        }

        [MenuItem("Assets/JUFrame/ExcelLoader/Compile All", false, 101)]
        public static void CompileAllExcel()
        {

        }

        // 这里的scriptobject的文件名，必须用asset后缀
        [MenuItem("Assets/JUFrame/ExcelLoader/Compile", false, 100)]
        public static void CreateExcelScriptObject()
        {
            // 检查选中的是资源否
            if (Selection.assetGUIDs.Length > 0)
            {
                string savePath = EditorUtility.OpenFolderPanel("Select Save Folder", Application.dataPath, "");

                
                for (int i = 0; i < Selection.assetGUIDs.Length; i++)
                {
                    ConvertExcelToObject(Selection.assetGUIDs[i], savePath);

                }
                AssetDatabase.Refresh();

            }
        }

        protected static IExcelDataReader CreateReader(Stream fileStream, string path)
        {
            var exten = Path.GetExtension(path);
            if(exten.Equals(".xls"))
            {
                return ExcelReaderFactory.CreateBinaryReader(fileStream);
            }
            else if (exten.Equals(".xlsx"))
            {
                return ExcelReaderFactory.CreateOpenXmlReader(fileStream);
            }
            else
            {
                return null;
            }
        }


        protected static void ConvertExcelToObject(string assetGUID, string savePath)
        {
            string filePath = Application.dataPath + "/../" + AssetDatabase.GUIDToAssetPath(assetGUID);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {

                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                using (var reader = CreateReader(stream, filePath))
                {
                    Debug.Assert(reader != null, "File(" + filePath + ") can't be read");

                    var excelData = reader.AsDataSet();
                    reader.IsFirstRowAsColumnNames = true;
                    Debug.Assert(excelData.Tables.Count > 0, "Excel must have 1 sheet!!!");

                    var ExcelObject = ScriptableObject.CreateInstance<ExcelScriptObject>();
                    ExcelObject.excelName = Path.GetFileNameWithoutExtension(filePath);
                    ExcelObject.excelPath = AssetDatabase.GUIDToAssetPath(assetGUID);

                    ExcelObject.SheetNumber = excelData.Tables.Count;

                    ExcelObject.SheetNames = new string[ExcelObject.SheetNumber];

                    ExcelObject.Rows = new int[ExcelObject.SheetNumber];
                    ExcelObject.Cols = new int[ExcelObject.SheetNumber];


                    List<ExcelTable> tmpID = new List<ExcelTable>(ExcelObject.SheetNumber);
                    List<ExcelTable> tmpTable = new List<ExcelTable>(ExcelObject.SheetNumber);

                    //ExcelObject.ID = new List<ExcelTable>(ExcelObject.SheetNumber).ToArray();
                    //ExcelObject.Table = new List<ExcelTable>(ExcelObject.SheetNumber).ToArray();

                    for (int tableIndex = 0; tableIndex < ExcelObject.SheetNumber; tableIndex ++)
                    {
                        Debug.Assert(excelData.Tables[tableIndex].Rows.Count > 3, "Excel must have more than 2 rows!!!");


                        ExcelObject.SheetNames[tableIndex] = excelData.Tables[tableIndex].TableName;

                        ExcelObject.Rows[tableIndex] = excelData.Tables[tableIndex].Rows.Count - 2;
                        ExcelObject.Cols[tableIndex] = excelData.Tables[tableIndex].Columns.Count;

                        tmpID.Add(new ExcelTable());
                        tmpTable.Add(new ExcelTable());


                        List<ExcelColums> tmpIDRow = new List<ExcelColums>(2);
                        tmpIDRow.Add(new ExcelColums());
                        tmpIDRow.Add(new ExcelColums());

                        tmpIDRow[0].Cols = new string[ExcelObject.Cols[tableIndex]];
                        tmpIDRow[1].Cols = new string[ExcelObject.Cols[tableIndex]];
                        tmpID[tableIndex].Rows = tmpIDRow.ToArray();

                        List<ExcelColums> tmpTableRow = new List<ExcelColums>(2);
                        for (int i = 0; i < ExcelObject.Rows[tableIndex]; i++)
                        {
                            tmpTableRow.Add(new ExcelColums());
                            tmpTableRow[i].Cols = new string[ExcelObject.Cols[tableIndex]];
                        }
                        tmpTable[tableIndex].Rows = tmpTableRow.ToArray();

                        for (int i = 0; i < ExcelObject.Cols[tableIndex]; i++)
                        {
                            
                            tmpID[tableIndex].Rows[0].Cols[i] = excelData.Tables[tableIndex].Rows[0][i].ToString();
                            tmpID[tableIndex].Rows[1].Cols[i] = excelData.Tables[tableIndex].Rows[1][i].ToString();
                            Debug.LogError("1." + excelData.Tables[tableIndex].Rows[0][i].ToString() + "," +  excelData.Tables[tableIndex].Rows[1][i].ToString());
                            Debug.LogError("1." + excelData.Tables[tableIndex].Rows[0][i].ToString().Length + "," + excelData.Tables[tableIndex].Rows[1][i].ToString().Length);
                            for (int j = 0; j < ExcelObject.Rows[tableIndex]; j++)
                            {
                                tmpTable[tableIndex].Rows[j].Cols[i] = excelData.Tables[tableIndex].Rows[2 + j][i].ToString();
                            }

                        }
                    }

                    ExcelObject.ID = tmpID.ToArray();
                    ExcelObject.Table = tmpTable.ToArray();

                    string tmpFileName = "Temp" + ExcelObject.excelName + ".asset";
                    AssetDatabase.CreateAsset(ExcelObject, "Assets/" + tmpFileName);

                    string targetPath = savePath + "/" + ExcelObject.excelName + ".asset";
                    if(File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                    File.Move(Application.dataPath + "/" + tmpFileName, targetPath);

                    AssetDatabase.SaveAssets();
                }
            }
        }
    }

}

