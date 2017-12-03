﻿using NUnit.Framework;

namespace SharpGEDWriter.Tests
{
    [TestFixture]
    class Basic : GedWriteTest
    {
        [Test]
        public void TryIt()
        {
            var indi1 = "0 @I1@ INDI\n1 SEX M";
            var res = ParseAndWrite(indi1);
            Assert.AreEqual(indi1+"\n", res);
        }

        private string [] records =
        {
            "0 @I1@ INDI",
            "1 NAME Fred",
            "1 SEX M",
            "1 FAMS @F1@",
            "1 FAMC @F2@",
            "1 _UID 9876543210", // Note: not valid value TODO parse may fail
            "1 NOTE @N1@",
            "1 CHAN",
            "2 DATE 2 DEC 2017",
            "0 @F1@ FAM",
            "1 HUSB @I1@",
            "0 @F2@ FAM",
            "1 CHIL @I1@",
            "0 @N1@ NOTE"
        };

        [Test]
        public void MultRec()
        {
            string str = MakeInput(records);
            var fr = ReadItHigher(str);
            Assert.AreEqual(0, fr.AllErrors.Count);
            var res = Write(fr);
            Assert.AreEqual(str, res);
        }

        [Test]
        public void SourRepo()
        {
            // SOUR.REPO was missing '@'s
            var txt = "0 @S1@ SOUR\n1 AUTH Fred\n1 REPO @R1@\n1 RIN rin-chan";
            var fr = ReadItHigher(txt);
            Assert.AreEqual(0, fr.AllErrors.Count);
            var res = Write(fr);
            Assert.AreEqual(res, txt + "\n");
        }
        [Test]
        public void SourRepo2()
        {
            // SOUR.REPO with no xref
            var txt = "0 @S1@ SOUR\n1 AUTH Fred\n1 REPO\n2 CALN blah\n1 RIN rin-chan";
            var fr = ReadItHigher(txt);
            Assert.AreEqual(0, fr.AllErrors.Count);
            var res = Write(fr);
            Assert.AreEqual(res, txt + "\n");
        }
    }
}
