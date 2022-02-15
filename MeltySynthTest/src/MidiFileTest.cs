using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class MidiFileTest
    {
        [TestCase(0.0)]
        [TestCase(1.1)]
        [TestCase(1.11)]
        [TestCase(1.111)]
        [TestCase(1.1111)]
        [TestCase(1.11111)]
        [TestCase(3.1415)]
        public void TimecentsToSecondsTest(double value)
        {
            var actual = MidiFile.GetTimeSpanFromSeconds(value);
            var expected = TimeSpan.FromSeconds(value);
            Assert.AreEqual(actual.Ticks, expected.Ticks);
        }
    }
}
