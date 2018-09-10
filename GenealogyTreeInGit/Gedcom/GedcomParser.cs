using GenealogyTreeInGit.Gedcom.Relations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomParser
    {
        private GedcomChunkLevels _gedcomChunkLevels;
        private Dictionary<string, GedcomChunk> _idChunks;
        private GedcomParseResult _parseResult;

        public ILogger Logger { get; set; }

        public GedcomParseResult Parse(string filePath)
        {
            GedcomDate.Logger = Logger;
            var topChunks = GenerateChunks(filePath);
            ParseTopChunks(topChunks);
            _parseResult.Title = Path.GetFileNameWithoutExtension(filePath);

            return _parseResult;
        }

        private List<GedcomChunk> GenerateChunks(string filePath)
        {
            _gedcomChunkLevels = new GedcomChunkLevels();
            _idChunks = new Dictionary<string, GedcomChunk>();
            _parseResult = new GedcomParseResult();

            GedcomLine.Logger = Logger;
            var gedcomLines = File.ReadAllLines(filePath).Select(GedcomLine.Parse).Where(line => line != null);
            var topChunks = new List<GedcomChunk>();

            foreach (GedcomLine gedcomLine in gedcomLines)
            {
                var chunk = new GedcomChunk(gedcomLine);
                if (gedcomLine.Level == 0)
                {
                    topChunks.Add(chunk);
                }
                else
                {
                    GedcomChunk parent = _gedcomChunkLevels.GetParentChunk(chunk);

                    List<GedcomChunk> values;
                    if (!parent.Subchunks.TryGetValue(chunk.Type, out values))
                    {
                        values = new List<GedcomChunk>();
                        parent.Subchunks.Add(chunk.Type, values);
                    }

                    values.Add(chunk);
                }

                _gedcomChunkLevels.Set(chunk);
            }

            return topChunks;
        }

        private void ParseTopChunks(ICollection<GedcomChunk> chunks)
        {
            foreach (var chunk in chunks)
            {
                switch (chunk.Type)
                {
                    case "FAM":
                        ParseFamily(chunk);
                        break;

                    case "INDI":
                        GedcomPerson person = ParseIndividual(chunk);
                        _parseResult.Persons.Add(person.Id, person);
                        break;

                    default:
                        Logger?.LogInfo(chunk + " skipped");
                        break;
                }

                // Save for lookup
                if (!string.IsNullOrEmpty(chunk.Reference))
                {
                    _idChunks.Add(chunk.Id, chunk);
                }
            }
        }

        private void ParseFamily(GedcomChunk famChunk)
        {
            GedcomEvent marriage = null;
            GedcomEvent divorce = null;
            string relation = null;
            string note = null;
            var parentsIds = new List<string>();
            var childrenIds = new List<string>();

            foreach (var chunk in famChunk.Subchunks)
            {
                switch (chunk.Key)
                {
                    case "CHIL":
                        childrenIds.AddRange(chunk.Value.Select(value => value.Reference));
                        break;

                    case "HUSB":
                    case "WIFE":
                        parentsIds.AddRange(chunk.Value.Select(value => value.Reference));
                        break;

                    case "DIV":
                        TryParseEvent(chunk.Key, chunk.Value.FirstOrDefault(), out divorce);
                        break;

                    case "_REL":
                        relation = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "MARR":
                        TryParseEvent(chunk.Key, chunk.Value.FirstOrDefault(), out marriage);
                        break;

                    case "NOTE":
                        note = ParseNote(note, chunk.Value.FirstOrDefault());
                        break;

                    default:
                        Logger?.LogInfo(ToString(chunk) + " skipped");
                        break;
                }
            }

            // Spouses
            if (parentsIds.Count == 2)
            {
                _parseResult.Relations.Add(new SpouseRelation(famChunk.Id, parentsIds[0], parentsIds[1])
                {
                    Marriage = marriage,
                    Divorce = divorce,
                    Relation = relation,
                    Note = note
                });
            }

            // Parents / Children
            foreach (string parent in parentsIds)
            {
                foreach (string child in childrenIds)
                {
                    var childRelation = new ChildRelation(famChunk.Id, child, parent);
                    AddStatus(childRelation);
                    _parseResult.Relations.Add(childRelation);
                }
            }
        }

        private GedcomPerson ParseIndividual(GedcomChunk indiChunk)
        {
            var person = new GedcomPerson(indiChunk.Id);

            foreach (var chunk in indiChunk.Subchunks)
            {
                switch (chunk.Key)
                {
                    case "_UID":
                        person.Uid = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "CHAN":
                        person.Changed = ParseDateTime(chunk.Value.FirstOrDefault());
                        break;

                    case "ADOP":
                    case "BAPM":
                    case "BIRT":
                    case "BURI":
                    case "CHR":
                    case "DEAT":
                    case "EMIG":
                    case "GRAD":
                    case "IMMI":
                    case "NATU":
                    case "RESI":
                        foreach (GedcomChunk value in chunk.Value)
                        {
                            if (TryParseEvent(chunk.Key, value, out GedcomEvent gedcomEvent))
                            {
                                person.Events.Add(gedcomEvent);
                            }
                        }
                        break;

                    case "EDUC":
                        person.Education = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "FACT":
                        person.Note = ParseNote(person.Note, chunk.Value.FirstOrDefault());
                        break;

                    case "HEAL":
                        person.Health = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "IDNO":
                        person.IdNumber = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "NAME":
                        var nameSections = chunk.Value.FirstOrDefault()?.Data.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (nameSections.Length > 0)
                        {
                            person.FirstName = nameSections[0].Trim();
                        }
                        if (nameSections.Length > 1)
                        {
                            person.LastName = nameSections[1].Trim();
                        }
                        break;

                    case "NOTE":
                        person.Note = ParseNote(person.Note, chunk.Value.FirstOrDefault());
                        break;

                    case "OCCU":
                        person.Occupation = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "RELI":
                        person.Religion = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "SEX":
                        person.Gender = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    case "TITL":
                        person.Title = chunk.Value.FirstOrDefault()?.Data;
                        break;

                    default:
                        Logger?.LogInfo(ToString(chunk) + " skipped");
                        break;
                }
            }

            return person;
        }

        private bool TryParseEvent(string key, GedcomChunk chunk, out GedcomEvent gedcomEvent)
        {
            if (key == null || chunk == null)
            {
                gedcomEvent = null;
                return false;
            }

            string dateString = chunk.Subchunks.TryGetFirstValue("DATE", out GedcomChunk value) ? value.Data : null;

            EventType eventType;
            if (!GedcomEvent.EventTypesMap.TryGetValue(key, out eventType))
            {
                eventType = EventType.Other;
            }

            if (eventType != EventType.Adoption)
            {
                gedcomEvent = new GedcomEvent(eventType);
            }
            else
            {
                var adoptionEvent = new GedcomAdoptionEvent();

                foreach (var adoptionSubChunk in chunk.Subchunks)
                {
                    switch (adoptionSubChunk.Key)
                    {
                        case "NOTE":
                            adoptionEvent.Note = ParseNote(adoptionEvent.Note, adoptionSubChunk.Value.FirstOrDefault());
                            break;

                        case "TYPE":
                            adoptionEvent.AdoptionType = adoptionSubChunk.Value.FirstOrDefault()?.Data;
                            break;

                        // Skip for now
                    }
                }

                gedcomEvent = adoptionEvent;
            }

            if (dateString != null)
            {
                gedcomEvent.Date = dateString;
            }
            gedcomEvent.Place = chunk.Subchunks.TryGetFirstValue("PLAC", out GedcomChunk data) ? data.Data : null;

            if (chunk.Subchunks.TryGetFirstValue("MAP", out GedcomChunk map))
            {
                gedcomEvent.Latitude = map.Subchunks.TryGetFirstValue("LATI", out GedcomChunk value1) ? value1.Data : null;
                gedcomEvent.Longitude = map.Subchunks.TryGetFirstValue("LONG", out GedcomChunk value2) ? value2.Data : null;
            }

            if (chunk.Subchunks.TryGetFirstValue("NOTE", out GedcomChunk note))
            {
                gedcomEvent.Note = ParseNote(gedcomEvent.Note, note);
            }

            return true;
        }

        private static string ParseDateTime(GedcomChunk chunk)
        {
            return (chunk.Subchunks.TryGetFirstValue("DATE", out GedcomChunk value1) ? value1.Data : "") + " " +
                   (chunk.Subchunks.TryGetFirstValue("TIME", out GedcomChunk value2) ? value2.Data : "").Trim();
        }

        private GedcomAddress ParseAddress(GedcomChunk addressChunk)
        {
            // Top level node can also contain a full address or first part of it ...
            var address = new GedcomAddress
            {
                Street = addressChunk.Data
            };

            foreach (var chunk in addressChunk.Subchunks)
            {
                GedcomChunk firstValue = chunk.Value.FirstOrDefault();
                if (firstValue == null)
                {
                    continue;
                }

                switch (chunk.Key)
                {
                    case "CONT":
                    case "ADR1":
                    case "ADR2":
                    case "ADR3":
                        address.Street += Environment.NewLine + firstValue.Data;
                        break;

                    case "CITY":
                        address.City += firstValue.Data;
                        break;

                    case "STAE":
                        address.State += firstValue.Data;
                        break;

                    case "POST":
                        address.ZipCode += firstValue.Data;
                        break;

                    case "CTRY":
                        address.Country += firstValue.Data;
                        break;

                    case "PHON":
                        address.Phone.Add(firstValue.Data);
                        break;

                    case "EMAIL":
                        address.Email.Add(firstValue.Data);
                        break;

                    case "FAX":
                        address.Fax.Add(firstValue.Data);
                        break;

                    case "WWW":
                        address.Web.Add(firstValue.Data);
                        break;

                    default:
                        Logger?.LogInfo(ToString(chunk) + " skipped");
                        break;
                }
            }

            return address;
        }

        private string ParseNote(string previousNote, GedcomChunk incomingChunk)
        {
            GedcomChunk noteChunk = incomingChunk;

            if (!string.IsNullOrEmpty(incomingChunk.Reference) && !_idChunks.TryGetValue(noteChunk.Reference, out noteChunk))
            {
                Logger?.LogError($"Unable to find Note with Id='{incomingChunk.Reference}'");
                return "";
            }

            var sb = new StringBuilder();

            foreach (var chunk in noteChunk.Subchunks)
            {
                GedcomChunk firstValue = chunk.Value.FirstOrDefault();

                if (firstValue == null)
                {
                    continue;
                }

                if (IsUnwantedBlob(firstValue))
                {
                    sb.AppendLine("(Skipped blob content)");
                    break;
                }

                switch (chunk.Key)
                {
                    case "CONC":
                        sb.Append(" " + firstValue.Data);
                        break;

                    case "CONT":
                        sb.AppendLine(firstValue.Data);
                        break;

                    default:
                        Logger?.LogInfo(ToString(chunk) + " skipped");
                        break;
                }
            }

            return !string.IsNullOrEmpty(previousNote) ? previousNote + Environment.NewLine + sb : sb.ToString();
        }

        private static bool IsUnwantedBlob(GedcomChunk chunk)
        {
            // TODO: We should make this check more intelligent :) 
            return chunk.Data?.Contains("<span") ?? false;
        }

        /// <summary>
        /// Lookup possible information on child legal status.
        /// It is stored in separate chunks outside the Individual and Family chunks.
        /// </summary>
        private void AddStatus(ChildRelation childRelation)
        {
            if (!_idChunks.TryGetValue(childRelation.FromId, out GedcomChunk childChunk))
                return;

            foreach (var chunk1 in childChunk.Subchunks)
            {
                if (chunk1.Key != "FAMC")
                {
                    continue;
                }

                foreach (var chunk in chunk1.Value)
                {
                    foreach (var subchunk in chunk.Subchunks)
                    {
                        switch (subchunk.Key)
                        {
                            case "PEDI":
                                childRelation.Pedigree = subchunk.Value.FirstOrDefault()?.Data;
                                break;

                            case "STAT":
                                childRelation.Validity = subchunk.Value.FirstOrDefault()?.Data;
                                break;

                            case "ADOP":
                                var adoptionInfo = new List<string>();

                                foreach (GedcomChunk chunk2 in chunk1.Value)
                                {
                                    foreach (var chunk3 in chunk2.Subchunks)
                                    {
                                        switch (chunk3.Key)
                                        {
                                            case "DATE":
                                                adoptionInfo.Add(ParseDateTime(chunk3.Value.FirstOrDefault()));
                                                break;
                                            case "STAT":
                                            case "NOTE":
                                                adoptionInfo.Add(chunk3.Value.FirstOrDefault().Data);
                                                break;
                                        }
                                    }
                                }

                                childRelation.Adoption = string.Join(", ", adoptionInfo);
                                break;

                            default:
                                Logger?.LogInfo(ToString(subchunk) + " skipped");
                                break;
                        }
                    }
                    break;
                }
            }
        }

        public static string ToString(KeyValuePair<string, List<GedcomChunk>> chunk)
        {
            return chunk.Key + " " + Utils.JoinNotEmpty(chunk.Value.Select(v => v.ToString()).ToArray());
        }
    }
}