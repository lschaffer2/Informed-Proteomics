﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Utils;
using MathNet.Numerics.Statistics;

namespace InformedProteomics.Backend.Data.Spectrometry
{
    /// <summary>
    /// Class to hold information about a single Spectrum
    /// </summary>
    public class Spectrum
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mzArr"></param>
        /// <param name="intensityArr"></param>
        /// <param name="scanNum"></param>
        public Spectrum(IList<double> mzArr, IList<double> intensityArr, int scanNum)
        {
            Peaks = new Peak[mzArr.Count];
            for(var i=0; i<mzArr.Count; i++) Peaks[i] = new Peak(mzArr[i], intensityArr[i]);
            ScanNum = scanNum;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peaks"></param>
        /// <param name="scanNum"></param>
        public Spectrum(ICollection<Peak> peaks, int scanNum)
        {
            Peaks = new Peak[peaks.Count];
            peaks.CopyTo(Peaks, 0);
            ScanNum = scanNum;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scanNum"></param>
        public Spectrum(int scanNum)
        {
            ScanNum = scanNum;
        }

        /// <summary>
        /// Scan Number
        /// </summary>
        public int ScanNum { get; protected set; }

        /// <summary>
        /// Native ID
        /// </summary>
        public string NativeId { get; set; }

        /// <summary>
        /// Total Ion Current
        /// </summary>
        public double TotalIonCurrent { get; set; }

        /// <summary>
        /// MS Level
        /// </summary>
        public int MsLevel
        {
            get { return _msLevel; }
            set { _msLevel = value; }
        }

        /// <summary>
        /// Elution time (minutes)
        /// </summary>
        public double ElutionTime { get; set; }

        /// <summary>
        /// Peaks
        /// </summary>
        /// <remarks>Peaks are assumed to be sorted according to m/z</remarks>
        public Peak[] Peaks { get; protected set; }

        /// <summary>
        /// Finds the maximum intensity peak within the specified range
        /// </summary>
        /// <param name="mz">m/z</param>
        /// <param name="tolerance">tolerance</param>
        /// <returns>maximum intensity peak</returns>
        public Peak FindPeak(double mz, Tolerance tolerance)
        {
            var tolTh = tolerance.GetToleranceAsMz(mz);
            var minMz = mz - tolTh;
            var maxMz = mz + tolTh;

            return FindPeak(minMz, maxMz);
        }

        /// <summary>
        /// Gets a list of peaks within [minMz, maxMz]
        /// </summary>
        /// <param name="minMz">minimum m/z</param>
        /// <param name="maxMz">maximum m/z</param>
        /// <returns>list of peaks within [minMz, maxMz]</returns>
        public List<Peak> GetPeakListWithin(double minMz, double maxMz)
        {
            var peakList = new List<Peak>();

            GetPeakListWithin(minMz, maxMz, ref peakList);

            return peakList;
        }

        /// <summary>
        /// Gets a list of peaks within [minMz, maxMz] and add to peakList
        /// </summary>
        /// <param name="minMz">minimum m/z</param>
        /// <param name="maxMz">maximum m/z</param>
        /// <param name="peakList">list of peaks where the peaks will be added</param>
        /// <returns>list of peaks within [minMz, maxMz]</returns>
        public void GetPeakListWithin(double minMz, double maxMz, ref List<Peak> peakList)
        {
            var index = Array.BinarySearch(Peaks, new Peak(minMz - float.Epsilon, 0));
            if (index < 0) index = ~index;

            // go up
            var i = index;
            while (i < Peaks.Length)
            {
                if (Peaks[i].Mz > maxMz) break;
                peakList.Add(Peaks[i]);
                ++i;
            }
        }

        /// <summary>
        /// Checks whether this spectrum contains all isotope peaks whose relative intensity is equal or larter than the threshold
        /// </summary>
        /// <param name="ion">ion</param>
        /// <param name="tolerance">tolerance</param>
        /// <param name="relativeIntensityThreshold">relative intensity threshold of the theoretical isotope profile</param>
        /// <returns>true if spectrum contains all ions; false otherwise.</returns>
        public bool ContainsIon(Ion ion, Tolerance tolerance, double relativeIntensityThreshold = 0.1)
        {
            var baseIsotopeIndex = ion.Composition.GetMostAbundantIsotopeZeroBasedIndex();
            var isotopomerEnvelope = ion.Composition.GetIsotopomerEnvelopeRelativeIntensities();
            var baseIsotopMz = ion.GetIsotopeMz(baseIsotopeIndex);
            var baseIsotopePeakIndex = FindPeakIndex(baseIsotopMz, tolerance);
            if (baseIsotopePeakIndex < 0) return false;

            // go down
            var peakIndex = baseIsotopePeakIndex;
            for (var isotopeIndex = baseIsotopeIndex - 1; isotopeIndex >= 0; isotopeIndex--)
            {
                if (isotopomerEnvelope[isotopeIndex] < relativeIntensityThreshold) break;
                var isotopeMz = ion.GetIsotopeMz(isotopeIndex);
                var tolTh = tolerance.GetToleranceAsMz(isotopeMz);
                var minMz = isotopeMz - tolTh;
                var maxMz = isotopeMz + tolTh;
                for (var i = peakIndex - 1; i >= 0; i--)
                {
                    var peakMz = Peaks[i].Mz;
                    if (peakMz < minMz) return false;
                    if (peakMz <= maxMz)    // find match, move to prev isotope
                    {
                        peakIndex = i;
                        break;
                    }
                }
            }

            // go up
            peakIndex = baseIsotopePeakIndex;
            for (var isotopeIndex = baseIsotopeIndex + 1; isotopeIndex < isotopomerEnvelope.Length; isotopeIndex++)
            {
                if (isotopomerEnvelope[isotopeIndex] < relativeIntensityThreshold) break;
                var isotopeMz = ion.GetIsotopeMz(isotopeIndex);
                var tolTh = tolerance.GetToleranceAsMz(isotopeMz);
                var minMz = isotopeMz - tolTh;
                var maxMz = isotopeMz + tolTh;
                for (var i = peakIndex + 1; i < Peaks.Length; i++)
                {
                    var peakMz = Peaks[i].Mz;
                    if (peakMz > maxMz) return false;
                    if (peakMz >= minMz)    // find match, move to prev isotope
                    {
                        peakIndex = i;
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Finds all isotope peaks corresponding to theoretical profiles with relative intensity higher than the threshold
        /// </summary>
        /// <param name="ion">ion</param>
        /// <param name="tolerance">tolerance</param>
        /// <param name="relativeIntensityThreshold">relative intensity threshold of the theoretical isotope profile</param>
        /// <returns>array of observed isotope peaks in the spectrum. null if no peak found.</returns>
        public Peak[] GetAllIsotopePeaks(Ion ion, Tolerance tolerance, double relativeIntensityThreshold = 0.1)
        {
            var mostAbundantIsotopeIndex = ion.Composition.GetMostAbundantIsotopeZeroBasedIndex();
            var isotopomerEnvelope = ion.Composition.GetIsotopomerEnvelopeRelativeIntensities();
            var mostAbundantIsotopeMz = ion.GetIsotopeMz(mostAbundantIsotopeIndex);
            var mostAbundantIsotopeMatchedPeakIndex = FindPeakIndex(mostAbundantIsotopeMz, tolerance);
            if (mostAbundantIsotopeMatchedPeakIndex < 0) return null;

            var observedPeaks = new Peak[isotopomerEnvelope.Length];
            observedPeaks[mostAbundantIsotopeIndex] = Peaks[mostAbundantIsotopeMatchedPeakIndex];

            // go down
            var peakIndex = mostAbundantIsotopeMatchedPeakIndex - 1;
            for (var isotopeIndex = mostAbundantIsotopeIndex - 1; isotopeIndex >= 0; isotopeIndex--)
            {
                if (isotopomerEnvelope[isotopeIndex] < relativeIntensityThreshold) break;
                var isotopeMz = ion.GetIsotopeMz(isotopeIndex);
                var tolTh = tolerance.GetToleranceAsMz(isotopeMz);
                var minMz = isotopeMz - tolTh;
                var maxMz = isotopeMz + tolTh;
                for (var i = peakIndex; i >= 0; i--)
                {
                    var peakMz = Peaks[i].Mz;
                    if (peakMz < minMz)
                    {
                        peakIndex = i;
                        break;
                    }
                    if (peakMz <= maxMz)    // find match, move to prev isotope
                    {
                        var peak = Peaks[i];
                        if (observedPeaks[isotopeIndex] == null ||
                            peak.Intensity > observedPeaks[isotopeIndex].Intensity)
                        {
                            observedPeaks[isotopeIndex] = peak;
                        }
                    }
                }
            }

            // go up
            peakIndex = mostAbundantIsotopeMatchedPeakIndex + 1;
            for (var isotopeIndex = mostAbundantIsotopeIndex + 1; isotopeIndex < isotopomerEnvelope.Length; isotopeIndex++)
            {
                if (isotopomerEnvelope[isotopeIndex] < relativeIntensityThreshold) break;
                var isotopeMz = ion.GetIsotopeMz(isotopeIndex);
                var tolTh = tolerance.GetToleranceAsMz(isotopeMz);
                var minMz = isotopeMz - tolTh;
                var maxMz = isotopeMz + tolTh;
                for (var i = peakIndex; i < Peaks.Length; i++)
                {
                    var peakMz = Peaks[i].Mz;
                    if (peakMz > maxMz)
                    {
                        peakIndex = i;
                        break;
                    }
                    if (peakMz >= minMz)    // find match, move to prev isotope
                    {
                        var peak = Peaks[i];
                        if (observedPeaks[isotopeIndex] == null ||
                            peak.Intensity > observedPeaks[isotopeIndex].Intensity)
                        {
                            observedPeaks[isotopeIndex] = peak;
                        }
                    }
                }
            }

            return observedPeaks;
        }

        public Peak[] GetAllIsotopePeaks(double monoIsotopeMass, int charge, IsotopomerEnvelope envelope, Tolerance tolerance, double relativeIntensityThreshold = 0.1)
        {
            var mostAbundantIsotopeIndex = envelope.MostAbundantIsotopeIndex;
            var isotopomerEnvelope = envelope.Envolope;
            var mostAbundantIsotopeMz = Ion.GetIsotopeMz(monoIsotopeMass, charge, mostAbundantIsotopeIndex);
            var mostAbundantIsotopePeakIndex = FindPeakIndex(mostAbundantIsotopeMz, tolerance);
            if (mostAbundantIsotopePeakIndex < 0) return null;

            var observedPeaks = new Peak[isotopomerEnvelope.Length];
            observedPeaks[mostAbundantIsotopeIndex] = Peaks[mostAbundantIsotopePeakIndex];

            // go down
            var peakIndex = mostAbundantIsotopePeakIndex - 1;
            for (var isotopeIndex = mostAbundantIsotopeIndex - 1; isotopeIndex >= 0; isotopeIndex--)
            {
                if (isotopomerEnvelope[isotopeIndex] < relativeIntensityThreshold) break;
                var isotopeMz = Ion.GetIsotopeMz(monoIsotopeMass, charge, isotopeIndex);
                var tolTh = tolerance.GetToleranceAsMz(isotopeMz);
                var minMz = isotopeMz - tolTh;
                var maxMz = isotopeMz + tolTh;
                for (var i = peakIndex; i >= 0; i--)
                {
                    var peakMz = Peaks[i].Mz;
                    if (peakMz < minMz)
                    {
                        peakIndex = i;
                        break;
                    }
                    if (peakMz <= maxMz)    // find match, move to prev isotope
                    {
                        var peak = Peaks[i];
                        if (observedPeaks[isotopeIndex] == null ||
                            peak.Intensity > observedPeaks[isotopeIndex].Intensity)
                        {
                            observedPeaks[isotopeIndex] = peak;
                        }
                    }
                }
            }

            // go up
            peakIndex = mostAbundantIsotopePeakIndex + 1;
            for (var isotopeIndex = mostAbundantIsotopeIndex + 1; isotopeIndex < isotopomerEnvelope.Length; isotopeIndex++)
            {
                if (isotopomerEnvelope[isotopeIndex] < relativeIntensityThreshold) break;
                var isotopeMz = Ion.GetIsotopeMz(monoIsotopeMass, charge, isotopeIndex);
                var tolTh = tolerance.GetToleranceAsMz(isotopeMz);
                var minMz = isotopeMz - tolTh;
                var maxMz = isotopeMz + tolTh;
                for (var i = peakIndex; i < Peaks.Length; i++)
                {
                    var peakMz = Peaks[i].Mz;
                    if (peakMz > maxMz)
                    {
                        peakIndex = i;
                        break;
                    }
                    if (peakMz >= minMz)    // find match, move to prev isotope
                    {
                        var peak = Peaks[i];
                        if (observedPeaks[isotopeIndex] == null ||
                            peak.Intensity > observedPeaks[isotopeIndex].Intensity)
                        {
                            observedPeaks[isotopeIndex] = peak;
                        }
                    }
                }
            }
            return observedPeaks;
        }

        /// <summary>
        /// Computes the Pearson correlation between the ion and corresponding peaks in the spectrum
        /// </summary>
        /// <param name="ion">ion</param>
        /// <param name="tolerance">tolerance</param>
        /// <param name="relativeIntensityThreshold">relative intensity threshold of the theoretical isotope profile</param>
        /// <returns>Pearson correlation</returns>
        public double GetCorrScore(Ion ion, Tolerance tolerance, double relativeIntensityThreshold = 0.1)
        {
            var observedPeaks = GetAllIsotopePeaks(ion, tolerance, relativeIntensityThreshold);
            if (observedPeaks == null) return 0;

            var isotopomerEnvelope = ion.Composition.GetIsotopomerEnvelopeRelativeIntensities();
            var observedIntensities = new double[observedPeaks.Length];

            for (var i = 0; i < observedPeaks.Length; i++)
            {
                var observedPeak = observedPeaks[i];
                observedIntensities[i] = observedPeak != null ? (float)observedPeak.Intensity : 0.0;
            }
            return FitScoreCalculator.GetPearsonCorrelation(isotopomerEnvelope, observedIntensities);
        }

        /// <summary>
        /// Computes the fit score between the ion and corresponding peaks in the spectrum
        /// </summary>
        /// <param name="ion">ion</param>
        /// <param name="tolerance">tolerance</param>
        /// <param name="relativeIntensityThreshold">relative intensity threshold of the theoretical isotope profile</param>
        /// <returns>fit score</returns>
        public double GetFitScore(Ion ion, Tolerance tolerance, double relativeIntensityThreshold = 0.1)
        {
            var isotopomerEnvelope = ion.Composition.GetIsotopomerEnvelopeRelativeIntensities();
            var observedPeaks = GetAllIsotopePeaks(ion, tolerance, relativeIntensityThreshold);

            if (observedPeaks == null) return 1;
            var theoIntensities = new double[observedPeaks.Length];
            Array.Copy(isotopomerEnvelope, theoIntensities, theoIntensities.Length);

            var maxObservedIntensity = observedPeaks.Select(p => p != null ? p.Intensity : 0).Max();
            var normalizedObs = observedPeaks.Select(p => p != null ? p.Intensity / maxObservedIntensity : 0).ToArray();
            return FitScoreCalculator.GetFitOfNormalizedVectors(isotopomerEnvelope, normalizedObs);
        }

        /// <summary>
        /// Computes the cosine between the ion and corresponding peaks in the spectrum
        /// </summary>
        /// <param name="ion">ion</param>
        /// <param name="tolerance">tolerance</param>
        /// <param name="relativeIntensityThreshold">relative intensity threshold of the theoretical isotope profile</param>
        /// <returns>cosine value</returns>
        public double GetConsineScore(Ion ion, Tolerance tolerance, double relativeIntensityThreshold = 0.1)
        {
            var observedPeaks = GetAllIsotopePeaks(ion, tolerance, relativeIntensityThreshold);
            if (observedPeaks == null) return 0;

            var isotopomerEnvelope = ion.Composition.GetIsotopomerEnvelopeRelativeIntensities();
            var theoIntensities = new double[observedPeaks.Length];
            var observedIntensities = new double[observedPeaks.Length];

            for (var i = 0; i < observedPeaks.Length; i++)
            {
                theoIntensities[i] = isotopomerEnvelope[i];
                var observedPeak = observedPeaks[i];
                observedIntensities[i] = observedPeak != null ? (float)observedPeak.Intensity : 0.0;
            }
            return FitScoreCalculator.GetCosine(isotopomerEnvelope, observedIntensities);
        }

        /// <summary>
        /// Finds the maximum intensity peak within the specified range
        /// </summary>
        /// <param name="minMz">minimum m/z</param>
        /// <param name="maxMz">maximum m/z</param>
        /// <returns>maximum intensity peak within the range</returns>
        public Peak FindPeak(double minMz, double maxMz)
        {
            var peakIndex = FindPeakIndex(minMz, maxMz);
            if (peakIndex < 0) return null;
            return Peaks[peakIndex];
        }

        public void Display(int maxPointsToShow = 50)
        {
            var sb = new StringBuilder();
            sb.AppendLine("m/z\tIntensity");

            var threshold = 1;
            if (maxPointsToShow > 0)
            {
                threshold = (int)Math.Round(Peaks.Length / (double)maxPointsToShow);
                if (threshold < 1)
                    threshold = 1;
            }

            var index = 0;
            var pointsShown = 0;
            foreach (var peak in Peaks)
            {
                if (index == 0 || index % threshold == 0)
                {
                    sb.Append(peak.Mz);
                    sb.Append("\t");
                    sb.Append(peak.Intensity);
                    sb.AppendLine();
                    pointsShown++;
                }
                index++;
            }

            Console.Write(sb.ToString());
            Console.WriteLine("Displayed {0} out of {1} data points", pointsShown, Peaks.Length);
        }

        public void FilterNoise(double signalToNoiseRatio = 1.4826)
        {
            if (Peaks.Length < 2) return;
            Array.Sort(Peaks, new IntensityComparer());
            var medianIntPeak = Peaks[Peaks.Length / 2];
            var noiseLevel = medianIntPeak.Intensity;

            var filteredPeaks = Peaks.TakeWhile(peak => !(peak.Intensity < noiseLevel * signalToNoiseRatio)).ToList();

            filteredPeaks.Sort();
            Peaks = filteredPeaks.ToArray();
        }

        public void FilterNosieByLocalWindow(double signalToNoiseRatio = 1.4826, int windowPpm = 10000)
        {
            var filteredPeaks = new List<Peak>();
            var tolerance = new Tolerance(windowPpm);
            var st = 0;
            var ed = 0;

            var prevSt = 0;
            var prevEd = 0;

            var intensityValues = new SortedSet<double>();

            foreach (var peak in Peaks)
            {
                var mzWindowWidth = tolerance.GetToleranceAsMz(peak.Mz);
                var mzStart = peak.Mz - mzWindowWidth;
                var mzEnd = peak.Mz + mzWindowWidth;

                while (st < Peaks.Length)
                {
                    if (st < Peaks.Length - 1 && Peaks[st].Mz < mzStart) st++;
                    else break;
                }
                while (ed < Peaks.Length)
                {
                    if (ed < Peaks.Length - 1 && Peaks[ed].Mz < mzEnd) ed++;
                    else break;
                }

                if (ed - st + 1 < 2)
                {
                    filteredPeaks.Add(peak);
                    continue;
                }

                if (intensityValues.Count < 1)
                {
                    for (var i = st; i <= ed; i++) intensityValues.Add(Peaks[i].Intensity);
                }
                else
                {
                    if (prevEd >= st)
                    {
                        for (var i = prevSt; i < ed; i++) intensityValues.Remove(Peaks[i].Intensity);
                        for (var i = prevEd+1; i <= ed; i++) intensityValues.Add(Peaks[i].Intensity);
                    }
                    else
                    {
                        for (var i = prevSt; i <= prevEd; i++) intensityValues.Remove(Peaks[i].Intensity);
                    }
                }

                var intensityMedian = intensityValues.Median();
                if (peak.Intensity > intensityMedian*signalToNoiseRatio) filteredPeaks.Add(peak);

                prevSt = st;
                prevEd = ed;
            }
            filteredPeaks.Sort();
            Peaks = filteredPeaks.ToArray();
        }

        private Bucket GetMostAbundantIntensity(int peakStartIndex, int peakEndIndex)
        {
            const int numberOfBins = 10;
            var iData = new double[peakEndIndex - peakStartIndex + 1];
            for (var i = peakStartIndex; i <= peakEndIndex; i++) iData[i - peakStartIndex] = Peaks[i].Intensity;
            var histogram = new Histogram(iData, numberOfBins);

            var maxCount = 0d;
            var mostAbundantBinIndex = 0;
            for (var i = 0; i < Math.Ceiling(numberOfBins * 0.5); i++)
            {
                if (!(histogram[i].Count > maxCount)) continue;
                maxCount = histogram[i].Count;
                mostAbundantBinIndex = i;
            }

            return histogram[mostAbundantBinIndex];
        }

        public void FilterNosieByIntensityHistogram()
        {
            var filteredPeaks = new List<Peak>();
            var intensities = new double[Peaks.Length];
            var tolerance = new Tolerance(10000);

            var st = 0;
            var ed = 0;

            foreach (var peak in Peaks)
            {
                var mzWindowWidth = tolerance.GetToleranceAsMz(peak.Mz);
                var intensity = peak.Intensity;

                var mzStart = peak.Mz - mzWindowWidth;
                var mzEnd = peak.Mz + mzWindowWidth;

                while (st < Peaks.Length)
                {
                    if (st < Peaks.Length - 1 && Peaks[st].Mz < mzStart) st++;
                    else break;
                }
                while (ed < Peaks.Length)
                {
                    if (ed < Peaks.Length - 1 && Peaks[ed].Mz < mzEnd) ed++;
                    else break;
                }

                var abundantIntensityBucket = GetMostAbundantIntensity(st, ed);
                if (abundantIntensityBucket.LowerBound < intensity && intensity < abundantIntensityBucket.UpperBound)
                    continue;

                filteredPeaks.Add(peak);
            }
            filteredPeaks.Sort();
            Peaks = filteredPeaks.ToArray();
        }

        public void FilterNoiseBySlope(double slopeThreshold = 10000)
        {
            if (Peaks.Length < 2) return;

            var mzData = new float[Peaks.Length];
            var intensityData = new float[Peaks.Length];
            var i = 0;

            for (i = 0; i < Peaks.Length; i++)
            {
                mzData[i] = (float) Peaks[i].Mz;
                intensityData[i] = (float) Peaks[i].Intensity;
            }

            var spline = new CubicSpline();
            spline.Fit(mzData, intensityData);

            var intensitySlope = spline.EvalSlope(mzData);

            var filteredPeaks = new List<Peak>();

            for (i = 0; i < Peaks.Length; i++)
            {
                if (Math.Abs(intensitySlope[i]) > slopeThreshold)
                    filteredPeaks.Add(Peaks[i]);
            }
            Peaks = filteredPeaks.ToArray();
        }

        public Spectrum GetFilteredSpectrumBySignalToNoiseRatio(double signalToNoiseRatio = 1.4826)
        {
            var filteredSpec = (Spectrum)MemberwiseClone();
            filteredSpec.FilterNoise(signalToNoiseRatio);
            return filteredSpec;
        }

        public Spectrum GetFilteredSpectrumBySlope(double slopeThreshold = 0.33)
        {
            var filteredSpec = (Spectrum)MemberwiseClone();
            filteredSpec.FilterNoiseBySlope(slopeThreshold);
            return filteredSpec;
        }

        public Spectrum GetFilteredSpectrumByLocalWindow(double signalToNoiseRatio = 1.4826, int windowPpm = 10000)
        {
            var filteredSpec = (Spectrum)MemberwiseClone();
            filteredSpec.FilterNosieByLocalWindow(signalToNoiseRatio, windowPpm);
            return filteredSpec;
        }

        private int _msLevel = 1;

        public int FindPeakIndex(double mz, Tolerance tolerance)
        {
            var tolTh = tolerance.GetToleranceAsMz(mz);
            var minMz = mz - tolTh;
            var maxMz = mz + tolTh;
            return FindPeakIndex(minMz, maxMz);
        }

        public int FindPeakIndex(double minMz, double maxMz)
        {
            var index = Array.BinarySearch(Peaks, new Peak((minMz + maxMz) / 2, 0));
            if (index < 0) index = ~index;

            var bestPeakIndex = -1;
            var bestIntensity = 0.0;

            // go down
            var i = index - 1;
            while (i >= 0 && i < Peaks.Length)
            {
                if (Peaks[i].Mz <= minMz) break;
                if (Peaks[i].Intensity > bestIntensity)
                {
                    bestIntensity = Peaks[i].Intensity;
                    bestPeakIndex = i;
                }
                --i;
            }

            // go up
            i = index;
            while (i >= 0 && i < Peaks.Length)
            {
                if (Peaks[i].Mz >= maxMz) break;
                if (Peaks[i].Intensity > bestIntensity)
                {
                    bestIntensity = Peaks[i].Intensity;
                    bestPeakIndex = i;
                }
                ++i;
            }
            return bestPeakIndex;
        }
    }
}
