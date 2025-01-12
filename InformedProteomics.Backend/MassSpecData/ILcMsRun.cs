﻿using System.Collections.Generic;

namespace InformedProteomics.Backend.MassSpecData
{
    /// <summary>
    /// Interface for LcMsRun data objects
    /// </summary>
    public interface ILcMsRun: IChromatogramExtractor, ISpectrumExtractor
    {
        /// <summary>
        /// Index of first LC scan in the dataset
        /// </summary>
        int MinLcScan { get; }

        /// <summary>
        /// Index of last LC scan in the dataset
        /// </summary>
        int MaxLcScan { get; }

        /// <summary>
        /// The number of spectra in the file.
        /// </summary>
        int NumSpectra { get; }

        /// <summary>
        /// True if the dataset is DIA data
        /// </summary>
        bool IsDia { get; }

        /// <summary>
        /// Get the elution time of the specified scan number
        /// </summary>
        /// <param name="scanNum"></param>
        /// <returns></returns>
        double GetElutionTime(int scanNum);

        /// <summary>
        /// Gets the MS level of the specified scan
        /// </summary>
        /// <param name="scanNum">scan number</param>
        /// <returns>MS level</returns>
        int GetMsLevel(int scanNum);

        /// <summary>
        /// Gets the greatest scan number smaller than ms2ScanNum
        /// </summary>
        /// <param name="scanNum">scan number</param>
        /// <param name="msLevel">MS level</param>
        /// <returns>previous scan number at the specified level</returns>
        int GetPrevScanNum(int scanNum, int msLevel);

        /// <summary>
        /// Gets the smallest scan number larger than ms2ScanNum
        /// </summary>
        /// <param name="scanNum">scan number</param>
        /// <param name="msLevel">MS level</param>
        /// <returns>next scan number at the specified level</returns>
        int GetNextScanNum(int scanNum, int msLevel);

        /// <summary>
        /// Gets the scan numbers of the specified msLevel
        /// </summary>
        /// <param name="msLevel">MS level</param>
        /// <returns>scan numbers of the specified msLevel</returns>
        IList<int> GetScanNumbers(int msLevel);
    }
}
