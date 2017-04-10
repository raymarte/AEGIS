using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Collections;
using System.Diagnostics;
using System.Configuration;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace AEGIS_ADDSOURCE
{
    class PRN_READER
    {
        public List<string> READPRN()
        {
            Console.WriteLine("---> READING PRN FILE");
            string szPRNPath = Properties.Settings.Default.PRN;
            List<string> list_prn = new List<string>();
            int rCnt = 0;
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range range;
            xlWorkBook = xlApp.Workbooks.Open(szPRNPath, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
            range = xlWorkSheet.UsedRange;
            string szDefectType = string.Empty;
            string szPolicyId = string.Empty;
            string szCaseId = string.Empty;
                
            for (rCnt = 2; rCnt <= range.Rows.Count; rCnt++)
            {
                szPolicyId = (string)(range.Cells[rCnt, 2] as Excel.Range).Value2;
                szDefectType = (string)(range.Cells[rCnt, 3] as Excel.Range).Value2;
                szCaseId = (string)(range.Cells[rCnt, 1] as Excel.Range).Value2;
                if (!(szPolicyId == null) || !(szDefectType == null))
                {
                    Console.WriteLine("---> RECODRING TASK " + szDefectType + " to Policy " + szPolicyId);
                    list_prn.Add(szDefectType + "," + szPolicyId + "," + szCaseId);
                }
            }

            xlWorkBook.Close();
            xlApp.Workbooks.Close();
            return list_prn;
        }

        public List<string> GET_DT_SUB(List<string> list_prntask, string szSearchString)
        {
            List<string> list_dt_sub_ret = new List<string>();
            foreach (string szLineGetNV in list_prntask)
            {
                string[] szSplitLine = szLineGetNV.Split(',');
                if (szSplitLine[0].Equals(szSearchString))
                    list_dt_sub_ret.Add(szLineGetNV);
            }
            return list_dt_sub_ret;
        }



    }
}
