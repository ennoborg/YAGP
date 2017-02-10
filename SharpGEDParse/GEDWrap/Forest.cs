﻿using System.IO;
using SharpGEDParser;
using SharpGEDParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;

// Container wrapper around an imported GEDCOM
// Use to access records
// Connects and verifies links between INDI and FAM

// ReSharper disable InconsistentNaming

namespace GEDWrap
{
    public class Forest : IDisposable
    {
        private FileRead _gedReader;

        public List<IndiRecord> Indi
        {
            get
            {
                return _gedReader.Data.OfType<IndiRecord>().Select(gedCommon => gedCommon).ToList();
            }
        }
        public List<FamRecord> Fams
        {
            get
            {
                return _gedReader.Data.OfType<FamRecord>().Select(gedCommon => gedCommon).ToList();
            }
        }

        public IndiRecord FindIndiByIdent(string ident)
        {
            return _gedReader.Data.OfType<IndiRecord>().FirstOrDefault(indi => indi.Ident == ident);
        }

        public List<UnkRec> Errors
        {
            get { return _gedReader.AllErrors; }
        }

        public List<UnkRec> Unknowns
        {
            get { return _gedReader.AllUnknowns; }
        }

        /// <summary>
        /// Only parse a GEDCOM file. Useful for syntax validation. 
        /// </summary>
        /// <param name="path"></param>
        public void ParseGEDCOM(string path)
        {
            _gedReader = new FileRead();
            _gedReader.ReadGed(path);
        }

        /// <summary>
        /// Parse a GEDCOM file, establish family relations 
        /// </summary>
        /// <param name="path"></param>
        public void LoadGEDCOM(string path)
        {
            ParseGEDCOM(path);
            BuildTree();
            CalcTrees();
        }

        public void LoadFromStream(StreamReader stream)
        {
            _gedReader = new FileRead();
            _gedReader.ReadLines(stream);
            BuildTree();
            CalcTrees();
        }

        public Person PersonById(string ident)
        {
            return _indiHash[ident];
        }

        public IEnumerable<string> AllIndiIds
        {
            get { return _indiHash.Keys; }
        }

        public int ErrorsCount
        {
            get
            {
                //return _issues.Count(issue => issue.Message().StartsWith("Error:"));
                // TODO for testing 
                return _issues.Count;
            }
        }

        public IEnumerable<Person> AllPeople
        {
            get { return _indiHash.Values; }
        }

        public IEnumerable<Union> AllUnions
        {
            get { return _famHash.Values; }
        }

        public List<GEDCommon> AllRecords
        {
            get { return _gedReader.Data; }
        }

        public int NumberOfTrees
        {
            get
            {
                int treeCount = 0;
                foreach (var person in _indiHash.Values)
                {
                    if (person.Tree > treeCount)
                        treeCount = person.Tree;
                }
                return treeCount;
            }
        }

        public IEnumerable<Issue> Issues { get { return _issues; } }

        #region BuildTree (Establish family relationships)

        private Dictionary<string, Person> _indiHash;  // INDI ident -> INDI record
        private Dictionary<string, Union> _famHash; // FAM  ident -> FAM  record
        private List<Issue> _issues;
        private string _firstPerson; // "First" person in tree - default initial person

        private void BuildTree()
        {
            WrapAndHashRecords();   

            // Making connections using _only_ the INDI, or _only_ the FAM,
            // has not proven to be effective.
            ConnectFamLinks();  // Make connections using FAM.HUSB/WIFE/CHIL
            ConnectIndiLinks(); // Make connections using INDI.FAMC/FAMS

            VerifyFAMLinks();
            VerifyINDILinks();
        }

        private void MakeError(Issue.IssueCode code, params object[] evidence)
        {
            Issue err = new Issue(code, evidence);
            _issues.Add(err);
        }

        private void WrapAndHashRecords()
        {
            // Wrap INDI and FAM records, collect into hashes

            _indiHash = new Dictionary<string, Person>();
            _famHash = new Dictionary<string, Union>();
            _issues = new List<Issue>();

            foreach (var indiRecord in Indi)
            {
                var ident = indiRecord.Ident;
                if (_indiHash.ContainsKey(ident))
                {
                    MakeError(Issue.IssueCode.DUPL_INDI, ident);
                }
                else
                {
                    Person iw = new Person(indiRecord);
                    _indiHash.Add(ident, iw);

                    if (_firstPerson == null)
                        _firstPerson = ident;
                }
            }
            foreach (var fam in Fams)
            {
                var ident = fam.Ident;
                if (string.IsNullOrWhiteSpace(ident))
                {
                    MakeError(Issue.IssueCode.MISS_FAMID, fam.BegLine);
                    continue;
                }
                if (!_famHash.ContainsKey(ident))
                    _famHash.Add(ident, new Union(fam));
                else
                {
                    MakeError(Issue.IssueCode.DUPL_FAM, ident);
                }
            }
        }

