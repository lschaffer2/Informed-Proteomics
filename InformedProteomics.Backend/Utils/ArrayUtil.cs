﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Utils
{
    public static class ArrayUtil
    {
        /// <summary>
        /// Create a string to display the array values.
        /// </summary>
        /// <param name="array">The array</param>
        /// <param name="format">Optional. A string to use to format each value. Must contain the colon, so something like ':0.000'</param>
        public static string ToString<T>(T[] array, string format = "")
        {
            var s = new StringBuilder();
            var formatString = "{0" + format + "}";

            for (var i = 0; i < array.Length; i++)
            {
                if (i < array.Length - 1)
                {
                    s.AppendFormat(formatString + ", ", array[i]);
                }
                else
                {
                    s.AppendFormat(formatString, array[i]);
                }
            }

            return s.ToString();
        }
        
        // Kadane's algorithm
        static int MaxSumSubarray(IList<int> a, out int start, out int len)
        {
            start = 0;
            len = 1;
            var sum = a[0];

            var curStart = 0;
            var curLen = 1;
            var curSum = a[0];

            for (var i = 1; i < a.Count; i++)
            {

                if (a[i] >= curSum + a[i])
                {
                    curStart = i;
                    curLen = 1;
                    curSum = a[i];
                }
                else
                {
                    curLen++;
                    curSum += a[i];
                }

                if ((curSum <= sum) && (curSum != sum || curLen >= len) &&
                    (curSum != sum || curLen != len || curStart >= start)) continue;

                start = curStart;
                len = curLen;
                sum = curSum;
            }

            return sum;
        }
        // Kadane's algorithm
        static double MaxSumSubarray(IList<double> a, out int start, out int len)
        {
            start = 0;
            len = 1;
            var sum = a[0];

            var curStart = 0;
            var curLen = 1;
            var curSum = a[0];

            for (var i = 1; i < a.Count; i++)
            {

                if (a[i] >= curSum + a[i])
                {
                    curStart = i;
                    curLen = 1;
                    curSum = a[i];
                }
                else
                {
                    curLen++;
                    curSum += a[i];
                }

                if ((curSum <= sum) && (Math.Abs(curSum - sum) > 1E-10 || curLen >= len) &&
                    (Math.Abs(curSum - sum) > 1E-10 || curLen != len || curStart >= start)) continue;

                start = curStart;
                len = curLen;
                sum = curSum;
            }

            return sum;
        }      

    }
}
