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
    class READ_SOURCE
    {

        public List<string> PARSEDIR(List<string> list_prntask)
        {
            List<string> list_output = new List<string>();
            List<string> list_output_final = new List<string>();
            string szSubmissionpath = Properties.Settings.Default.SUB_PATH;
            foreach (string szLinePRN in list_prntask)
            {
                string[] szLinePRNSplit = szLinePRN.Split(',');
                string szXMLPath = string.Format("{0}\\{1}-TMTD-{2}.XML",szSubmissionpath,szLinePRNSplit[0],szLinePRNSplit[2]);
                if(!File.Exists(szXMLPath))
                    szXMLPath = string.Format("{0}\\{1}-TMTD-{2}.xml", szSubmissionpath, szLinePRNSplit[0], szLinePRNSplit[2]);
                string szOutputPolicy = GETPOLICY(szXMLPath, szLinePRNSplit[1]);
                string szAttribPolicy = GETATTRIB(szXMLPath, szLinePRNSplit[1]);
                list_output.Add(szAttribPolicy);
                list_output.Add(szOutputPolicy);
            }
            return list_output;
        }

        public List<string> LIST_MARKER()
        {
            List<string> ret_list_section_marker = new List<string>();
            foreach (string szLineInMarkers in File.ReadAllLines(Properties.Settings.Default.MARKERS))
            {
                ret_list_section_marker.Add(szLineInMarkers);
            }
            return ret_list_section_marker;
        }

        public string GETPOLICY(string f_szSubPath, string szPolicyId)
        {
            List<string> list_policy = new List<string>();
            bool COPY_POLICY = false;
            foreach (string szSubLine in File.ReadAllLines(f_szSubPath))
            {
                string szSubLineTemp = szSubLine.Trim();
                string szPolId = string.Empty;
                if (szSubLineTemp.StartsWith("<Policy id="))
                {
                    szPolId = szSubLineTemp.Substring(0, szSubLineTemp.IndexOf("\" "));
                    szPolId = szPolId.Substring(szPolId.IndexOf("=\"") + 2);
                    if (szPolId.Equals(szPolicyId))
                        COPY_POLICY = true;
                }
                if (COPY_POLICY)
                    list_policy.Add(szSubLine);
                if (szSubLineTemp.Equals("</Policy>"))
                    COPY_POLICY = false;
            }
            string szOutputGetPol = string.Join("\r\n", list_policy.ToArray());
            return szOutputGetPol;
        }

        public static string GETATTRIB(string f_szSubPath, string szPolicyId)
        {
            string szOutGetAttrib = string.Empty;
            string szPrevLine = string.Empty;
            READ_SOURCE READS = new READ_SOURCE();
            List<string> list_section_markers = READS.LIST_MARKER();
            foreach (string szLineAt in File.ReadAllLines(f_szSubPath))
            {
                string szLineAtTemp = szLineAt.Trim();
                if (szLineAtTemp.StartsWith("<Policy id="))
                {
                    string szPolId = string.Empty;
                    szPolId = szLineAtTemp.Substring(0, szLineAtTemp.IndexOf("\" "));
                    szPolId = szPolId.Substring(szPolId.IndexOf("=\"") + 2);
                    if (szPolId.Equals(szPolicyId))
                    {
                        if (szPrevLine.Equals("<Both>"))
                        {
                            szOutGetAttrib = GETFORMAT_BOTH(szLineAtTemp, list_section_markers, "Both");
                        }
                        else if(szPrevLine.Equals("<Platform32bit>"))
                        {
                            szOutGetAttrib = GETFORMAT_BOTH(szLineAtTemp, list_section_markers, "32bit");
                        }
                        else if (szPrevLine.Equals("<Platform64bit>"))
                        {
                            szOutGetAttrib = GETFORMAT_BOTH(szLineAtTemp, list_section_markers, "64bit");
                        }
                    }
                }
                if(!szLineAtTemp.Equals(string.Empty))
                    szPrevLine = szLineAtTemp;
            }
            return szOutGetAttrib;
        }

        public static int GET_MARKER_NUM(string szSearchSTR, List<string> list_section_markers)
        {
            int iMarkerNum = 0;
            for (int iCount = 0; iCount < list_section_markers.Count; iCount++)
            {
                if (list_section_markers[iCount].Equals(szSearchSTR))
                    iMarkerNum = iCount;
            }
            return iMarkerNum;
        }

        public static string GETFORMAT_BOTH(string szPolicyLine, List<string> list_section_markers, string szBitInfo)
        {
            string szRetFormat = string.Empty;
            string [] szSplitPolLine = szPolicyLine.Split(' ');
            string szPolicyId = szSplitPolLine[1].Substring(szSplitPolLine[1].LastIndexOf("=") + 1);
            szPolicyId = szPolicyId.TrimStart('\"');
            szPolicyId = szPolicyId.TrimEnd('\"');

            #region ---------- if BOTH ----------
            if (szBitInfo.Equals("Both"))
            {
                if (szPolicyId.EndsWith("F") || szPolicyId.EndsWith("S"))
                {
                    szRetFormat = GET_MARKER_NUM("<!-- ##Feedback/Sourcing## -->", list_section_markers).ToString();
                }
                else if (szPolicyId.EndsWith("T"))
                {
                    if (szPolicyLine.Contains("cleanItem"))
                        szRetFormat = GET_MARKER_NUM("<!-- ##Terminate_with_CleanItem## -->", list_section_markers).ToString();
                    else
                        szRetFormat = GET_MARKER_NUM("<!-- ##Terminate## -->", list_section_markers).ToString();
                }
                else if (szPolicyId.EndsWith("Q"))
                {
                    if (szPolicyLine.Contains("queryModule=\"DCE\""))
                        szRetFormat = GET_MARKER_NUM("<!-- ##DCE-QUERY## -->", list_section_markers).ToString();
                    else if (szPolicyLine.Contains("queryModule=\"Census\""))
                    {
                        if (szPolicyLine.Contains("suggestionAction=\"Terminate\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##CENSUS-QUERY(suggestionAction:TERMINATE)## -->", list_section_markers).ToString();
                        else if (szPolicyLine.Contains("suggestionAction=\"Clean\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##CENSUS-QUERY(suggestionAction:CLEAN)## -->", list_section_markers).ToString();
                        else if (szPolicyLine.Contains("suggestionAction=\"Deny\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##CENSUS-QUERY(suggestionAction:DENY)## -->", list_section_markers).ToString();
                    }
                }
                else if (szPolicyId.EndsWith("D"))
                {
                    szRetFormat = GET_MARKER_NUM("<!-- ##DENY## -->", list_section_markers).ToString();
                }
            }
            #endregion

            #region ---------- if 32bit ----------
            else if (szBitInfo.Equals("32bit"))
            {
                if (szPolicyId.EndsWith("F") || szPolicyId.EndsWith("S"))
                {
                    szRetFormat = GET_MARKER_NUM("<!-- ##Feedback/Sourcing32## -->", list_section_markers).ToString();
                }
                else if (szPolicyId.EndsWith("T"))
                {
                    if (szPolicyLine.Contains("cleanItem"))
                        szRetFormat = GET_MARKER_NUM("<!-- ##Terminate_32bit_with_Clean## -->", list_section_markers).ToString();
                    else
                        szRetFormat = GET_MARKER_NUM("<!-- ##Terminate_32bit## -->", list_section_markers).ToString();
                }
                else if (szPolicyId.EndsWith("Q"))
                {
                    if (szPolicyLine.Contains("queryModule=\"DCE\""))
                        szRetFormat = GET_MARKER_NUM("<!-- ##Query_32bit_DCE-QUERY## -->", list_section_markers).ToString();
                    else if (szPolicyLine.Contains("queryModule=\"Census\""))
                    {
                        if (szPolicyLine.Contains("suggestionAction=\"Terminate\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##Query_32bit_CENSUS-QUERY(suggestionAction:TERMINATE)## -->", list_section_markers).ToString();
                        else if (szPolicyLine.Contains("suggestionAction=\"Clean\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##Query_32bit_CENSUS-QUERY(suggestionAction:CLEAN)## -->", list_section_markers).ToString();
                        else if (szPolicyLine.Contains("suggestionAction=\"Deny\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##Query_32bit_CENSUS-QUERY(suggestionAction:DENY)## -->", list_section_markers).ToString();
                    }
                }
                else if (szPolicyId.EndsWith("D"))
                {
                    szRetFormat = GET_MARKER_NUM("<!-- ##32bit_DENY## -->", list_section_markers).ToString();
                }
            }
            #endregion

            #region ---------- if 64bit ----------
            else if (szBitInfo.Equals("64bit"))
            {
                if (szPolicyId.EndsWith("F") || szPolicyId.EndsWith("S"))
                {
                    szRetFormat = GET_MARKER_NUM("<!-- ##Feedback/Sourcing64## -->", list_section_markers).ToString();
                }
                else if (szPolicyId.EndsWith("T"))
                {
                    if (szPolicyLine.Contains("cleanItem"))
                        szRetFormat = GET_MARKER_NUM("<!-- ##Terminate_64bit_with_Clean## -->", list_section_markers).ToString();
                    else
                        szRetFormat = GET_MARKER_NUM("<!-- ##Terminate_64bit## -->", list_section_markers).ToString();
                }
                else if (szPolicyId.EndsWith("Q"))
                {
                    if (szPolicyLine.Contains("queryModule=\"DCE\""))
                        szRetFormat = GET_MARKER_NUM("<!-- ##Query_64bit_DCE-QUERY## -->", list_section_markers).ToString();
                    else if (szPolicyLine.Contains("queryModule=\"Census\""))
                    {
                        if (szPolicyLine.Contains("suggestionAction=\"Terminate\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##Query_64bit_CENSUS-QUERY(suggestionAction:TERMINATE)## -->", list_section_markers).ToString();
                        else if (szPolicyLine.Contains("suggestionAction=\"Clean\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##Query_64bit_CENSUS-QUERY(suggestionAction:CLEAN)## -->", list_section_markers).ToString();
                        else if (szPolicyLine.Contains("suggestionAction=\"Deny\""))
                            szRetFormat = GET_MARKER_NUM("<!-- ##Query_64bit_CENSUS-QUERY(suggestionAction:DENY)## -->", list_section_markers).ToString();
                    }
                }
                else if (szPolicyId.EndsWith("D"))
                {
                    szRetFormat = GET_MARKER_NUM("<!-- ##64bit_DENY## -->", list_section_markers).ToString();
                }
            }
            #endregion

            return szRetFormat;
        }

        public string GET_POLICY_POSITION(string szPolicyPRN, List<string> pattern_list)
        {
            string szReturnPosition = string.Empty;

            for (int iCount = 0; iCount < pattern_list.Count; iCount++)
            {
                string szPolicyLine = pattern_list[iCount].Trim();
                if (szPolicyLine.StartsWith("<Policy id="))
                {
                    string[] szPolTemp = szPolicyLine.Split(' ');
                    string szPolicyId = szPolTemp[1].Substring(szPolTemp[1].LastIndexOf("=") + 1);
                    szPolicyId = szPolicyId.TrimStart('\"');
                    szPolicyId = szPolicyId.TrimEnd('\"');
                    if (szPolicyId.Equals(szPolicyPRN))
                    {
                        szReturnPosition = iCount.ToString();
                    }
                }
            }

            return szReturnPosition;
        }

    }
}
 