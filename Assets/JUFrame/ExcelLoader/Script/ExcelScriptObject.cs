using UnityEngine;
using System.Collections;
using System;

namespace JUFrame
{

    public class ExcelScriptObject : ScriptableObject
    {

        public string excelName;
        public string excelPath;

        public int SheetNumber;
        public string[] SheetNames;

        public int[] Rows;
        public int[] Cols;
        public string[][,] ID;
        public string[][,] Table;
    }

}

