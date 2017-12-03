﻿using SharpGEDParser.Model;
using System.Collections.Generic;
using System.IO;

// ReSharper disable InconsistentNaming

namespace SharpGEDWriter
{
    class WriteCommon
    {
        internal static void writeSubNotes(StreamWriter file, NoteHold rec, int level = 1)
        {
            // TODO embedded notes -> note xref
            if (rec == null || rec.Notes.Count == 0)
                return;
            foreach (var note in rec.Notes)
            {
                if (!string.IsNullOrWhiteSpace(note.Xref))
                    file.WriteLine("{0} NOTE @{1}@", level, note.Xref);
                else
                {
                    writeExtended(file, level, "NOTE", note.Text);
                }
            }
        }

        //static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        //{
        //    for (int i = 0; i < str.Length; i += maxChunkSize)
        //        yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        //}

        private static void writeWithConc(StreamWriter file, int level, string tag, string text, bool incrLevel)
        {
            // TODO really handle CONC tag here!
            file.WriteLine("{0} {1} {2}", level, tag, text);
        }

        private static char[] nlSplit = {'\n'};

        public static void writeExtended(StreamWriter file, int level, string tag, string text)
        {
            // write a tag which may have extended text (requiring the use of CONC/CONT tags)
            // GEDCOM standard specifies that line length is limited to 255 chars

            // Don't do extra work for short/unsplit lines
            if (text.Length < 247 && !text.Contains("\n"))
            {
                file.WriteLine("{0} {1} {2}", level, tag, text);
                return;
            }

            // original CONT tags were marked as embedded newlines; separate out and add required tags
            var lines = text.Split(nlSplit);
            writeWithConc(file, level, tag, lines[0], false);
            for (int i = 1; i < lines.Length; i++)
            {
                writeWithConc(file, level + 1, "CONT", lines[i], true);
            }
        }

        internal static void writeObjeLink(StreamWriter file, MediaHold rec, int level=1)
        {
            // TODO embedded OBJE -> OBJE xref
            if (rec == null || rec.Media.Count == 0)
                return;
            foreach (var mediaLink in rec.Media)
            {
                file.WriteLine("{0} OBJE @{1}@", level, mediaLink.Xref);
            }
        }

        internal static void writeChan(StreamWriter file, GEDCommon rec)
        {
            if (rec.CHAN.Date == null)
                return;
            file.WriteLine("1 CHAN");
            file.WriteLine("2 DATE {0}", rec.CHAN.Date.Value.ToString("d MMM yyyy").ToUpper());
            // TODO change time?
            writeSubNotes(file, rec.CHAN, 2);

            // TODO otherlines
        }

        // TODO conversion from descriptive to reference source citations

        internal static void writeSourCit(StreamWriter file, SourceCitHold rec, int level=1)
        {
            if (rec == null || rec.Cits.Count == 0)
                return;
            foreach (var cit in rec.Cits)
            {
                file.WriteLine("{0} SOUR @{1}@", level, cit.Xref);
                if (!string.IsNullOrEmpty(cit.Page))
                    file.WriteLine("{0} PAGE {1}", level+1, cit.Page);

                // TODO events
                // TODO data

                if (!string.IsNullOrEmpty(cit.Quay))
                    file.WriteLine("{0} PAGE {1}", level + 1, cit.Quay);

                writeSubNotes(file, cit, level+1);
                writeObjeLink(file, cit, level+1);
            }
        }

        internal static void writeIds(StreamWriter file, GEDCommon rec, int level = 1)
        {
            if (rec.Ids.REFNs.Count < 1 && rec.Ids.Others.Count < 1)
                return;
            foreach (var refN in rec.Ids.REFNs)
            {
                file.WriteLine("{0} REFN {1}", level, refN.Value);
                // TODO don't have original 'TYPE' lines
            }

            foreach (var other in rec.Ids.Others)
            {
                file.WriteLine("{0} {1} {2}", level, other.Key, other.Value.Value);
            }
        }

        public static void writeExtIfNotEmpty(StreamWriter file, string tag, string value, int level)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            writeExtended(file, level, tag, value);
        }

        public static void writeIfNotEmpty(StreamWriter file, string tag, string value, int level)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            file.WriteLine("{0} {1} {2}", level, tag, value);
        }
        public static void writeXrefIfNotEmpty(StreamWriter file, string tag, string value, int level)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            file.WriteLine("{0} {1} @{2}@", level, tag, value);
        }

        public static void writeMultiple(StreamWriter file, string tag, List<string> vals, int level)
        {
            foreach (var val in vals)
            {
                writeIfNotEmpty(file, tag, val, level);
            }
        }

        public static void writeAddr(StreamWriter file, Address addr, int level)
        {
            if (addr == null)
                return;

            // TODO continuation
            file.WriteLine("{0} ADDR {1}", level, addr.Adr);
            writeIfNotEmpty(file, "ADR1", addr.Adr1, level + 1);
            writeIfNotEmpty(file, "ADR2", addr.Adr2, level + 1);
            writeIfNotEmpty(file, "ADR3", addr.Adr3, level + 1);
            writeIfNotEmpty(file, "CITY", addr.City, level + 1);
            writeIfNotEmpty(file, "STAE", addr.Stae, level + 1);
            writeIfNotEmpty(file, "POST", addr.Post, level + 1);
            writeIfNotEmpty(file, "CTRY", addr.Ctry, level + 1);

            writeMultiple(file, "PHON", addr.Phon, level);
            writeMultiple(file, "EMAIL", addr.Email, level);
            writeMultiple(file, "FAX", addr.Fax, level);
            writeMultiple(file, "WWW", addr.WWW, level);
        }

        internal static void writeRecordTrailer(StreamWriter file, GEDCommon rec, int level)
        {
            writeIds(file, rec);
            writeIfNotEmpty(file, "RIN", rec.RIN, level);
            writeSubNotes(file, rec as NoteHold);
            writeSourCit(file, rec as SourceCitHold);
            writeObjeLink(file, rec as MediaHold);
            writeChan(file, rec);
        }
    }
}
