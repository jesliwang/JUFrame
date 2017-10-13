using UnityEngine;
using System.Collections;
using System;

namespace JUFrame
{
    [Serializable]
    public class ExcelColums
    {
        public string[] Cols;
    }

    [Serializable]
    public class ExcelTable
    {
        public ExcelColums[] Rows;
    }

    [Serializable]
    public class ExcelScriptObject : ScriptableObject
    {

        public string excelName;
        public string excelPath;

        public int SheetNumber;
        public string[] SheetNames;

        public int[] Rows;
        public int[] Cols;
        public ExcelTable[] ID;
        public ExcelTable[] Table;
    }

}