        private void ConnectFamLinks()
        {
            // This is a good 'first guess' at family relations: also need to use INDI.FAMS/INDI.FAMC

            // Iterate through the family records
            // For each HUSB/WIFE, connect to INDI
            // For each CHIL, connect to INDI
            foreach (var familyUnit in _famHash.Values)
            {
                foreach (var dadId in familyUnit.FamRec.Dads)
                {
                    Person dadWrap;
                    if (_indiHash.TryGetValue(dadId, out dadWrap))
                    {
                        dadWrap.SpouseIn.Add(familyUnit);
                        familyUnit.Spouses.Add(dadWrap);
                        familyUnit.Husband = dadWrap; // TODO taking the last one... save all?
                    }
                }
                foreach (var momId in familyUnit.FamRec.Moms)
                {
                    Person momWrap;
                    if (_indiHash.TryGetValue(momId, out momWrap))
                    {
                        momWrap.SpouseIn.Add(familyUnit);
                        familyUnit.Spouses.Add(momWrap);
                        familyUnit.Wife = momWrap; // TODO taking the last one... save all?
                    }
                }
                foreach (var childId in familyUnit.FamRec.Childs)
                {
                    // does childId exist in _indiHash: if yes, add to familyUnit.Childs
                    // if no, error
                    Person childWrap;
                    if (_indiHash.TryGetValue(childId, out childWrap))
                    {
                        childWrap.ChildIn.Add(familyUnit);
                        familyUnit.Childs.Add(childWrap);
                    }
                }
            }
        }

        private void ConnectIndiLinks()
        {
            // Iterate through INDI records.
            // For each FAMC, connect to FAM.
            // For each FAMS, connect to FAM.
            foreach (var person in _indiHash.Values)
            {
                var indiId = person.Id;
                foreach (var link in person.Indi.Links)
                {
                    var famId = link.Xref;

                    // Some GEDCOM end up with gibberish xref. TODO were these marked during parse?
                    if (string.IsNullOrWhiteSpace(famId))
                    {
                        MakeError(Issue.IssueCode.MISS_XREFID, indiId, link.Tag);
                        continue;
                    }
                    Union famU;
                    if (!_famHash.TryGetValue(famId, out famU))
                        continue; // Ignore missing family here, catch later
                    switch (link.Tag)
                    {
                        case "FAMS":
                            person.SpouseIn.Add(famU);
                            if (!famU.ReconcileFams(person))
                                MakeError(Issue.IssueCode.SPOUSE_CONN, famId, indiId);
                            break;
                        case "FAMC":
                            famU.Childs.Add(person);
                            person.ChildIn.Add(famU);
                            break;
                    }
                }
            }
        }

        private int verifyFAMLink(string ident, string famIdent, string linkType)
        {
            // Verify a single FAM record link
            // returns: 0 no problem; -1 missing; 1 unmatched

            if (string.IsNullOrEmpty(ident))
                return 0;

            Person indi;
            if (!_indiHash.TryGetValue(ident, out indi))
            {
                return -1;
            }

            // TODO need a simple link accessor
            bool found = false;
            foreach (var link in indi.Indi.Links)
            {
                if (link.Tag == linkType && link.Xref == famIdent)
                    found = true;
            }
            return !found ? 1 : 0;
        }

        private void VerifyFAMLinks()
        {
            // Verify links from FAM records.
            // 1. CHIL link to missing INDI
            // 2. HUSB link to missing INDI
            // 3. WIFE link to missing INDI
            // 4. CHIL link w/ no matching FAMC
            // 5. HUSB link w/ no matching FAMS
            // 6. WIFE link w/ no matching FAMS

            foreach (var familyUnit in _famHash.Values)
            {
                var famIdent = familyUnit.FamRec.Ident;

                // Identify spouse ambiguity: more than one HUSB; mismatch in spouses and HUSB/WIFE counts
                int identifiedSpouseCount = 0;
                bool warned = false; // only one message
                if (familyUnit.Husband != null)
                {
                    identifiedSpouseCount++;
                    if (familyUnit.FamRec.Dads.Count > 1)
                    {
                        MakeError(Issue.IssueCode.AMB_CONN, "HUSB", famIdent);
                        warned = true;
                    }
                }

                if (familyUnit.Wife != null)
                {
                    identifiedSpouseCount++;
                    if (familyUnit.FamRec.Dads.Count > 1)
                    {
                        MakeError(Issue.IssueCode.AMB_CONN, "WIFE", famIdent);
                        warned = true;
                    }
                }

                if (identifiedSpouseCount < familyUnit.Spouses.Count && !warned) // don't repeat
                {
                    MakeError(Issue.IssueCode.AMB_CONN, "unknown", famIdent);
                }

                foreach (var husbId in familyUnit.FamRec.Dads)
                {
                    switch (verifyFAMLink(husbId, famIdent, "FAMS"))
                    {
                        case -1:
                            MakeError(Issue.IssueCode.SPOUSE_CONN_MISS, famIdent, husbId, "HUSB");
                            break;
                        case 1:
                            MakeError(Issue.IssueCode.SPOUSE_CONN_UNM, famIdent, husbId, "HUSB");
                            break;
                    }
                }
                foreach (var wifeId in familyUnit.FamRec.Moms)
                {
                    switch (verifyFAMLink(wifeId, famIdent, "FAMS"))
                    {
                        case -1:
                            MakeError(Issue.IssueCode.SPOUSE_CONN_MISS, famIdent, wifeId, "WIFE");
                            break;
                        case 1:
                            MakeError(Issue.IssueCode.SPOUSE_CONN_UNM, famIdent, wifeId, "WIFE");
                            break;
                    }
                }
                foreach (var childId in familyUnit.FamRec.Childs)
                {
                    switch (verifyFAMLink(childId, famIdent, "FAMC"))
                    {
                        case -1:
                            MakeError(Issue.IssueCode.CHIL_MISS, famIdent, childId);
                            break;
                        case 1:
                            MakeError(Issue.IssueCode.CHIL_NOTMATCH, famIdent, childId);
                            break;
                    }
                }
            }
        }

