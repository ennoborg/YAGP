﻿using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace SharpGEDWriter.Tests
{
    [ExcludeFromCodeCoverage]
    [TestFixture]
    class Names : GedWriteTest
    {
        [Test]
        public void Basic()
        {
            var inp = "0 @I1@ INDI\n1 NAME Fred";
            var res = ParseAndWrite(inp);
            Assert.AreEqual(inp+"\n", res);
        }

        [Test]
        public void Suffix()
        {
            var inp = "0 @I1@ INDI\n1 NAME Fred /Flintstone/ Jr.";
            var res = ParseAndWrite(inp);
            Assert.AreEqual(inp + "\n", res);
        }

        [Test]
        public void Parts()
        {
            var inp = "0 @I1@ INDI\n1 NAME Fred /Flintstone/\n2 SURN Flintstone\n2 GIVN Fred\n2 NICK Yabba dabba do\n2 NPFX blah\n2 SPFX blah\n2 NSFX blah";
            var res = ParseAndWrite(inp);
            Assert.AreEqual(inp + "\n", res);
        }
        [Test,Ignore("INDI.NAME.NOTE NYI")]
        public void Note()
        {
            // TODO INDI.NAME.NOTE not parsed!
            var inp = "0 @I1@ INDI\n1 NAME Fred /Flintstone/\n2 NOTE This is a note";
            var res = ParseAndWrite(inp);
            Assert.AreEqual(inp + "\n", res);
        }
        [Test, Ignore("INDI.NAME.SOUR NYI")]
        public void SourCit()
        {
            // TODO INDI.NAME.SOUR not parsed!
            var inp = "0 @I1@ INDI\n1 NAME Fred /Flintstone/\n2 SOUR @S3@";
            var res = ParseAndWrite(inp);
            Assert.AreEqual(inp + "\n", res);
        }
    }
}