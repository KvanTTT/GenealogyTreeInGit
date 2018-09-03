using GenealogyTreeInGit.Gedcom;
using GenealogyTreeInGit.Gedcom.Relations;
using GenealogyTreeInGit.Git;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenealogyTreeInGit
{
    public class GedcomToGitFamilyConverter
    {
        public Family Convert(GedcomParseResult parseResult)
        {
            var persons = new Dictionary<string, GitPerson>();

            Dictionary<string, DateTime> minDates = GetMinDatesForPersons(parseResult);

            foreach (KeyValuePair<string, GedcomPerson> gedcomPerson in parseResult.Persons)
            {
                GitPerson gitPerson = Convert(gedcomPerson.Value,
                    minDates.TryGetValue(gedcomPerson.Key, out DateTime minDate) ? minDate : DateTime.MinValue);
                persons.Add(gitPerson.Id, gitPerson);
            }

            FillParents(parseResult, persons);

            var family = new Family(parseResult.Title, persons, new List<GitPersonEvent>());
            return family;
        }

        private static Dictionary<string, DateTime> GetMinDatesForPersons(GedcomParseResult parseResult)
        {
            Dictionary<string, DateTime> minDates = new Dictionary<string, DateTime>();

            foreach (GedcomRelation relation in parseResult.Relations)
            {
                if (relation is ChildRelation childRelation)
                {
                    if (parseResult.Persons.TryGetValue(childRelation.ToId, out GedcomPerson parent) &&
                        parseResult.Persons.TryGetValue(childRelation.FromId, out GedcomPerson child))
                    {
                        GedcomEvent gedcomEvent = parent.Events.FirstOrDefault(ev => ev.Type == EventType.Birth);
                        if (gedcomEvent?.Date != null &&
                            (minDates.TryGetValue(child.Id, out DateTime minDate) ? minDate > (DateTime)gedcomEvent.Date : true))
                        {
                            minDates[child.Id] = gedcomEvent.Date.DefaultDate.AddTicks(1);
                        }
                    }
                }
            }

            return minDates;
        }

        private static void FillParents(GedcomParseResult parseResult, Dictionary<string, GitPerson> persons)
        {
            foreach (GedcomRelation relation in parseResult.Relations)
            {
                if (relation is ChildRelation childRelation)
                {
                    if (persons.TryGetValue(childRelation.ToId, out GitPerson parent) &&
                        persons.TryGetValue(childRelation.FromId, out GitPerson child))
                    {
                        for (int i = 0; i < child.Events.Count; i++)
                        {
                            if (child.Events[i].Type == EventType.Birth)
                            {
                                var personEvent = child.Events[i] is GitExtendedPersonEvent gitExtendedPersonEvent
                                    ? gitExtendedPersonEvent
                                    : new GitExtendedPersonEvent(child.Events[i]);
                                personEvent.Parents.Add(parent);
                                child.Events[i] = personEvent;
                            }
                        }
                    }
                }
            }
        }

        private GitPerson Convert(GedcomPerson gedcomPerson, DateTime minDate)
        {
            var result = new GitPerson(gedcomPerson.Id)
            {
                FirstName = gedcomPerson.FirstName,
                LastName = gedcomPerson.LastName,
            };

            DateTime curDate = minDate;

            var events = new List<GitPersonEvent>();

            foreach (GedcomEvent ev in gedcomPerson.Events)
            {
                GitDateType dateType;

                if (ev.Date?.IsDefined == true)
                {
                    curDate = (DateTime)ev.Date;
                    dateType = GitDateType.Exact;
                }
                else
                {
                    curDate = curDate.AddTicks(1);
                    dateType = GitDateType.After;
                }

                string dateStr = dateType == GitDateType.Exact ? "at " + curDate.ToShortDateString() : "";
                string description =
                    Utils.JoinNotEmpty(result.FirstName, result.LastName, ":", ev.Type.ToString(), dateStr, ev.Place, ev.Latitude, ev.Longitude, ev.Note);

                GitPersonEvent gitPersonEvent;

                if (ev.Type == EventType.Birth)
                {
                    description += " " + Utils.JoinNotEmpty(gedcomPerson.Gender, gedcomPerson.Education,
                        gedcomPerson.Religion, gedcomPerson.Note, gedcomPerson.Changed,
                        gedcomPerson.Occupation, gedcomPerson.Health, gedcomPerson.Title);
                    gitPersonEvent = new GitExtendedPersonEvent(result, ev.Type, curDate, description, dateType);
                }
                else
                {
                    gitPersonEvent = new GitPersonEvent(result, ev.Type, curDate, description, dateType);
                }

                events.Add(gitPersonEvent);
            }

            result.Events = events.OrderBy(ev => ev.Date).ToList();

            return result;
        }
    }
}
