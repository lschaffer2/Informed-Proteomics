﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Data.Spectrometry
{
    public class ProductSpectrum: Spectrum
    {
        public ProductSpectrum(double[] mzArr, double[] intensityArr) : base(mzArr, intensityArr)
        {
        }

        public ActivationMethod ActivationMethod { get; set; }
        public PrecursorInfo PrecursorInfo { get; set; }

        public void SetMsLevel(int msLevel)
        {
            MsLevel = msLevel;
        }
    }
}
