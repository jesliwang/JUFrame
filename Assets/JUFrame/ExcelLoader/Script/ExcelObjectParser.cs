using UnityEngine;
using System.Collections;

namespace JUFrame{


    public class ExcelObjectParser
    {
        protected ExcelScriptObject Excel;

        public ExcelObjectParser(ExcelScriptObject excel)
        {
            Debug.Assert(null != excel, "excel is null");
            Excel = excel;
        }

        protected void Log(string message)
        {
            Debug.LogError("[ExcelObjectParser] " + message);
        }

        /// <summary>
        /// 配置表名字
        /// </summary>
        /// <returns></returns>
        public string ExcelName()
        {
            return Excel.excelName;
        }

        public string GetSheetName(int sheetIndex)
        {
            Debug.Assert(sheetIndex < Excel.SheetNumber, "Request sheet out of Range Sheet");
            return Excel.SheetNames[sheetIndex];
        }

        public int GetColIndex(int sheetIndex, string key)
        {
            Debug.Assert(sheetIndex < Excel.SheetNumber, "Request sheet out of Range Sheet");

            for(int i = 0; i < Excel.Cols[sheetIndex]; i++)
            {
                
                if (Excel.ID[sheetIndex].Rows[1].Cols[i].Equals(key))
                {
                    return i;
                }
            }

            Log("Cant'find key(" + key + ") in " + ExcelName() + ":" + GetSheetName(sheetIndex));

            return 0;
        }

        public string GetString(int sheetIndex, int rowIndex, int colIndex)
        {
            Debug.Assert(sheetIndex < Excel.SheetNumber, "Request sheet out of Range Sheet");

            Debug.Assert(rowIndex < Excel.Rows[sheetIndex], "Request row out of Range row");

            Debug.Assert(colIndex < Excel.Cols[sheetIndex], "Request row out of Range row");

            return Excel.Table[sheetIndex].Rows[rowIndex].Cols[colIndex];

        }

        public int GetInt(int sheetIndex, int rowIndex, int colIndex)
        {
            string val = GetString(sheetIndex, rowIndex, colIndex);

            int result;
            if(int.TryParse(val, out result))
            {
                return result;
            }
            else
            {
                Log(ExcelName() + ":" + GetSheetName(sheetIndex) + " (" + (rowIndex + 2) + "," + colIndex + ") can't transfer to int.");
                return 0;
            }
        }

        public float GetFloat(int sheetIndex, int rowIndex, int colIndex)
        {
            string val = GetString(sheetIndex, rowIndex, colIndex);

            float result;
            if (float.TryParse(val, out result))
            {
                return result;
            }
            else
            {
                Log(ExcelName() + ":" + GetSheetName(sheetIndex) + " (" + (rowIndex + 2) + "," + colIndex + ") can't transfer to float.");
                return 0;
            }
        }

        public double GetDouble(int sheetIndex, int rowIndex, int colIndex)
        {
            string val = GetString(sheetIndex, rowIndex, colIndex);

            double result;
            if (double.TryParse(val, out result))
            {
                return result;
            }
            else
            {
                Log(ExcelName() + ":" + GetSheetName(sheetIndex) + " (" + (rowIndex + 2) + "," + colIndex + ") can't transfer to double.");
                return 0;
            }
        }
    }

}

