﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Sequence;

namespace InformedProteomics.Backend.Utils
{
    /// <summary>
    /// Simple string processing functions
    /// </summary>
    public class SimpleStringProcessing
    {
        /// <summary>
        /// Random shuffle a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Shuffle(string str)
        {
            var indices = Enumerable.Range(0, str.Length).OrderBy(r => Random.Next()).ToArray();
            var sflStr = new StringBuilder(str.Length);
            foreach (var index in indices) sflStr.Append(str[index]);
            return sflStr.ToString();
        }

        /// <summary>
        /// Perform a set number of random mutations on a string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="numMutations"></param>
        /// <returns></returns>
        public static string Mutate(string str, int numMutations)
        {
            var length = str.Length;
            var selectedIndexSet = new HashSet<int>();
            while(selectedIndexSet.Count < numMutations)
            {
                selectedIndexSet.Add(Random.Next(length));
            }
            var mutated = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                if (!selectedIndexSet.Contains(i))
                {
                    mutated.Append(str[i]);
                }
                else
                {
                    var mutatedResidue = str[i];
                    while (mutatedResidue == str[i])
                    {
                        mutatedResidue = AminoAcid.StandardAminoAcidCharacters[Random.Next(AminoAcid.StandardAminoAcidCharacters.Length)];
                    }
                    mutated.Append(mutatedResidue);
                }
            }
            return mutated.ToString();
        }

        /// <summary>
        /// Get the string between 2 periods, so A.BCDEFGHI.J returns BCDEFGHI
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetStringBetweenDots(string str)
        {
            //if (!Regex.IsMatch(str, @"^[A-Z" + FastaDatabase.Delimiter + @"]\.[A-Z]+\.[A-Z" + FastaDatabase.Delimiter + @"]$")) return null;
            var firstDotIndex = str.IndexOf('.');
            var lastDotIndex = str.LastIndexOf('.');
            if (firstDotIndex >= lastDotIndex) return null;
            return str.Substring(firstDotIndex + 1, lastDotIndex - firstDotIndex - 1);
        }
        private static readonly Random Random = new Random();
    }
}
