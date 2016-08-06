﻿using System;
using System.Collections.Generic;
using SharpGEDParser.Model;

namespace SharpGEDParser.Parser
{
    public class ChanStructParse : StructParser
    {
        private static readonly Dictionary<string, TagProc> tagDict = new Dictionary<string, TagProc>()
        {
            {"DATE", dateProc},
            {"NOTE", noteProc}
        };

        private static void dateProc(StructParseContext ctx, int linedex, char level)
        {
            var chan = ctx.Parent as ChangeRec;
            DateTime res;
            if (DateTime.TryParse(ctx.Remain, out res))
                chan.Date = res;

            // NOTE: could not parse date: will be caught by 'missing data' check
        }

        public static void ChanParse(GedRecParse.ParseContext2 ctx, ChangeRec chan)
        {
            StructParseContext ctx2 = new StructParseContext(ctx, chan);
            StructParse(ctx2, tagDict);
            ctx.Endline = ctx2.Endline;
        }

        public static void ChanProc(GedRecParse.ParseContext2 ctx)
        {
            ChangeRec chan = ctx.Parent.CHAN;
            if (chan.Date != null)
            {
                UnkRec err = new UnkRec();
                err.Error = "More than one change record";
                GedRecParse.LookAhead(ctx);
                err.Beg = ctx.Begline;
                err.End = ctx.Endline;
                ctx.Parent.Errors.Add(err);
                return;
            }

            ChanParse(ctx, chan);
            if (chan.Date == null)
            {
                UnkRec err = new UnkRec();
                err.Error = "Missing/invalid date for CHAN";
                err.Beg = ctx.Begline;
                err.End = ctx.Endline;
                ctx.Parent.Errors.Add(err);
            }
        }

    }
}