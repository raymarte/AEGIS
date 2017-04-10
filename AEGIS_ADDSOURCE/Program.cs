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

namespace AEGIS_ADDSOURCE
{
    class Program
    {
        static void Main(string[] args)
        {

            #region ---------- HEADER ----------
            Console.Title = "AEGIS_SOURCE BUILDER_v.1.001";
            Console.WriteLine("*===========================================================================*");
            Console.WriteLine("*========================   AEGIS SOURCE BUILDER   =========================*");
            Console.WriteLine("*=================   By: Solutions and Services Team   =====================*");
            Console.WriteLine("*===============================    BIRDY    ===============================*");
            Console.WriteLine("*===========================================================================*");
            #endregion

            #region ---------- INITIALIZE VARIABLES ----------
            string szCurDir = Directory.GetCurrentDirectory();
            string szBigSourcePath = Properties.Settings.Default.OFFICIAL_XML;
            string szPolId = string.Empty;
            string szPPairName = string.Empty;
            List<string> list_pattern = new List<string>();
            List<string> list_PRNTASK = new List<string>();
            ADD_SOURCE ADDS = new ADD_SOURCE();
            PRN_READER PRN = new PRN_READER();
            #endregion

            #region ---------- PATTERN DICTIONARY ----------
            List<string> list_temp_platform = new List<string>();
            List<string> list_temp = new List<string>();
            Dictionary<string, string> dict_pol = new Dictionary<string, string>();
            bool COPY_POL = false;
            List<string> list_policy = new List<string>();
            bool POL_ON = false;
            foreach (string szLinePol in File.ReadAllLines(szBigSourcePath))
            {
                string szTempLinePol = szLinePol.Trim();
                if (szTempLinePol.StartsWith("<Policy id="))
                {
                    szPolId = szTempLinePol.Substring(0, szTempLinePol.IndexOf("\" "));
                    szPolId = szPolId.Substring(szPolId.IndexOf("=\"") + 2);
                    COPY_POL = true;
                }
                if (COPY_POL)
                    list_temp.Add(szLinePol);
                if (szTempLinePol.Equals("</Policy>") && COPY_POL)
                {
                    COPY_POL = false;
                    string szPolStr = string.Join("\r\n", list_temp.ToArray());
                    dict_pol.Add(szPolId, szPolStr);
                    list_temp.Clear();
                }
            }
            #endregion

            #region ---------- PARSE BIG XML ----------
            Console.WriteLine("---> STARTING TO PARSE OFFICIAL XML FILE --> " + szBigSourcePath);
            foreach (string szLine in File.ReadAllLines(szBigSourcePath))
            {
                string szTempLine = szLine.Trim();
                string szLineCopy = szLine;

                #region ---------- COMBINE POLICY TO SINGLE Line ----------
                if (szTempLine.StartsWith("<Policy id="))
                {
                    szPolId = szTempLine.Substring(0, szTempLine.IndexOf("\" "));
                    szPolId = szPolId.Substring(szPolId.IndexOf("=\"") + 2);
                    POL_ON = true;
                }
                
                if (POL_ON)
                    list_policy.Add(szLine);
                else
                    list_pattern.Add(szLine);
                
                if (szTempLine.Equals("</Policy>"))
                {
                    POL_ON = false;
                    szLineCopy = string.Join("\r\n", list_policy.ToArray());
                    //Console.WriteLine("---> SAVING POLICY " + szPolId);
                    list_pattern.Add(szLineCopy);
                    list_policy.Clear();
                }
                #endregion

            }
            #endregion

            //------------------------------------------------------------------------------------------//
            //------------------------------- PRN READER SECTION ---------------------------------------//
            list_PRNTASK = PRN.READPRN();
            //---------------------------------------------------------------------------------------------------------------//
            //------------------------------- This section will handle NV/DP/MV/DP-RP ---------------------------------------//

            #region ---------- DP SECTION ----------

            foreach (string szLineInDPList in list_PRNTASK)
            {
                if (szLineInDPList.Substring(0, szLineInDPList.IndexOf(",")).Equals("DP"))
                {
                    Console.WriteLine("---> FOUND A DROP POLICY TASK");
                    string[] szPol2DP = szLineInDPList.Split(',');
                    Console.WriteLine("---> DROPPING POLICY --> " + szPol2DP[1]);
                    DROP_POL(szPol2DP[1], ref list_pattern);
                }
            }

            #endregion

            #region ---------- NV SECTION ----------
            READ_SOURCE READS = new READ_SOURCE();
            List<string> list_submission_nv = PRN.GET_DT_SUB(list_PRNTASK, "NV");
            List<string> list_nv = READS.PARSEDIR(list_submission_nv);
            if(!list_nv.Count.Equals(0))
                ADDS.ADD_POLICY(list_nv, ref list_pattern);
            #endregion

            #region ---------- RP SECTION ----------
            List<string> list_submission_rp = PRN.GET_DT_SUB(list_PRNTASK, "RP");
            List<string> list_rp = READS.PARSEDIR(list_submission_rp);
            if (!list_rp.Count.Equals(0))
                ADDS.ADD_POLICY(list_rp, ref list_pattern);
            #endregion

            #region ---------- MV SECTION ----------
            List<string> list_submission_mv = PRN.GET_DT_SUB(list_PRNTASK, "MV");
            string szSubmissionpath = Properties.Settings.Default.SUB_PATH;
            foreach (string szPolicyToMV in list_submission_mv)
            {
                string [] szPolToMVTemp = szPolicyToMV.Split(',');
                string szPolicyPosition = READS.GET_POLICY_POSITION(szPolToMVTemp[1], list_pattern);
                string szXMLPath = string.Format("{0}\\{1}-TMTD-{2}.XML", szSubmissionpath, szPolToMVTemp[0], szPolToMVTemp[2]);
                string szMVPolicy = READS.GETPOLICY(szXMLPath, szPolToMVTemp[1]);
                ADDS.MODPATTERN(szPolicyPosition, szMVPolicy, ref list_pattern);
            }

            #endregion

            //---------------------------------------------------------------------------------------------------------//
            //---------------------------------------------------------------------------------------------------------//

            #region ---------- PRINT NEW XML ----------
            string szLogs = string.Join("\r\n", list_pattern.ToArray()) + "\r\n";
            string szOutputPath = Properties.Settings.Default.OUT_PATH;
            
            StreamWriter sw1 = new StreamWriter(szOutputPath + "\\tmtd.xml");
            sw1.Write(szLogs);
            sw1.Close();

            Console.WriteLine("---> A NEW TMTD XML HAS BEEN CREATED --> " + szOutputPath + "\\tmtd.xml");
            Console.WriteLine("---> PRESS ANY KEY TO CONTINUE");
            Console.ReadKey();
            #endregion

        }
        // end of main func

        //================================================== F U N C T I O N S ==================================================//

        public static void DROP_POL(string szDPPolicy, ref List<string> list_pattern)
        {
            for (int iDPLineCnt = 0; iDPLineCnt < list_pattern.Count; iDPLineCnt++)
            {
                string szLineInList = list_pattern[iDPLineCnt];
                szLineInList = szLineInList.Trim();
                if (szLineInList.StartsWith("<Policy id=\"" + szDPPolicy + "\""))
                {
                    list_pattern.RemoveAt(iDPLineCnt);
                }
            }
        }

    }
}
