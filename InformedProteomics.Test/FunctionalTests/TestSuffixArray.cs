﻿using System;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Database;
using NUnit.Framework;

namespace InformedProteomics.Test.FunctionalTests
{
    [TestFixture]
    public class TestSuffixArray
    {
        [Test]
        public void GetTrypticPeptideMassMzDistribution()
        {
            const int minPeptideLength = 6;
            const int maxPeptideLength = 30;
            const int numTolerableTermini = 2;
            const int numMissedCleavages = 1;
            var enzyme = Enzyme.Trypsin;

            const string dbFilePath = @"\\protoapps\UserData\Sangtae\TestData\H_sapiens_Uniprot_SPROT_2013-05-01_withContam.fasta";
            var targetDb = new FastaDatabase(dbFilePath);

            var indexedDbTarget = new IndexedDatabase(targetDb);
            var aminoAcidSet = new AminoAcidSet(Modification.Carbamidomethylation);

            var hist = new int[31];
            var numPeptides = 0;
            foreach (
                var annotationAndOffset in
                    indexedDbTarget.AnnotationsAndOffsets(minPeptideLength, maxPeptideLength, numTolerableTermini,
                                                       numMissedCleavages, enzyme))
            {
                var annotation = annotationAndOffset.Annotation;
                var pepSequence = annotation.Substring(2, annotation.Length - 4);
                var pepComposition = aminoAcidSet.GetComposition(pepSequence) + Composition.H2O;
                numPeptides++;
                for (var charge = 2; charge < 3; charge++)
                {
                    var ion = new Ion(pepComposition, charge);
                    var precursorMz = ion.GetMonoIsotopicMz();

                    var massIndex = (int)Math.Round(precursorMz / 100.0);
                    if (massIndex > 30) massIndex = 30;
                    hist[massIndex]++;
                }
            }

            Console.WriteLine("Mass\t#Pep\tRatio");
            for (var i = 1; i < hist.Length; i++)
            {
                Console.WriteLine("{0}\t{1}\t{2}", i*100, hist[i], hist[i]/(float)numPeptides);
            }
        }

        public void TestReadingBigFile()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            const string bigDbFile = @"C:\cygwin\home\kims336\Research\SuffixArray\uniprot2012_7_ArchaeaBacteriaFungiSprotTrembl_2012-07-11.fasta";
            var lastLine = File.ReadLines(bigDbFile).Last();
            sw.Stop();

            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"{0:f4} sec", sec);
            Console.WriteLine(lastLine);
        }

        [Test]
        public void TestGeneratingDecoyDatabase()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            const string dbFile = @"\\protoapps\UserData\Sangtae\TestData\BSA.fasta";
            var db = new FastaDatabase(dbFile);
            db.Decoy(Enzyme.Trypsin);
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"{0:f4} sec", sec);
        }
    }
}