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
        [Category("ExternalAssets")]
        [TestCase]
        public void SoundFont3FailTest()
        {
            var path = TestAssets.RequireExternal("MuseScore_General.sf3");
            Assert.Throws<NotSupportedException>(() => new SoundFont(path));
        }
    }
}
