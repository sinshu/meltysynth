using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class SoundFont3Test
    {
        [TestCase]
        public void SoundFont3FailTest()
        {
            Assert.Throws<NotSupportedException>(() => new SoundFont("MuseScore_General.sf3"));
        }
    }
}
