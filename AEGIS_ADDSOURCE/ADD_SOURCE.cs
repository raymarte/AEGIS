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
    class ADD_SOURCE
    {

        public void ADD_POLICY(List<string> list_nv, ref List<string> list_pattern)
        {
            READ_SOURCE READS = new READ_SOURCE();
            List<string> list_markers = READS.LIST_MARKER();

            for (int iCount = 0; iCount < list_nv.Count; iCount += 2)
            {
                string szFlag = list_markers[int.Parse(list_nv[iCount])];
                string szPattern = list_nv[iCount + 1];
                ADDPATTERN(szFlag, szPattern, ref list_pattern);
            }
        }

        public void ADDPATTERN(string szFlag, string szPattern, ref List<string> list_pattern)
        {
            for (int iCount = 0; iCount < list_pattern.Count; iCount++)
            {
                if (list_pattern[iCount].Trim().Equals(szFlag))
                {
                    list_pattern.Insert(iCount + 1, szPattern);
                }
            }
        }

        public void MODPATTERN(string szPosition, string szModPolicy, ref List<string> list_pattern)
        {
            int iPosition = int.Parse(szPosition);
            for (int iCount = 0; iCount < list_pattern.Count; iCount++)
            {
                if (iPosition.Equals(iCount))
                {
                    list_pattern.RemoveAt(iCount);
                    list_pattern.Insert(iCount, szModPolicy);
                }
            }
        }

    }
}
