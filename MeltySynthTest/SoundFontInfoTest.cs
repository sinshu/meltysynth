using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace MeltySynthTest
{
    public class SoundFontInfoTest
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.SoundFontPaths))]
        public void ReadTest(string path)
        {
            var expected = new NAudio.SoundFont.SoundFont(path).FileInfo;
            var actual = new MeltySynth.SoundFont.SoundFont(path).Info;

            AreEqual(expected.SoundFontVersion, actual.Version);
            AreEqual(expected.WaveTableSoundEngine, actual.TargetSoundEngine);
            AreEqual(expected.BankName, actual.BankName);
            AreEqual(expected.DataROM, actual.RomName);
            AreEqual(expected.ROMVersion, actual.RomVersion);
            AreEqual(expected.CreationDate, actual.CeationDate);
            AreEqual(expected.Author, actual.Author);
            AreEqual(expected.TargetProduct, actual.TargetProduct);
            AreEqual(expected.Copyright, actual.Copyright);
            AreEqual(expected.Comments, actual.Comments);
            AreEqual(expected.Tools, actual.Tools);
        }

        private void AreEqual(string expected, string actual)
        {
            if (expected == null)
            {
                Assert.AreEqual(string.Empty, actual);
            }
            else
            {
                Assert.AreEqual(expected, actual);
            }
        }

        private void AreEqual(NAudio.SoundFont.SFVersion expected, MeltySynth.SoundFont.SoundFontVersion actual)
        {
            if (expected == null)
            {
                Assert.AreEqual(0, actual.Major);
                Assert.AreEqual(0, actual.Minor);
            }
            else
            {
                Assert.AreEqual(expected.Major, actual.Major);
                Assert.AreEqual(expected.Minor, actual.Minor);
            }
        }
    }
}
