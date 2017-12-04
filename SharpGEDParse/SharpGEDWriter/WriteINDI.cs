﻿using SharpGEDParser.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// TODO can record ident be generalized?

namespace SharpGEDWriter
{
    class WriteINDI
    {
        internal static void WriteINDIs(StreamWriter file, List<GEDCommon> records)
        {
            foreach (var gedCommon in records)
            {
                if (gedCommon is IndiRecord)
                    WriteOneIndi(file, gedCommon as IndiRecord);
            }
        }

        private static void writeLink(StreamWriter file, IndiLink indiLink)
        {
            // TODO extra text
            WriteCommon.writeXrefIfNotEmpty(file, indiLink.Tag, indiLink.Xref, 1);
            WriteCommon.writeIfNotEmpty(file, "PEDI", indiLink.Pedi, 2);
            WriteCommon.writeIfNotEmpty(file, "STAT", indiLink.Stat, 2);
            WriteCommon.writeSubNotes(file, indiLink, 2);
        }

        // TODO more closely match PAF order? - specifically _UID
        // INDI records are written to be as close to PAF order as possible
        private static void WriteOneIndi(StreamWriter file, IndiRecord indiRecord)
        {
            file.WriteLine("0 @{0}@ INDI", indiRecord.Ident);

            WriteCommon.writeIfNotEmpty(file, "RESN", indiRecord.Restriction, 1);

            foreach (var nameRec in indiRecord.Names)
            {
                writeName(file, nameRec);
                foreach (var tuple in nameRec.Parts)
                {
                    file.WriteLine("2 {0} {1}", tuple.Item1, tuple.Item2);
                }
                // TODO other name types

                // TODO parser missing these???
                //WriteCommon.writeSubNotes(file, nameRec, 2);
                //WriteCommon.writeSourCit(file, nameRec, 2);
            }

            // TODO original text or corrected?
            WriteCommon.writeIfNotEmpty(file, "SEX", indiRecord.FullSex, 1);

            WriteEvent.writeEvents(file, indiRecord.Events, 1);
            WriteEvent.writeEvents(file, indiRecord.Attribs, 1);

            // Insure FAMS/FAMC output in consistent order
            foreach (var indiLink in indiRecord.Links.Where(indiLink => indiLink.Tag == "FAMS"))
            {
                writeLink(file, indiLink);
            }
            foreach (var indiLink in indiRecord.Links.Where(indiLink => indiLink.Tag == "FAMC"))
            {
                writeLink(file, indiLink);
            }

            // TODO LDS events

            foreach (var aliasLink in indiRecord.AliasLinks)
            {
                WriteCommon.writeXrefIfNotEmpty(file, "ALIA", aliasLink, 1);
            }

            foreach (var assoRec in indiRecord.Assocs)
            {
                WriteCommon.writeXrefIfNotEmpty(file, "ASSO", assoRec.Ident, 1);
                WriteCommon.writeIfNotEmpty(file, "RELA", assoRec.Relation, 2);
                WriteCommon.writeSubNotes(file, assoRec, 2);
                WriteCommon.writeSourCit(file, assoRec, 2);
            }

            // TODO why are INDI and FAM submitters treated different?
            foreach (var submitter in indiRecord.Submitters)
            {
                file.WriteLine("1 SUBM @{0}@", submitter.Xref);
            }

            WriteCommon.writeRecordTrailer(file, indiRecord, 1);
        }

        private static void writeName(StreamWriter file, NameRec name)
        {
            var names = "";
            if (!string.IsNullOrWhiteSpace(name.Names))
                names = name.Names;
            var sur = "";
            if (!string.IsNullOrWhiteSpace(name.Surname))
                sur = "/" + name.Surname + "/";
            var suf = "";
            if (!string.IsNullOrWhiteSpace(name.Suffix))
                suf = name.Suffix;
            string line = string.Format("1 NAME {0}{1}{2}{3}{4}", names, 
                names.Length > 1 ? " " : "",
                sur,
                suf.Length > 1 ? " " : "",
                suf
                );
            file.WriteLine(line.Trim());
        }
    }
}