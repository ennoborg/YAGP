﻿using System.Reflection;
using SharpGEDParser.Model;
using System;
using System.Collections.Generic;
using System.IO;

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

        private static void WriteOneIndi(StreamWriter file, IndiRecord indiRecord)
        {
            file.WriteLine("0 @{0}@ INDI", indiRecord.Ident);

            WriteCommon.writeIfNotEmpty(file, "RESN", indiRecord.Restriction, 1);

            // TODO original text or corrected?
            WriteCommon.writeIfNotEmpty(file, "SEX", indiRecord.FullSex, 1);

            // TODO NAME

            // FAMC/FAMS
            foreach (var indiLink in indiRecord.Links)
            {
                // TODO extra text
                WriteCommon.writeXrefIfNotEmpty(file, indiLink.Tag, indiLink.Xref, 2);
                WriteCommon.writeIfNotEmpty(file, "PEDI", indiLink.Pedi, 3);
                WriteCommon.writeIfNotEmpty(file, "STAT", indiLink.Stat, 3);
                WriteCommon.writeSubNotes(file, indiLink, 3);
            }

            WriteEvent.writeEvents(file, indiRecord.Events, 1);
            WriteEvent.writeEvents(file, indiRecord.Attribs, 1);

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
    }
}