        private void VerifyINDILinks()
        {
            foreach (var person in _indiHash.Values)
            {
                var indiId = person.Id;
                foreach (var link in person.Indi.Links)
                {
                    // TODO verify parsing error
                    if (string.IsNullOrEmpty(link.Xref))
                        continue; // tabors.ged, twparker.ged
                    switch (link.Tag)
                    {
                        case "FAMC":
                            if (!_famHash.ContainsKey(link.Xref))
                            {
                                MakeError(Issue.IssueCode.FAMC_MISSING, indiId, link.Xref);
                            }
                            else
                            {
                                // verify that the FAM record has a matching CHIL link
                                bool found = false;
                                foreach (var famU in person.ChildIn)
                                {
                                    foreach (var child in famU.FamRec.Childs)
                                    {
                                        if (child == indiId)
                                            found = true;
                                    }
                                }
                                if (!found)
                                {
                                    MakeError(Issue.IssueCode.FAMC_UNM, indiId, link.Xref);
                                }
                            }
                            break;
                        case "FAMS":
                            if (!_famHash.ContainsKey(link.Xref))
                            {
                                MakeError(Issue.IssueCode.FAMS_MISSING, indiId, link.Xref);
                            }
                            else
                            {
                                // verify that the FAM record has a matching HUSB/WIFE link
                                bool found = false;
                                foreach (var famU in person.SpouseIn)
                                {
                                    foreach (var dad in famU.FamRec.Dads)
                                    {
                                        if (dad == indiId)
                                            found = true;
                                    }
                                    foreach (var mom in famU.FamRec.Moms)
                                    {
                                        if (mom == indiId)
                                            found = true;
                                    }
                                }
                                if (!found)
                                {
                                    MakeError(Issue.IssueCode.FAMS_UNM, indiId, link.Xref);
                                }
                            }
                            break;
                    }
                }
            }
        }
        #endregion

        #region Mark Trees

        // stack-based list of individuals to mark - eliminate recursion due to deep
        // trees resulting in stack overflow (at depth 15,700+).
        private Stack<Person> _treeStack = new Stack<Person>();

        private void AllInFamily(Union fu)
        {
            // Add all individuals referenced from this family to the processing stack
            if (fu == null)
                return;
            if (fu.Husband != null)
            {
                var iw2 = fu.Husband;
                if (iw2.Tree == -1)
                    _treeStack.Push(iw2);
            }
            if (fu.Wife != null)
            {
                var iw2 = fu.Wife;
                if (iw2.Tree == -1)
                    _treeStack.Push(iw2);
            }

            foreach (var iw2 in fu.Childs)
            {
                if (iw2.Tree == -1)
                    _treeStack.Push(iw2);
            }
        }

        private void MakeTree(int treenum)
        {
            while (_treeStack.Count > 0)
            {
                var iw = _treeStack.Pop();
                iw.Tree = treenum;

                // everybody where this person is a spouse
                foreach (var familyUnit in iw.SpouseIn)
                {
                    AllInFamily(familyUnit);
                }
                // everybody where this person is a child
                foreach (var familyUnit in iw.ChildIn)
                {
                    AllInFamily(familyUnit);
                }
            }
        }

        private void CalcTrees()
        {
            // determine which tree a person belongs
            int treenum = 1;
            foreach (var person in _indiHash.Values)
            {
                if (person.Tree != -1)
                    continue;
                _treeStack.Push(person);
                MakeTree(treenum);
                treenum += 1;
            }

            _treeStack = null;
        }
        #endregion

        public void Dispose()
        {
            _gedReader.Dispose();
        }
    }
}