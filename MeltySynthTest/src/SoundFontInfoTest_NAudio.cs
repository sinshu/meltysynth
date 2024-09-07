using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class SoundFontInfoTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public void ReadTest(string soundFontName, MeltySynth.SoundFont soundFont)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").FileInfo;
            var actual = soundFont.Info;

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
                Assert.That(string.Empty, Is.EqualTo(actual));
            }
            else
            {
                Assert.That(expected, Is.EqualTo(actual));
            }
        }

        private void AreEqual(NAudio.SoundFont.SFVersion expected, MeltySynth.SoundFontVersion actual)
        {
            if (expected == null)
            {
                Assert.That(0, Is.EqualTo(actual.Major));
                Assert.That(0, Is.EqualTo(actual.Minor));
            }
            else
            {
                Assert.That(expected.Major, Is.EqualTo(actual.Major));
                Assert.That(expected.Minor, Is.EqualTo(actual.Minor));
            }
        }
    }
}
