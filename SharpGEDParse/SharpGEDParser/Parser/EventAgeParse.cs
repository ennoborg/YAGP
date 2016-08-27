﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpGEDParser.Model;

namespace SharpGEDParser.Parser
{
    class EventAgeParse : StructParser
    {
        private static readonly Dictionary<string, TagProc> tagDict = new Dictionary<string, TagProc>()
        {
            {"AGE", ageProc}
        };

        private static void ageProc(StructParseContext context, int linedex, char level)
        {
            var det = context.Parent as AgeDetail;
            det.Age = context.Remain;
        }

        public static AgeDetail AgeParser(StructParseContext ctx, int linedex, char level)
        {
            AgeDetail det = new AgeDetail();
            StructParseContext ctx2 = new StructParseContext(ctx, linedex, det);
            ctx2.Level = level;
            if (!string.IsNullOrWhiteSpace(ctx.Remain))
            {
                det.Detail = ctx.Remain;
            }
            StructParse(ctx2, tagDict);
            ctx.Endline = ctx2.Endline;
            return det;
        }
    }
}
