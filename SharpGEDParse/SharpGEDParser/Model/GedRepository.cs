﻿
/*
0 @REPO24@ REPO
0 @REPO25@ REPO
1 NAME County Clerk's Office, Greene County, NY
1 ADDR Hudson, NY

0 @R29@ REPO
1 NAME Superintendent Registrar (York)
1 ADDR York Register Office
2 CONT 56 Bootham
2 CONT York,,  YO30 7DA
2 CONT England (UK)
2 ADR1 York Register Office
2 ADR2 56 Bootham
2 CITY York,
2 POST YO30 7DA
2 CTRY England (UK)

*/
namespace SharpGEDParser.Model
{
    public class GedRepository : GEDCommon
    {
        public static string Tag = "REPO";

        public string Name { get; set; }

        private Address _addr;
        public Address Addr { get { return _addr ?? (_addr = new Address()); } }

        public GedRepository(GedRecord lines, string ident)
        {
            BegLine = lines.Beg;
            EndLine = lines.End;
            Ident = ident;
        }
    }
}